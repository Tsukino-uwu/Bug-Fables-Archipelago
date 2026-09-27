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
            log.LogInfo($"[saves] redirect installed; Archipelago mod {(On ? "enabled" : "disabled")}");
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

        // Mirrors InputIO.Save's temp file, backup, move sequence, in the randomizer folder.
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
