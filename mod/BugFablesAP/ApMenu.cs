using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The Archipelago panel, opened from "Archipelago" on the main menu. It's drawn with the game's own box and
    // font (MainManager.Create9Box / SetText) and takes real typing, so an address can be typed or pasted.
    //
    // Rows: Address, Slot, Password, Archipelago mode, Connect, Back. Up/down move; confirm (C / Enter) edits
    // a text row or presses a button; cancel (X / Escape) closes. While a row is being edited, the keyboard
    // types into it: Backspace deletes, Ctrl+V pastes, Ctrl+C copies the row, Enter keeps, Escape reverts.
    // The title screen's own input is suspended while the panel is open (StartMenu.canselect), so the game's
    // key letters (C, X, Z, V) can be typed.
    internal sealed class ApMenu : MonoBehaviour
    {
        private const int Address = 0, SlotRow = 1, PasswordRow = 2, ModeRow = 3, ConnectRow = 4, BackRow = 5, Rows = 6;

        internal static ApMenu Open;

        private static ManualLogSource log;
        private StartMenu owner;
        private ConfigEntry<string> server, slot, password;
        private ConfigEntry<bool> mode;
        private Action connect;
        private Func<string> status;

        private Transform box;
        private Transform textRoot;
        private int row;
        private bool editing;
        private string edited;
        private string before;
        private int settleFrames;
        private string shownStatus;

        internal static void Show(ManualLogSource logger, StartMenu owner, ConfigEntry<string> server, ConfigEntry<string> slot,
            ConfigEntry<string> password, ConfigEntry<bool> mode, Action connect, Func<string> status)
        {
            if (Open != null)
            {
                return;
            }
            log = logger;
            var go = new GameObject("ArchipelagoMenu");
            ApMenu menu = go.AddComponent<ApMenu>();
            menu.owner = owner;
            menu.server = server;
            menu.slot = slot;
            menu.password = password;
            menu.mode = mode;
            menu.connect = connect;
            menu.status = status;
            Open = menu;
            menu.Build();
        }

        private void Build()
        {
            Traverse.Create(owner).Field("canselect").SetValue(false);
            // Hide the title screen under the panel (the menu text, the logo, the cursor), as the game does for the
            // file select, and draw the panel in front: its first draw sat behind the logo (the user, 2026-09-24).
            SetTitleVisible(false);
            box = MainManager.Create9Box(new Vector3(0f, 0f, 10f), new Vector2(13f, 7.5f), 0, BoxSort, Color.white, false);
            box.parent = owner.transform;
            box.localPosition = new Vector3(0f, 0.2f, 0f);
            textRoot = new GameObject("text").transform;
            textRoot.parent = box;
            textRoot.localPosition = Vector3.zero;
            settleFrames = 10; // the press that opened the panel must not also act inside it
            Redraw();
            log.LogInfo("[apmenu] opened");
        }

        // A hot reload unloads the plugin while the panel may be open: restore the title screen first.
        internal void CloseNow() => Close();

        private void Close()
        {
            if (box != null)
            {
                Destroy(box.gameObject);
            }
            SetTitleVisible(true);
            Traverse.Create(owner).Field("canselect").SetValue(true);
            Traverse.Create(owner).Field("cd").SetValue(10f);
            MenuToggle.RefreshLabel(owner);
            Open = null;
            Destroy(gameObject);
            log.LogInfo("[apmenu] closed");
        }

        private const int BoxSort = 100;
        private const string TextSort = "|sort,110|";

        private void SetTitleVisible(bool visible)
        {
            Transform menu = Traverse.Create(owner).Field("menu1").GetValue<Transform>();
            if (menu != null)
            {
                menu.gameObject.SetActive(visible);
            }
            SpriteRenderer[] sprites = Traverse.Create(owner).Field("sprites").GetValue<SpriteRenderer[]>();
            if (sprites != null && sprites.Length > 1 && sprites[1] != null)
            {
                sprites[1].enabled = visible; // the logo
            }
            if (MainManager.instance.cursor != null)
            {
                MainManager.instance.cursor.enabled = visible;
            }
        }

        private void Update()
        {
            try
            {
                if (settleFrames > 0)
                {
                    settleFrames--;
                    return;
                }
                if (editing)
                {
                    TypeInto();
                }
                else
                {
                    Navigate();
                }
                string s = status();
                if (s != shownStatus)
                {
                    Redraw();
                }
            }
            catch (Exception e)
            {
                log.LogError("[apmenu] " + e);
                Close();
            }
        }

        private void Navigate()
        {
            if (MainManager.GetKey(0, hold: false))
            {
                row = (row + Rows - 1) % Rows;
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (MainManager.GetKey(1, hold: false))
            {
                row = (row + 1) % Rows;
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (MainManager.GetKey(5, hold: false) || Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
            else if (MainManager.GetKey(4, hold: false) || Input.GetKeyDown(KeyCode.Return))
            {
                MainManager.PlaySound("Confirm", -1);
                switch (row)
                {
                    case Address:
                    case SlotRow:
                    case PasswordRow:
                        editing = true;
                        before = Field(row).Value;
                        edited = before;
                        settleFrames = 1;
                        Redraw();
                        break;
                    case ModeRow:
                        MenuToggle.SetMode(owner, !mode.Value);
                        Redraw();
                        break;
                    case ConnectRow:
                        connect();
                        Redraw();
                        break;
                    case BackRow:
                        Close();
                        break;
                }
            }
        }

        private void TypeInto()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.V))
            {
                edited += Clean(GUIUtility.systemCopyBuffer);
                Redraw();
                return;
            }
            if (ctrl && Input.GetKeyDown(KeyCode.C))
            {
                GUIUtility.systemCopyBuffer = edited;
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                edited = before;
                FinishEdit(keep: false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                FinishEdit(keep: true);
                return;
            }
            bool changed = false;
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    if (edited.Length > 0)
                    {
                        edited = edited.Substring(0, edited.Length - 1);
                        changed = true;
                    }
                }
                else if (c >= ' ' && !ctrl && edited.Length < 64)
                {
                    edited += c;
                    changed = true;
                }
            }
            if (changed)
            {
                Redraw();
            }
        }

        private void FinishEdit(bool keep)
        {
            if (keep)
            {
                Field(row).Value = edited.Trim();
            }
            editing = false;
            settleFrames = 2;
            MainManager.PlaySound(keep ? "Confirm" : "Cancel", -1);
            Redraw();
        }

        private ConfigEntry<string> Field(int r) => r == Address ? server : r == SlotRow ? slot : password;

        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (c >= ' ' && c != '|')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Trim();
        }

        // The game's text commands are written between '|', so a '|' typed into a field would be read as one.
        private static string Safe(string s) => (s ?? "").Replace("|", "");

        private void Redraw()
        {
            MainManager.DestroyText(textRoot);
            shownStatus = status();
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + "|center||size,0.9|Archipelago", new Vector3(0f, 3f, 0f), textRoot));
            string pw = editing && row == PasswordRow ? edited : new string('*', password.Value.Length);
            Line(Row(Address, "Address", editing && row == Address ? edited : server.Value), 2f);
            Line(Row(SlotRow, "Slot", editing && row == SlotRow ? edited : slot.Value), 1.2f);
            Line(Row(PasswordRow, "Password", pw), 0.4f);
            Line(Row(ModeRow, "Archipelago mode", mode.Value ? "On" : "Off"), -0.6f);
            Line(Row(ConnectRow, "Connect", ""), -1.4f);
            Line(Row(BackRow, "Back", ""), -2.2f);
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + "|center||size,0.55|" + Safe(shownStatus), new Vector3(0f, -3.1f, 0f), textRoot));
        }

        private string Row(int r, string label, string value)
        {
            // Game text colours (MainManager.textcolors): 1 red, 3 blue. Editing = red, selected = blue.
            string marker = r == row ? (editing ? "|color,1|" : "|color,3|") : "";
            string caret = editing && r == row ? "_" : "";
            string shown = value.Length == 0 && !(editing && r == row) && r <= PasswordRow ? "(empty)" : Safe(value);
            string sep = r <= ModeRow ? ": " : "";
            return "|size,0.7|" + marker + label + sep + shown + caret;
        }

        private void Line(string text, float y)
        {
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + text, new Vector3(-5.8f, y, 0f), textRoot));
        }
    }
}
