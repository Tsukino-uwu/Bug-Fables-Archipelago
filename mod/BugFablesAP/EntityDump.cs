using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: dumps every map's entity table (fields the logic needs) and every item and medal name to the BepInEx folder.
    internal static class EntityDump
    {
        private const int RequiresCount = 38, LimitCount = 49, DataCount = 60, DialogueCount = 102;
        // vectordata: a count, then (x, y, z) each; for item grass, x is the item list.
        private const int VectorCount = 71;
        private const int RegionalFlag = 190, ActivationFlag = 194;
        // The inside (a building's interior on the same map) the entity belongs to; -1 outdoors.
        private const int InsideId = 178;
        // Fields 175-177: emoticonoffset, whose x on a door is the jump on arrival.
        private const int Position = 6, EmoticonOffset = 175;
        // An enemy's encounter: a count, then up to four enemy ids.
        private const int BattleIds = 166;

        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.languageid < 0 || MainManager.itemdata == null || MainManager.badgedata == null)
            {
                return false;
            }
            WriteEntities(log);
            WriteNames(log);
            WriteEnemies(log);
            return true;
        }

        // The enemy table's columns the enemy shuffle needs: stats, start position, can't fall, event on death.
        private static void WriteEnemies(ManualLogSource log)
        {
            if (MainManager.enemydata == null)
            {
                log.LogWarning("[dump] no enemy table loaded yet");
                return;
            }
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-enemies.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("id\tenum\tname\thp\tdef\texp\tposition\tcantfall\teventondeath\tswapid");
            int rows = MainManager.enemydata.GetLength(0);
            for (int id = 0; id < rows; id++)
            {
                string name = MainManager.enemynames != null && id < MainManager.enemynames.Length ? MainManager.enemynames[id] : "";
                sb.Append(id).Append('\t').Append((MainManager.Enemies)id).Append('\t').Append(name).Append('\t')
                  .Append(MainManager.enemydata[id, 1]).Append('\t').Append(MainManager.enemydata[id, 2]).Append('\t')
                  .Append(MainManager.enemydata[id, 3]).Append('\t').Append(MainManager.enemydata[id, 19]).Append('\t')
                  .Append(MainManager.enemydata[id, 29]).Append('\t').Append(MainManager.enemydata[id, 26]).Append('\t')
                  .Append(MainManager.enemydata[id, 25]).AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {rows} enemies -> {outPath}");
        }

        private static void WriteEntities(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-entitydump.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("map\tindex\tname\tentitytype\tobjecttype\tinteract\tanimid\teventid\trequires\tlimit\tdata\tdialogues\tregionalflag\tactivationflag\tinsideid\tvectordata\tposition\tjump\tbattleids");
            int maps = 0, rows = 0, bad = 0;
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                int id = (int)map;
                TextAsset data = Resources.Load<TextAsset>("Data/EntityData/" + id);
                TextAsset names = Resources.Load<TextAsset>("Data/EntityData/Names/" + id + "names");
                if (data == null || names == null)
                {
                    continue;
                }
                maps++;
                string[] lines = data.ToString().Split('\n');
                string[] nameLines = names.ToString().Split('\n');
                // The last line is the empty one after the final newline.
                for (int i = 0; i < lines.Length - 1 && i < nameLines.Length; i++)
                {
                    try
                    {
                        string[] f = lines[i].Split('}');
                        sb.Append(map).Append('\t').Append(i).Append('\t').Append(nameLines[i].Trim()).Append('\t')
                          .Append(f[0]).Append('\t').Append(f[1]).Append('\t').Append(f[4]).Append('\t')
                          .Append(f[9]).Append('\t').Append(f[37]).Append('\t')
                          .Append(List(f, RequiresCount, 1)).Append('\t')
                          .Append(List(f, LimitCount, 1)).Append('\t')
                          .Append(List(f, DataCount, 1)).Append('\t')
                          .Append(List(f, DialogueCount, 3)).Append('\t')
                          .Append(f[RegionalFlag]).Append('\t').Append(f[ActivationFlag].Trim()).Append('\t')
                          .Append(f[InsideId].Trim()).Append('\t')
                          .Append(List(f, VectorCount, 3)).Append('\t')
                          .Append(f[Position].Trim()).Append(':').Append(f[Position + 1].Trim()).Append(':').Append(f[Position + 2].Trim()).Append('\t')
                          .Append(f[EmoticonOffset].Trim()).Append('\t')
                          .Append(List(f, BattleIds, 1)).AppendLine();
                        rows++;
                    }
                    catch (Exception e)
                    {
                        bad++;
                        log.LogWarning($"[dump] {map} entity {i}: {e.GetType().Name}");
                    }
                }
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {rows} entities from {maps} maps ({bad} unreadable) -> {outPath}");
        }

        // A count slot, then that many values in groups of `width`: values joined by spaces, groups by ':'.
        private static string List(string[] f, int countAt, int width)
        {
            int n = int.Parse(f[countAt].Trim(), CultureInfo.InvariantCulture);
            var parts = new List<string>();
            for (int k = 0; k < n; k++)
            {
                var group = new string[width];
                for (int w = 0; w < width; w++)
                {
                    group[w] = f[countAt + 1 + k * width + w].Trim();
                }
                parts.Add(string.Join(":", group));
            }
            return string.Join(" ", parts.ToArray());
        }

        private static void WriteNames(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-names.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("kind\tid\tenum\tname");
            int items = 0, medals = 0;
            foreach (MainManager.Items item in Enum.GetValues(typeof(MainManager.Items)))
            {
                int id = (int)item;
                if (id < 0 || id >= MainManager.itemdata.GetLength(1))
                {
                    continue;
                }
                sb.Append("item\t").Append(id).Append('\t').Append(item).Append('\t')
                  .Append(MainManager.itemdata[0, id, 0]).AppendLine();
                items++;
            }
            foreach (MainManager.BadgeTypes badge in Enum.GetValues(typeof(MainManager.BadgeTypes)))
            {
                int id = (int)badge;
                if (id < 0 || id >= MainManager.badgedata.GetLength(0))
                {
                    continue;
                }
                sb.Append("medal\t").Append(id).Append('\t').Append(badge).Append('\t')
                  .Append(MainManager.badgedata[id, 0]).AppendLine();
                medals++;
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {items} items and {medals} medals named -> {outPath}");
        }
    }
}
