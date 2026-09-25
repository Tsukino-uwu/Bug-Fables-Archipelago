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

        internal static bool Active => StartMember >= 0 && randomizerOn != null && randomizerOn();

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
            log.LogInfo($"[members] installed on MainManager.ChangeParty (starting member {StartMember})");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void BeforeChangeParty(ref int[] ids)
        {
            if (!Active || ids == null)
            {
                return;
            }
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
