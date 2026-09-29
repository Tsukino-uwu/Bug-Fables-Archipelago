using System;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using InputIOManager;
using UnityEngine;

namespace BugFablesAP
{
    // With the randomizer on, every save file lives in the "archipelago" folder, never beside normal saves.
    // Every save access goes through these five InputIO methods; Save() calls File.* itself.
    internal static class SaveRedirect
    {
        internal const string Folder = "archipelago";
        // The only names the game gives its save files.
        private static readonly Regex SaveName = new Regex(@"^save\d+(backup|t)?\.dat$", RegexOptions.IgnoreCase);

        private static ManualLogSource log;

        internal static bool On { get; set; }

        // Required: without every redirect a randomizer save could land beside the normal ones, so the plugin stops.
        internal static void Enable(ManualLogSource logger)
        {
            log = logger;
            Hooks.Install(typeof(SaveRedirect), "saves", "the plugin can't keep randomizer saves apart",
                required: true);
            log.LogInfo($"[saves] redirect installed; Archipelago mod {(On ? "enabled" : "disabled")}");
        }

        internal static string Redirect(string path)
        {
            if (!On || string.IsNullOrEmpty(path) || !SaveName.IsMatch(path))
            {
                return path;
            }
            Directory.CreateDirectory(Folder);
            return Path.Combine(Folder, path);
        }

        [HarmonyPatch(typeof(InputIO), nameof(InputIO.ReadFile))]
        [HarmonyPatch(typeof(InputIO), nameof(InputIO.DeleteFile))]
        [HarmonyPatch(typeof(InputIO), nameof(InputIO.CreateFile))]
        [HarmonyPrefix]
        private static void RewriteFirstPath(ref string path)
        {
            path = Redirect(path);
        }

        [HarmonyPatch(typeof(InputIO), nameof(InputIO.SaveExists))]
        [HarmonyPrefix]
        private static bool SaveExistsPrefix(int id, ref bool __result)
        {
            if (!On)
            {
                return true;
            }
            __result = File.Exists(Redirect("save" + id + ".dat"));
            return false;
        }

        // Mirrors InputIO.Save's temp file, backup, move sequence, in the randomizer folder.
        [HarmonyPatch(typeof(InputIO), nameof(InputIO.Save))]
        [HarmonyPrefix]
        private static bool SavePrefix(Vector3? savepos, ref bool __result)
        {
            if (!On)
            {
                return true;
            }
            int slot = MainManager.saveslot;
            string main = Redirect("save" + slot + ".dat");
            string backup = Redirect("save" + slot + "backup.dat");
            string temp = Redirect("save" + slot + "t.dat");
            try
            {
                File.WriteAllText(temp, InputIO.Encrypt(MainManager.SaveFile(savepos)));
                if (File.Exists(main))
                {
                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }
                    File.Move(main, backup);
                }
                File.Move(temp, main);
                log.LogInfo($"[saves] saved slot {slot} to {main}");
                __result = true;
            }
            catch (Exception e)
            {
                log.LogError($"[saves] saving slot {slot} to {main} failed: {e.Message}");
                __result = false;
            }
            return false;
        }
    }
}
