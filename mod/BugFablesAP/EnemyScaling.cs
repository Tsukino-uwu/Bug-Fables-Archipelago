using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Enemy scaling (Quality of life): each enemy has a home level, where vanilla expects it; a fight's enemies are
    // scaled from their home level to the target (the party's level, or the artifacts found). Starting values, tuned
    // by play; the numbers' sources are in the mod guide, step 17.
    internal static class EnemyScaling
    {
        internal static readonly string[] Modes = { "Off", "PartyLevel", "Artifacts" };

        private static Func<bool> randomizerOn;
        private static Func<string> mode;
        private static Harmony harmony;
        private static ManualLogSource log;

        // Home level below "outgrown" (the level where an enemy's EXP runs out): fits both the new game (level 1, the
        // first enemies outgrown near 4) and the level cap (27, the last enemies outgrown near 30).
        private const float OutgrownGap = 3f;
        // HP scales by (target + HpBase) / (home + HpBase); attack and defence by one step per so many levels.
        private const float HpBase = 5f;
        private const int LevelsPerAttack = 4, LevelsPerDefence = 6, MinAttackStep = -3, MaxAttackStep = 6;

        // Bosses and special enemies: flat EXP says nothing, so their home level is their chapter's (the story event
        // that starts their fight); summoned parts take their boss's.
        private static readonly int[] ChapterLevel = { 1, 3, 8, 11, 14, 17, 21, 26 };
        private static readonly Dictionary<int, int> BossChapter = new Dictionary<int, int>
        {
            { 13, 1 },
            { 3, 2 }, { 15, 2 }, { 21, 2 }, { 22, 2 }, { 24, 2 },
            { 31, 3 }, { 42, 3 }, { 46, 3 },
            { 40, 4 }, { 49, 4 }, { 54, 4 }, { 59, 4 }, { 60, 4 },
            { 35, 5 }, { 41, 5 }, { 47, 5 }, { 50, 5 }, { 99, 5 }, { 55, 5 }, { 62, 5 }, { 69, 5 }, { 72, 5 }, { 77, 5 },
            { 98, 5 }, { 104, 5 },
            { 23, 6 }, { 34, 6 }, { 36, 6 }, { 51, 6 }, { 74, 6 }, { 75, 6 }, { 76, 6 }, { 85, 6 }, { 86, 6 }, { 100, 6 },
            { 95, 6 }, { 96, 6 }, { 97, 6 },
            { 90, 7 }, { 91, 7 }, { 92, 7 }, { 93, 7 }, { 94, 7 }, { 111, 7 }, { 112, 7 }, { 113, 7 }, { 114, 7 },
            { 115, 7 },
        };
        // Scripted fights left as they are: the intro spider (can't be won), the tutorials and test fights, and the
        // ids the game itself leaves out of Hard Mode's x1.5 (the keys and tablet, the Wasp General).
        private static readonly HashSet<int> Untouched = new HashSet<int> { 2, 11, 12, 18, 110, 101, 102, 103, 72 };
        // The artifact flags, one per chapter end, and the level of the areas vanilla opens after that many.
        private static readonly int[] ArtifactFlags = { 41, 88, 299, 345, 347, 346, 555 };
        private static readonly int[] ArtifactLevel = { 1, 6, 9, 13, 16, 19, 23, 27 };

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled, Func<string> scalingMode)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            mode = scalingMode;
            var method = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetEnemyData), new[] { typeof(int), typeof(bool), typeof(bool) });
            if (method == null)
            {
                log.LogError("[scale] NOT installed: MainManager.GetEnemyData(int, bool, bool) wasn't found; enemies keep vanilla stats.");
                return;
            }
            harmony = new Harmony(guid + ".scale." + DateTime.UtcNow.Ticks);
            harmony.Patch(method, postfix: new HarmonyMethod(typeof(EnemyScaling), nameof(AfterGetEnemyData)));
            log.LogInfo("[scale] installed on MainManager.GetEnemyData");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        internal static int? HomeLevel(int id)
        {
            if (Untouched.Contains(id))
            {
                return null;
            }
            if (BossChapter.TryGetValue(id, out int chapter))
            {
                return ChapterLevel[chapter];
            }
            if (MainManager.enemydata == null || id < 0 || id >= MainManager.enemydata.GetLength(0))
            {
                return null;
            }
            // Some ids read another row's data; that row's EXP is the one the game uses.
            int row = id;
            if (int.TryParse(MainManager.enemydata[id, 25], out int swapped) && swapped >= 0 && swapped < MainManager.enemydata.GetLength(0))
            {
                row = swapped;
            }
            if (!int.TryParse(MainManager.enemydata[row, 3], out int exp))
            {
                return null;
            }
            return Mathf.Clamp(Mathf.RoundToInt(exp / 2.5f + 1f - OutgrownGap), 1, 27);
        }

        internal static int? TargetLevel()
        {
            string m = mode?.Invoke();
            if (m == "PartyLevel")
            {
                return MainManager.instance.partylevel;
            }
            if (m == "Artifacts")
            {
                int found = 0;
                foreach (int flag in ArtifactFlags)
                {
                    if (MainManager.instance.flags[flag])
                    {
                        found++;
                    }
                }
                return ArtifactLevel[found];
            }
            return null;
        }

        private static void AfterGetEnemyData(int id, bool createentity, bool noexp, ref MainManager.BattleData __result)
        {
            if (!createentity || randomizerOn == null || !randomizerOn() || MainManager.instance == null)
            {
                return;
            }
            int? target = TargetLevel();
            int? home = HomeLevel(id);
            if (target == null || home == null)
            {
                return;
            }
            int diff = target.Value - home.Value;
            if (diff == 0)
            {
                return;
            }
            int hp = Mathf.Max(1, Mathf.RoundToInt(__result.hp * (target.Value + HpBase) / (home.Value + HpBase)));
            int atk = Mathf.Clamp(diff / LevelsPerAttack, MinAttackStep, MaxAttackStep);
            // A defence of -1 means "shown as ?", left alone; otherwise never below 0.
            int def = __result.def < 0 ? __result.def : Mathf.Max(0, __result.def + diff / LevelsPerDefence);
            int exp = __result.exp;
            // EXP as the game gives it at the enemy's home level, so levelling keeps its pace. Left alone where the game
            // fixes it: fixed EXP, no EXP, the level cap, hologram fights.
            if (!noexp && !__result.fixedexp && MainManager.instance.partylevel < 27 && !MainManager.instance.flags[613]
                && !MainManager.instance.flags[162] && int.TryParse(MainManager.enemydata[__result.animid, 3], out int baseExp))
            {
                // animid is the row the game read (column 25 can point an id at another row).
                int asIf = Mathf.Clamp(MainManager.instance.partylevel - diff, 1, 27);
                exp = MainManager.GetEXP(baseExp, asIf, (MainManager.Enemies)__result.animid);
            }
            log.LogInfo($"[scale] {(MainManager.Enemies)id} ({id}): home {home}, target {target}: hp {__result.hp} -> {hp}, "
                + $"attack {(atk >= 0 ? "+" : "")}{atk}, def {__result.def} -> {def}, exp {__result.exp} -> {exp}");
            __result.hp = hp;
            __result.maxhp = hp;
            __result.hardatk += atk;
            __result.def = def;
            __result.exp = exp;
        }
    }
}
