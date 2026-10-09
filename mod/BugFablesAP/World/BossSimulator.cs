using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // B.O.S.S. (HB's computer, Event85) lists only bosses already met; an empty list throws as its picker opens and
    // freezes the party. A seed reaches it early (HB asks for the Explorer Permit from the start), so with nobody met in
    // the chosen list the computer logs off, as its own cancel does (flag 162 off), instead of opening either picker.
    internal static class BossSimulator
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(Lists), "boss", "B.O.S.S. with nobody met in a list freezes the party");
        }

        [HarmonyPatch]
        private static class Lists
        {
            private static MethodBase TargetMethod() =>
                AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EventControl), "Event85"));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "boss");

            // The list is chosen just before the scene's first read of flags[163] (its EX check): ldsfld instance,
            // ldfld flags, ldc.i4 163, ldelem.u1.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                FieldInfo instance = AccessTools.Field(typeof(MainManager), nameof(MainManager.instance));
                FieldInfo flags = AccessTools.Field(typeof(MainManager), nameof(MainManager.flags));
                MethodInfo check = AccessTools.Method(typeof(BossSimulator), nameof(CheckList))
                    ?? throw new MissingMethodException(nameof(BossSimulator), nameof(CheckList));
                int at = Enumerable.Range(2, Math.Max(0, code.Count - 3)).FirstOrDefault(i =>
                    code[i].opcode == OpCodes.Ldc_I4 && Convert.ToInt32(code[i].operand) == 163
                    && code[i + 1].opcode == OpCodes.Ldelem_U1 && code[i - 1].LoadsField(flags)
                    && code[i - 2].LoadsField(instance));
                if (at < 2)
                {
                    log.LogError("[boss] NOT installed in Event85: its read of flags[163] not found");
                    return code;
                }
                var call = new CodeInstruction(OpCodes.Call, check);
                code[at - 2].MoveLabelsTo(call);
                code.Insert(at - 2, call);
                log.LogInfo($"[boss] installed in Event85 (instruction {at - 2})");
                return code;
            }
        }

        // flagvar[1]: the list chosen, 0 the mini-bosses, 1 the bosses (any other: cancelled, left alone).
        private static void CheckList()
        {
            MainManager mm = MainManager.instance;
            int list = mm.flagvar[1];
            if (randomizerOn == null || !randomizerOn() || (list != 0 && list != 1))
            {
                return;
            }
            string which = list == 0 ? "mini-boss" : "boss";
            int met = MainManager.GetBosses().Length;
            if (met > 0)
            {
                log.LogInfo($"[boss] B.O.S.S.'s {which} list: {met} met");
                return;
            }
            mm.flags[162] = false;
            mm.flagvar[0] = -1;
            log.LogInfo($"[boss] B.O.S.S.'s {which} list is empty (none met yet): it logs off instead of opening it");
        }
    }
}
