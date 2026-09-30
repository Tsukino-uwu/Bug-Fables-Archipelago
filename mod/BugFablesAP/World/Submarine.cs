using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The submarine is an item (slot_data submarine_item): the docks' scene answers its two story checks from the
    // submarine's key item in the bag, flag 447 (the Termite pier's introduction, else it starts that scene) and 448
    // (the first landing at the Bugaria pier, else most docks refuse). The flags stay the game's; the docks themselves
    // exist by the same key item (KeptOpen, present_with_item).
    internal static class Submarine
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        private static readonly FieldInfo flagsField = AccessTools.Field(typeof(MainManager), "flags");
        private static readonly int[] storyFlags = { 447, 448 };

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(Submarine), "submarine",
                "the docks follow the story's flags, and a submarine received early won't sail");
        }

        [HarmonyPatch(typeof(EventControl), "Event153", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TranspileEvent153(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditEvent153, "submarine");

        // `ldfld flags; ldc.i4 n; ldelem.u1` becomes `ldfld flags; ldc.i4 n; call Docked`, as Abilities does.
        private static IEnumerable<CodeInstruction> EditEvent153(List<CodeInstruction> code)
        {
            List<int> reads = Enumerable.Range(2, Math.Max(0, code.Count - 2))
                .Where(i => code[i].opcode == OpCodes.Ldelem_U1 && code[i - 2].LoadsField(flagsField)
                    && (code[i - 1].opcode == OpCodes.Ldc_I4 || code[i - 1].opcode == OpCodes.Ldc_I4_S)
                    && storyFlags.Contains(Convert.ToInt32(code[i - 1].operand)))
                .ToList();
            MethodInfo docked = AccessTools.Method(typeof(Submarine), nameof(Docked))
                ?? throw new MissingMethodException(nameof(Submarine), nameof(Docked));
            foreach (int i in reads)
            {
                code[i].opcode = OpCodes.Call;
                code[i].operand = docked;
            }
            string counts = $"{reads.Count} of 2 story reads answered from the bag";
            if (reads.Count != 2)
            {
                log.LogError($"[submarine] Event153: {counts}, which differs from what was measured; a dock may follow "
                    + "its story flag instead of the item");
            }
            log.LogInfo($"[submarine] installed in Event153: {counts}");
            return code;
        }

        // The game's flags[index], or, in a seed where the submarine is an item, whether its key item has come.
        public static bool Docked(bool[] flags, int index)
        {
            if (flags[index] || randomizerOn == null || !randomizerOn() || !(seed?.Invoke()?.SubmarineItem ?? false))
            {
                return flags[index];
            }
            List<int>[] items = MainManager.instance?.items;
            return items != null && items.Length > 1 && items[1].Contains(CustomItems.Submarine);
        }
    }
}
