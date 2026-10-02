using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The game puts the party back after water, a hole or spikes, and below the map's floor, at player.lastpos (from the
    // 5th quick try at lastloadzone, but only while a direction is held). When that spot isn't solid ground the party
    // falls in again forever, and the pause menu, so the Warp, never opens. With Archipelago on (or Use on normal saves),
    // six respawns in a row with no standing on ground between them end with Warp to Start; a transfer whose walk-in
    // never ends is stopped.
    internal static class RespawnLoop
    {
        private static ManualLogSource log;
        private static Func<bool> on;

        // The game's own fallback comes at the 5th quick respawn; failing once more after it is a loop.
        private const int Limit = 6;
        // Standing this long on ground between two respawns is play, not a loop.
        private const float Stood = 0.5f;
        // A loop this soon after the last warp only logs, so a bad landing never warps back and forth.
        private const float Rearm = 15f;
        // A door's walk-in takes a second or two; one still going after this never ends (a target over water).
        private const float StuckWalk = 8f;

        private static int inARow;
        private static bool stood = true;
        private static float groundSince = -1f;
        private static float walkingSince = -1f;
        private static float warpedAt = -100f;
        private static string lastTransfer = "none since the game started";

        internal static void Enable(ManualLogSource logger, Func<bool> enabled)
        {
            log = logger;
            on = enabled;
            if (Hooks.Install(typeof(RespawnLoop), "respawn", "a respawn loop in water or a hole still locks the game"))
            {
                log.LogInfo("[respawn] installed on Hazards.HazardAction, PlayerControl.LateUpdate and MainManager.TransferMap");
            }
        }

        internal static void Tick()
        {
            PlayerControl player = MainManager.player;
            if (on == null || !on() || player == null || player.entity == null)
            {
                groundSince = -1f;
                walkingSince = -1f;
                return;
            }
            float now = Time.realtimeSinceStartup;
            if (MainManager.roomtransition && player.entity.forcemove && !MainManager.instance.inevent)
            {
                if (walkingSince < 0f)
                {
                    walkingSince = now;
                }
                else if (now - walkingSince > StuckWalk)
                {
                    walkingSince = -1f;
                    log.LogWarning($"[respawn] a transfer's walk-in still going after {StuckWalk:0} s on {MainManager.map?.mapid} (last transfer {lastTransfer}, at {player.transform.position}); stopped as the game stops a walk");
                    player.entity.StopForceMove();
                }
            }
            else
            {
                walkingSince = -1f;
            }
            if (MainManager.instance.minipause || MainManager.roomtransition || !player.entity.onground)
            {
                groundSince = -1f;
                return;
            }
            if (groundSince < 0f)
            {
                groundSince = now;
            }
            else if (now - groundSince >= Stood)
            {
                stood = true;
            }
        }

        private static void Respawned(string how)
        {
            if (on == null || !on())
            {
                return;
            }
            inARow = stood ? 1 : inARow + 1;
            stood = false;
            groundSince = -1f;
            if (inARow < 3)
            {
                return;
            }
            PlayerControl player = MainManager.player;
            string where = $"on {MainManager.map?.mapid}, lastpos {player?.lastpos}, lastloadzone {player?.lastloadzone}, "
                + $"transfer running {MainManager.roomtransition}, last transfer {lastTransfer}";
            if (inARow < Limit)
            {
                log.LogInfo($"[respawn] {how}: {inARow} respawns in a row without standing on ground, {where}");
                return;
            }
            inARow = 0;
            float now = Time.realtimeSinceStartup;
            if (now - warpedAt < Rearm)
            {
                log.LogError($"[respawn] a loop again {now - warpedAt:0} s after the last warp; not warping again, {where}");
                return;
            }
            warpedAt = now;
            log.LogWarning($"[respawn] loop ({how}): {Limit} respawns in a row without standing on ground, {where}; warping to the start");
            MainManager.instance.StartCoroutine(WarpWhenRespawned());
        }

        private static IEnumerator WarpWhenRespawned()
        {
            // The hazard's own respawn holds minipause until it's done.
            float since = Time.realtimeSinceStartup;
            while (MainManager.instance.minipause && Time.realtimeSinceStartup - since < 5f)
            {
                yield return null;
            }
            WarpButton.WarpToStart("a respawn loop");
        }

        [HarmonyPatch(typeof(Hazards), "HazardAction")]
        [HarmonyPrefix]
        private static void BeforeHazard() => Respawned("water, a hole or spikes");

        // LateUpdate puts the party at lastpos once it's below the map's floor.
        [HarmonyPatch(typeof(PlayerControl), "LateUpdate")]
        [HarmonyPrefix]
        private static void BeforePlayerLate(PlayerControl __instance)
        {
            if (MainManager.map != null && __instance.transform.position.y < MainManager.map.ylimit)
            {
                Respawned("below the map's floor");
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.TransferMap), typeof(int), typeof(Vector3),
            typeof(Vector3), typeof(Vector3), typeof(NPCControl))]
        [HarmonyPrefix]
        private static void BeforeTransfer(int targetmap, NPCControl caller)
        {
            string from = MainManager.map != null ? MainManager.map.mapid.ToString() : "?";
            lastTransfer = caller == null ? $"{from} -> {(MainManager.Maps)targetmap} (no door: a warp or a scene)"
                : $"{from} -> {(MainManager.Maps)targetmap} through {caller.name}"
                + (DoorShuffle.Rewrote(from, caller.name) ? " (rewritten by door_targets)" : "");
        }
    }
}
