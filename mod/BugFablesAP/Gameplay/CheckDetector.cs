using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archipelago.MultiClient.Net;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Detector beeps for any of the seed's checks left on this map. Its whole effect is map.hiddenitem = 100, so in a
    // seed CheckDisc is replaced, CheckHidden skipped and a music record's value cleared: the mod's answer is the only one.
    internal static class CheckDetector
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (!Hooks.Install(typeof(CheckDetector), "detector", "the Detector finds only what the medal finds"))
            {
                return;
            }
            bool hidden = Hooks.Install(typeof(Hidden), "detector", "hidden spots show as the game has them");
            bool spinners = Hooks.Install(typeof(Spinners), "detector", "music spinners show as the game has them");
            log.LogInfo("[detector] installed on MapControl.CheckDisc" + (hidden ? ", NPCControl.CheckHidden" : " (NOT CheckHidden)")
                + (spinners ? ", MusicSpinner.Start" : " (NOT MusicSpinner.Start)"));
        }

        private static bool InSeed => randomizerOn != null && randomizerOn() && connection != null && connection.SeedKnown;

        private static class Hidden
        {
            [HarmonyPatch(typeof(NPCControl), "CheckHidden")]
            [HarmonyPrefix]
            private static bool BeforeCheckHidden() => !InSeed;
        }

        private static class Spinners
        {
            [HarmonyPatch(typeof(MusicSpinner), "Start")]
            [HarmonyPostfix]
            private static void AfterSpinnerStart()
            {
                if (InSeed && MainManager.map != null)
                {
                    MainManager.map.hiddenitem = null;
                }
            }
        }

        [HarmonyPatch(typeof(MapControl), "CheckDisc")]
        [HarmonyPrefix]
        private static bool BeforeCheckDisc(MapControl __instance)
        {
            if (!InSeed)
            {
                return true;
            }
            if (!MainManager.BadgeIsEquipped(MedalAssist.DetectorMedal))
            {
                return false;
            }
            string map = __instance.mapid.ToString();
            long left = OnThisMap(__instance, map).FirstOrDefault(id => !Done(id));
            __instance.hiddenitem = left != 0 ? 100 : (int?)null;
            log.LogInfo(left != 0 ? $"[detector] {map}: location {left} not done yet: the Detector beeps"
                                  : $"[detector] {map}: no check left here: quiet");
            return false;
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

        // Done when the server has it, or, offline or not sent yet, when the save or this session says so.
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
