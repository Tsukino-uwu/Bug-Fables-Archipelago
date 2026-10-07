using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Termite gate's first opening (Event149) before flag 384. From inside (the plaza) it looks for the outside
    // guards and stops: in a seed that says so, the gate is marked opened as the scene starts, so it takes the opened
    // gate's way through. From outside, with Skip cutscenes, the same: no talk outside, no escort inside. Either way the
    // follower the first opening lets go goes too.
    internal static class TermiteGate
    {
        private const int GateScene = 149;
        private const int Opened = 384;
        // Elizant, following during the escort; the first opening's own cleanup removes her.
        private const int Escort = 96;

        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (Hooks.Install(typeof(TermiteGate), "gate", "the Termite gate opens from outside only, with its scene"))
            {
                log.LogInfo("[gate] installed on EventControl.StartEvent (the Termite gate's first opening)");
            }
        }

        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
        [HarmonyPrefix]
        private static void BeforeStartEvent(int id)
        {
            MainManager mm = MainManager.instance;
            if (id != GateScene || mm == null || MainManager.map == null || mm.flags[Opened])
            {
                return;
            }
            bool opens;
            if (MainManager.map.mapid == MainManager.Maps.TermiteMainPlaza)
            {
                opens = randomizerOn != null && randomizerOn() && seed?.Invoke()?.TermiteGateFromInside == true;
                log.LogInfo($"[gate] the Termite gate from inside, never opened from outside: "
                    + (opens ? "marked opened (flag 384), it lets the party through" : "left to the game"));
            }
            else if (MainManager.map.mapid == MainManager.Maps.TermiteOutside)
            {
                opens = QualityOfLife.SkipCutscenes.Value && QualityOfLife.SettingsOn != null
                    && QualityOfLife.SettingsOn();
                log.LogInfo($"[gate] the Termite gate's first opening from outside: "
                    + (opens ? "skipped (flag 384 marked), straight through" : "left to the game (Skip cutscenes off)"));
            }
            else
            {
                return;
            }
            if (opens)
            {
                mm.flags[Opened] = true;
                mm.extrafollowers?.Remove(Escort);
            }
        }
    }
}
