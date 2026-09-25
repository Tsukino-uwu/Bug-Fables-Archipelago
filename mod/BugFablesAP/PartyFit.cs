using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Chapter 1 scenes are written for a party of two. With a third member there early (an open start with Leif; the
    // user's rehearsal with addleif, 2026-09-25), the trapdoor scene (Event5) recreated the party with
    // SetPlayers(positions) and a list of two positions, and SetPlayers indexed it for all three: IndexOutOfRange, the
    // scene stopped halfway and the player was stuck. SetPlayers places member j at newentitypos[j]
    // (MainManager.cs:9416-9439), so a list shorter than the party is lengthened first: each extra member stands a step
    // behind the last listed one. Only while the Archipelago mod is enabled.
    internal static class PartyFit
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            MethodInfo setPlayers = AccessTools.Method(typeof(MainManager), nameof(MainManager.SetPlayers), new[] { typeof(Vector3[]) });
            if (setPlayers == null)
            {
                log.LogError("[party] MainManager.SetPlayers(Vector3[]) not found: scenes written for two may break with three.");
                return;
            }
            harmony = new Harmony(guid + ".party." + DateTime.UtcNow.Ticks);
            harmony.Patch(setPlayers, prefix: new HarmonyMethod(typeof(PartyFit), nameof(BeforeSetPlayers)));
            MethodInfo getEntity = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetEntity), new[] { typeof(int) });
            if (getEntity != null)
            {
                harmony.Patch(getEntity, prefix: new HarmonyMethod(typeof(PartyFit), nameof(BeforeGetEntity)));
            }
            else
            {
                log.LogError("[party] MainManager.GetEntity(int) not found: a missing companion will still crash lines and scenes.");
            }
            MethodInfo byId = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetPartyEntities), new[] { typeof(bool) });
            MethodInfo plain = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetPartyEntities), Type.EmptyTypes);
            if (byId != null && plain != null)
            {
                harmony.Patch(byId, postfix: new HarmonyMethod(typeof(PartyFit), nameof(AfterPartyById)));
                harmony.Patch(plain, postfix: new HarmonyMethod(typeof(PartyFit), nameof(AfterParty)));
            }
            else
            {
                log.LogError("[party] MainManager.GetPartyEntities not found: scenes written for three still crash with two.");
            }
            // Hidden as late as possible: the per-frame Tick came before the scene's step and the entity's own updates,
            // which switched a stand-in's sprite back on for a frame now and then (the user, 2026-09-25: stand-ins
            // flashing). A postfix on its own LateUpdate (EntityControl.cs:3672) runs after both, just before drawing.
            MethodInfo lateUpdate = AccessTools.Method(typeof(EntityControl), "LateUpdate");
            if (lateUpdate != null)
            {
                harmony.Patch(lateUpdate, postfix: new HarmonyMethod(typeof(PartyFit), nameof(AfterLateUpdate)));
            }
            else
            {
                log.LogError("[party] EntityControl.LateUpdate not found: stand-ins may flash into view.");
            }
            // Every MoveTowards overload ends in this one (EntityControl.cs:4911-4960).
            MethodInfo moveTowards = AccessTools.Method(typeof(EntityControl), nameof(EntityControl.MoveTowards),
                new[] { typeof(Vector3), typeof(float), typeof(int), typeof(int), typeof(bool) });
            if (moveTowards != null)
            {
                harmony.Patch(moveTowards, postfix: new HarmonyMethod(typeof(PartyFit), nameof(AfterMoveTowards)));
            }
            else
            {
                log.LogError("[party] EntityControl.MoveTowards not found: a scene waiting for a stand-in to walk somewhere waits for good.");
            }
            log.LogInfo("[party] installed on MainManager.SetPlayers" + (getEntity != null ? ", GetEntity" : "") + (byId != null ? ", GetPartyEntities" : "")
                + (moveTowards != null ? " and MoveTowards" : ""));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            ClearStandIns();
        }

        // Stand-ins (the user, 2026-09-25: make scenes work with one or two members). A scene takes the party as a list and
        // uses fixed slots, p[0] to p[2] (about 110 lookups; the barkeeper's first talk, Event83, crashed on p[2] with Vi
        // and Kabbu, EventControl.cs:13055-13058). While a scene runs, the list is padded to three with an invisible
        // stand-in for each missing member, made as the game makes scene characters (EntityControl.CreateNewEntity, with
        // that member's animid: Vi 0, Kabbu 1, Leif 2) at the leader's feet, hidden and without collision, and removed
        // when the scene ends. By id order (Vi, Kabbu, Leif; GetPartyEntities(true), MainManager.cs:9483-9503) the stand-in
        // takes the missing member's own slot; otherwise it is added after the party. Outside scenes nothing changes, so
        // nothing can take a stand-in for a member who has joined. Each scene that used one is logged once.
        private static readonly EntityControl[] standIns = new EntityControl[3];
        private static readonly System.Collections.Generic.HashSet<string> standInReported = new System.Collections.Generic.HashSet<string>();

        // A scene or a conversation: an NPC's line can hand the talk to a member by name (|next,-4|, Vi), outside any scene
        // (Artis's talk with Leif alone crashed SetText on the missing speaker, the user, 2026-09-25).
        private static bool Talking => MainManager.instance != null && (MainManager.instance.inevent || MainManager.instance.message);

        private static bool InScene() =>
            randomizerOn != null && randomizerOn() && Talking && MainManager.player != null;

        // The leader acts a missing member's part (the user, 2026-09-25: Leif walking up to the grass, picking up the
        // mushroom and falling like Kabbu, rather than standing idle beside invisible stand-ins). Once per scene or
        // conversation, at its first stand-in: if the story's leader (the first member of the party it last asked for) isn't
        // in the party, the real leader plays that member; every other missing member stays an invisible stand-in. Chosen
        // once, since a scene can change the party partway (the spider fight's does). Animations play by number, so the
        // leader shows his own animation for the role's (the field action is 100 for everyone: Leif's ice for Kabbu's horn).
        private static EntityControl actor;
        private static int actorRole = -1; // -1 not chosen yet this scene, -2 nobody acts

        // Whether the story's party holds this member at this point: Vi from the opening (flag 15), Kabbu always, Leif once
        // he has joined (flag 16). A leader the story already has plays himself (the user, 2026-09-25: in the briefing
        // Leif doing Leif's part is right, rather than one member doing another's); he acts the lead only in scenes whose
        // party doesn't have him yet (chapter 1 before Leif joins).
        private static bool InStoryParty(int member)
        {
            bool[] flags = MainManager.instance.flags;
            return member == 1 || (member == 0 && flags[15]) || (member == 2 && flags[16]);
        }

        private static void ChooseActor()
        {
            MainManager mm = MainManager.instance;
            // A scene that reloads the map partway remakes the party characters (Event45's throne room, LoadMap with
            // recreateplayers): the actor was the old leader character, now gone; the new leader takes the part on.
            if (actorRole >= 0 && actor == null && mm.playerdata != null && mm.playerdata.Length > 0 && mm.playerdata[0].entity != null)
            {
                actor = mm.playerdata[0].entity;
                log.LogInfo($"[party] Event{MainManager.lastevent}: the map was remade; {actor.name} acts member {actorRole}'s part again");
            }
            if (actorRole != -1)
            {
                return;
            }
            if (mm.playerdata != null && mm.playerdata.Length > 0 && InStoryParty(mm.playerdata[0].trueid))
            {
                actorRole = -2;
                return;
            }
            int[] story = PartyMembers.LastStoryParty;
            int lead = story != null && story.Length > 0 ? story[0] : -1;
            // Unknown (a reload forgets it): the first missing member by id, Vi before Kabbu (the droplet scene,
            // Event21, ran with no actor after a reload, the user, 2026-09-25).
            if (lead < 0 && mm.playerdata != null)
            {
                lead = Enumerable.Range(0, 3).Where(m => !mm.playerdata.Any(p => p.trueid == m)).DefaultIfEmpty(-1).First();
            }
            EntityControl leader = mm.playerdata != null && mm.playerdata.Length > 0 ? mm.playerdata[0].entity : null;
            if (lead >= 0 && lead <= 2 && leader != null && !mm.playerdata.Any(p => p.trueid == lead))
            {
                actorRole = lead;
                actor = leader;
                log.LogInfo($"[party] Event{MainManager.lastevent}: the leader ({leader.name}, member {mm.playerdata[0].trueid}) acts member {lead}'s part");
            }
            else
            {
                actorRole = -2;
            }
        }

        private static EntityControl StandIn(int member)
        {
            ChooseActor();
            if (member == actorRole && actor != null)
            {
                return actor;
            }
            if (standIns[member] == null)
            {
                standIns[member] = EntityControl.CreateNewEntity("apstandin" + member, member, MainManager.player.transform.position);
                // A new character gets its physics body only in its Start, a frame later (EntityControl.cs:524-528), and a
                // scene using the stand-in at once crashed: the spider fight's lead-in made it Jump(), whose Unfix uses the
                // body (Event6, the user, 2026-09-25). Start adds one only when there is none, so this one is kept.
                EntityControl made = standIns[member];
                if (made.rigid == null)
                {
                    made.rigid = made.gameObject.GetComponent<Rigidbody>() ?? made.gameObject.AddComponent<Rigidbody>();
                    made.rigid.constraints = RigidbodyConstraints.FreezeRotation;
                    made.rigid.useGravity = false;
                    made.rigid.isKinematic = true;
                }
                string where = (MainManager.map != null ? MainManager.map.mapid.ToString() : "no map") + " Event" + MainManager.lastevent + " member " + member;
                if (standInReported.Add(where))
                {
                    log.LogWarning($"[party] a scene asked for party member {member} (0 Vi, 1 Kabbu, 2 Leif), not in the party: an invisible stand-in ({where})");
                }
            }
            return standIns[member];
        }

        // A scene walking a stand-in somewhere, then waiting until it arrives (Event10, the horn tutorial: while
        // (entities[0].forcemove), EventControl.cs:2935): a stand-in has no collision and never got there, so the scene
        // stood still (the user, 2026-09-25, Leif alone). A stand-in arrives at once.
        private static void AfterLateUpdate(EntityControl __instance)
        {
            if (__instance == null || !standIns.Contains(__instance))
            {
                return;
            }
            foreach (Renderer r in __instance.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }

        private static void AfterMoveTowards(EntityControl __instance, Vector3 pos)
        {
            if (__instance == null || !standIns.Contains(__instance))
            {
                return;
            }
            // Except toward the real player: PartyMover walks every party member, stand-ins included, to the player
            // (EventControl PartyMover), who with one member isn't in the scene at all (left where the map put him). The
            // trapdoor's end then placed Leif on stand-in Vi's spot, by then teleported to Leif: far left instead of the
            // landing spot (the user, 2026-09-25). A stand-in doesn't follow the real party; it stays where the scene put it.
            if (MainManager.player != null && Vector3.Distance(pos, MainManager.player.transform.position) < 2f)
            {
                __instance.forcemove = false;
                return;
            }
            __instance.transform.position = pos;
            __instance.forcemove = false;
        }

        private static void AfterPartyById(bool idorder, ref EntityControl[] __result)
        {
            if (!idorder || __result == null || __result.Length >= 3 || !InScene())
            {
                return;
            }
            ChooseActor();
            var full = new EntityControl[3];
            for (int member = 0; member < 3; member++)
            {
                EntityControl found = __result.FirstOrDefault(e => e != null && e.animid == member);
                // The acting leader fills the role's slot; his own gets an invisible stand-in, so a scene moving "each
                // member" never moves the player twice.
                if (found != null && found == actor && member != actorRole)
                {
                    found = null;
                }
                full[member] = found ?? StandIn(member);
            }
            __result = full;
        }

        private static void AfterParty(ref EntityControl[] __result)
        {
            if (__result == null || __result.Length >= 3 || !InScene())
            {
                return;
            }
            ChooseActor();
            var longer = __result.ToList();
            for (int member = 0; member < 3 && longer.Count < 3; member++)
            {
                // The acting leader is already first in the party's own list.
                if (!__result.Any(e => e != null && e.animid == member) && member != actorRole)
                {
                    longer.Add(StandIn(member));
                }
            }
            __result = longer.ToArray();
        }

        // A scene that ends with stand-ins treated them as the party (the spider fight's end, Event6, EventControl.cs:
        // 2272-2289, the user, 2026-09-25): it walked Vi and Kabbu to where the player should stand, never the real player,
        // and made the fall room's character follow Kabbu (entities[2].following = entities[1]). Before the stand-ins go:
        // if the story's leader (the first member of the party it last asked for) was a stand-in, the real party moves to
        // where it was left; anyone following a stand-in follows the real party's last member instead.
        private static void HandOver()
        {
            MainManager mm = MainManager.instance;
            if (mm == null || mm.playerdata == null || mm.playerdata.Length == 0 || mm.playerdata[0].entity == null)
            {
                return;
            }
            int[] story = PartyMembers.LastStoryParty;
            int lead = story != null && story.Length > 0 ? story[0] : -1;
            if (lead >= 0 && lead < standIns.Length && standIns[lead] != null && !mm.playerdata.Any(p => p.trueid == lead))
            {
                Vector3 spot = standIns[lead].transform.position;
                for (int i = 0; i < mm.playerdata.Length; i++)
                {
                    if (mm.playerdata[i].entity != null)
                    {
                        mm.playerdata[i].entity.transform.position = spot + new Vector3(-0.6f * i, 0f, 0.1f * i);
                    }
                }
                log.LogInfo($"[party] Event{MainManager.lastevent} over: the story's leader ({lead}) was a stand-in; the party moved to where it was left, {spot}");
            }
            EntityControl last = mm.playerdata[mm.playerdata.Length - 1].entity;
            foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>())
            {
                if (e.following != null && standIns.Contains(e.following) && !standIns.Contains(e))
                {
                    // A character of someone already in the party (the story's Leif after the spider fight, with Leif
                    // the one member): a copy, so it goes.
                    if (e.animid >= 0 && e.animid <= 2 && mm.playerdata.Any(p => p.trueid == e.animid))
                    {
                        UnityEngine.Object.Destroy(e.gameObject);
                        log.LogInfo($"[party] {e.name} (member {e.animid}) followed a stand-in but is already in the party: removed");
                        continue;
                    }
                    e.following = last;
                    log.LogInfo($"[party] {e.name} followed a stand-in; now follows {last.name}");
                }
            }
        }

        private static void ClearStandIns()
        {
            actor = null;
            actorRole = -1;
            for (int member = 0; member < standIns.Length; member++)
            {
                if (standIns[member] != null)
                {
                    UnityEngine.Object.Destroy(standIns[member].gameObject);
                }
                standIns[member] = null;
            }
        }

        // Each frame: stand-ins stay invisible and solid-free while the scene runs, and go when it ends.
        internal static void Tick()
        {
            if (!standIns.Any(e => e != null) && actorRole == -1)
            {
                return;
            }
            if (!Talking)
            {
                HandOver();
                ClearStandIns();
                log.LogInfo("[party] scene or conversation over: stand-ins removed");
                return;
            }
            foreach (EntityControl e in standIns)
            {
                if (e == null)
                {
                    continue;
                }
                foreach (Renderer r in e.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = false;
                }
                foreach (Collider c in e.GetComponentsInChildren<Collider>(true))
                {
                    c.enabled = false;
                }
                // Without collision a stand-in fell through the floor, and a scene that then places a real member on its spot
                // (the trapdoor's end puts member m at the m-th scene character's position, EventControl.cs:1476-1484) put
                // Leif where it had sunk to (the user, 2026-09-25: "down/left at a rock" instead of on the mushroom). So a
                // stand-in stays exactly where the scene puts it.
                if (e.rigid != null)
                {
                    if (!e.rigid.isKinematic)
                    {
                        e.rigid.velocity = Vector3.zero;
                    }
                    e.rigid.useGravity = false;
                    e.rigid.isKinematic = true;
                }
            }
        }

        // The town open from the start (the user, 2026-09-25) reaches lines and scenes written for after chapter 1, when a
        // companion travels with the party: GetEntity(1000 + n) reads map.tempfollowers[n] (MainManager.cs:18512-18515),
        // and with nobody there it threw ArgumentOutOfRange (the town's arrival scene, then a theater NPC's line). The
        // user chose a fallback: the party's leader answers instead, so nothing crashes (the companion's line comes from
        // the leader), and each place is logged once, so a scene that truly needs the companion can be held back.
        private static readonly System.Collections.Generic.HashSet<string> reported = new System.Collections.Generic.HashSet<string>();

        private static bool BeforeGetEntity(int id, ref EntityControl __result)
        {
            // A member asked for by name (-4 Vi, -5 Kabbu, -6 Leif, MainManager.cs:18538-18570) while a scene runs, and
            // not in the party: the stand-in answers, as in GetPartyEntities (no code tests these for null, grep).
            if (id <= -4 && id >= -6 && InScene())
            {
                int member = -4 - id;
                MainManager party = MainManager.instance;
                if (party.playerdata != null && !party.playerdata.Any(p => p.entity != null && p.entity.animid == member))
                {
                    __result = StandIn(member);
                    return false;
                }
                // The acting leader asked for by his own name: cast twice, he'd follow whichever order came last. The
                // briefing (Event45) sends "Vi" up to the Queen and "Leif" back, and Leif stayed back (the user,
                // 2026-09-25). His own part goes to an invisible stand-in; he plays only the lead's.
                ChooseActor();
                if (actor != null && actorRole != member && actor.animid == member)
                {
                    __result = StandIn(member);
                    return false;
                }
                return true;
            }
            // The second and third member by position (-2, -3; MainManager.cs:18526-18537) with a smaller party: the droplet
            // scene's end walks both to the player (Event21, EventControl.cs:4112-4114) and threw on nothing (the user,
            // 2026-09-25). While a scene runs, slot k beyond the party answers with the k-th stand-in by member id (the
            // acting leader already counts as the first).
            if ((id == -2 || id == -3) && InScene())
            {
                MainManager party = MainManager.instance;
                int slot = -1 - id; // 1 or 2
                if (party.playerdata != null && party.playerdata.Length <= slot)
                {
                    // The story's order: the acting role first (the leader, slot 0), then the others by id.
                    ChooseActor();
                    int[] order = (actorRole >= 0 ? new[] { actorRole } : new int[0])
                        .Concat(Enumerable.Range(0, 3).Where(m => m != actorRole)).ToArray();
                    __result = StandIn(order[slot]);
                    return false;
                }
                return true;
            }
            if (id < 1000 || randomizerOn == null || !randomizerOn())
            {
                return true;
            }
            MapControl map = MainManager.map;
            int index = id - 1000;
            if (map != null && map.tempfollowers != null && index < map.tempfollowers.Count)
            {
                return true;
            }
            MainManager mm = MainManager.instance;
            __result = mm != null && mm.playerdata != null && mm.playerdata.Length > 0 ? mm.playerdata[0].entity : null;
            string where = (map != null ? map.mapid.ToString() : "no map") + " #" + id;
            if (reported.Add(where))
            {
                log.LogWarning($"[party] companion {id} asked for on {where.Split(' ')[0]} (Event{MainManager.lastevent}), nobody there: the leader answers");
            }
            return false;
        }

        private static void BeforeSetPlayers(ref Vector3[] newentitypos)
        {
            int party = MainManager.instance?.playerdata?.Length ?? 0;
            if (newentitypos != null && standIns.Any(e => e != null) && randomizerOn())
            {
                log.LogInfo($"[party] Event{MainManager.lastevent} places the party at {string.Join(" ", newentitypos.Select(v => v.ToString()).ToArray())}; "
                    + $"stand-ins at {string.Join(" ", standIns.Where(e => e != null).Select(e => e.name + " " + e.transform.position).ToArray())}; "
                    + $"player at {(MainManager.player != null ? MainManager.player.transform.position.ToString() : "none")}");
            }
            if (newentitypos == null || newentitypos.Length == 0 || newentitypos.Length >= party || !randomizerOn())
            {
                return;
            }
            var longer = new Vector3[party];
            Array.Copy(newentitypos, longer, newentitypos.Length);
            for (int j = newentitypos.Length; j < party; j++)
            {
                longer[j] = longer[j - 1] + new Vector3(-0.6f, 0f, 0.1f);
            }
            log.LogInfo($"[party] Event{MainManager.lastevent} placed {newentitypos.Length} of {party} members; the rest stand behind");
            newentitypos = longer;
        }
    }
}
