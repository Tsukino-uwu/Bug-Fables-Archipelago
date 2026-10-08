using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Platinum Card's one effect, the bank's doubled interest, is the game's flag 630, which the banker sets as he
    // hands the card over: in a seed that flag is his check's, and the card travels as an item. So the clock's interest
    // asks whether the card (key item 176) is in the bag instead; the banker's own reads of the flag stay the game's.
    internal static class PlatinumCard
    {
        private const int Flag = 630;
        private const int Card = 176;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static readonly FieldInfo flagsField = AccessTools.Field(typeof(MainManager), "flags");

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            Hooks.Install(typeof(Interest), "card", "the bank's doubled interest follows flag 630, not the card");
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.DoClock))]
        private static class Interest
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "card");

            // `ldfld flags; ldc.i4 630; ldelem.u1` becomes `ldfld flags; ldc.i4 630; call HasCard`: bool[] and int in,
            // bool out.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo hasCard = AccessTools.Method(typeof(PlatinumCard), nameof(HasCard))
                    ?? throw new MissingMethodException(nameof(PlatinumCard), nameof(HasCard));
                List<int> reads = Enumerable.Range(2, Math.Max(0, code.Count - 2))
                    .Where(i => code[i].opcode == OpCodes.Ldelem_U1 && code[i - 2].LoadsField(flagsField)
                        && (code[i - 1].opcode == OpCodes.Ldc_I4 || code[i - 1].opcode == OpCodes.Ldc_I4_S)
                        && Convert.ToInt32(code[i - 1].operand) == Flag)
                    .ToList();
                foreach (int i in reads)
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = hasCard;
                }
                if (reads.Count == 1)
                {
                    log.LogInfo("[card] installed in MainManager.DoClock: the interest's flag 630 answered by the card");
                }
                else
                {
                    log.LogError($"[card] DoClock reads flag {Flag} {reads.Count} times, not once as measured: "
                        + "the doubled interest may follow the flag instead of the card");
                }
                return code;
            }
        }

        // The game's flags[index], or, in a seed, whether the Platinum Card is in the bag.
        public static bool HasCard(bool[] flags, int index)
        {
            if (index != Flag || randomizerOn == null || !randomizerOn())
            {
                return flags[index];
            }
            List<int>[] items = MainManager.instance?.items;
            return items != null && items.Length > 1 && items[1].Contains(Card);
        }
    }
}
