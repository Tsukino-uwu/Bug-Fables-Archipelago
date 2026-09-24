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
    // Dev-only measurement: for every map, load its dialogue table the way the game does
    // (Resources "Data/Dialogues<lang>/Maps/<map>", MainManager.cs:2981). For each line that carries an item
    // command or starts an event, write out only the command tokens: the item commands plus the flag and
    // event commands on the same line.
    //
    // Never the prose: this is the game's text, so only the |command,args| tokens leave it. The output goes to
    // the BepInEx folder, not the repo; facts taken from it are written into agent_docs by hand.
    internal static class ScriptDump
    {
        private static readonly Regex Token = new Regex(@"\|([a-zA-Z]+)((?:,[^|]*)?)\|");
        private static readonly HashSet<string> ItemCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "giveitem", "additem", "additemtoss", "checkitem", "getitem", "removeitem", "pickitem", "createitem"
        };
        private static readonly HashSet<string> FlagCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "flag", "regionalflag", "flagvar", "event"
        };

        // Returns true once it has run (successfully or not), so the caller stops asking.
        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.languageid < 0)
            {
                return false;
            }
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-scriptdump.tsv");
            var sb = new StringBuilder();
            sb.AppendLine("map\tline\titem_commands\tflag_commands");
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
                    }
                    // Lines that start an event are kept too: they're where story steps begin.
                    if (items.Count > 0 || flags.Exists(t => t.StartsWith("event,")))
                    {
                        lines++;
                        sb.Append(map).Append('\t').Append(i).Append('\t')
                          .Append(string.Join(" ", items.ToArray())).Append('\t')
                          .Append(string.Join(" ", flags.ToArray())).AppendLine();
                    }
                }
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {lines} item or event lines from {maps} maps ({missing} maps with no dialogue table) -> {outPath}");
            return true;
        }
    }
}
