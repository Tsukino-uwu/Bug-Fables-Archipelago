using System;
using System.IO;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;
using InputIOManager;
using UnityEngine;

namespace BugFablesAP
{
    // Keeps randomizer saves apart from normal ones (CLAUDE.md, "Randomizer saves are separate files").
    // With Archipelago mode on, every save file the game touches lives in the "archipelago" folder, never
    // in the game folder where normal saves (and Steam Cloud's copies) are.
    //
    // Measured 2026-09-24 (agent_docs/MEASURED.md, "Save files"): every access to a save goes through
    // InputIO. ReadFile, CreateFile and DeleteFile take the path; SaveExists(id) builds it; Save() calls File.*
    // on its own. So those five are patched, and nothing else needs to be. With the mode off, all five behave
    // exactly as the game wrote them.
    internal static class SaveRedirect
    {
        internal const string Folder = "archipelago";
        // save0.dat, save0backup.dat, save0t.dat, the only names the game gives its save files.
        private static readonly Regex SaveName = new Regex(@"^save\d+(backup|t)?\.dat$", RegexOptions.IgnoreCase);

        private static ManualLogSource log;
        private static Harmony harmony;

        internal static bool On { get; set; }

        internal static void Enable(ManualLogSource logger, string guid)
        {
            log = logger;
            harmony = new Harmony(guid + ".saves." + DateTime.UtcNow.Ticks);
            Type io = typeof(InputIO);
            PatchPath(io, "ReadFile", nameof(RewriteFirstPath));
            PatchPath(io, "DeleteFile", nameof(RewriteFirstPath));
            PatchPath(io, "CreateFile", nameof(RewriteFirstPath));
            harmony.Patch(AccessTools.Method(io, "SaveExists"), prefix: new HarmonyMethod(typeof(SaveRedirect), nameof(SaveExistsPrefix)));
            harmony.Patch(AccessTools.Method(io, "Save"), prefix: new HarmonyMethod(typeof(SaveRedirect), nameof(SavePrefix)));
            log.LogInfo($"[saves] redirect installed; Archipelago mode is {(On ? "ON" : "off")}");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void PatchPath(Type type, string method, string prefix)
        {
            var target = AccessTools.Method(type, method);
            if (target == null)
            {
                throw new MissingMethodException(type.Name, method);
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(SaveRedirect), prefix));
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

        // ReadFile(string path), DeleteFile(string path), CreateFile(string path, string content).
        private static void RewriteFirstPath(ref string path)
        {
            path = Redirect(path);
        }

        private static bool SaveExistsPrefix(int id, ref bool __result)
        {
            if (!On)
            {
                return true;
            }
            __result = File.Exists(Redirect("save" + id + ".dat"));
            return false;
        }

        // InputIO.Save writes its files itself. With the mode on, the same safe sequence is done here, in the
        // randomizer folder: write a temp file, keep the previous save as the backup, then move the temp file
        // into place. The content comes from the game's own serializer and encryption.
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
