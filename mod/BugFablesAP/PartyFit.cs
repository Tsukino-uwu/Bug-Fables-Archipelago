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

        private static EntityControl StandIn(int member)
        {
            if (standIns[member] == null)
            {
                standIns[member] = EntityControl.CreateNewEntity("apstandin" + member, member, MainManager.player.transform.position);
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
        private static void AfterMoveTowards(EntityControl __instance, Vector3 pos)
        {
            if (__instance == null || !standIns.Contains(__instance))
            {
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
            var full = new EntityControl[3];
            for (int member = 0; member < 3; member++)
            {
                full[member] = __result.FirstOrDefault(e => e != null && e.animid == member) ?? StandIn(member);
            }
            __result = full;
        }

        private static void AfterParty(ref EntityControl[] __result)
        {
            if (__result == null || __result.Length >= 3 || !InScene())
            {
                return;
            }
            var longer = __result.ToList();
            for (int member = 0; member < 3 && longer.Count < 3; member++)
            {
                if (!__result.Any(e => e != null && e.animid == member))
                {
                    longer.Add(StandIn(member));
                }
            }
            __result = longer.ToArray();
        }

        private static void ClearStandIns()
        {
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
            if (!standIns.Any(e => e != null))
            {
                return;
            }
            if (!Talking)
            {
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
