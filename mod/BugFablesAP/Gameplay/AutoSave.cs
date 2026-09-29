using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // The Gameplay page's Auto-save: after walking into a new room, once play is free, the game's own save at the
    // room's entrance, at most once per MinSeconds. A death then costs one room.
    internal static class AutoSave
    {
        internal static ConfigEntry<bool> Enabled;

        private static ManualLogSource log;
        private static Func<bool> settingsOn;

        private const float MinSeconds = 15f;
        // Free this many frames in a row first: a map's auto-event starts once the player is free and sets its flag as
        // it starts, so a save a frame too early could hold that flag and skip the scene on a reload.
        private const int SettleFrames = 20;

        private static bool wasTransferring;
        private static MapControl arrivedIn;
        private static bool armed;
        private static int freeFrames;
        private static float lastSave = -MinSeconds;
        private static string waitingFor;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> settings)
        {
            log = logger;
            settingsOn = settings;
            Enabled = config.Bind("Gameplay", "AutoSave", false,
                "On: walking into a new room saves the game at its entrance, once you can move (at most every 15 "
                + "seconds). Switch it on the Gameplay page.");
        }

        internal static void Tick()
        {
            // A door's transfer (MainManager.TransferMap) holds roomtransition until the party has walked in.
            bool transferring = MainManager.roomtransition;
            if (wasTransferring && !transferring && MainManager.map != null && MainManager.map != arrivedIn)
            {
                arrivedIn = MainManager.map;
                if (On())
                {
                    armed = true;
                    freeFrames = 0;
                    waitingFor = null;
                }
            }
            wasTransferring = transferring;
            if (!armed)
            {
                return;
            }
            if (!On() || MainManager.map != arrivedIn)
            {
                armed = false;
                return;
            }
            MainManager mm = MainManager.instance;
            PlayerControl player = MainManager.player;
            if (DeathLinkGame.Busy)
            {
                Wait("a received death (never saved over)");
                freeFrames = 0;
                return;
            }
            if (mm == null || player == null || MainManager.battle != null || transferring || mm.intransition
                || mm.inbattle
                || !MainManager.FreePlayer() || player.entity == null || !player.entity.onground)
            {
                freeFrames = 0;
                return;
            }
            if (++freeFrames < SettleFrames)
            {
                return;
            }
            float since = Time.realtimeSinceStartup - lastSave;
            if (since < MinSeconds)
            {
                Wait(MinSeconds + " seconds to pass since the last auto-save");
                return;
            }
            armed = false;
            Vector3 spot = player.lastloadzone;
            bool saved = MainManager.Save(spot);
            lastSave = Time.realtimeSinceStartup;
            log.LogInfo($"[autosave] {(saved ? "saved" : "save FAILED")} in map {MainManager.map.mapid} at the entrance {spot}");
        }

        private static bool On() => Enabled != null && Enabled.Value && settingsOn != null && settingsOn();

        private static void Wait(string what)
        {
            if (waitingFor != what)
            {
                waitingFor = what;
                log.LogInfo("[autosave] waiting: " + what);
            }
        }
    }
}
