using System;
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
            log.LogInfo("[party] installed on MainManager.SetPlayers");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
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
