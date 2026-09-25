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
    // Dev-only measurement: for every map, read its entity table the way MapControl.CreateEntities does
    // (Resources "Data/EntityData/<map id>" plus "Data/EntityData/Names/<map id>names", MapControl.cs:1446) and
    // write out the fields logic needs: what each entity is, its item (for pickups), the flags it needs
    // (requires), the flags that hide it (limit), its data, its dialogue selectors, and its regional and
    // activation flags. The offsets are the parser's (MapControl.cs:1476-1640); every count slot is followed by
    // a fixed-size block, so the positions never move.
    //
    // A second file lists every item and medal id with its enum name and in-game name, to name the apworld's
    // items. Both go to the BepInEx folder, not the repo; facts taken from them are written into agent_docs.
    internal static class EntityDump
    {
        private const int RequiresCount = 38, LimitCount = 49, DataCount = 60, DialogueCount = 102;
        // vectordata: a count, then that many (x, y, z). Grass that drops an item picks one entry at random and drops
        // item x (NPCControl.cs:5976-5983), so this is the grass's item list.
        private const int VectorCount = 71;
        private const int RegionalFlag = 190, ActivationFlag = 194;
        // Which inside (a building's interior on the same map) the entity belongs to; -1 outdoors. An indoor pickup
        // is reached through that inside's door, whose own flags gate it (found 2026-09-24: a pickup the apworld had
        // outdoors was in a house that opens later).
        private const int InsideId = 178;
        // Where the entity starts (fields 6-8, MapControl.cs:1661) and its emoticonoffset's x (fields 175-177,
        // MapControl.cs:1629), which for a door is the jump on arrival (MainManager.TransferMap reads the door's
        // entity.emoticonoffset.x). Positions pair a door with its way back (dev-scripts/door-graph.py).
        private const int Position = 6, EmoticonOffset = 175;

        // Returns true once it has run (successfully or not), so the caller stops asking.
        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.languageid < 0 || MainManager.itemdata == null || MainManager.badgedata == null)
            {
                return false;
            }
            WriteEntities(log);
            WriteNames(log);
            return true;
        }

        private static void WriteEntities(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-entitydump.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("map\tindex\tname\tentitytype\tobjecttype\tinteract\tanimid\teventid\trequires\tlimit\tdata\tdialogues\tregionalflag\tactivationflag\tinsideid\tvectordata\tposition\tjump");
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
                // The game stops one short: the last line is the empty one after the final newline.
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
                          .Append(f[EmoticonOffset].Trim()).AppendLine();
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

        // A count slot, then that many values right after it (in groups of `width`), joined by spaces; groups
        // joined by ':'. Empty when the count is 0.
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
