using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Each chapter's scene files its main quest (11 to 17) at a fixed place in the quest lists, which throws, freezing the
    // scene, when fewer quests come before it: only possible out of story order. In a seed it goes to the end instead.
    internal static class ChapterQuests
    {
        // The scenes whose inserts use an index past 0 (MEASURED.md, the treasure room); 9 inserts in all.
        private static readonly string[] Scenes =
            { "Event45", "Event73", "Event99", "Event118", "Event142", "Event194", "Event203" };

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(Filing), "chapters",
                "a chapter's scene reached out of story order can freeze filing its main quest");
        }

        [HarmonyPatch]
        private static class Filing
        {
            private static IEnumerable<MethodBase> TargetMethods() => Scenes.Select(scene =>
                (MethodBase)AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EventControl), scene)
                    ?? throw new MissingMethodException(nameof(EventControl), scene)));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "chapters");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo insert = AccessTools.Method(typeof(List<int>), nameof(List<int>.Insert));
                MethodInfo safe = AccessTools.Method(typeof(ChapterQuests), nameof(Insert))
                    ?? throw new MissingMethodException(nameof(ChapterQuests), nameof(Insert));
                int[] at = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(insert)).ToArray();
                string[] filed = at.Select(i => $"quest {Constant(code, i - 1)} at {Constant(code, i - 2)}").ToArray();
                foreach (int i in at)
                {
                    // Same stack shape (list, index, quest); labels stay on the instruction.
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = safe;
                }
                log.LogInfo(at.Length > 0
                    ? $"[chapters] installed: {string.Join(", ", filed)}"
                    : "[chapters] NOT installed in one scene: no quest-list insert found");
                return code;
            }

            private static string Constant(List<CodeInstruction> code, int i)
            {
                if (i < 0)
                {
                    return "?";
                }
                CodeInstruction c = code[i];
                if (c.opcode == OpCodes.Ldc_I4_S || c.opcode == OpCodes.Ldc_I4)
                {
                    return Convert.ToInt32(c.operand).ToString();
                }
                OpCode[] small = { OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4,
                    OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8 };
                int n = Array.IndexOf(small, c.opcode);
                return n >= 0 ? n.ToString() : "?";
            }
        }

        private static void Insert(List<int> list, int index, int quest)
        {
            if (index > list.Count && randomizerOn != null && randomizerOn())
            {
                log.LogInfo($"[chapters] quest {quest} filed at {list.Count}, not {index}: the list held {list.Count}");
                index = list.Count;
            }
            list.Insert(index, quest);
        }
    }
}
