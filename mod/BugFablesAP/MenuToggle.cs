using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // A fourth main-menu entry, "Archipelago", that opens the Archipelago panel (ApMenu). Measured 2026-09-24
    // (agent_docs/MEASURED.md, "The main menu"): the menu is three lines built by StartMenu.SetMenuText, one unit
    // apart; navigation wraps on maxoptions; Update's confirm branch acts only on options 0-2; the cursor is
    // placed each frame at y = -option - 0.25. A fourth line one unit lower overlapped the credit line
    // (the user's screenshot, 2026-09-24), so the four lines are spaced by Spacing and the cursor follows.
    internal static class MenuToggle
    {
        private const int Option = 3;
        private const float Spacing = 0.8f;

        private static ManualLogSource log;
        private static ConfigEntry<bool> mode;
        private static ConfigEntry<string> server, slot, password;
        private static Action connect;
        private static Func<string> status;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid, ConfigEntry<bool> randomizerEnabled,
            ConfigEntry<string> serverEntry, ConfigEntry<string> slotEntry, ConfigEntry<string> passwordEntry,
            Action connectAction, Func<string> statusText)
        {
            log = logger;
            mode = randomizerEnabled;
            server = serverEntry;
            slot = slotEntry;
            password = passwordEntry;
            connect = connectAction;
            status = statusText;
            harmony = new Harmony(guid + ".menu." + DateTime.UtcNow.Ticks);
            harmony.Patch(AccessTools.Method(typeof(StartMenu), "SetMenuText"),
                prefix: new HarmonyMethod(typeof(MenuToggle), nameof(BeforeSetMenuText)),
                postfix: new HarmonyMethod(typeof(MenuToggle), nameof(AfterSetMenuText)));
            harmony.Patch(AccessTools.Method(typeof(StartMenu), "Update"),
                prefix: new HarmonyMethod(typeof(MenuToggle), nameof(BeforeUpdate)),
                postfix: new HarmonyMethod(typeof(MenuToggle), nameof(AfterUpdate)));
        }

        internal static void Disable()
        {
            ApMenu.Open?.CloseNow();
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static string Label => "Archipelago" + (mode.Value ? " (On)" : "");

        // The game calls SetMenuText again whenever it returns to the main menu, and its own loop indexes a
        // three-label array by selections.Length. With our fourth entry still in the list that read past the end
        // (IndexOutOfRangeException in SetMenuText, 2026-09-24). So hand the game back its three entries first.
        private static void BeforeSetMenuText(StartMenu __instance)
        {
            Transform[] selections = __instance.selections;
            if (selections != null && selections.Length > Option)
            {
                if (selections[Option] != null)
                {
                    UnityEngine.Object.Destroy(selections[Option].gameObject);
                }
                Array.Resize(ref selections, Option);
                __instance.selections = selections;
            }
        }

        private static void AfterSetMenuText(StartMenu __instance, Transform ___menu1)
        {
            try
            {
                Transform[] selections = __instance.selections;
                if (selections == null || selections.Length != Option)
                {
                    log.LogWarning($"[menu] expected {Option} main-menu entries, found {selections?.Length ?? 0}; no entry added");
                    return;
                }
                Array.Resize(ref selections, Option + 1);
                __instance.selections = selections;
                Transform line = new GameObject("option" + Option).transform;
                line.parent = ___menu1;
                line.localEulerAngles = Vector3.zero;
                selections[Option] = line;
                for (int i = 0; i < selections.Length; i++)
                {
                    selections[i].localPosition = new Vector3(0f, -0.5f - i * Spacing, 0f);
                }
                MainManager.instance.StartCoroutine(MainManager.SetText("|center|" + Label, new Vector3(0f, 0f, 10f), line));
                MainManager.instance.maxoptions = selections.Length;
            }
            catch (Exception e)
            {
                log.LogError("[menu] adding the entry failed: " + e);
            }
        }

        internal static void RefreshLabel(StartMenu menu)
        {
            Transform line = menu.selections != null && menu.selections.Length > Option ? menu.selections[Option] : null;
            if (line != null)
            {
                MainManager.DestroyText(line);
                MainManager.instance.StartCoroutine(MainManager.SetText("|center|" + Label, new Vector3(0f, 0f, 10f), line));
            }
        }

        // Called by the panel's "Archipelago mode" row.
        internal static void SetMode(StartMenu menu, bool on)
        {
            SaveRedirect.On = on;
            mode.Value = on;
            // Show the other mode's saves: the file select reads slot summaries through ReadFile.
            AccessTools.Method(typeof(StartMenu), "ReloadData").Invoke(menu, null);
            RefreshLabel(menu);
            log.LogInfo($"[menu] Archipelago mode {(on ? "ON: saves in the archipelago folder" : "off: normal saves")}");
        }

        private static void BeforeUpdate(StartMenu __instance, int ___menuid, float ___cd, bool ___canselect)
        {
            try
            {
                if (ApMenu.Open != null || ___menuid != 1 || ___cd > 0f || !___canselect || MainManager.pausemenu != null)
                {
                    return;
                }
                if (MainManager.instance.option != Option || !MainManager.GetKey(4, hold: false))
                {
                    return;
                }
                ApMenu.Show(log, __instance, server, slot, password, mode, connect, status);
            }
            catch (Exception e)
            {
                log.LogError("[menu] opening the panel failed: " + e);
            }
        }

        // The game places the cursor at y = -option - 0.25 each frame; move it to the tighter spacing.
        private static void AfterUpdate(int ___menuid)
        {
            if (___menuid != 1 || MainManager.instance?.cursor == null)
            {
                return;
            }
            Transform cursor = MainManager.instance.cursor.transform;
            Vector3 p = cursor.localPosition;
            cursor.localPosition = new Vector3(p.x, -MainManager.instance.option * Spacing - 0.25f, p.z);
        }
    }
}
