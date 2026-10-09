using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Doors a seed adds or re-points (door_rows): written into a map's entity rows before CreateEntities parses them, so
    // the game builds and runs them like its own. A copy is appended; a re-point is rewritten in place, since scenes
    // find entities by their row.
    internal static class DoorRows
    {
        internal sealed class Row
        {
            internal string Map;
            internal string Door;
            // Null for a re-point of the map's own door.
            internal string CopyMap;
            internal string CopyEntity;
            internal Vector3? At;
            internal int[] Data;
            internal Vector3[] Vectors;
            internal float? Jump;
        }

        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        // The copies built in this CreateEntities' rows pass, their names added in its names pass, in the same order.
        private static readonly List<string> pendingNames = new List<string>();

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(Create), "doorrows",
                "doors the seed adds are missing, and doors it re-points lead where the game has them");
        }

        private static List<Row> Rows(string map) =>
            (seed?.Invoke()?.DoorRows ?? new List<Row>()).Where(r => r.Map == map).ToList();

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        private static class Create
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "doorrows");

            // After the rows' Split (0) and the names' Split (1), as DayNight's copies are added.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo split = AccessTools.Method(typeof(string), nameof(string.Split), new[] { typeof(char[]) });
                MethodInfo apply = AccessTools.Method(typeof(DoorRows), nameof(Apply))
                    ?? throw new MissingMethodException(nameof(DoorRows), nameof(Apply));
                int[] at = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(split)).Take(2).ToArray();
                if (at.Length != 2)
                {
                    log.LogError($"[doorrows] NOT installed in MapControl.CreateEntities: expected two Splits, found {at.Length}");
                    return code;
                }
                for (int k = 1; k >= 0; k--)
                {
                    code.InsertRange(at[k] + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldc_I4, k),
                        new CodeInstruction(OpCodes.Call, apply),
                    });
                }
                log.LogInfo("[doorrows] installed in MapControl.CreateEntities");
                return code;
            }
        }

        // names: 0 the rows, 1 their names.
        private static string[] Apply(string[] lines, MapControl map, int names)
        {
            if (names == 0)
            {
                pendingNames.Clear();
            }
            if (randomizerOn == null || !randomizerOn() || map == null || lines == null)
            {
                return lines;
            }
            int table = map.readdatafromothermap == MainManager.Maps.TestRoom
                ? Convert.ToInt32(map.name) : (int)map.readdatafromothermap;
            string here = ((MainManager.Maps)table).ToString();
            List<Row> rows = Rows(here);
            if (rows.Count == 0)
            {
                return lines;
            }
            List<string> result = lines.ToList();
            if (names == 1)
            {
                foreach (string name in pendingNames)
                {
                    result.Insert(Math.Max(0, result.Count - 1), name);
                }
                pendingNames.Clear();
                return result.ToArray();
            }
            string[] ownNames = Resources.Load<TextAsset>("Data/EntityData/Names/" + table + "names")?.ToString()
                .Split('\n') ?? new string[0];
            foreach (Row r in rows)
            {
                if (r.CopyMap == null)
                {
                    int[] found = Enumerable.Range(0, ownNames.Length).Where(i => ownNames[i].Trim() == r.Door).ToArray();
                    string rewritten = found.Length == 1 && found[0] < result.Count ? Rewrite(result[found[0]], r) : null;
                    if (rewritten == null)
                    {
                        log.LogWarning($"[doorrows] {here}: {r.Door} NOT re-pointed ({found.Length} rows of that name)");
                        continue;
                    }
                    result[found[0]] = rewritten;
                    log.LogInfo($"[doorrows] {here}: {r.Door} now leads to {(MainManager.Maps)r.Data[0]} (appear {r.Vectors[1]}, walk to {r.Vectors[2]})");
                    continue;
                }
                int from = (int)(MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), r.CopyMap);
                string[] sourceNames = Resources.Load<TextAsset>("Data/EntityData/Names/" + from + "names")?.ToString()
                    .Split('\n');
                string[] sourceRows = Resources.Load<TextAsset>("Data/EntityData/" + from)?.ToString().Split('\n');
                int row = sourceNames == null ? -1 : Array.FindIndex(sourceNames, n => n.Trim() == r.CopyEntity);
                string copy = row < 0 || sourceRows == null || row >= sourceRows.Length ? null
                    : Rewrite(sourceRows[row], r);
                if (copy == null || ownNames.Any(n => n.Trim() == r.Door))
                {
                    log.LogWarning($"[doorrows] {here}: {r.Door} NOT added (copy of {r.CopyMap}/{r.CopyEntity} unreadable, or the name is taken)");
                    continue;
                }
                result.Insert(Math.Max(0, result.Count - 1), copy);
                pendingNames.Add(r.Door);
                log.LogInfo($"[doorrows] {here}: {r.CopyMap}/{r.CopyEntity} copied as {r.Door} at {r.At}, to {(MainManager.Maps)r.Data[0]} (walk in {r.Vectors[0]}, appear {r.Vectors[1]}, walk to {r.Vectors[2]})");
            }
            return result.ToArray();
        }

        // A door row with this seed's place, target and spots; a copy loses its requires and limit (always there). Null
        // when the row isn't a door or the seed's values don't fit: when data has more than one entry, TransferMap reads
        // data 1-3 and vectordata 3-6.
        private static string Rewrite(string row, Row r)
        {
            string[] f = row.Split('}');
            if (f.Length <= 175 || f[1].Trim() != "DoorOtherMap" || r.Data == null || r.Vectors == null
                || r.Data.Length < 1 || r.Data.Length > 10 || r.Vectors.Length < 3 || r.Vectors.Length > 10
                || (r.Data.Length > 1 && (r.Data.Length < 4 || r.Vectors.Length < 7))
                || !Enum.IsDefined(typeof(MainManager.Maps), r.Data[0]))
            {
                return null;
            }
            if (r.At.HasValue)
            {
                f[6] = Num(r.At.Value.x);
                f[7] = Num(r.At.Value.y);
                f[8] = Num(r.At.Value.z);
            }
            if (r.CopyMap != null)
            {
                f[38] = "0";
                f[49] = "0";
            }
            f[60] = r.Data.Length.ToString(CultureInfo.InvariantCulture);
            for (int k = 0; k < r.Data.Length; k++)
            {
                f[61 + k] = r.Data[k].ToString(CultureInfo.InvariantCulture);
            }
            f[71] = r.Vectors.Length.ToString(CultureInfo.InvariantCulture);
            for (int k = 0; k < r.Vectors.Length; k++)
            {
                f[72 + k * 3] = Num(r.Vectors[k].x);
                f[73 + k * 3] = Num(r.Vectors[k].y);
                f[74 + k * 3] = Num(r.Vectors[k].z);
            }
            if (r.Jump.HasValue)
            {
                f[175] = Num(r.Jump.Value);
            }
            return string.Join("}", f);
        }

        private static string Num(float v) => v.ToString(CultureInfo.InvariantCulture);

        // The arrival spots of the seed's door on fromMap leading to target (by name when given), for DoorInto; null
        // when the seed has none.
        internal static Vector3[] Into(MainManager.Maps target, string fromMap, string door)
        {
            if (randomizerOn == null || !randomizerOn() || fromMap == null)
            {
                return null;
            }
            Row r = (seed?.Invoke()?.DoorRows ?? new List<Row>()).FirstOrDefault(x =>
                string.Equals(x.Map, fromMap, StringComparison.OrdinalIgnoreCase) && x.Data != null && x.Data.Length > 0
                && x.Data[0] == (int)target && (door == null || x.Door == door));
            return r?.Vectors;
        }

        // Whether the seed sends that map's door of this name elsewhere.
        internal static bool Repointed(string map, string door) =>
            randomizerOn != null && randomizerOn() && (seed?.Invoke()?.DoorRows ?? new List<Row>()).Any(x =>
                x.CopyMap == null && string.Equals(x.Map, map, StringComparison.OrdinalIgnoreCase) && x.Door == door);
    }
}
