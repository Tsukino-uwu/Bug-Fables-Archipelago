using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Detector for every check (the user, 2026-09-25: beep for any kind of Archipelago check left in the room, not
    // only the hidden items the medal looks for). The medal's whole effect is one value: a second after a map loads, the
    // game asks its objects (NPCControl.CheckHidden) and the map (MapControl.CheckDisc) whether something hidden is here,
    // and if so sets map.hiddenitem = 100; the map's update then shows the "!" over the leader and plays the beep
    // (MapControl.cs:885-896). So a postfix on CheckDisc (run by Invoke one second after every map load, MapControl.cs:319,
    // whether or not the map has discoveries) sets that same value when one of the seed's locations on this map isn't done
    // yet: pickups, gifts, shops and item shops by their map, discoveries by the map's own discoveryids. Only while the
    // Detector counts as equipped (the medal, or the panel's Detector row through MedalAssist) and the mod is enabled.
    internal static class CheckDetector
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo checkDisc = AccessTools.Method(typeof(MapControl), "CheckDisc");
            if (checkDisc == null)
            {
                log.LogError("[detector] MapControl.CheckDisc not found: the Detector finds only what the medal finds.");
                return;
            }
            harmony = new Harmony(guid + ".detector." + DateTime.UtcNow.Ticks);
            harmony.Patch(checkDisc, postfix: new HarmonyMethod(typeof(CheckDetector), nameof(AfterCheckDisc)));
            log.LogInfo("[detector] installed on MapControl.CheckDisc");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void AfterCheckDisc(MapControl __instance)
        {
            if (randomizerOn == null || !randomizerOn() || connection == null || !connection.SeedKnown
                || !MainManager.BadgeIsEquipped(MedalAssist.DetectorMedal) || __instance.hiddenitem.HasValue)
            {
                return;
            }
            string map = __instance.mapid.ToString();
            long left = OnThisMap(__instance, map).FirstOrDefault(id => !Done(id));
            if (left != 0)
            {
                __instance.hiddenitem = 100;
                log.LogInfo($"[detector] {map}: location {left} not done yet: the Detector beeps");
            }
        }

        private static IEnumerable<long> OnThisMap(MapControl map, string name)
        {
            if (connection.LocationPickups != null)
            {
                foreach (var e in connection.LocationPickups.Where(e => e.Value.Map == name))
                {
                    yield return e.Key;
                }
            }
            if (connection.LocationGives != null)
            {
                foreach (var e in connection.LocationGives.Where(e => e.Value.Map == name))
                {
                    yield return e.Key;
                }
            }
            if (connection.LocationItemShops != null)
            {
                foreach (var e in connection.LocationItemShops.Where(e => e.Value.Map == name))
                {
                    yield return e.Key;
                }
            }
            if (connection.LocationDiscoveries != null && map.discoveryids != null)
            {
                foreach (var e in connection.LocationDiscoveries.Where(e => map.discoveryids.Contains(e.Value)))
                {
                    yield return e.Key;
                }
            }
        }

        // Done when the server has the check, or (offline, or not sent yet) when the save says so: the location's flag,
        // its crystal berry, its journal entry, a shop copy's bought bit, or a respawning pickup or item shop's first
        // purchase taken this session.
        private static bool Done(long id)
        {
            ArchipelagoSession session = connection.Session;
            if (session != null && session.Locations.AllLocationsChecked.Contains(id))
            {
                return true;
            }
            MainManager mm = MainManager.instance;
            if (connection.IsDone(id) || ShopSwap.BoughtInSave(id))
            {
                return true;
            }
            if (connection.LocationFlags != null && connection.LocationFlags.TryGetValue(id, out int flag)
                && flag >= 0 && flag < mm.flags.Length && mm.flags[flag])
            {
                return true;
            }
            if (connection.LocationBerries != null && connection.LocationBerries.TryGetValue(id, out int berry)
                && berry >= 0 && berry < mm.crystalbflags.Length && mm.crystalbflags[berry])
            {
                return true;
            }
            if (connection.LocationDiscoveries != null && connection.LocationDiscoveries.TryGetValue(id, out int entry)
                && mm.librarystuff[0, entry])
            {
                return true;
            }
            return false;
        }
    }
}
