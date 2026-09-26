using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Makes scenes written for a fixed party (two or three) run with the party the seed has. Only while Archipelago is on.
    // SetPlayers indexes its position list for every member, so a shorter list is lengthened first.
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
            // Hidden as late as possible: after the scene's step and the entity's own updates, just before drawing.
            MethodInfo lateUpdate = AccessTools.Method(typeof(EntityControl), "LateUpdate");
            if (lateUpdate != null)
            {
                harmony.Patch(lateUpdate, postfix: new HarmonyMethod(typeof(PartyFit), nameof(AfterLateUpdate)));
            }
            else
            {
                log.LogError("[party] EntityControl.LateUpdate not found: stand-ins may flash into view.");
            }
            // Every MoveTowards overload ends in this one.
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

        // Scenes use fixed slots p[0] to p[2]: while one runs, the party list is padded with an invisible stand-in per
        // missing member (id order keeps its own slot). Outside scenes nothing changes.
        private static readonly EntityControl[] standIns = new EntityControl[3];
        private static readonly System.Collections.Generic.HashSet<string> standInReported = new System.Collections.Generic.HashSet<string>();

        // A conversation counts too: an NPC's line can hand the talk to a member by name.
        private static bool Talking => MainManager.instance != null && (MainManager.instance.inevent || MainManager.instance.message);

        private static bool InScene() =>
            randomizerOn != null && randomizerOn() && Talking && MainManager.player != null;

        // The leader acts the part of the story's missing leader, chosen once per scene since a scene can change the party
        // partway. Animations play by number, so he shows his own for the role's.
        private static EntityControl actor;
        private static int actorRole = -1; // -1 not chosen yet this scene, -2 nobody acts
        // Members in the party the story doesn't have yet act a missing member's part too (role -> character).
        private static readonly System.Collections.Generic.Dictionary<int, EntityControl> spares = new System.Collections.Generic.Dictionary<int, EntityControl>();

        // Vi from the opening (flag 15), Kabbu always, Leif once joined (flag 16). A member the story has plays himself.
        internal static bool InStoryParty(int member)
        {
            bool[] flags = MainManager.instance.flags;
            return member == 1 || (member == 0 && flags[15]) || (member == 2 && flags[16]);
        }

        private static void ChooseActor()
        {
            MainManager mm = MainManager.instance;
            // A scene that reloads the map remakes the party characters: the new leader takes the part on.
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
                ChooseSpares(mm);
                return;
            }
            int[] story = PartyMembers.LastStoryParty;
            int lead = story != null && story.Length > 0 ? story[0] : -1;
            // Unknown after a reload: the first missing member by id.
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
            ChooseSpares(mm);
        }

        // Each member the story has but the party doesn't gets, in turn, a party member the story doesn't have yet.
        private static void ChooseSpares(MainManager mm)
        {
            spares.Clear();
            if (mm.playerdata == null)
            {
                return;
            }
            var missing = Enumerable.Range(0, 3).Where(m => InStoryParty(m) && m != actorRole && !mm.playerdata.Any(p => p.trueid == m)).ToList();
            foreach (MainManager.BattleData p in mm.playerdata)
            {
                if (missing.Count == 0)
                {
                    break;
                }
                if (p.entity == null || p.entity == actor || InStoryParty(p.trueid))
                {
                    continue;
                }
                spares[missing[0]] = p.entity;
                log.LogInfo($"[party] Event{MainManager.lastevent}: {p.entity.name} (member {p.trueid}) acts member {missing[0]}'s part");
                missing.RemoveAt(0);
            }
        }

        // A spare acting another member's part isn't also asked for as himself.
        private static bool ActsOtherPart(EntityControl e, int member) =>
            e != null && spares.Any(s => s.Value == e && s.Key != member);

        private static EntityControl StandIn(int member)
        {
            ChooseActor();
            if (member == actorRole && actor != null)
            {
                return actor;
            }
            if (spares.TryGetValue(member, out EntityControl spare))
            {
                if (spare != null)
                {
                    return spare;
                }
                spares.Remove(member); // the map was remade: a stand-in from here on
            }
            if (standIns[member] == null)
            {
                standIns[member] = EntityControl.CreateNewEntity("apstandin" + member, member, MainManager.player.transform.position);
                // A new character gets its body only in Start, a frame later; a scene using it at once crashed. Start keeps this one.
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

        // A scene that waits for a stand-in to arrive would wait for good (no collision): it arrives at once.
        private static void AfterMoveTowards(EntityControl __instance, Vector3 pos)
        {
            if (__instance == null || !standIns.Contains(__instance))
            {
                return;
            }
            // Except toward the real player: a stand-in stays where the scene put it rather than follow the party.
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
                // The acting leader fills the role's slot; his own gets a stand-in, so the player never moves twice.
                if (found != null && ((found == actor && member != actorRole) || ActsOtherPart(found, member)))
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
                if (!__result.Any(e => e != null && e.animid == member) && member != actorRole && !spares.ContainsKey(member))
                {
                    longer.Add(StandIn(member));
                }
            }
            __result = longer.ToArray();
        }

        // A scene can end treating stand-ins as the party: if the story's leader was a stand-in, the party moves to its
        // spot; anyone following a stand-in follows the party's last member.
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
                    // A copy of someone already in the party: remove it.
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
            spares.Clear();
            for (int member = 0; member < standIns.Length; member++)
            {
                if (standIns[member] != null)
                {
                    UnityEngine.Object.Destroy(standIns[member].gameObject);
                }
                standIns[member] = null;
            }
        }

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
                // Kinematic: a scene may place a real member on a stand-in's spot, so it must not sink through the floor.
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

        // GetEntity(1000 + n) reads map.tempfollowers[n]: with no companion there, the leader answers (logged once per place).
        private static readonly System.Collections.Generic.HashSet<string> reported = new System.Collections.Generic.HashSet<string>();

        private static bool BeforeGetEntity(int id, ref EntityControl __result)
        {
            // A member by name (-4 Vi, -5 Kabbu, -6 Leif) not in the party: the stand-in answers (callers never null-check).
            if (id <= -4 && id >= -6 && InScene())
            {
                int member = -4 - id;
                MainManager party = MainManager.instance;
                if (party.playerdata != null && !party.playerdata.Any(p => p.entity != null && p.entity.animid == member))
                {
                    __result = StandIn(member);
                    return false;
                }
                // The acting leader asked for by his own name: his own part goes to a stand-in, or he'd follow two sets of orders.
                ChooseActor();
                if (actor != null && actorRole != member && actor.animid == member)
                {
                    __result = StandIn(member);
                    return false;
                }
                EntityControl own = party.playerdata.Select(p => p.entity).FirstOrDefault(e => e != null && e.animid == member);
                if (ActsOtherPart(own, member))
                {
                    __result = StandIn(member);
                    return false;
                }
                return true;
            }
            // The second and third member by position (-2, -3) beyond the party: the stand-ins, acting leader counted first.
            if ((id == -2 || id == -3) && InScene())
            {
                MainManager party = MainManager.instance;
                int slot = -1 - id; // 1 or 2
                if (party.playerdata != null && party.playerdata.Length <= slot)
                {
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
