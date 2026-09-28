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

        // -1 off, 0 Vi, 1 Kabbu, 2 Leif. A seed that names one (slot_data starting_member, -1 the story's party included)
        // wins; the dev setting only stands in without one.
        internal static int DevStartMember = -1;
        private static Func<SeedData> seed;
        internal static int SeedStartMember => seed?.Invoke()?.StartingMember ?? -1;
        internal static bool SeedSaysMember => seed?.Invoke()?.StartingMemberGiven ?? false;
        internal static int StartMember => SeedSaysMember ? SeedStartMember : DevStartMember;
        // starting_member 3: the whole party from the start (none of them an item).
        internal const int AllMembers = 3;
        internal static readonly HashSet<int> Received = new HashSet<int>();

        // Needs a map: the title screen sets up a party of its own.
        internal static bool Active => StartMember >= 0 && randomizerOn != null && randomizerOn() && MainManager.map != null;

        internal static bool Allowed(int id) => id == StartMember || StartMember == AllMembers || Received.Contains(id);

        // The apworld's item names.
        internal static string Name(int id) => id == 0 ? "Vi" : id == 1 ? "Kabbu" : id == 2 ? "Leif" : "member " + id;

        // The members this save has been given (ItemReceiver, from the received items it counts): with a seed start,
        // exactly these and the start are allowed, so another file's members never carry over.
        internal static void SetReceived(IEnumerable<int> ids)
        {
            if (SeedStartMember < 0)
            {
                return;
            }
            Received.Clear();
            Received.UnionWith(ids);
        }

        // A party member item: allowed from now on, and joins at once (ItemReceiver gives only while the player is free).
        internal static string Receive(int id)
        {
            Received.Add(id);
            if (StartMember < 0)
            {
                return $"{Name(id)}: this seed keeps the story's party, nothing to add";
            }
            MainManager mm = MainManager.instance;
            if (mm.playerdata != null && mm.playerdata.Any(p => p.trueid == id))
            {
                return $"{Name(id)} allowed, already in the party";
            }
            return $"{Name(id)} joins: " + Add(id);
        }

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (!Hooks.Install(typeof(PartyMembers), "members", "the story adds every member as usual"))
            {
                return;
            }
            Hooks.Install(typeof(JoiningScenes), "members", "a joining scene for a member already in the party plays and may crash");
            log.LogInfo($"[members] installed on MainManager.ChangeParty (starting member {StartMember})");
        }

        // The story's party before the guard; its first member is who the story thinks leads (PartyFit reads it).
        internal static int[] LastStoryParty;

        // Last, so QualityOfLife's opening-skip prefix sees the story's ids unchanged.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.ChangeParty), typeof(int[]), typeof(bool), typeof(bool))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void BeforeChangeParty(ref int[] ids)
        {
            if (!Active || ids == null)
            {
                return;
            }
            LastStoryParty = (int[])ids.Clone();
            if (ids.All(Allowed))
            {
                KeepMembersAhead(ref ids);
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

        // A member already in the party whom the story hasn't reached yet (Leif before the spider) stays when the story
        // sets its own party, e.g. the opening's Vi and Kabbu; with all three from the start he joins there. Not in the spider scene (Event6): its fights are the story's,
        // and he rejoins after it.
        private static void KeepMembersAhead(ref int[] ids)
        {
            MainManager mm = MainManager.instance;
            if (mm?.playerdata == null || (MainManager.lastevent == 6 && mm.inevent))
            {
                return;
            }
            int[] asked = ids;
            // With all three from the start, whoever isn't here yet joins too (the opening then has Leif at once).
            IEnumerable<int> candidates = StartMember == AllMembers ? Enumerable.Range(0, 3) : mm.playerdata.Select(p => p.trueid);
            int[] ahead = candidates.Where(m => Allowed(m) && !asked.Contains(m) && !PartyFit.InStoryParty(m)).ToArray();
            if (ahead.Length == 0)
            {
                return;
            }
            ids = ids.Concat(ahead).ToArray();
            log.LogInfo($"[members] the story asked for party {string.Join(",", asked.Select(i => i.ToString()).ToArray())}; "
                + $"kept {string.Join(",", ahead.Select(i => i.ToString()).ToArray())} too (event {MainManager.lastevent})");
        }

        // Leif's lake scene (Event14) never starts with Archipelago on: it reads its Leif from map.tempfollowers[0] and
        // crashes when he's in the party. Its effects are done here instead: flag 16, entity 5's regional flag, the follower entry.
        private static class JoiningScenes
        {
            [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
            [HarmonyPrefix]
            private static bool BeforeStartEvent(int id)
            {
                MainManager mm = MainManager.instance;
                if (id != 14 || randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.playerdata == null)
                {
                    return true;
                }
                bool joined = JoinLeif(mm);
                mm.flags[GameFlags.LeifJoined] = true;
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
        }

        // Leif's one line at the start of his first battle (BattleControl.EventDialogue 3, while 16 is set and 24 isn't) is
        // marked said as soon as he has joined, whatever Skip cutscenes says.
        private static void SkipLeifsFirstBattleLine(MainManager mm)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.flags == null || !mm.flags[GameFlags.LeifJoined] || mm.flags[24]
                || MainManager.battle != null)
            {
                return;
            }
            mm.flags[24] = true;
            log.LogInfo("[members] Leif has joined (flag 16): his first-battle line marked said (flag 24)");
        }

        // Leif joins once the spider scene is over (flag 27, not yet 16), where the story has him start following.
        private static void TickLeifJoins(MainManager mm)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null || mm.flags == null || !mm.flags[GameFlags.LeifFollows] || mm.flags[GameFlags.LeifJoined]
                || mm.inevent || mm.message || MainManager.battle != null || MainManager.player == null || mm.playerdata == null
                || (StartMember >= 0 && !Allowed(2)))
            {
                return;
            }
            bool joined = JoinLeif(mm);
            mm.flags[GameFlags.LeifJoined] = true;
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
            SkipLeifsFirstBattleLine(mm);
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
                DevStartMember = left[0];
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
