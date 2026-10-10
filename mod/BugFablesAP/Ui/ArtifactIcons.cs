using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The pause menu draws the first N artifact icons for N artifact flags, and blacks out a chapter's quest-page artifact
    // until N reaches it: right only in story order. In a seed it shows each chapter's own: the artifact item received,
    // for a chapter the world holds as an item; the story's artifact flag for the others.
    internal static class ArtifactIcons
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        // The seed's artifact items in the server's received list, (index, chapter) (ItemReceiver): kept after a dropped
        // connection, so a file of that seed loaded offline still counts what it was given.
        private static string trackedSeed;
        private static int trackedCount = -1;
        private static readonly List<KeyValuePair<int, int>> tracked = new List<KeyValuePair<int, int>>();
        // Chapters (1-7) given to the file in play: those before its received count; rebuilt when either changes.
        private static readonly HashSet<int> given = new HashSet<int>();
        private static string givenSeed;
        private static int givenCount = -1;
        private static int trackedVersion;
        private static int givenVersion = -1;
        private static readonly Sprite[] itemSized = new Sprite[7];

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(Row), "artifacts", "the pause menu's artifacts show from the first chapter on");
            Hooks.Install(typeof(QuestPage), "artifacts", "a chapter's quest page shows its artifact by count");
        }

        private static bool On => randomizerOn != null && randomizerOn();

        internal static bool Tracking(string seedName, int count) => seedName == trackedSeed && count == trackedCount;

        internal static void Track(string seedName, int count, IEnumerable<KeyValuePair<int, int>> artifacts)
        {
            trackedSeed = seedName;
            trackedCount = count;
            tracked.Clear();
            tracked.AddRange(artifacts);
            trackedVersion++;
        }

        // The file's seed (ItemReceiver.SeedSlot) must be the tracked one: a file of another seed, an unbound one, or one
        // just re-tied by AdoptSeed (its count back to 0) counts none.
        private static HashSet<int> Given()
        {
            MainManager mm = MainManager.instance;
            string fileSeed = mm?.flagstring?[ItemReceiver.SeedSlot];
            int count = mm?.flagvar?[ItemReceiver.CountSlot] ?? 0;
            if (fileSeed == givenSeed && count == givenCount && trackedVersion == givenVersion)
            {
                return given;
            }
            given.Clear();
            givenSeed = fileSeed;
            givenCount = count;
            givenVersion = trackedVersion;
            if (!string.IsNullOrEmpty(fileSeed) && fileSeed == trackedSeed)
            {
                given.UnionWith(tracked.Where(entry => entry.Key < count).Select(entry => entry.Value));
            }
            return given;
        }

        // The goal's count: the artifact items the file in play has been given.
        internal static int ReceivedCount => Given().Count;

        private static bool IsItem(int chapter)
        {
            Dictionary<long, int> kinds = seed?.Invoke()?.ItemKinds;
            return kinds != null && kinds.TryGetValue(ItemIds.Base + ItemIds.ArtifactOffset + chapter, out int kind)
                && kind == ItemIds.ArtifactKind;
        }

        // By the pause menu's index, 0-6.
        private static bool Has(int index)
        {
            if (index < 0 || index >= EnemyScaling.ArtifactFlags.Length)
            {
                return false;
            }
            return IsItem(index + 1)
                ? Given().Contains(index + 1)
                : MainManager.instance.flags[EnemyScaling.ArtifactFlags[index]];
        }

        // The pause menu's icon for a chapter (1-7), copied to an item's size for a hold-up or a pickup.
        internal static Sprite Sprite(int chapter)
        {
            int index = chapter - 1;
            if (index < 0 || index >= StartMenu.psprite.Length)
            {
                return ApIcon.Get();
            }
            Sprite icon = MainManager.guisprites[StartMenu.psprite[index]];
            if (itemSized[index] == null && icon != null)
            {
                itemSized[index] = ItemSwap.ScaledToItem(icon);
            }
            return itemSized[index] ?? icon;
        }

        // The row reads StartMenu.psprite[k] for k below the count: here the count is the chapters shown and their icons
        // come first.
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
                MethodInfo count = AccessTools.Method(typeof(MainManager), nameof(MainManager.SaveProgressIcons));
                MethodInfo ordered = AccessTools.Method(typeof(ArtifactIcons), nameof(RowIcons))
                    ?? throw new MissingMethodException(nameof(ArtifactIcons), nameof(RowIcons));
                MethodInfo shown = AccessTools.Method(typeof(ArtifactIcons), nameof(RowCount))
                    ?? throw new MissingMethodException(nameof(ArtifactIcons), nameof(RowCount));
                int[] at = Enumerable.Range(0, code.Count).Where(i => code[i].LoadsField(icons)).ToArray();
                int[] counted = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(count)).ToArray();
                if (at.Length != 1 || counted.Length != 1)
                {
                    log.LogError("[artifacts] NOT installed in PauseMenu.BuildWindow: expected one read of "
                        + $"StartMenu.psprite and one SaveProgressIcons call, found {at.Length} and {counted.Length}");
                    return code;
                }
                code[at[0]].opcode = OpCodes.Call;
                code[at[0]].operand = ordered;
                code[counted[0]].operand = shown;
                log.LogInfo($"[artifacts] installed in PauseMenu.BuildWindow (instructions {counted[0]}, {at[0]})");
                return code;
            }
        }

        // The quest page's `SaveProgressIcons() < chapter + 1` blacks the artifact out: here it's the chapter's own.
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

        private static int RowCount() =>
            On ? Enumerable.Range(0, StartMenu.psprite.Length).Count(Has) : MainManager.SaveProgressIcons();

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
