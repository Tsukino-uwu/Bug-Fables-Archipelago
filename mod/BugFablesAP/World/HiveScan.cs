using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Bee Kingdom's scan (Event84's first part, flag 159) also sets what its kept-away second part would (flags_with:
    // 160, which teaches Leif Bubble Shield Lite and moves HB from beside his lab), in a seed.
    internal static class HiveScan
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(Scan), "scan", "the scan sets only its own flag, 159");
        }

        [HarmonyPatch]
        private static class Scan
        {
            private static MethodBase TargetMethod() =>
                AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EventControl), "Event84"));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "scan");

            // flags[159] = true: ldc.i4 159, ldc.i4.1, stelem.i1, written once, in the scan's part.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo after = AccessTools.Method(typeof(HiveScan), nameof(AfterScan))
                    ?? throw new MissingMethodException(nameof(HiveScan), nameof(AfterScan));
                int[] at = Enumerable.Range(0, code.Count - 2).Where(i => code[i].opcode == OpCodes.Ldc_I4
                    && Convert.ToInt32(code[i].operand) == 159 && code[i + 1].opcode == OpCodes.Ldc_I4_1
                    && code[i + 2].opcode == OpCodes.Stelem_I1).ToArray();
                if (at.Length != 1)
                {
                    log.LogError($"[scan] NOT installed in Event84: expected one write of flag 159, found {at.Length}");
                    return code;
                }
                code.Insert(at[0] + 3, new CodeInstruction(OpCodes.Call, after));
                log.LogInfo($"[scan] installed in Event84 (instruction {at[0]})");
                return code;
            }
        }

        private static void AfterScan()
        {
            List<int> also = (seed?.Invoke()?.FlagsWith ?? new List<ApConnection.FlagWith>())
                .Where(w => w.Event == 84 && w.Flag == 159).Select(w => w.Also).ToList();
            if (randomizerOn == null || !randomizerOn() || also.Count == 0)
            {
                log.LogInfo("[scan] flag 159 set by the scan: nothing set with it (not in a seed that asks)");
                return;
            }
            foreach (int flag in also)
            {
                MainManager.instance.flags[flag] = true;
            }
            log.LogInfo($"[scan] flag 159 set by the scan: flag(s) {string.Join(", ", also)} set with it (flags_with)");
        }
    }
}
