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
        private static ManualLogSource log;
        private static readonly HashSet<int> held = new HashSet<int>();

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled, Func<bool> achievementsOn)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            allowed = achievementsOn;
            if (Hooks.Install(typeof(Unlock), "achievements", "achievements unlock as usual"))
            {
                log.LogInfo("[achievements] installed on InputIO.Achivement");
            }
        }

        [HarmonyPatch(typeof(InputIOManager.InputIO), "Achivement", typeof(int))]
        private static class Unlock
        {
            [HarmonyPrefix]
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
}
