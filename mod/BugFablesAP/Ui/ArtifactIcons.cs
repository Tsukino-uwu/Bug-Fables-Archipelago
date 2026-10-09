using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The pause menu draws the first N artifact icons for N artifact flags, and blacks out a chapter's quest-page artifact
    // until N reaches it: right only in story order. In a seed each set flag shows its own chapter's icon.
    internal static class ArtifactIcons
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(Row), "artifacts", "the pause menu's artifacts show from the first chapter on");
            Hooks.Install(typeof(QuestPage), "artifacts", "a chapter's quest page shows its artifact by count");
        }

        private static bool On => randomizerOn != null && randomizerOn();

        private static bool Has(int chapter) =>
            chapter >= 0 && chapter < EnemyScaling.ArtifactFlags.Length
            && MainManager.instance.flags[EnemyScaling.ArtifactFlags[chapter]];

        // The row reads StartMenu.psprite[k] for k below the count: here the set chapters' icons come first.
        [HarmonyPatch(typeof(PauseMenu), "BuildWindow")]
        [HarmonyPatch(MethodType.Enumerator)]
        private static class Row
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "artifacts");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                FieldInfo icons = AccessTools.Field(typeof(StartMenu), nameof(StartMenu.psprite));
                MethodInfo ordered = AccessTools.Method(typeof(ArtifactIcons), nameof(RowIcons))
                    ?? throw new MissingMethodException(nameof(ArtifactIcons), nameof(RowIcons));
                int[] at = Enumerable.Range(0, code.Count).Where(i => code[i].LoadsField(icons)).ToArray();
                if (at.Length != 1)
                {
                    log.LogError($"[artifacts] NOT installed in PauseMenu.BuildWindow: expected one read of StartMenu.psprite, found {at.Length}");
                    return code;
                }
                code[at[0]].opcode = OpCodes.Call;
                code[at[0]].operand = ordered;
                log.LogInfo($"[artifacts] installed in PauseMenu.BuildWindow (instruction {at[0]})");
                return code;
            }
        }

        // The quest page's `SaveProgressIcons() < chapter + 1` blacks the artifact out: here it's the chapter's own flag.
        [HarmonyPatch(typeof(PauseMenu), "UpdateText")]
        private static class QuestPage
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "artifacts");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo count = AccessTools.Method(typeof(MainManager), nameof(MainManager.SaveProgressIcons));
                MethodInfo own = AccessTools.Method(typeof(ArtifactIcons), nameof(QuestPageCount))
                    ?? throw new MissingMethodException(nameof(ArtifactIcons), nameof(QuestPageCount));
                int[] at = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(count)).ToArray();
                if (at.Length != 1)
                {
                    log.LogError($"[artifacts] NOT installed in PauseMenu.UpdateText: expected one SaveProgressIcons call, found {at.Length}");
                    return code;
                }
                code[at[0]].operand = own;
                log.LogInfo($"[artifacts] installed in PauseMenu.UpdateText (instruction {at[0]})");
                return code;
            }
        }

        private static int[] RowIcons()
        {
            int[] game = StartMenu.psprite;
            if (!On)
            {
                return game;
            }
            List<int> chapters = Enumerable.Range(0, game.Length).ToList();
            return chapters.Where(Has).Concat(chapters.Where(c => !Has(c))).Select(c => game[c]).ToArray();
        }

        private static int QuestPageCount()
        {
            if (!On)
            {
                return MainManager.SaveProgressIcons();
            }
            int chapter = Math.Abs(MainManager.listvar[MainManager.instance.option]) - 11;
            return Has(chapter) ? EnemyScaling.ArtifactFlags.Length : 0;
        }
    }
}
