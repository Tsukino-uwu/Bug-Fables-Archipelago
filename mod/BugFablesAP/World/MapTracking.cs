using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Each map loaded in a seed, for the trackers: added to the maps visited and made the map the player is on
    // (ApConnection.MapLoaded).
    internal static class MapTracking
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(Loaded), "map", "the trackers never learn which maps were visited"))
            {
                log.LogInfo("[map] installed on MapControl.CreateEntities (maps visited)");
            }
        }

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        private static class Loaded
        {
            [HarmonyPostfix]
            private static void AfterCreate(MapControl __instance)
            {
                if (randomizerOn == null || !randomizerOn() || connection == null)
                {
                    return;
                }
                // Only for the trackers: a failure here must never stop the map.
                try
                {
                    // Another seed's save adds nothing to this slot's keys (as checks and shops are gated).
                    if (ItemReceiver.SaveMatchesSeed(connection, log) == false)
                    {
                        log.LogInfo($"[map] {__instance.mapid}: not recorded, this save belongs to another seed");
                        return;
                    }
                    connection.MapLoaded(__instance.mapid.ToString());
                }
                catch (Exception e)
                {
                    log.LogWarning($"[map] {__instance.mapid} loaded but not recorded: {e.GetBaseException().Message}");
                }
            }
        }
    }
}
