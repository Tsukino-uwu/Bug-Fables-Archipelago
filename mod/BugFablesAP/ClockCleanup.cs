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
        private static Harmony harmony;
        private static bool skipLogged;
        private static readonly MethodInfo UnloadUnused = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
        private static readonly MethodInfo Collect = AccessTools.Method(typeof(GC), nameof(GC.Collect), Type.EmptyTypes);

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> settingsEnabled)
        {
            log = logger;
            settingsOn = settingsEnabled;
            MethodInfo doClock = AccessTools.Method(typeof(MainManager), nameof(MainManager.DoClock));
            if (doClock == null || UnloadUnused == null || Collect == null)
            {
                log.LogError($"[clock] NOT installed (MainManager.DoClock {doClock != null}, UnloadUnusedAssets {UnloadUnused != null}, "
                    + $"GC.Collect {Collect != null}): the stall every 5 s stays.");
                return;
            }
            harmony = new Harmony(guid + ".clock." + DateTime.UtcNow.Ticks);
            harmony.Patch(doClock, transpiler: new HarmonyMethod(typeof(ClockCleanup), nameof(Transpile)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            int replaced = 0;
            foreach (CodeInstruction instruction in code)
            {
                if (instruction.Calls(UnloadUnused))
                {
                    instruction.operand = AccessTools.Method(typeof(ClockCleanup), nameof(Unload));
                    replaced++;
                }
                else if (instruction.Calls(Collect))
                {
                    instruction.operand = AccessTools.Method(typeof(ClockCleanup), nameof(CollectGarbage));
                    replaced++;
                }
            }
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
