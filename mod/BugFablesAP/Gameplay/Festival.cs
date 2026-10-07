using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Golden Settlement festival, as a seed says (the user, 2026-10-07): the eating contest (Event57) always won
    // (contest_always_won), so its prize is the Moon Offering and Chubee's gift after it the Weak Stomach medal, both
    // locations, every time.
    internal static class Festival
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        private static bool forcedLogged;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(Contest), "festival", "the eating contest can be lost");
        }

        private static bool On => randomizerOn != null && randomizerOn() && seed?.Invoke() != null;

        // Event57 stores `won = b <= 0f` in a field of its coroutine: the value passes through here first.
        [HarmonyPatch]
        private static class Contest
        {
            private static MethodBase TargetMethod() =>
                AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EventControl), "Event57"));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "festival");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo force = AccessTools.Method(typeof(Festival), nameof(ForceWin))
                    ?? throw new MissingMethodException(nameof(Festival), nameof(ForceWin));
                int at = code.FindIndex(i => i.opcode == OpCodes.Stfld && i.operand is FieldInfo f
                    && f.Name.Contains("<won>"));
                if (at >= 0)
                {
                    code.Insert(at, new CodeInstruction(OpCodes.Call, force));
                }
                log.LogInfo($"[festival] {(at >= 0 ? "installed" : "NOT installed")} in Event57 (the eating contest's result)");
                return code;
            }
        }

        private static bool ForceWin(bool won)
        {
            if (won || !On || !seed().ContestAlwaysWon)
            {
                return won;
            }
            if (!forcedLogged)
            {
                forcedLogged = true;
                log.LogInfo("[festival] the eating contest lost: counted as won (contest_always_won)");
            }
            return true;
        }
    }
}
