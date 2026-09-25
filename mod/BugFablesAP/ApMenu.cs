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
    // Rows: Address, Port, Slot, Password, Difficulty, Detector, Archipelago (the mod on/off), Quality of life (a second
    // page of on/off rows, QualityOfLife); cancel backs out. It connects on its own (Plugin.AutoConnect). Up/down move; confirm (C / Enter) edits
    // a text row or presses a button; cancel (X / Escape) closes. While a row is being edited, the keyboard
    // types into it: Backspace deletes, Ctrl+V pastes, Ctrl+C copies the row, Enter keeps, Escape reverts.
    // The title screen's own input is suspended while the panel is open (StartMenu.canselect), so the game's
    // key letters (C, X, Z, V) can be typed.
    internal sealed class ApMenu : MonoBehaviour
    {
        private const int Address = 0, PortRow = 1, SlotRow = 2, PasswordRow = 3, DifficultyRow = 4, DetectorRow = 5, ModeRow = 6,
            QolRow = 7, Rows = 8;
        // The Quality of life page's rows (the user, 2026-09-25: a sub-menu inside the panel). Cancel goes back to the
        // first page, on the Quality of life row.
        private const int FastTextRow = 0, SkipIntroRow = 1, FreeBoatRow = 2, QolRows = 3;
        private bool qolPage;

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
        // Eight rows at 0.65 apart (seven at 0.7 until the Quality of life row, 2026-09-25), with room below for the
        // description and status lines. No Back row: cancel (X / B) backs out, as the hint box above says (the user,
        // 2026-09-24).
        private static readonly float[] RowY = { 2.65f, 2.0f, 1.35f, 0.7f, 0.05f, -0.6f, -1.25f, -1.9f };
        private const float DescribeY = -2.55f, StatusY = -3.1f;
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
            int rows = qolPage ? QolRows : Rows;
            if (MainManager.GetKey(0, hold: false))
            {
                row = (row + rows - 1) % rows;
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (MainManager.GetKey(1, hold: false))
            {
                row = (row + 1) % rows;
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (IsChoice(row) && (MainManager.GetKey(2, hold: false) || MainManager.GetKey(3, hold: false)))
            {
                Step(row, MainManager.GetKey(3, hold: false) ? 1 : -1);
                Redraw();
            }
            else if (MainManager.GetKey(5, hold: false) || Input.GetKeyDown(KeyCode.Escape))
            {
                // The sound the game plays backing out of the file select (StartMenu.cs:619).
                MainManager.PlaySound("Cancel", 10);
                if (qolPage)
                {
                    SwitchPage(qol: false, QolRow);
                }
                else
                {
                    Close();
                }
            }
            else if (qolPage && (MainManager.GetKey(4, hold: false) || Input.GetKeyDown(KeyCode.Return)))
            {
                Step(row, 1);
                Redraw();
            }
            else if (MainManager.GetKey(4, hold: false) || Input.GetKeyDown(KeyCode.Return))
            {
                if (!IsChoice(row))
                {
                    MainManager.PlaySound("Confirm", -1);
                }
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
                    case QolRow:
                        SwitchPage(qol: true, 0);
                        break;
                }
            }
        }

        private void SwitchPage(bool qol, int at)
        {
            qolPage = qol;
            row = at;
            // The arrows sit by the choice rows, which differ per page.
            if (arrows != null)
            {
                Destroy(arrows.gameObject);
                arrows = null;
            }
            settleFrames = 2;
            Redraw();
            log.LogInfo("[apmenu] " + (qol ? "Quality of life page" : "first page"));
        }

        private string Describe(int r)
        {
            if (qolPage)
            {
                switch (r)
                {
                    case FastTextRow: return "Dialogue text is instant, but still requires a button press to proceed.";
                    case SkipIntroRow: return "Skips the story slides at the start of a new game.";
                    case FreeBoatRow: return "The boat to Metal Island costs nothing.";
                    default: return "";
                }
            }
            switch (r)
            {
                case Address: return "The server the room is hosted on, e.g. archipelago.gg.";
                case PortRow: return "The room's port, e.g. 38281.";
                case SlotRow: return "Your player slot name.";
                case PasswordRow: return "The room's password, if it has one.";
                case DifficultyRow: return "Only affects how tough enemies are; every check stays the same.";
                case DetectorRow: return "Acts like the Detector medal is always equipped, to find hidden items.";
                case ModeRow: return "Turns Archipelago on or off. While on, normal saves are never touched.";
                case QolRow: return "Settings that speed up the game.";
                default: return "";
            }
        }

        // The settings screen's sound for changing a value (PauseMenu.SettingsToggleSound, PauseMenu.cs:233): Confirm0
        // on channel 10, at the sound volume. The main menu has no pause menu to read the volume from, so it's the
        // game's own global one (the user, 2026-09-24: the scroll sound didn't match).
        private static void ChangeSound()
        {
            MainManager.PlaySound(Resources.Load<AudioClip>("Audio/Sounds/Confirm0"), 10, 1f, 1f);
            MainManager.sounds[10].volume = MainManager.pausemenu != null ? MainManager.pausemenu.svolume : MainManager.soundvolume;
        }

        private bool IsChoice(int r) => qolPage || r == ModeRow || r == DifficultyRow || r == DetectorRow;

        // Left/right (or confirm) on a choice row: the next or previous value.
        private void Step(int r, int by)
        {
            ChangeSound();
            if (qolPage)
            {
                ConfigEntry<bool> setting = QolSetting(r);
                if (setting != null)
                {
                    setting.Value = !setting.Value;
                    log.LogInfo("[apmenu] " + setting.Definition.Key + ": " + (setting.Value ? "On" : "Off"));
                }
            }
            else if (r == ModeRow)
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

        private static ConfigEntry<bool> QolSetting(int r) =>
            r == FastTextRow ? QualityOfLife.FastText
            : r == SkipIntroRow ? QualityOfLife.SkipIntro
            : r == FreeBoatRow ? QualityOfLife.FreeBoat
            : null;

        private static string OnOff(ConfigEntry<bool> setting) => setting != null && setting.Value ? "ON" : "OFF";

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
            if (arrows == null)
            {
                BuildArrows();
            }
            shownStatus = status();
            if (qolPage)
            {
                Choice(FastTextRow, "Fast text", OnOff(QualityOfLife.FastText));
                Choice(SkipIntroRow, "Skip intro", OnOff(QualityOfLife.SkipIntro));
                Choice(FreeBoatRow, "Free boat", OnOff(QualityOfLife.FreeBoat));
                Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
                Text("|center||size,0.5|Quality of life. Cancel goes back.", 0f, StatusY);
                leaf.transform.localPosition = new Vector3(LabelX + LeafOffset, RowY[row] + LeafRise, 0f);
                return;
            }
            string pw = editing && row == PasswordRow ? edited : new string('*', password.Value.Length);
            Row(Address, "Address", editing && row == Address ? edited : server.Value);
            Row(PortRow, "Port", editing && row == PortRow ? edited : port.Value);
            Row(SlotRow, "Slot", editing && row == SlotRow ? edited : slot.Value);
            Row(PasswordRow, "Password", pw);

            Choice(ModeRow, "Archipelago", mode.Value ? "ENABLED" : "DISABLED");
            Choice(DifficultyRow, "Difficulty", (Difficulty?.Value ?? "Normal").ToUpperInvariant());
            Choice(DetectorRow, "Detector", Detector == null || Detector.Value ? "ON" : "OFF");
            Label(QolRow, "Quality of life");

            // What the highlighted row does, one line, the way the game's settings screen explains its rows (the
            // user, 2026-09-24: "Detector" alone doesn't say it means the medal; the wording is the user's). Then the
            // connection's state.
            Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
            Text("|center||size,0.5|" + Safe(shownStatus), 0f, StatusY);
            leaf.transform.localPosition = new Vector3(LabelX + LeafOffset, RowY[row] + LeafRise, 0f);
        }

        // The choice rows' arrows, made once: the settings screen's own, the plain arrow sprite (guisprites[1])
        // turned -90 and +90 degrees on either side of the value (MainManager.cs:15905-15917). Button prompts
        // rebuilt on every redraw played their pop-in each time the cursor moved (the user, 2026-09-24).
        private void BuildArrows()
        {
            arrows = new GameObject("arrows").transform;
            arrows.parent = box;
            arrows.localPosition = Vector3.zero;
            arrows.localEulerAngles = Vector3.zero;
            foreach (int r in qolPage ? new[] { FastTextRow, SkipIntroRow, FreeBoatRow } : new[] { DifficultyRow, DetectorRow, ModeRow })
            {
                for (int side = 0; side < 2; side++)
                {
                    GameObject arrow = MainManager.NewUIObject("arrow" + r + side, arrows,
                        new Vector3(side == 0 ? ArrowLeftX : ArrowRightX, RowY[r] + ArrowRise), Vector3.one * ArrowScale,
                        MainManager.guisprites[1], ButtonSort);
                    arrow.transform.localEulerAngles = new Vector3(0f, 0f, side == 0 ? -90f : 90f);
                    arrow.layer = 5;
                }
            }
        }

        private const float ArrowLeftX = 0.9f, ArrowRightX = 4.3f, ArrowRise = 0.15f, ArrowScale = 0.75f;

        private void Choice(int r, string label, string value)
        {
            Label(r, label);
            Text("|center||size,0.75|" + value, 2.6f, RowY[r]);
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
