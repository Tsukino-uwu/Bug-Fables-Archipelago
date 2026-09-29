using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The game's berry total counts berry spots found (crystalbflags). In a seed it counts berries received instead,
    // plus the ones picked up at spots that aren't this seed's locations.
    internal static class CrystalBerryTotal
    {
        internal const int ReceivedSlot = 69;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static string lastLogged;

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(Total), "berries", "the berry total counts spots found"))
            {
                log.LogInfo("[berries] installed on MainManager.CrystalBerryAmmount");
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.CrystalBerryAmmount))]
        private static class Total
        {
            [HarmonyPrefix]
            private static bool BeforeTotal(ref int __result)
            {
                MainManager mm = MainManager.instance;
                var berryLocations = connection?.LocationBerries;
                if (randomizerOn == null || !randomizerOn() || connection == null || !connection.SeedKnown
                    || berryLocations == null
                    || mm == null || mm.flagvar == null || mm.crystalbflags == null)
                {
                    return true;
                }
                var seedSpots = berryLocations.Values.ToList();
                int vanillaSpots = 0;
                for (int i = 0; i < mm.crystalbflags.Length; i++)
                {
                    if (mm.crystalbflags[i] && !seedSpots.Contains(i))
                    {
                        vanillaSpots++;
                    }
                }
                int received = mm.flagvar[ReceivedSlot];
                __result = received + vanillaSpots;
                string decision = $"[berries] total asked on {MainManager.map?.name}: {__result} ({received} received, "
                    + $"{vanillaSpots} from spots outside the seed); the game's own count would be {mm.crystalbflags.Count(f => f)}";
                if (decision != lastLogged)
                {
                    log.LogInfo(decision);
                    lastLogged = decision;
                }
                return false;
            }
        }
    }
}
