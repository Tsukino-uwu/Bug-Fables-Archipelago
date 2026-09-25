using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // A fourth main-menu entry, "Archipelago", that opens the Archipelago panel (ApMenu). The four lines are
    // spaced tighter (Spacing) so the fourth clears the credit line, and the cursor follows.
    internal static class MenuToggle
    {
        private const int Option = 3;
        private const float Spacing = 0.8f;

        private static ManualLogSource log;
        private static ConfigEntry<bool> mode;
        private static ConfigEntry<string> server, port, slot, password;
        private static Action connect;
        private static Func<string> status;
        private static Func<bool> seedKnown;
        private static Harmony harmony;

        internal static void Enable(ManualLogSource logger, string guid, ConfigEntry<bool> randomizerEnabled,
            ConfigEntry<string> serverEntry, ConfigEntry<string> portEntry, ConfigEntry<string> slotEntry, ConfigEntry<string> passwordEntry,
            Action connectAction, Func<string> statusText, Func<bool> seedIsKnown)
        {
            log = logger;
            mode = randomizerEnabled;
            server = serverEntry;
            port = portEntry;
            slot = slotEntry;
            password = passwordEntry;
            connect = connectAction;
            status = statusText;
            seedKnown = seedIsKnown;
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
            ClosePopup();
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // Centred like the others so the leaf cursor (a fixed column) clears it; the state is a smaller tag to the right.
        private const string Label = "|center|Archipelago";
        private static string StateTag => "|size,0.55|" + (mode.Value ? "(Enabled)" : "(Disabled)");
        private const float StateTagX = 1.75f;

        private static void DrawLabel(Transform line)
        {
            MainManager.instance.StartCoroutine(MainManager.SetText(Label, new Vector3(0f, 0f, 10f), line));
            MainManager.instance.StartCoroutine(MainManager.SetText(StateTag, new Vector3(StateTagX, 0.22f, 10f), line));
        }

        // The game's SetMenuText loop indexes a three-label array by selections.Length: hand it back its three entries first.
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
                DrawLabel(line);
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
                DrawLabel(line);
            }
        }

        internal static void SetMode(StartMenu menu, bool on)
        {
            SaveRedirect.On = on;
            mode.Value = on;
            // Show the other mode's saves.
            AccessTools.Method(typeof(StartMenu), "ReloadData").Invoke(menu, null);
            RefreshLabel(menu);
            log.LogInfo($"[menu] Archipelago mod {(on ? "enabled: saves in the archipelago folder" : "disabled: normal saves")}");
        }

        private static bool BeforeUpdate(StartMenu __instance, int ___menuid, int ___submenu, float ___cd, bool ___canselect)
        {
            try
            {
                if (HoldBackFile(___menuid, ___submenu, ___cd, ___canselect))
                {
                    return false;
                }
                if (ApMenu.Open != null || ___menuid != 1 || ___cd > 0f || !___canselect || MainManager.pausemenu != null)
                {
                    return true;
                }
                if (MainManager.instance.option != Option || !MainManager.GetKey(4, hold: false))
                {
                    return true;
                }
                // The game plays this for every main-menu choice; we take the press before its code runs.
                MainManager.PlaySound("Confirm", -1);
                ApMenu.Show(log, __instance, server, port, slot, password, mode, connect, status);
            }
            catch (Exception e)
            {
                log.LogError("[menu] opening the panel failed: " + e);
            }
            return true;
        }

        // With the mod on and no seed known yet, choosing a file (load or new game) gets the buzzer and a popup saying
        // why, and the game never sees the press. The file select is frozen under the popup.
        private static Transform popup, popupStatus;
        private static string shownPopupStatus;
        private static int popupFrame;

        // Over the save slots: their boxes sort at -20 to -60 and their text at 10.
        private const int PopupDimSort = 50, PopupBoxSort = 60, PopupTextSort = 70;

        private static bool HoldBackFile(int menuid, int submenu, float cd, bool canselect)
        {
            if (popup != null)
            {
                TickPopup();
                return true;
            }
            if (!mode.Value || seedKnown() || menuid != 2 || submenu != 0 || cd > 0f || !canselect
                || MainManager.pausemenu != null || MainManager.instance.option >= Option || !MainManager.GetKey(4, hold: false))
            {
                return false;
            }
            MainManager.PlayBuzzer();
            ShowPopup();
            log.LogInfo("[menu] held back file " + MainManager.instance.option + ": the seed isn't known yet (no login this run)");
            return true;
        }

        // Box type 1 is the game's orange box.
        private static void ShowPopup()
        {
            popup = new GameObject("apnotconnected").transform;
            popup.parent = MainManager.GUICamera.transform;
            popup.localPosition = new Vector3(0f, 0f, 10f);
            popup.localEulerAngles = Vector3.zero;
            popup.gameObject.layer = 5;
            var pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.black);
            pixel.Apply();
            SpriteRenderer dim = new GameObject("Dimmer").AddComponent<SpriteRenderer>();
            dim.sprite = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            dim.transform.parent = popup;
            dim.transform.localPosition = Vector3.zero;
            dim.transform.localEulerAngles = Vector3.zero;
            dim.transform.localScale = new Vector3(3000f, 3000f, 1f);
            dim.gameObject.layer = 5;
            dim.sortingOrder = PopupDimSort;
            Transform box = MainManager.Create9Box(new Vector3(0f, 0f, 10f), new Vector2(12f, 4.75f), 1, PopupBoxSort, Color.white, false);
            box.parent = popup;
            // Over the three save slots.
            box.localPosition = new Vector3(0f, 0.9f, 0f);
            string sort = "|sort," + PopupTextSort + "|";
            MainManager.instance.StartCoroutine(MainManager.SetText(sort + "|center||size,0.8|Not connected to Archipelago", new Vector3(0f, 1.45f, 0f), box));
            MainManager.instance.StartCoroutine(MainManager.SetText(sort + "|center||size,0.6|Connect in the Archipelago panel on the main menu,", new Vector3(0f, 0.6f, 0f), box));
            MainManager.instance.StartCoroutine(MainManager.SetText(sort + "|center||size,0.6|then choose your file again.", new Vector3(0f, 0.05f, 0f), box));
            popupStatus = new GameObject("status").transform;
            popupStatus.parent = box;
            popupStatus.localPosition = Vector3.zero;
            shownPopupStatus = null;
            // ButtonSprite's label has no sort of its own and fell behind the box: the label carries the sort.
            new GameObject("okbutton").AddComponent<ButtonSprite>().SetUp(4, -1, sort + "OK", new Vector3(-2.4f, -1.6f), Vector3.one * 0.5f, PopupTextSort, box);
            new GameObject("closebutton").AddComponent<ButtonSprite>().SetUp(5, -1, sort + "Close", new Vector3(0.6f, -1.6f), Vector3.one * 0.5f, PopupTextSort, box);
            popupFrame = Time.frameCount;
            DrawPopupStatus();
        }

        private static void DrawPopupStatus()
        {
            string s = status() ?? "";
            if (s == shownPopupStatus || popupStatus == null)
            {
                return;
            }
            shownPopupStatus = s;
            MainManager.DestroyText(popupStatus);
            MainManager.instance.StartCoroutine(MainManager.SetText("|sort," + PopupTextSort + "||center||size,0.5|" + s.Replace("|", "/"),
                new Vector3(0f, -0.75f, 0f), popupStatus));
        }

        private static void TickPopup()
        {
            DrawPopupStatus();
            // The press that opened it is still "down" this frame.
            if (Time.frameCount > popupFrame && (MainManager.GetKey(4, hold: false) || MainManager.GetKey(5, hold: false)))
            {
                MainManager.PlaySound("Confirm", -1);
                ClosePopup();
            }
        }

        private static void ClosePopup()
        {
            if (popup != null)
            {
                UnityEngine.Object.Destroy(popup.gameObject);
            }
            popup = null;
            popupStatus = null;
        }

        // The game places the cursor at y = -option - 0.25 each frame; move it to the tighter spacing.
        private static void AfterUpdate(int ___menuid)
        {
            // Only the main menu's own cursor: under the game's Settings screen it is Settings' cursor.
            if (___menuid != 1 || MainManager.instance?.cursor == null || MainManager.pausemenu != null || ApMenu.Open != null)
            {
                return;
            }
            // Closing the game's Settings resets maxoptions to 3, leaving "Archipelago" unreachable.
            if (MainManager.instance.maxoptions == Option)
            {
                MainManager.instance.maxoptions = Option + 1;
            }
            Transform cursor = MainManager.instance.cursor.transform;
            Vector3 p = cursor.localPosition;
            cursor.localPosition = new Vector3(p.x, -MainManager.instance.option * Spacing - 0.25f, p.z);
        }
    }
}
