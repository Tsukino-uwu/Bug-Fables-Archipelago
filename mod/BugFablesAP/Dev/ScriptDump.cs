using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only: dumps the item, flag, event, money and transfer command tokens of every map's dialogue lines.
    // Never the prose: the game's text stays out; only |command,args| tokens leave it.
    internal static class ScriptDump
    {
        private static readonly Regex Token = new Regex(@"\|([a-zA-Z]+)((?:,[^|]*)?)\|");
        private static readonly HashSet<string> ItemCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "giveitem", "additem", "additemtoss", "checkitem", "getitem", "removeitem", "pickitem", "createitem"
        };
        private static readonly HashSet<string> FlagCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flag", "regionalflag", "flagvar", "event", "discovery"
        };
        private static readonly HashSet<string> MoneyCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "checkmoney", "money", "setvar", "checkvar"
        };
        // Dialogue commands that send the party to another map.
        private static readonly HashSet<string> TransferCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "transfer", "warp", "loadmap"
        };

        // True once it has run, successfully or not.
        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.languageid < 0)
            {
                return false;
            }
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-scriptdump.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("map\tline\titem_commands\tflag_commands\tmoney_commands\ttransfer_commands");
            int maps = 0, missing = 0, lines = 0;
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                TextAsset asset = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid + "/Maps/" + map);
                if (asset == null)
                {
                    missing++;
                    continue;
                }
                maps++;
                string[] rows = asset.ToString().Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < rows.Length; i++)
                {
                    var items = new List<string>();
                    var flags = new List<string>();
                    var money = new List<string>();
                    var transfers = new List<string>();
                    foreach (Match m in Token.Matches(rows[i]))
                    {
                        string cmd = m.Groups[1].Value;
                        string tokenText = cmd.ToLowerInvariant() + m.Groups[2].Value;
                        if (ItemCommands.Contains(cmd))
                        {
                            items.Add(tokenText);
                        }
                        else if (FlagCommands.Contains(cmd))
                        {
                            flags.Add(tokenText);
                        }
                        else if (MoneyCommands.Contains(cmd))
                        {
                            money.Add(tokenText);
                        }
                        else if (TransferCommands.Contains(cmd))
                        {
                            transfers.Add(tokenText);
                        }
                    }
                    if (items.Count > 0 || money.Count > 0 || transfers.Count > 0 || flags.Exists(t => t.StartsWith("event,") || t.StartsWith("discovery,")))
                    {
                        lines++;
                        sb.Append(map).Append('\t').Append(i).Append('\t')
                          .Append(string.Join(" ", items.ToArray())).Append('\t')
                          .Append(string.Join(" ", flags.ToArray())).Append('\t')
                          .Append(string.Join(" ", money.ToArray())).Append('\t')
                          .Append(string.Join(" ", transfers.ToArray())).AppendLine();
                    }
                }
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {lines} item, money, transfer or event lines from {maps} maps ({missing} maps with no dialogue table) -> {outPath}");
            return true;
        }
    }
}
