using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Termite gate from inside (Event149 on the plaza), before it was ever opened from outside: the scene's first
    // opening looks for the outside guards and stops. In a seed that says so, the gate is marked opened (flag 384) as
    // the scene starts, so it takes the opened gate's way through; the follower the first opening lets go goes too.
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
            if (Hooks.Install(typeof(TermiteGate), "gate", "the Termite gate opens from outside only"))
            {
                log.LogInfo("[gate] installed on EventControl.StartEvent (the Termite gate from inside)");
            }
        }

        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
        [HarmonyPrefix]
        private static void BeforeStartEvent(int id)
        {
            MainManager mm = MainManager.instance;
            if (id != GateScene || mm == null || MainManager.map == null
                || MainManager.map.mapid != MainManager.Maps.TermiteMainPlaza || mm.flags[Opened])
            {
                return;
            }
            bool opens = randomizerOn != null && randomizerOn() && seed?.Invoke()?.TermiteGateFromInside == true;
            log.LogInfo($"[gate] the Termite gate from inside, never opened from outside: "
                + (opens ? "marked opened (flag 384), it lets the party through" : "left to the game"));
            if (opens)
            {
                mm.flags[Opened] = true;
                mm.extrafollowers?.Remove(Escort);
            }
        }
    }
}
