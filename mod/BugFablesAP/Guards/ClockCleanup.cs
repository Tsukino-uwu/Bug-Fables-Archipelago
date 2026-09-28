using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The play-time clock unloads unused assets and forces a garbage collection every fifth second, a ~110 ms stall. With
    // Archipelago on (or Use on normal saves) it doesn't: leaving a map still does both, and the runtime collects when it
    // needs to.
    internal static class ClockCleanup
    {
        private static ManualLogSource log;
        private static Func<bool> settingsOn;
        private static bool skipLogged;
        private static readonly MethodInfo UnloadUnused = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
        private static readonly MethodInfo Collect = AccessTools.Method(typeof(GC), nameof(GC.Collect), Type.EmptyTypes);

        internal static void Enable(ManualLogSource logger, Func<bool> settingsEnabled)
        {
            log = logger;
            settingsOn = settingsEnabled;
            if (UnloadUnused == null || Collect == null)
            {
                log.LogError($"[clock] NOT installed (UnloadUnusedAssets {UnloadUnused != null}, GC.Collect {Collect != null}): "
                    + "the stall every 5 s stays.");
                return;
            }
            Hooks.Install(typeof(ClockCleanup), "clock", "the stall every 5 s stays");
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.DoClock))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditDoClock, "clock");

        private static IEnumerable<CodeInstruction> EditDoClock(List<CodeInstruction> code)
        {
            List<CodeInstruction> unloads = code.Where(i => i.Calls(UnloadUnused)).ToList();
            List<CodeInstruction> collects = code.Where(i => i.Calls(Collect)).ToList();
            MethodInfo unload = AccessTools.Method(typeof(ClockCleanup), nameof(Unload))
                ?? throw new MissingMethodException(nameof(ClockCleanup), nameof(Unload));
            MethodInfo collect = AccessTools.Method(typeof(ClockCleanup), nameof(CollectGarbage))
                ?? throw new MissingMethodException(nameof(ClockCleanup), nameof(CollectGarbage));
            unloads.ForEach(i => i.operand = unload);
            collects.ForEach(i => i.operand = collect);
            int replaced = unloads.Count + collects.Count;
            log.LogInfo($"[clock] {(replaced == 2 ? "installed" : "NOT fully installed")} in MainManager.DoClock ({replaced} of 2 calls)");
            return code;
        }

        private static bool Skip()
        {
            bool skip = settingsOn != null && settingsOn();
            if (skip && !skipLogged)
            {
                skipLogged = true;
                log.LogInfo("[clock] the 5-second unload and collection skipped (Archipelago on, or Use on normal saves)");
            }
            return skip;
        }

        private static AsyncOperation Unload() => Skip() ? null : Resources.UnloadUnusedAssets();

        private static void CollectGarbage()
        {
            if (!Skip())
            {
                GC.Collect();
            }
        }
    }
}
