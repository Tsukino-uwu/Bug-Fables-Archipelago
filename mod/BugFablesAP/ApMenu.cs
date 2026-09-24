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
    // Rows: Address, Port, Slot, Password, Archipelago mod, Difficulty, Detector, Back. It connects on its own (Plugin.AutoConnect). Up/down move; confirm (C / Enter) edits
    // a text row or presses a button; cancel (X / Escape) closes. While a row is being edited, the keyboard
    // types into it: Backspace deletes, Ctrl+V pastes, Ctrl+C copies the row, Enter keeps, Escape reverts.
    // The title screen's own input is suspended while the panel is open (StartMenu.canselect), so the game's
    // key letters (C, X, Z, V) can be typed.
    internal sealed class ApMenu : MonoBehaviour
    {
        private const int Address = 0, PortRow = 1, SlotRow = 2, PasswordRow = 3, ModeRow = 4, DifficultyRow = 5, DetectorRow = 6,
            BackRow = 7, Rows = 8;

        // The Difficulty and Detector rows' settings (Plugin, MedalAssist).
        internal static readonly string[] Difficulties = { "Normal", "Hard", "Hardest" };
        internal static ConfigEntry<string> Difficulty;
        internal static ConfigEntry<bool> Detector;

        internal static ApMenu Open;

        private static ManualLogSource log;
        private StartMenu owner;
        private ConfigEntry<string> server, port, slot, password;
        private ConfigEntry<bool> mode;
        private Action connect;
        private Func<string> status;

        private Transform box;
        private Transform help;
        private SpriteRenderer leaf;
        private SpriteRenderer dimmer;
        private Transform arrows;
        private Transform textRoot;
        private int row;
        private bool editing;
        private string edited;
        private string before;
        private int settleFrames;
        private string shownStatus;

        internal static void Show(ManualLogSource logger, StartMenu owner, ConfigEntry<string> server, ConfigEntry<string> port, ConfigEntry<string> slot,
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
            menu.port = port;
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
            // The same two boxes as the game's settings screen (PauseMenu window 4, PauseMenu.cs:2707): the orange
            // leafy box (type 1) and the controls box above it (type 4), with the game's own button hints.
            // Hang off the GUI camera at (0, 0, 10) as PauseMenu does (PauseMenu.cs:143). Under the title screen's
            // own object (at y = -1) everything sat one unit low (the user's screenshot, 2026-09-24).
            transform.parent = MainManager.GUICamera.transform;
            transform.localPosition = new Vector3(0f, 0f, 10f);
            transform.localEulerAngles = Vector3.zero;
            gameObject.layer = 5;
            // PauseMenu's dimmer: a black square stretched over the screen, faded to half (PauseMenu.cs:146-157,
            // 229). On the settings screen it's what hides the title screen's white haze.
            var pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.black);
            pixel.Apply();
            dimmer = new GameObject("Dimmer").AddComponent<SpriteRenderer>();
            dimmer.sprite = Sprite.Create(pixel, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            dimmer.color = Color.clear;
            dimmer.transform.parent = transform;
            dimmer.transform.localPosition = Vector3.zero;
            dimmer.transform.localEulerAngles = Vector3.zero;
            dimmer.transform.localScale = new Vector3(3000f, 3000f, 1f);
            dimmer.gameObject.layer = 5;
            dimmer.sortingOrder = -100;
            box = MainManager.Create9Box(new Vector3(0f, -1f, 10f), new Vector2(13.5f, 7.25f), 1, BoxSort, Color.white, false);
            box.parent = transform;
            box.localPosition = new Vector3(0f, -1f, 0f);
            help = MainManager.Create9Box(new Vector3(0f, 3.75f, 10f), new Vector2(12.5f, 2f), 4, HelpSort, Color.white, false);
            help.parent = transform;
            help.localPosition = new Vector3(0f, 3.75f, 0f);
            new GameObject("confirmbutton").AddComponent<ButtonSprite>().SetUp(4, -1, "Select / Edit", new Vector3(-4.5f, 0.25f), Vector3.one * 0.5f, ButtonSort, help);
            new GameObject("cancelbutton").AddComponent<ButtonSprite>().SetUp(5, -1, "Back", new Vector3(0.5f, 0.25f), Vector3.one * 0.5f, ButtonSort, help);
            new GameObject("enterbutton").AddComponent<ButtonSprite>().SetUp(9, -1, "Done typing", new Vector3(-4.5f, -0.5f), Vector3.one * 0.5f, ButtonSort, help);
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + "|size,0.55|Ctrl+V paste   Ctrl+C copy", new Vector3(0.5f, -0.45f, 0f), help));
            textRoot = new GameObject("text").transform;
            textRoot.parent = box;
            textRoot.localPosition = Vector3.zero;
            leaf = new GameObject("leaf").AddComponent<SpriteRenderer>();
            leaf.sprite = MainManager.cursorsprite[0];
            leaf.sortingOrder = CursorSort;
            leaf.gameObject.layer = 5;
            leaf.transform.parent = box;
            leaf.transform.localEulerAngles = Vector3.zero;
            leaf.transform.localScale = Vector3.one;
            // The game's own menu cursor wiggles through this, set up the same way (MainManager.cs:14822). It
            // animates the scale only, so it doesn't fight the per-row position (SpriteBounce.FixedUpdate).
            leaf.gameObject.AddComponent<SpriteBounce>().MessageBounce();
            settleFrames = 10; // the press that opened the panel must not also act inside it
            Redraw();
            log.LogInfo("[apmenu] opened");
        }

        // A hot reload unloads the plugin while the panel may be open: restore the title screen first.
        internal void CloseNow() => Close();

        private void Close()
        {
            SetTitleVisible(true);
            Traverse.Create(owner).Field("canselect").SetValue(true);
            Traverse.Create(owner).Field("cd").SetValue(10f);
            MenuToggle.RefreshLabel(owner);
            // Back on "Start Game", as the game does when leaving its own screens: SetMenuText ends with
            // option = 0 (StartMenu.cs:319). The user chose this over staying on "Archipelago" (2026-09-24).
            MainManager.instance.option = 0;
            Open = null;
            Destroy(gameObject);
            log.LogInfo("[apmenu] closed");
        }

        // The settings screen's own draw orders (PauseMenu.cs:2707-2712): box -20, controls box -10, hints 5.
        // Higher values drew the panel over its own button labels (the user's screenshot, 2026-09-24).
        private const int BoxSort = -20;
        private const int HelpSort = -10;
        private const int ButtonSort = 5;
        private const int CursorSort = 20;
        private const string TextSort = "|sort,10|";
        // Row heights inside the orange box, top to bottom; labels on the left, values on the right, as in the
        // settings screen.
        // Eight rows at 0.7 apart (six were 0.85 apart), so the status line still fits inside the box.
        private static readonly float[] RowY = { 2.6f, 1.9f, 1.2f, 0.5f, -0.2f, -0.9f, -1.6f, -2.3f };
        // Matched to the game's Settings screen from the user's side-by-side screenshots (2026-09-24): there the
        // labels start ~88 px in from the vine border, with the leaf's tip ~15 px before them. Two earlier nudges
        // misread a cropped screenshot (-6.3 touched the vine); -5.15 puts the labels at Settings' distance.
        private const float LabelX = -5.15f;
        private const float LeafOffset = -0.1f;
        // Settings points the leaf's tip at the middle of the label; ours sat ~10 px high (the user's close-ups).
        private const float LeafRise = 0.15f;
        private const float ValueX = -1.9f;

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
                if (dimmer != null)
                {
                    dimmer.color = Color.Lerp(dimmer.color, new Color(1f, 1f, 1f, 0.5f), 0.15f);
                }
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
            else if (IsChoice(row) && (MainManager.GetKey(2, hold: false) || MainManager.GetKey(3, hold: false)))
            {
                MainManager.PlayScrollSound();
                Step(row, MainManager.GetKey(3, hold: false) ? 1 : -1);
                Redraw();
            }
            else if (MainManager.GetKey(5, hold: false) || Input.GetKeyDown(KeyCode.Escape))
            {
                // The sound the game plays backing out of the file select (StartMenu.cs:619).
                MainManager.PlaySound("Cancel", 10);
                Close();
            }
            else if (MainManager.GetKey(4, hold: false) || Input.GetKeyDown(KeyCode.Return))
            {
                MainManager.PlaySound("Confirm", -1);
                switch (row)
                {
                    case Address:
                    case PortRow:
                    case SlotRow:
                    case PasswordRow:
                        editing = true;
                        before = Field(row).Value;
                        edited = before;
                        settleFrames = 1;
                        Redraw();
                        break;
                    case ModeRow:
                    case DifficultyRow:
                    case DetectorRow:
                        Step(row, 1);
                        Redraw();
                        break;
                    case BackRow:
                        Close();
                        break;
                }
            }
        }

        private static bool IsChoice(int r) => r == ModeRow || r == DifficultyRow || r == DetectorRow;

        // Left/right (or confirm) on a choice row: the next or previous value.
        private void Step(int r, int by)
        {
            if (r == ModeRow)
            {
                MenuToggle.SetMode(owner, !mode.Value);
            }
            else if (r == DifficultyRow && Difficulty != null)
            {
                int at = Array.IndexOf(Difficulties, Difficulty.Value);
                Difficulty.Value = Difficulties[((at < 0 ? 0 : at) + by + Difficulties.Length) % Difficulties.Length];
                log.LogInfo("[apmenu] Difficulty: " + Difficulty.Value);
            }
            else if (r == DetectorRow && Detector != null)
            {
                Detector.Value = !Detector.Value;
                log.LogInfo("[apmenu] Detector: " + (Detector.Value ? "On" : "Off"));
            }
        }

        private void TypeInto()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.V))
            {
                string pasted = Clean(GUIUtility.systemCopyBuffer);
                if (row == PortRow)
                {
                    pasted = new string(Array.FindAll(pasted.ToCharArray(), char.IsDigit));
                }
                else if (row == Address)
                {
                    // A room page shows "archipelago.gg:63560": paste it whole and the port goes to its own row.
                    var m = System.Text.RegularExpressions.Regex.Match(pasted, @"^(.+):(\d{1,5})$");
                    if (m.Success)
                    {
                        pasted = m.Groups[1].Value;
                        port.Value = m.Groups[2].Value;
                    }
                }
                edited += pasted;
                Redraw();
                return;
            }
            if (ctrl && Input.GetKeyDown(KeyCode.C))
            {
                GUIUtility.systemCopyBuffer = edited;
                return;
            }
            // The gamepad's own confirm and cancel end editing too (the user: stuck until Enter). They're read the
            // way MainManager.GetKey does for a gamepad: action 4 (confirm) is joykeys[0], action 5 (cancel) is
            // joykeys[1] (MainManager.cs, GetKey's default branch), and joykeys are raw buttons (InputIO.cs:571).
            // A first try used joykeys[4]/[5], which are Start and Back. Only the pad is read here, so typing the
            // keyboard's C or X (the game's keyboard confirm and cancel) still just types.
            bool pad = MainManager.usejoystick > 0;
            bool padConfirm = pad && InputIOManager.InputIO.GetKeyDown(0, true);
            bool padCancel = pad && InputIOManager.InputIO.GetKeyDown(1, true);
            if (Input.GetKeyDown(KeyCode.Escape) || padCancel)
            {
                edited = before;
                FinishEdit(keep: false);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || padConfirm)
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
                else if (c >= ' ' && !ctrl && edited.Length < 64 && (row != PortRow || char.IsDigit(c)))
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

        private ConfigEntry<string> Field(int r) => r == Address ? server : r == PortRow ? port : r == SlotRow ? slot : password;

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
            if (arrows != null)
            {
                Destroy(arrows.gameObject);
            }
            shownStatus = status();
            string pw = editing && row == PasswordRow ? edited : new string('*', password.Value.Length);
            Row(Address, "Address", editing && row == Address ? edited : server.Value);
            Row(PortRow, "Port", editing && row == PortRow ? edited : port.Value);
            Row(SlotRow, "Slot", editing && row == SlotRow ? edited : slot.Value);
            Row(PasswordRow, "Password", pw);
            Row(BackRow, "Back", null);

            // The choice rows, like a settings value: left and right arrows around the value.
            arrows = new GameObject("arrows").transform;
            arrows.parent = box;
            arrows.localPosition = Vector3.zero;
            Choice(ModeRow, "Archipelago mod", mode.Value ? "ENABLED" : "DISABLED");
            Choice(DifficultyRow, "Difficulty", (Difficulty?.Value ?? "Normal").ToUpperInvariant());
            Choice(DetectorRow, "Detector", Detector == null || Detector.Value ? "ON" : "OFF");

            Text("|center||size,0.5|" + Safe(shownStatus), 0f, -3.0f);
            leaf.transform.localPosition = new Vector3(LabelX + LeafOffset, RowY[row] + LeafRise, 0f);
        }

        private void Choice(int r, string label, string value)
        {
            Label(r, label);
            new GameObject("left").AddComponent<ButtonSprite>().SetUp(2, -1, "", new Vector3(0.7f, RowY[r] + 0.25f), Vector3.one * 0.5f, ButtonSort, arrows);
            new GameObject("right").AddComponent<ButtonSprite>().SetUp(3, -1, "", new Vector3(4.5f, RowY[r] + 0.25f), Vector3.one * 0.5f, ButtonSort, arrows);
            Text("|center||size,0.7|" + value, 2.6f, RowY[r]);
        }

        private void Label(int r, string label)
        {
            string colour = editing && r == row ? "|color,1|" : "";
            Text("|size,0.8|" + colour + label, LabelX, RowY[r]);
        }

        private void Row(int r, string label, string value)
        {
            Label(r, label);
            if (value == null)
            {
                return;
            }
            bool typing = editing && r == row;
            string shown = value.Length == 0 && !typing ? "|color,5|(empty)" : Safe(value);
            Text("|size,0.65|" + (typing ? "|color,1|" : "") + shown + (typing ? "_" : ""), ValueX, RowY[r]);
        }

        private void Text(string text, float x, float y)
        {
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + text, new Vector3(x, y, 0f), textRoot));
        }
    }
}
