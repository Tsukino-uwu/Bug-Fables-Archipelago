using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // A fourth main-menu entry, "Archipelago: On/Off", that switches between the randomizer's saves and the
    // normal ones (SaveRedirect). Measured 2026-09-24 (agent_docs/MEASURED.md, "The main menu"): the menu is
    // three lines built by StartMenu.SetMenuText, navigation wraps on maxoptions, and Update's confirm branch
    // only acts on options 0-2. So this adds the fourth line after SetMenuText and handles option 3 itself.
    internal static class MenuToggle
    {
        private const int Option = 3;
        private static ManualLogSource log;
        private static ConfigEntry<bool> setting;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid, ConfigEntry<bool> randomizerEnabled)
        {
            log = logger;
            setting = randomizerEnabled;
            harmony = new Harmony(guid + ".menu." + DateTime.UtcNow.Ticks);
            harmony.Patch(AccessTools.Method(typeof(StartMenu), "SetMenuText"),
                postfix: new HarmonyMethod(typeof(MenuToggle), nameof(AfterSetMenuText)));
            harmony.Patch(AccessTools.Method(typeof(StartMenu), "Update"),
                prefix: new HarmonyMethod(typeof(MenuToggle), nameof(BeforeUpdate)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static string Label => "Archipelago: " + (SaveRedirect.On ? "On" : "Off");

        private static void AfterSetMenuText(StartMenu __instance, Transform ___menu1)
        {
            try
            {
                Transform[] selections = __instance.selections;
                if (selections == null || selections.Length != Option)
                {
                    log.LogWarning($"[menu] expected {Option} main-menu entries, found {selections?.Length ?? 0}; no toggle added");
                    return;
                }
                Array.Resize(ref selections, Option + 1);
                __instance.selections = selections;
                Transform line = new GameObject("option" + Option).transform;
                line.parent = ___menu1;
                line.localPosition = new Vector3(0f, -0.5f - Option, 0f);
                line.localEulerAngles = Vector3.zero;
                selections[Option] = line;
                MainManager.instance.StartCoroutine(MainManager.SetText("|center|" + Label, new Vector3(0f, 0f, 10f), line));
                MainManager.instance.maxoptions = selections.Length;
            }
            catch (Exception e)
            {
                log.LogError("[menu] adding the toggle failed: " + e);
            }
        }

        private static void BeforeUpdate(StartMenu __instance, int ___menuid, float ___cd, bool ___canselect)
        {
            try
            {
                if (___menuid != 1 || ___cd > 0f || !___canselect || MainManager.pausemenu != null)
                {
                    return;
                }
                if (MainManager.instance.option != Option || !MainManager.GetKey(4, hold: false))
                {
                    return;
                }
                SaveRedirect.On = !SaveRedirect.On;
                setting.Value = SaveRedirect.On;
                // Show the other mode's saves: the file select reads slot summaries through ReadFile.
                AccessTools.Method(typeof(StartMenu), "ReloadData").Invoke(__instance, null);
                Transform line = __instance.selections.Length > Option ? __instance.selections[Option] : null;
                if (line != null)
                {
                    MainManager.DestroyText(line);
                    MainManager.instance.StartCoroutine(MainManager.SetText("|center|" + Label, new Vector3(0f, 0f, 10f), line));
                }
                log.LogInfo($"[menu] Archipelago mode {(SaveRedirect.On ? "ON: saves in the archipelago folder" : "off: normal saves")}");
            }
            catch (Exception e)
            {
                log.LogError("[menu] toggling failed: " + e);
            }
        }
    }
}
