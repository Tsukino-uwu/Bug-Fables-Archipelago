using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: one row per board quest with its name, its BoardData numbers (column 3 is the flag taking it sets)
    // and its QuestChecks row (the flags, or negative visited areas, that put it on the board). No descriptions.
    internal static class QuestDump
    {
        internal static void Run(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-questdump.tsv");
            string[,] data = MainManager.boardquestdata;
            int textColumns = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid + "/BoardQuests")
                .ToString().Split('\n')[0].Split('@').Length;
            string[] checks = Resources.Load<TextAsset>("Data/QuestChecks").ToString().Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine("id\tenum\tname\tboard_data (from column " + textColumns + ")\tquest_checks");
            for (int id = 0; id < data.GetLength(0); id++)
            {
                var numbers = new StringBuilder();
                for (int c = textColumns; c < data.GetLength(1); c++)
                {
                    numbers.Append(c).Append('=').Append((data[id, c] ?? "").Trim()).Append(' ');
                }
                string name = Enum.IsDefined(typeof(MainManager.BoardQuests), id) ? ((MainManager.BoardQuests)id).ToString() : "?";
                sb.Append(id).Append('\t').Append(name).Append('\t').Append((data[id, 0] ?? "").Trim()).Append('\t')
                    .Append(numbers.ToString().Trim()).Append('\t')
                    .Append(id < checks.Length ? checks[id].Trim() : "").AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {data.GetLength(0)} board quests ({textColumns} text columns) -> {outPath}");
        }
    }
}
