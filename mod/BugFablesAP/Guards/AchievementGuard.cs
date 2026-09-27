using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The panel's Achievements row (off by default): while Archipelago is enabled and it's off, the game's one Steam
    // unlock, InputIO.Achivement, is skipped, as normal saves are kept apart. It only concerns Steam, never Archipelago.
    internal static class AchievementGuard
    {
        private static Func<bool> randomizerOn;
        private static Func<bool> allowed;
        private static Harmony harmony;
        private static ManualLogSource log;
        private static readonly HashSet<int> held = new HashSet<int>();

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled, Func<bool> achievementsOn)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            allowed = achievementsOn;
            var unlock = AccessTools.Method(typeof(InputIOManager.InputIO), "Achivement", new[] { typeof(int) });
            if (unlock == null)
            {
                log.LogError("[achievements] NOT installed: InputIO.Achivement(int) wasn't found; achievements unlock as usual.");
                return;
            }
            harmony = new Harmony(guid + ".achievements." + DateTime.UtcNow.Ticks);
            harmony.Patch(unlock, prefix: new HarmonyMethod(typeof(AchievementGuard), nameof(BeforeUnlock)));
            log.LogInfo("[achievements] installed on InputIO.Achivement");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static bool BeforeUnlock(int id)
        {
            if (randomizerOn == null || !randomizerOn() || allowed())
            {
                return true;
            }
            if (held.Add(id))
            {
                log.LogInfo($"[achievements] Steam achievement {id} not unlocked (Achievements is off while Archipelago is on)");
            }
            return false;
        }
    }
}
