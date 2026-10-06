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
                string name = Enum.IsDefined(typeof(MainManager.BoardQuests), id) ? ((MainManager.BoardQuests)id)
                    .ToString() : "?";
                sb.Append(id).Append('\t').Append(name).Append('\t').Append((data[id, 0] ?? "").Trim()).Append('\t')
                    .Append(numbers.ToString().Trim()).Append('\t')
                    .Append(id < checks.Length ? checks[id].Trim() : "").AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {data.GetLength(0)} board quests ({textColumns} text columns) -> {outPath}");
            RunPrizes(log);
        }

        // The Termacade's prize stand (Data/Termacade, as Event121 reads it): kind, item, price, once only, its flag.
        private static void RunPrizes(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-termacade.tsv");
            int[,] prizes = MainManager.termacadeprize;
            var sb = new StringBuilder();
            sb.AppendLine("id\tkind\titem\tname\tprice\tonce\tflag");
            for (int id = 0; id < prizes.GetLength(0); id++)
            {
                int kind = prizes[id, 0], item = prizes[id, 1];
                string name = kind < 2 ? MainManager.itemdata[0, item, 0] : MainManager.badgedata[item, 0];
                sb.Append(id).Append('\t').Append(kind).Append('\t').Append(item).Append('\t').Append(name).Append('\t')
                    .Append(prizes[id, 2]).Append('\t').Append(prizes.GetLength(1) > 3 ? prizes[id, 3] : 0).Append('\t')
                    .Append(prizes.GetLength(1) > 4 ? prizes[id, 4] : -1).AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {prizes.GetLength(0)} Termacade prizes -> {outPath}");
        }
    }
}
