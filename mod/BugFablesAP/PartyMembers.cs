using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // One starting party member (the user, 2026-09-25: Starting Party Member Off / Vi / Kabbu / Leif / Random, the other
    // two as items). Every party change the story makes goes through MainManager.ChangeParty(ids, fromscratch,
    // destroyoldentity) (the two-argument form forwards to it, MainManager.cs:3635; EventControl calls it about 20 times),
    // so a prefix takes out of `ids` every member not allowed yet: Vi joining in the opening, Leif at the lake. What a
    // scene then asks of a missing member is PartyFit's (the invisible stand-ins). Allowed: the starting member and the
    // members received. Only while the Archipelago mod is enabled, and only while a starting member is set.
    internal static class PartyMembers
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // Dev only for now ([Debug] TestStartMember): -1 off, 0 Vi, 1 Kabbu, 2 Leif. Later from slot_data.
        internal static int StartMember = -1;
        // Members received (dev: the console's addmember). Later from the server's items.
        internal static readonly HashSet<int> Received = new HashSet<int>();

        // Only with a map loaded: the title screen sets up a party of its own (seen 2026-09-25: the guard rewrote it there).
        internal static bool Active => StartMember >= 0 && randomizerOn != null && randomizerOn() && MainManager.map != null;

        internal static bool Allowed(int id) => id == StartMember || Received.Contains(id);

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            MethodInfo changeParty = AccessTools.Method(typeof(MainManager), nameof(MainManager.ChangeParty), new[] { typeof(int[]), typeof(bool), typeof(bool) });
            if (changeParty == null)
            {
                log.LogError("[members] MainManager.ChangeParty(int[], bool, bool) not found: the story adds every member as usual.");
                return;
            }
            harmony = new Harmony(guid + ".members." + DateTime.UtcNow.Ticks);
            // Last, so the opening skip's own prefix (QualityOfLife, which refuses Event8's Kabbu-alone call) sees the
            // story's ids as they are.
            harmony.Patch(changeParty, prefix: new HarmonyMethod(typeof(PartyMembers), nameof(BeforeChangeParty)) { priority = Priority.Last });
            MethodInfo startEvent = AccessTools.Method(typeof(EventControl), nameof(EventControl.StartEvent), new[] { typeof(int), typeof(NPCControl) });
            if (startEvent != null)
            {
                harmony.Patch(startEvent, prefix: new HarmonyMethod(typeof(PartyMembers), nameof(BeforeStartEvent)));
            }
            else
            {
                log.LogError("[members] EventControl.StartEvent not found: a joining scene for a member already in the party plays and may crash.");
            }
            log.LogInfo($"[members] installed on MainManager.ChangeParty (starting member {StartMember})");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // The party the story last asked for, before the guard: its first member is who the story thinks leads, which
        // PartyFit uses when a scene played that member with a stand-in.
        internal static int[] LastStoryParty;

        private static void BeforeChangeParty(ref int[] ids)
        {
            if (!Active || ids == null)
            {
                return;
            }
            LastStoryParty = (int[])ids.Clone();
            int[] kept = ids.Where(Allowed).ToArray();
            if (kept.Length == ids.Length)
            {
                return;
            }
            if (kept.Length == 0)
            {
                // A scene asking for only members not allowed yet (Kabbu alone after the slides): keep who is here.
                MainManager mm = MainManager.instance;
                kept = mm?.playerdata != null && mm.playerdata.Length > 0
                    ? mm.playerdata.Select(p => p.trueid).Where(Allowed).ToArray()
                    : new int[0];
                if (kept.Length == 0)
                {
                    kept = new[] { StartMember };
                }
            }
            log.LogInfo($"[members] the story asked for party {string.Join(",", ids.Select(i => i.ToString()).ToArray())}; "
                + $"allowed {string.Join(",", kept.Select(i => i.ToString()).ToArray())} (event {MainManager.lastevent})");
            ids = kept;
        }

        // Leif's joining scene (Event14, the lake) when Leif is already in the party (the user, 2026-09-25, Leif alone): it
        // takes its Leif from the follower list (map.tempfollowers[0], EventControl.cs:3339), which Leif isn't on, and threw
        // ArgumentOutOfRange at its start. Its point, Leif joining, is moot, and its fight (two of enemy 1, no escape) needs
        // something that hits enemies in the air, in chapter 1 only Vi's beemerang (the user). So it doesn't start; the mod
        // leaves what it leaves: flag 16 (Leif joined, set before the fight), the regional flag of the creature it removes
        // (entity 5, EventControl.cs:3501-3502) with that creature gone, and Leif off the follower list.
        //
        // Always skipped with Archipelago on (the user, 2026-09-25: "just always skip it, it's not a check"): it's no location,
        // only the logic's "Leif Joins" event at the lake, and without its fight the lake no longer quietly needs Vi. When Leif
        // isn't in the party yet he joins right there, as the scene's ChangeParty({0, 1, 2}) would have him; with one
        // starting member the guard above still decides whether he may.
        private static bool BeforeStartEvent(int id)
        {
            MainManager mm = MainManager.instance;
            if (id != 14 || randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.playerdata == null)
            {
                return true;
            }
            bool joined = false;
            if (!mm.playerdata.Any(p => p.trueid == 2) && MainManager.player != null)
            {
                Vector3 at = MainManager.player.transform.position;
                MainManager.ChangeParty(mm.playerdata.Select(p => p.trueid).Concat(new[] { 2 }).ToArray(), true, true);
                var spots = new Vector3[mm.playerdata.Length];
                for (int i = 0; i < spots.Length; i++)
                {
                    spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
                }
                MainManager.SetPlayers(spots);
                if (mm.playerdata.Length > 0 && mm.playerdata[0].entity != null)
                {
                    mm.camtarget = mm.playerdata[0].entity.transform;
                }
                joined = mm.playerdata.Any(p => p.trueid == 2);
            }
            mm.flags[16] = true;
            EntityControl creature = MainManager.GetEntity(5);
            if (creature != null && creature.npcdata != null && creature.npcdata.regionalflag >= 0)
            {
                mm.regionalflags[creature.npcdata.regionalflag] = true;
                creature.gameObject.SetActive(false);
            }
            mm.extrafollowers?.RemoveAll(f => f == 2);
            log.LogInfo($"[members] Leif's joining scene (Event14) skipped: " + (joined ? "Leif joined the party" : "Leif not added (already in, or not allowed)") + "; flag 16 set, "
                + (creature != null ? $"entity 5 ({creature.name}) removed" : "no entity 5"));
            return false;
        }

        // A party member can't also be a story follower (the user, 2026-09-25: three Leifs after the spider fight). The
        // story makes Leif a temporary follower there (extrafollowers.Add(2), EventControl.cs:2281, removed when he joins,
        // :3533), and every map load makes a follower character for each entry (MapControl.cs:826-829, AddFollower). The
        // entries are character ids (animid: 0 Vi, 1 Kabbu, 2 Leif). So while a member is in the party, their entry goes and
        // any follower copy of them with it. Each frame; the cost is a list lookup.
        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (!Active || mm.extrafollowers == null || mm.playerdata == null)
            {
                return;
            }
            RemoveStrayPlayers(mm);
            foreach (int id in mm.playerdata.Select(p => p.trueid).ToArray())
            {
                if (!mm.extrafollowers.Contains(id))
                {
                    continue;
                }
                mm.extrafollowers.RemoveAll(f => f == id);
                int removed = 0;
                if (MainManager.map.tempfollowers != null)
                {
                    foreach (EntityControl copy in MainManager.map.tempfollowers.Where(f => f != null && f.animid == id).ToList())
                    {
                        MainManager.map.tempfollowers.Remove(copy);
                        UnityEngine.Object.Destroy(copy.gameObject);
                        removed++;
                    }
                }
                log.LogInfo($"[members] the story made member {id} a follower while in the party: taken off the follower list, {removed} follower copy removed");
            }
        }

        // Only one character carries the player's controls (the user, 2026-09-25: a second Leif copying every move). The
        // spider fight's scene calls ChangeParty({0, 1}) then the no-argument SetPlayers() (EventControl.cs:1711-1712), which
        // makes new player characters without removing the old ones (MainManager.SetPlayers()); in the story the old ones
        // are the scene's own actors, but with one member those are stand-ins, and the old Leif stayed, controls and all
        // (the console's who: two "Player 0", both playerentity, tag Player). Outside scenes, twice a second, any other
        // character with a PlayerControl than the leader's goes.
        private static float nextSweep;

        private static void RemoveStrayPlayers(MainManager mm)
        {
            if (mm.inevent || mm.message || MainManager.battle != null || Time.realtimeSinceStartup < nextSweep)
            {
                return;
            }
            nextSweep = Time.realtimeSinceStartup + 0.5f;
            EntityControl leader = mm.playerdata.Length > 0 ? mm.playerdata[0].entity : null;
            if (leader == null)
            {
                return;
            }
            foreach (PlayerControl control in UnityEngine.Object.FindObjectsOfType<PlayerControl>())
            {
                EntityControl stray = control.GetComponent<EntityControl>();
                if (stray != null && stray != leader && !mm.playerdata.Any(p => p.entity == stray))
                {
                    log.LogInfo($"[members] a stray player character ({stray.name} at {stray.transform.position}, animid {stray.animid}) removed; the leader is {leader.name}");
                    UnityEngine.Object.Destroy(stray.gameObject);
                }
            }
        }

        // A member arriving (dev: addmember; later a received item): into the party behind the others, with a character
        // where the party stands, as the dev console's addleif does (ChangeParty with fromscratch, then SetPlayers).
        internal static string Add(int id)
        {
            MainManager mm = MainManager.instance;
            Received.Add(id);
            if (MainManager.player == null || mm.inevent || mm.message || MainManager.battle != null)
            {
                return $"member {id} allowed; joins at the next party change (not now: an event, dialogue or battle)";
            }
            if (mm.playerdata.Any(p => p.trueid == id))
            {
                return $"member {id} is already in the party";
            }
            Vector3 at = MainManager.player.transform.position;
            int[] party = mm.playerdata.Select(p => p.trueid).Concat(new[] { id }).ToArray();
            MainManager.ChangeParty(party, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            return "party now " + string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())
                + $"; characters {mm.playerdata.Count(p => p.entity != null)} of {mm.playerdata.Length}";
        }
    }
}
