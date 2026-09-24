using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only measurement: which flagvar and flagstring slots the game's text uses. Both arrays are saved
    // (MainManager.SaveFile), and one unused slot of each can hold the received-item count and the seed's name
    // without a new save format. The code's own uses are read from the decompiled source; this covers the other
    // half, the dialogue scripts, which address slots by number.
    //
    // Scans every TextAsset the game ships under Resources, in every language. Writes only command tokens that
    // name a slot, never prose, to the BepInEx folder (not the repo), with how often and where each appears.
    internal static class VarDump
    {
        private static readonly Regex Token = new Regex(@"\|([a-zA-Z]+)((?:,[^|]*)?)\|");
        // Commands that take a flagvar or flagstring slot as an argument (MainManager.SetText's switch), plus any
        // token with a "var,N" argument (giveitem,1,var,0 and the like).
        private static readonly HashSet<string> SlotCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "setvar", "addvar", "copyvar", "checkvar", "flagvar", "flagvalue", "var", "string", "sstring", "clonestring",
            "anstring", "optiontovar", "numberprompt", "letterprompt", "itemname", "setprize", "addprize", "define",
        };

        internal static void Run(ManualLogSource log)
        {
            string outPath = Path.Combine(Paths.BepInExRootPath, "bugfablesap-vardump.tsv");
            var seen = new Dictionary<string, (int count, string where)>();
            TextAsset[] assets = Resources.LoadAll<TextAsset>("");
            foreach (TextAsset asset in assets)
            {
                string text;
                try
                {
                    text = asset.text;
                }
                catch
                {
                    continue;
                }
                if (string.IsNullOrEmpty(text) || text.IndexOf('|') < 0)
                {
                    continue;
                }
                foreach (Match m in Token.Matches(text))
                {
                    string cmd = m.Groups[1].Value.ToLowerInvariant();
                    string args = m.Groups[2].Value;
                    if (!SlotCommands.Contains(cmd) && args.IndexOf("var", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    string token = cmd + args;
                    seen[token] = seen.TryGetValue(token, out var entry) ? (entry.count + 1, entry.where) : (1, asset.name);
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine("token\tcount\tfirst_asset");
            foreach (KeyValuePair<string, (int count, string where)> entry in seen.OrderBy(e => e.Key))
            {
                sb.Append(entry.Key).Append('\t').Append(entry.Value.count).Append('\t').Append(entry.Value.where).AppendLine();
            }
            File.WriteAllText(outPath, sb.ToString());
            log.LogInfo($"[dump] {seen.Count} distinct slot tokens from {assets.Length} text assets -> {outPath}");
            log.LogInfo("[dump] prizeflags (flagvar slots) = " + string.Join(",", MainManager.instance.prizeflags.Select(p => p.ToString()).ToArray()));
        }
    }
}
