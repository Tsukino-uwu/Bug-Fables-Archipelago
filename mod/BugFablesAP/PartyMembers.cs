using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // One starting party member: a prefix on MainManager.ChangeParty, which every story party change goes through,
    // drops members not allowed yet (the starting one and those received are).
    internal static class PartyMembers
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // -1 off, 0 Vi, 1 Kabbu, 2 Leif.
        internal static int StartMember = -1;
        internal static readonly HashSet<int> Received = new HashSet<int>();

        // Needs a map: the title screen sets up a party of its own.
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
            // Last, so QualityOfLife's opening-skip prefix sees the story's ids unchanged.
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

        // The story's party before the guard; its first member is who the story thinks leads (PartyFit reads it).
        internal static int[] LastStoryParty;

        private static void BeforeChangeParty(ref int[] ids)
        {
            if (!Active || ids == null)
            {
                return;
            }
            LastStoryParty = (int[])ids.Clone();
            if (ids.All(Allowed))
            {
                return;
            }
            // A member not allowed yet is played by an allowed one the story doesn't have yet, as in scenes (PartyFit):
            // the story's "Kabbu alone" becomes Leif alone, its "Vi and Kabbu" Vi and Leif.
            int[] asked = ids;
            var substitutes = Enumerable.Range(0, 3).Where(m => Allowed(m) && !asked.Contains(m) && !PartyFit.InStoryParty(m)).ToList();
            var kept = new List<int>();
            foreach (int id in ids)
            {
                if (Allowed(id))
                {
                    kept.Add(id);
                }
                else if (substitutes.Count > 0)
                {
                    kept.Add(substitutes[0]);
                    substitutes.RemoveAt(0);
                }
            }
            if (kept.Count == 0)
            {
                // No one to stand in: keep who is here.
                MainManager mm = MainManager.instance;
                kept = mm?.playerdata != null && mm.playerdata.Length > 0
                    ? mm.playerdata.Select(p => p.trueid).Where(Allowed).ToList()
                    : new List<int>();
                if (kept.Count == 0)
                {
                    kept.Add(StartMember);
                }
            }
            log.LogInfo($"[members] the story asked for party {string.Join(",", ids.Select(i => i.ToString()).ToArray())}; "
                + $"given {string.Join(",", kept.Select(i => i.ToString()).ToArray())} (event {MainManager.lastevent})");
            ids = kept.ToArray();
        }

        // Leif's lake scene (Event14) never starts with Archipelago on: it reads its Leif from map.tempfollowers[0] and
        // crashes when he's in the party. Its effects are done here instead: flag 16, entity 5's regional flag, the follower entry.
        private static bool BeforeStartEvent(int id)
        {
            MainManager mm = MainManager.instance;
            if (id != 14 || randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.playerdata == null)
            {
                return true;
            }
            bool joined = JoinLeif(mm);
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

        // Leif joins once the spider scene is over (flag 27, not yet 16), where the story has him start following.
        private static void TickLeifJoins(MainManager mm)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.flags == null || !mm.flags[27] || mm.flags[16]
                || mm.inevent || mm.message || MainManager.battle != null || MainManager.player == null || mm.playerdata == null
                || (StartMember >= 0 && !Allowed(2)))
            {
                return;
            }
            bool joined = JoinLeif(mm);
            mm.flags[16] = true;
            log.LogInfo("[members] after the spider scene: " + (joined ? "Leif joined the party" : "Leif was already in the party") + "; flag 16 set");
        }

        private static bool JoinLeif(MainManager mm)
        {
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
            if (mm.playerdata.Any(p => p.trueid == 2))
            {
                mm.extrafollowers?.RemoveAll(f => f == 2);
                foreach (EntityControl copy in UnityEngine.Object.FindObjectsOfType<EntityControl>()
                    .Where(e => e.animid == 2 && !e.playerentity && !mm.playerdata.Any(p => p.entity == e) && (e.tempfollower || e.following != null)).ToList())
                {
                    MainManager.map.tempfollowers?.Remove(copy);
                    UnityEngine.Object.Destroy(copy.gameObject);
                    log.LogInfo($"[members] a story copy of Leif ({copy.name}) removed: he's in the party");
                }
            }
            return joined;
        }

        // Each map load makes a follower per extrafollowers entry (an animid), so a party member's entry and copies go.
        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (mm == null)
            {
                return;
            }
            TickLeifJoins(mm);
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

        // SetPlayers() without arguments leaves the old player characters, controls and all: remove any not the party's.
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

        // Dev: takes a member out, and turns the guard on for the members left (the first as the start), so the story's
        // next party change doesn't put them back.
        internal static string Remove(int id)
        {
            MainManager mm = MainManager.instance;
            if (MainManager.player == null || mm.inevent || mm.message || MainManager.battle != null)
            {
                return "not now: an event, dialogue or battle";
            }
            int[] left = mm.playerdata.Select(p => p.trueid).Where(m => m != id).ToArray();
            if (left.Length == mm.playerdata.Length)
            {
                return $"member {id} isn't in the party";
            }
            if (left.Length == 0)
            {
                return "the party can't be empty";
            }
            Received.Remove(id);
            if (StartMember < 0 || StartMember == id)
            {
                StartMember = left[0];
            }
            foreach (int m in left)
            {
                Received.Add(m);
            }
            Vector3 at = MainManager.player.transform.position;
            MainManager.ChangeParty(left, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            MainManager.map?.Invoke("SetPlayerColliders", 0.2f);
            return "party now " + string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())
                + $"; the guard keeps member {id} out (start {StartMember})";
        }

        // As the dev console's addleif: ChangeParty with fromscratch (without it the list comes out empty), then SetPlayers.
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
            // The new characters must pass the map's enemy-only walls too: redone as a map load does, 0.2 s later.
            MainManager.map?.Invoke("SetPlayerColliders", 0.2f);
            return "party now " + string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())
                + $"; characters {mm.playerdata.Count(p => p.entity != null)} of {mm.playerdata.Length}";
        }
    }
}
