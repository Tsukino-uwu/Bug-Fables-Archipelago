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
    // Enemy scaling (the Gameplay page): each enemy has a home level, where vanilla expects it; a fight's enemies are
    // scaled from their home level to the target (the party's level, or the artifacts found). Starting values, tuned
    // by play.
    internal static class EnemyScaling
    {
        internal static readonly string[] Modes = { "Off", "PartyLevel", "Artifacts" };

        private static Func<bool> randomizerOn;
        private static Func<string> mode;
        private static ManualLogSource log;

        // Home level below "outgrown" (the level where an enemy's EXP runs out): fits both the new game (level 1, the
        // first enemies outgrown near 4) and the level cap (27, the last enemies outgrown near 30).
        private const float OutgrownGap = 3f;
        // HP and each hit's damage scale by (target + Base) / (home + Base); defence by one step per so many levels.
        private const float Base = 5f;
        private const int LevelsPerDefence = 6;

        // Bosses and special enemies: flat EXP says nothing, so their home level is their chapter's (the story event
        // that starts their fight); summoned parts take their boss's.
        private static readonly int[] ChapterLevel = { 1, 3, 8, 11, 14, 17, 21, 26 };
        private static readonly Dictionary<int, int> BossChapter = new Dictionary<int, int>
        {
            { 13, 1 },
            { 3, 2 }, { 15, 2 }, { 21, 2 }, { 22, 2 }, { 24, 2 },
            { 31, 3 }, { 42, 3 }, { 46, 3 },
            { 40, 4 }, { 49, 4 }, { 54, 4 }, { 59, 4 }, { 60, 4 },
            { 35, 5 }, { 41, 5 }, { 47, 5 }, { 50, 5 }, { 99, 5 }, { 55, 5 }, { 62, 5 }, { 69, 5 }, { 72, 5 },
                { 77, 5 },
            { 98, 5 }, { 104, 5 },
            { 23, 6 }, { 34, 6 }, { 36, 6 }, { 51, 6 }, { 74, 6 }, { 75, 6 }, { 76, 6 }, { 85, 6 }, { 86, 6 },
                { 100, 6 },
            { 95, 6 }, { 96, 6 }, { 97, 6 },
            { 90, 7 }, { 91, 7 }, { 92, 7 }, { 93, 7 }, { 94, 7 }, { 111, 7 }, { 112, 7 }, { 113, 7 }, { 114, 7 },
            { 115, 7 },
        };
        // Left as they are: the intro spider (can't be won), the tutorials and test fights, and the Everlasting King's
        // keys and tablet (parts of his fight).
        private static readonly HashSet<int> Untouched = new HashSet<int> { 2, 11, 12, 18, 110, 101, 102, 103 };
        // The artifact flags, one per chapter end, and the level of the areas vanilla opens after that many.
        private static readonly int[] ArtifactFlags = { 41, 88, 299, 345, 347, 346, 555 };
        private static readonly int[] ArtifactLevel = { 1, 6, 9, 13, 16, 19, 23, 27 };

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled, Func<string> scalingMode)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            mode = scalingMode;
            if (!Hooks.Install(typeof(EnemyScaling), "scale", "enemies keep vanilla stats"))
            {
                return;
            }
            bool damage = Hooks.Install(typeof(Damage), "scale", "enemy damage isn't scaled");
            log.LogInfo("[scale] installed on MainManager.GetEnemyData"
                + (damage ? " and BattleControl.CalculateBaseDamage" : ""));
            if (BestiaryRows == null)
            {
                log.LogWarning("[scale] PauseMenu's enemydata wasn't found; the bestiary shows vanilla stats.");
            }
            else
            {
                Hooks.Install(typeof(Bestiary), "scale", "the bestiary shows vanilla stats");
            }
            Hooks.Install(typeof(ScriptNumbers), "scale", "fixed numbers in enemy scripts (heals, HP set) stay vanilla");
        }

        // The ratio enemy scaling gives this enemy now; 1 when it leaves the enemy alone.
        internal static float RatioFor(int id)
        {
            if (randomizerOn == null || !randomizerOn())
            {
                return 1f;
            }
            int? target = TargetLevel();
            int? home = HomeLevel(id);
            return target == null || home == null ? 1f : Ratio(home.Value, target.Value);
        }

        // A number in HP units, scaled as the enemy's HP is; at least 1.
        private static int Scaled(int n, float ratio) =>
            Mathf.Approximately(ratio, 1f) ? n : Mathf.Max(1, Mathf.RoundToInt(n * ratio));

        private static readonly HashSet<string> numbersLogged = new HashSet<string>();
        private static BattleControl numbersBattle;

        private static int LogScaled(string what, int id, int n, float ratio)
        {
            int scaled = Scaled(n, ratio);
            if (MainManager.battle != numbersBattle)
            {
                numbersBattle = MainManager.battle;
                numbersLogged.Clear();
            }
            if (scaled != n && numbersLogged.Add($"{what}/{id}/{n}"))
            {
                log.LogInfo($"[scale] {(MainManager.Enemies)id} ({id}) {what} {n} -> {scaled} (x{ratio:0.00})");
            }
            return scaled;
        }

        // The fixed numbers in enemy scripts that scale, each by the ratio of the enemy whose HP it measures.
        private static class ScriptNumbers
        {
            // A literal heal amount marks itself on its way to the call; the heal is scaled only when the one healed
            // is an enemy, and only that literal (the other branch of a "? :", such as the enemy's max HP, passes
            // untouched). The call is replaced rather than Heal patched: Heal(ref, int?) is a one-line wrapper the
            // runtime may inline, where a patch never runs.
            private static int pendingHeal = int.MinValue;

            private static int HealLiteral(int n)
            {
                pendingHeal = n;
                return n;
            }

            private static readonly MethodInfo GameHeal = AccessTools.Method(typeof(BattleControl), "Heal",
                new[] { typeof(MainManager.BattleData).MakeByRefType(), typeof(int?) });

            private static void ScaledHeal(BattleControl battle, ref MainManager.BattleData entity, int? ammount)
            {
                int literal = pendingHeal;
                pendingHeal = int.MinValue;
                if (literal != int.MinValue && ammount == literal && entity.battleentity != null
                    && !entity.battleentity.CompareTag("Player"))
                {
                    ammount = LogScaled("heals", entity.animid, literal, RatioFor(entity.animid));
                }
                object[] args = { entity, ammount };
                GameHeal.Invoke(battle, args);
                entity = (MainManager.BattleData)args[0];
            }

            // Stratos and Delilah revive each other at 7 HP, Maki's summoned ally is set to 10.
            private static void SetHp(ref MainManager.BattleData target, int n) =>
                target.hp = LogScaled("is set to HP", target.animid, n, RatioFor(target.animid));

            // The 7 the revive shows, by the reviver's ratio (the pair share their chapter, so their ratio).
            private static int ByActor(int n, object step)
            {
                int id = PartySlots.ActingEnemy(step, out _);
                return id < 0 ? n : LogScaled("shows", id, n, RatioFor(id));
            }

            private static readonly Dictionary<Type, FieldInfo> startIndexFields = new Dictionary<Type, FieldInfo>();

            // The battle start's flat HP adjustments (Spuder, Zasp and Mothiva, Maki's team, fire areas).
            private static int AtBattleStart(int n, object step)
            {
                Type type = step.GetType();
                if (!startIndexFields.TryGetValue(type, out FieldInfo field))
                {
                    startIndexFields[type] = field = AccessTools.GetDeclaredFields(type)
                        .FirstOrDefault(f => f.Name.StartsWith("<i>") && f.FieldType == typeof(int));
                }
                MainManager.BattleData[] enemies = MainManager.battle?.enemydata;
                int i = field != null ? (int)field.GetValue(step) : -1;
                if (enemies == null || i < 0 || i >= enemies.Length)
                {
                    return n;
                }
                return LogScaled("gets a start adjustment of", enemies[i].animid, n, RatioFor(enemies[i].animid));
            }

            private static readonly FieldInfo CurrentEnemy = AccessTools.Field(typeof(BattleControl), "currentEnemy");

            // The holo party's AI thresholds, and EnemyHeavyThrow's.
            private static int ForCurrentEnemy(int n)
            {
                BattleControl battle = MainManager.battle;
                MainManager.BattleData[] enemies = battle?.enemydata;
                int i = battle != null ? (int)CurrentEnemy.GetValue(battle) : -1;
                if (enemies == null || i < 0 || i >= enemies.Length)
                {
                    return n;
                }
                return LogScaled("acts at", enemies[i].animid, n, RatioFor(enemies[i].animid));
            }

            private static int? Constant(CodeInstruction c)
            {
                for (int v = 2; v <= 99; v++)
                {
                    if (c.LoadsConstant(v))
                    {
                        return v;
                    }
                }
                return null;
            }

            private static bool IsBranch(CodeInstruction c) => c.opcode == OpCodes.Br || c.opcode == OpCodes.Br_S;

            private static CodeInstruction Call(string helper) =>
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ScriptNumbers), helper));

            [HarmonyPatch(typeof(BattleControl), "DoAction", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileDoAction(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, EditDoAction, "scale");

            private static IEnumerable<CodeInstruction> EditDoAction(List<CodeInstruction> code)
            {
                MethodInfo heal = AccessTools.Method(typeof(BattleControl), "Heal",
                    new[] { typeof(MainManager.BattleData).MakeByRefType(), typeof(int?) });
                ConstructorInfo nullable = AccessTools.Constructor(typeof(int?), new[] { typeof(int) });
                FieldInfo hp = AccessTools.Field(typeof(MainManager.BattleData), nameof(MainManager.BattleData.hp));
                // Heal(ref x, N): N right before the Nullable, or one branch of a "? :" just above it.
                var heals = new List<int>();
                var healCalls = new List<int>();
                for (int h = 2; h < code.Count; h++)
                {
                    if (!code[h].Calls(heal) || code[h - 1].opcode != OpCodes.Newobj || !Equals(code[h - 1].operand, nullable))
                    {
                        continue;
                    }
                    int before = heals.Count;
                    if (Constant(code[h - 2]) != null)
                    {
                        heals.Add(h - 2);
                    }
                    for (int k = h - 3; k > Math.Max(h - 14, 1); k--)
                    {
                        if (IsBranch(code[k]) && Constant(code[k - 1]) != null)
                        {
                            heals.Add(k - 1);
                            break;
                        }
                    }
                    if (heals.Count > before)
                    {
                        healCalls.Add(h);
                    }
                }
                List<int> sets = Enumerable.Range(1, code.Count - 1)
                    .Where(i => code[i].opcode == OpCodes.Stfld && Equals(code[i].operand, hp)
                        && (code[i - 1].LoadsConstant(7) || code[i - 1].LoadsConstant(10)))
                    .ToList();
                MethodInfo counter = AccessTools.Method(typeof(BattleControl), "ShowDamageCounter",
                    new[] { typeof(int), typeof(int), typeof(Vector3), typeof(Vector3) });
                List<int> shown = Enumerable.Range(1, code.Count - 1)
                    .Where(i => code[i].LoadsConstant(7) && code[i - 1].LoadsConstant(1)
                        && code.Skip(i + 1).Take(24).Any(c => c.Calls(counter)))
                    .ToList();
                if (heals.Count != 19 || healCalls.Count != 18 || sets.Count != 3 || shown.Count != 2)
                {
                    throw new InvalidOperationException($"expected 19 heal amounts in 18 heals, 3 HP sets and 2 "
                        + $"counters, found {heals.Count} in {healCalls.Count}, {sets.Count} and {shown.Count}");
                }
                var edits = heals.Select(i => new KeyValuePair<int, Action<int>>(i,
                        at => code.Insert(at + 1, Call(nameof(HealLiteral)))))
                    .Concat(healCalls.Select(i => new KeyValuePair<int, Action<int>>(i, at =>
                    {
                        code[at].opcode = OpCodes.Call;
                        code[at].operand = AccessTools.Method(typeof(ScriptNumbers), nameof(ScaledHeal));
                    })))
                    .Concat(sets.Select(i => new KeyValuePair<int, Action<int>>(i, at =>
                    {
                        code[at].opcode = OpCodes.Call;
                        code[at].operand = AccessTools.Method(typeof(ScriptNumbers), nameof(SetHp));
                    })))
                    .Concat(shown.Select(i => new KeyValuePair<int, Action<int>>(i, at => code.InsertRange(at + 1,
                        new[] { new CodeInstruction(OpCodes.Ldarg_0), Call(nameof(ByActor)) }))));
                foreach (KeyValuePair<int, Action<int>> e in edits.OrderByDescending(e => e.Key))
                {
                    e.Value(e.Key);
                }
                log.LogInfo("[scale] installed in BattleControl.DoAction (heal amounts 19 of 19 in 18 heals, HP sets 3 "
                    + "of 3, counters 2 of 2)");
                return code;
            }

            [HarmonyPatch(typeof(BattleControl), nameof(BattleControl.StartBattle), MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileStart(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, EditStart, "scale");

            // hp and maxhp += or -= N: ldflda, dup, ldind.i4, N, add or sub, stind.i4.
            private static IEnumerable<CodeInstruction> EditStart(List<CodeInstruction> code)
            {
                FieldInfo hp = AccessTools.Field(typeof(MainManager.BattleData), nameof(MainManager.BattleData.hp));
                FieldInfo maxhp = AccessTools.Field(typeof(MainManager.BattleData),
                    nameof(MainManager.BattleData.maxhp));
                List<int> sites = Enumerable.Range(3, Math.Max(0, code.Count - 5))
                    .Where(i => Constant(code[i]) != null && code[i - 1].opcode == OpCodes.Ldind_I4
                        && code[i - 2].opcode == OpCodes.Dup && code[i - 3].opcode == OpCodes.Ldflda
                        && (Equals(code[i - 3].operand, hp) || Equals(code[i - 3].operand, maxhp))
                        && (code[i + 1].opcode == OpCodes.Add || code[i + 1].opcode == OpCodes.Sub)
                        && code[i + 2].opcode == OpCodes.Stind_I4)
                    .ToList();
                if (sites.Count != 8)
                {
                    throw new InvalidOperationException($"expected the start's 8 HP adjustments, found {sites.Count}");
                }
                foreach (int i in sites.OrderByDescending(i => i))
                {
                    code.InsertRange(i + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0), Call(nameof(AtBattleStart)) });
                }
                log.LogInfo("[scale] installed in BattleControl.StartBattle (HP adjustments 8 of 8)");
                return code;
            }

            [HarmonyPatch(typeof(BattleControl), "HoloVi", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileHoloVi(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => EditThresholds(code, "HoloVi", 2), "scale");

            [HarmonyPatch(typeof(BattleControl), "EnemyHeavyThrow", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileHeavyThrow(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, code => EditThresholds(code, "EnemyHeavyThrow", 1), "scale");

            // enemydata[currentEnemy].hp against N (HoloVi's 10, EnemyHeavyThrow's 20), and HoloVi's hp -= 3.
            private static IEnumerable<CodeInstruction> EditThresholds(List<CodeInstruction> code, string where,
                int expected)
            {
                FieldInfo hp = AccessTools.Field(typeof(MainManager.BattleData), nameof(MainManager.BattleData.hp));
                List<int> sites = Enumerable.Range(3, Math.Max(0, code.Count - 4))
                    .Where(i => Constant(code[i]) != null
                        && ((code[i - 1].opcode == OpCodes.Ldfld && Equals(code[i - 1].operand, hp))
                            || (code[i - 1].opcode == OpCodes.Ldind_I4 && code[i + 1].opcode == OpCodes.Sub
                                && code[i - 3].opcode == OpCodes.Ldflda && Equals(code[i - 3].operand, hp))))
                    .ToList();
                if (sites.Count != expected)
                {
                    throw new InvalidOperationException($"expected {where}'s {expected} HP numbers, found {sites.Count}");
                }
                foreach (int i in sites.OrderByDescending(i => i))
                {
                    code.Insert(i + 1, Call(nameof(ForCurrentEnemy)));
                }
                log.LogInfo($"[scale] installed in BattleControl.{where} (HP numbers {expected} of {expected})");
                return code;
            }
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
            if (int.TryParse(MainManager.enemydata[id, 25], out int swapped) && swapped >= 0
                && swapped < MainManager.enemydata.GetLength(0))
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

        private static float Ratio(int home, int target) => (target + Base) / (home + Base);

        // A boss whose script ends the fight at 10 HP (SurviveWith10, the game's own number): the Everlasting King from
        // its data, the Beast from its scene. Only the HP above the 10 scales, so the fight before the script keeps its
        // share and the scripted end is the game's.
        private const int ScriptedEnd = 10;
        private const int TheBeast = 69;

        private static int ScaledHp(int hp, float ratio, bool scriptedEnd) => scriptedEnd && hp > ScriptedEnd
            ? ScriptedEnd + Mathf.Max(1, Mathf.RoundToInt((hp - ScriptedEnd) * ratio))
            : Mathf.Max(1, Mathf.RoundToInt(hp * ratio));

        // The bestiary page reads the raw enemy table (PauseMenu's own copy), not GetEnemyData: the shown enemy's row
        // is swapped for a scaled one while the page's text is built, then put back. The field holds other text
        // elsewhere.
        private static readonly System.Reflection.FieldInfo BestiaryRows = AccessTools.Field(typeof(PauseMenu),
            "enemydata");
        private static int swappedRow = -1;
        private static string originalRow;

        private static class Bestiary
        {
            [HarmonyPatch(typeof(PauseMenu), "UpdateText")]
            [HarmonyPrefix]
            private static void BeforeBestiary(PauseMenu __instance)
            {
                swappedRow = -1;
                if (randomizerOn == null || !randomizerOn() || MainManager.listvar == null
                    || MainManager.instance == null)
                {
                    return;
                }
                int option = MainManager.instance.option;
                if (!(BestiaryRows.GetValue(__instance) is string[] rows) || option < 0
                    || option >= MainManager.listvar.Length)
                {
                    return;
                }
                int id = MainManager.listvar[option];
                int? target = TargetLevel();
                int? home = HomeLevel(id);
                if (id < 0 || id >= rows.Length || target == null || home == null || target == home)
                {
                    return;
                }
                string[] f = rows[id].Split(',');
                if (f.Length < 38 || !int.TryParse(f[1], out int hp) || !int.TryParse(f[36], out int hardHp)
                    || !int.TryParse(f[2], out int def))
                {
                    return;
                }
                float ratio = Ratio(home.Value, target.Value);
                bool scriptedEnd = id == TheBeast || (f.Length > 23 && f[23].Contains("SurviveWith10"));
                f[1] = ScaledHp(hp, ratio, scriptedEnd).ToString();
                f[36] = Mathf.RoundToInt(hardHp * ratio).ToString();
                if (def >= 0)
                {
                    f[2] = Mathf.Max(0, def + (target.Value - home.Value) / LevelsPerDefence).ToString();
                }
                swappedRow = id;
                originalRow = rows[id];
                rows[id] = string.Join(",", f);
            }

            [HarmonyPatch(typeof(PauseMenu), "UpdateText")]
            [HarmonyPostfix]
            private static void AfterBestiary(PauseMenu __instance)
            {
                if (swappedRow >= 0 && BestiaryRows.GetValue(__instance) is string[] rows && swappedRow < rows.Length)
                {
                    rows[swappedRow] = originalRow;
                }
                swappedRow = -1;
            }
        }

        // Each hit an enemy lands: its move's own damage scaled, before hardatk (Hard/Hardest) is added on top, so a
        // many-hit attack and a single big one shrink or grow alike. The game's floor of 1 per hit stays.
        private static class Damage
        {
            [HarmonyPatch(typeof(BattleControl), "CalculateBaseDamage")]
            [HarmonyPrefix]
            private static void BeforeBaseDamage(MainManager.BattleData? attacker, ref int basevalue)
            {
                if (basevalue <= 0 || attacker == null || attacker.Value.battleentity == null
                    || attacker.Value.battleentity.CompareTag("Player") || randomizerOn == null || !randomizerOn())
                {
                    return;
                }
                int? target = TargetLevel();
                int? home = HomeLevel(attacker.Value.animid);
                if (target == null || home == null || target == home)
                {
                    return;
                }
                basevalue = Mathf.Max(1, Mathf.RoundToInt(basevalue * Ratio(home.Value, target.Value)));
            }
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.GetEnemyData), typeof(int), typeof(bool), typeof(bool))]
        [HarmonyPostfix]
        private static void AfterGetEnemyData(int id, bool createentity, bool noexp,
            ref MainManager.BattleData __result)
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
            float ratio = Ratio(home.Value, target.Value);
            bool scriptedEnd = (id == TheBeast && MainManager.lastevent == 137 && MainManager.instance.inevent)
                || (__result.weakness != null && __result.weakness.Contains(BattleControl.AttackProperty.SurviveWith10));
            int hp = ScaledHp(__result.hp, ratio, scriptedEnd);
            // A defence of -1 means "shown as ?", left alone; otherwise never below 0.
            int def = __result.def < 0 ? __result.def : Mathf.Max(0, __result.def + diff / LevelsPerDefence);
            int exp = __result.exp;
            // EXP as the game gives it at the enemy's home level, so levelling keeps its pace. Left alone where the
            // game fixes it: fixed EXP, no EXP, the level cap, hologram fights.
            if (!noexp && !__result.fixedexp && MainManager.instance.partylevel < 27
                && !MainManager.instance.flags[GameFlags.NoExp]
                && !MainManager.instance.flags[162] && int.TryParse(MainManager.enemydata[__result.animid, 3],
                out int baseExp))
            {
                // animid is the row the game read (column 25 can point an id at another row).
                int asIf = Mathf.Clamp(MainManager.instance.partylevel - diff, 1, 27);
                exp = MainManager.GetEXP(baseExp, asIf, (MainManager.Enemies)__result.animid);
            }
            log.LogInfo(
                $"[scale] {(MainManager.Enemies)id} ({id}): home {home}, target {target}: hp {__result.hp} -> {hp}"
                + (scriptedEnd ? $" (only the HP above its scripted end at {ScriptedEnd} scaled)" : "") + ", "
                + $"hits x{ratio:0.00}, def {__result.def} -> {def}, exp {__result.exp} -> {exp}");
            __result.hp = hp;
            __result.maxhp = hp;
            __result.def = def;
            __result.exp = exp;
        }
    }
}
