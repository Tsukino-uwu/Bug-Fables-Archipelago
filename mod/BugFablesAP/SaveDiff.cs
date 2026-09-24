using System;
using System.IO;
using BepInEx.Logging;
using InputIOManager;

namespace BugFablesAP
{
    // Dev-only measurement: what changed between two saves. It decodes both files with the game's own
    // InputIO.Encrypt (a symmetric XOR with the game's key, InputIO.cs:538), so the key never leaves the game,
    // and logs which lines differ. For a line of comma-separated true/false values, such as the flags, it logs
    // which positions flipped.
    //
    // Read-only: it only reads the two files, and never writes any save.
    internal static class SaveDiff
    {
        internal static void Run(ManualLogSource log, string before, string after)
        {
            try
            {
                if (!File.Exists(before) || !File.Exists(after))
                {
                    log.LogWarning($"[savediff] missing file: {before} exists={File.Exists(before)}, {after} exists={File.Exists(after)}");
                    return;
                }
                string[] a = InputIO.Encrypt(File.ReadAllText(before)).Split('\n');
                string[] b = InputIO.Encrypt(File.ReadAllText(after)).Split('\n');
                log.LogInfo($"[savediff] {before} ({a.Length} lines) -> {after} ({b.Length} lines)");
                int lines = Math.Max(a.Length, b.Length);
                for (int i = 0; i < lines; i++)
                {
                    string x = i < a.Length ? a[i].TrimEnd('\r') : "<none>";
                    string y = i < b.Length ? b[i].TrimEnd('\r') : "<none>";
                    if (x == y)
                    {
                        continue;
                    }
                    // A table of true/false rows separated by '@' (e.g. a 2D array): report row and column.
                    string[] xr = x.Split('@');
                    string[] yr = y.Split('@');
                    if (xr.Length > 1 && xr.Length == yr.Length)
                    {
                        var cells = new System.Text.StringBuilder();
                        bool table = true;
                        for (int r = 0; r < xr.Length && table; r++)
                        {
                            string[] xc = xr[r].Split(',');
                            string[] yc = yr[r].Split(',');
                            if (xc.Length != yc.Length || !IsBools(xc) || !IsBools(yc))
                            {
                                table = false;
                                break;
                            }
                            for (int c = 0; c < xc.Length; c++)
                            {
                                if (xc[c] != yc[c])
                                {
                                    cells.Append($" [{r},{c}] {xc[c]}->{yc[c]}");
                                }
                            }
                        }
                        if (table)
                        {
                            log.LogInfo($"[savediff] line {i} ({xr.Length} rows of true/false) flipped:{cells}");
                            continue;
                        }
                    }
                    string[] xs = x.Split(',');
                    string[] ys = y.Split(',');
                    if (xs.Length == ys.Length && xs.Length > 8 && IsBools(xs) && IsBools(ys))
                    {
                        var flips = new System.Text.StringBuilder();
                        for (int k = 0; k < xs.Length; k++)
                        {
                            if (xs[k] != ys[k])
                            {
                                flips.Append($" [{k}] {xs[k]}->{ys[k]}");
                            }
                        }
                        log.LogInfo($"[savediff] line {i} ({xs.Length} true/false values) flipped:{flips}");
                    }
                    else
                    {
                        // Only lengths and a short start; never the whole line.
                        log.LogInfo($"[savediff] line {i} changed: {Short(x)} -> {Short(y)}");
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError("[savediff] failed: " + e);
            }
        }

        private static bool IsBools(string[] parts)
        {
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t != "True" && t != "False" && t != "true" && t != "false" && t != "")
                {
                    return false;
                }
            }
            return true;
        }

        private static string Short(string s) => s.Length <= 60 ? $"'{s}'" : $"'{s.Substring(0, 60)}…' ({s.Length} chars)";
    }
}
