using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // MultiClient.Net caches each game's data package in <local app data>\Archipelago\Cache\datapackage\<game>\
    // <checksum>.json, both names as the server sent them: in 6.7.1 its safe-name function returns its input, and the
    // checksum it reads by is never cleaned, so a server could pick where the file is read or written. Both become a
    // plain file name here, as the library meant. Its cache class is internal, so the patches name it.
    internal static class CachePaths
    {
        internal const int MaxLength = 100;
        private const string Provider =
            "Archipelago.MultiClient.Net.DataPackage.FileSystemCheckSumDataPackageProvider, Archipelago.MultiClient.Net";
        // Path.GetInvalidFileNameChars depends on the platform; Windows' set is refused on every one.
        private const string NeverInName = "\"<>|:*?\\/";
        private static readonly char[] Invalid = Path.GetInvalidFileNameChars();
        private static readonly Regex DeviceName = new Regex(@"^(CON|PRN|AUX|NUL|COM\d|LPT\d)(\.|$)",
            RegexOptions.IgnoreCase);

        // Required: without both, a server could choose where a file is written, so the plugin stops.
        internal static void Enable(ManualLogSource log)
        {
            Hooks.Install(typeof(CachePaths), "cache", "a server could choose where its data package is cached",
                required: true);
            log.LogInfo("[cache] data package cache names made safe");
        }

        internal static string Safe(string name)
        {
            var kept = new StringBuilder();
            foreach (char c in name ?? "")
            {
                if (kept.Length == MaxLength)
                {
                    break;
                }
                if (!char.IsControl(c) && NeverInName.IndexOf(c) < 0 && Array.IndexOf(Invalid, c) < 0)
                {
                    kept.Append(c);
                }
            }
            // Windows drops trailing dots and spaces, a name of dots alone would climb out of the folder, and a
            // device name (CON, NUL) is no file at all.
            string safe = kept.ToString().TrimStart(' ').TrimEnd('.', ' ');
            return safe.Length == 0 ? "_" : DeviceName.IsMatch(safe) ? "_" + safe : safe;
        }

        [HarmonyPatch(Provider, "GetFileSystemSafeFileName")]
        [HarmonyPrefix]
        private static bool InsteadOfSafeName(string gameName, ref string __result)
        {
            __result = Safe(gameName);
            return false;
        }

        [HarmonyPatch(Provider, "TryGetDataPackage")]
        [HarmonyPrefix]
        private static void BeforeRead(ref string checksum)
        {
            checksum = Safe(checksum);
        }
    }
}
