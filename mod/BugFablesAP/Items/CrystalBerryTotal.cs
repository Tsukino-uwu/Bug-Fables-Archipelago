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
        private static Harmony harmony;
        private static string lastLogged;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo total = AccessTools.Method(typeof(MainManager), nameof(MainManager.CrystalBerryAmmount), Type.EmptyTypes);
            if (total == null)
            {
                log.LogError("[berries] MainManager.CrystalBerryAmmount not found: the berry total counts spots found.");
                return;
            }
            harmony = new Harmony(guid + ".berries." + DateTime.UtcNow.Ticks);
            harmony.Patch(total, prefix: new HarmonyMethod(typeof(CrystalBerryTotal), nameof(BeforeTotal)));
            log.LogInfo("[berries] installed on MainManager.CrystalBerryAmmount");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static bool BeforeTotal(ref int __result)
        {
            MainManager mm = MainManager.instance;
            var berryLocations = connection?.LocationBerries;
            if (randomizerOn == null || !randomizerOn() || connection == null || !connection.SeedKnown || berryLocations == null
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
