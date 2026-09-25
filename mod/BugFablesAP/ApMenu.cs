using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The Archipelago panel on the main menu, drawn with the game's own box and font, with real typing (paste included).
    // The title screen's input is suspended while it's open (StartMenu.canselect), so C, X, Z and V can be typed.
    internal sealed class ApMenu : MonoBehaviour
    {
        private const int Address = 0, PortRow = 1, SlotRow = 2, PasswordRow = 3, DifficultyRow = 4, DetectorRow = 5, ModeRow = 6,
            QolRow = 7, Rows = 8;
        // The Quality of life page; cancel goes back to the first page, on the Quality of life row.
        private const int FastTextRow = 0, FreeBoatRow = 1, WarpRow = 2, CutscenesRow = 3, AnimationRow = 4, PricesRow = 5, ScalingRow = 6, QolRows = 7;
        private bool qolPage;

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
            // Hide the title screen under the panel, as the game does for the file select.
            SetTitleVisible(false);
            // The settings screen's two boxes (leafy type 1, controls type 4), hung off the GUI camera at (0, 0, 10)
            // as PauseMenu does; under the title screen's object everything sat one unit low.
            transform.parent = MainManager.GUICamera.transform;
            transform.localPosition = new Vector3(0f, 0f, 10f);
            transform.localEulerAngles = Vector3.zero;
            gameObject.layer = 5;
            // PauseMenu's dimmer: it hides the title screen's white haze.
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
            // The game's own cursor wiggle; it animates the scale only, so it doesn't fight the per-row position.
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
            MainManager.instance.option = 0;
            Open = null;
            Destroy(gameObject);
            log.LogInfo("[apmenu] closed");
        }

        // The settings screen's own draw orders; higher values drew the panel over its own button labels.
        private const int BoxSort = -20;
        private const int HelpSort = -10;
        private const int ButtonSort = 5;
        private const int CursorSort = 20;
        private const string TextSort = "|sort,10|";
        private static readonly float[] RowY = { 2.65f, 2.0f, 1.35f, 0.7f, 0.05f, -0.6f, -1.25f, -1.9f };
        private const float DescribeY = -2.55f, StatusY = -3.1f;
        // Matched to the game's Settings screen: labels ~88 px in from the vine border.
        private const float LabelX = -5.15f;
        private const float LeafOffset = -0.1f;
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
                    case FreeBoatRow: return "The boat to Metal Island costs nothing.";
                    case WarpRow: return "Adds a Warp to Start button to the pause menu.";
                    case CutscenesRow: return "Skips the intro and scenes you don't need to watch.";
                    case AnimationRow: return "Which items from other players are shown held up.";
                    case PricesRow: return "What medal shops charge.";
                    case ScalingRow: return "Enemies scaled to your level, your artifacts, or not at all.";
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

        // The settings screen's value-change sound; the main menu has no pause menu, so the global sound volume.
        private static void ChangeSound()
        {
            MainManager.PlaySound(Resources.Load<AudioClip>("Audio/Sounds/Confirm0"), 10, 1f, 1f);
            MainManager.sounds[10].volume = MainManager.pausemenu != null ? MainManager.pausemenu.svolume : MainManager.soundvolume;
        }

        private bool IsChoice(int r) => qolPage || r == ModeRow || r == DifficultyRow || r == DetectorRow;

        private void Step(int r, int by)
        {
            ChangeSound();
            if (qolPage && r == PricesRow && QualityOfLife.ShopPrices != null)
            {
                string[] prices = QualityOfLife.ShopPriceValues;
                int at = Array.IndexOf(prices, QualityOfLife.ShopPrices.Value);
                QualityOfLife.ShopPrices.Value = prices[((at < 0 ? 0 : at) + by + prices.Length) % prices.Length];
                log.LogInfo("[apmenu] ShopPrices: " + QualityOfLife.ShopPrices.Value);
            }
            else if (qolPage && r == ScalingRow && QualityOfLife.EnemyScaling != null)
            {
                string[] modes = EnemyScaling.Modes;
                int at = Array.IndexOf(modes, QualityOfLife.EnemyScaling.Value);
                QualityOfLife.EnemyScaling.Value = modes[((at < 0 ? 0 : at) + by + modes.Length) % modes.Length];
                log.LogInfo("[apmenu] EnemyScaling: " + QualityOfLife.EnemyScaling.Value);
            }
            else if (qolPage && r == AnimationRow && QualityOfLife.ItemAnimation != null)
            {
                string[] values = QualityOfLife.ItemAnimations;
                int at = Array.IndexOf(values, QualityOfLife.ItemAnimation.Value);
                QualityOfLife.ItemAnimation.Value = values[((at < 0 ? 0 : at) + by + values.Length) % values.Length];
                log.LogInfo("[apmenu] ItemAnimation: " + QualityOfLife.ItemAnimation.Value);
            }
            else if (qolPage)
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
            : r == FreeBoatRow ? QualityOfLife.FreeBoat
            : r == WarpRow ? QualityOfLife.WarpButton
            : r == CutscenesRow ? QualityOfLife.SkipCutscenes
            : null;

        private static string OnOff(ConfigEntry<bool> setting) => setting != null && setting.Value ? "ON" : "OFF";

        private static string ScalingLabel(string value) => value == "PartyLevel" ? "PARTY LEVEL" : value.ToUpperInvariant();

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
            // The gamepad's confirm/cancel end editing: joykeys are raw buttons, [0] confirm and [1] cancel ([4]/[5] are
            // Start and Back). Only the pad is read, so the keyboard's C and X still type.
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
                Choice(FreeBoatRow, "Free boat", OnOff(QualityOfLife.FreeBoat));
                Choice(WarpRow, "Warp button", OnOff(QualityOfLife.WarpButton));
                Choice(CutscenesRow, "Skip cutscenes", OnOff(QualityOfLife.SkipCutscenes));
                Choice(AnimationRow, "Item animation", (QualityOfLife.ItemAnimation?.Value ?? "All").ToUpperInvariant());
                Choice(PricesRow, "Shop prices", (QualityOfLife.ShopPrices?.Value ?? "Normal").ToUpperInvariant());
                Choice(ScalingRow, "Enemy scaling", ScalingLabel(QualityOfLife.EnemyScaling?.Value ?? "PartyLevel"));
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

            Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
            Text("|center||size,0.5|" + Safe(shownStatus), 0f, StatusY);
            leaf.transform.localPosition = new Vector3(LabelX + LeafOffset, RowY[row] + LeafRise, 0f);
        }

        // Made once: prompts rebuilt on every redraw replayed their pop-in each time the cursor moved.
        private void BuildArrows()
        {
            arrows = new GameObject("arrows").transform;
            arrows.parent = box;
            arrows.localPosition = Vector3.zero;
            arrows.localEulerAngles = Vector3.zero;
            foreach (int r in qolPage ? new[] { FastTextRow, FreeBoatRow, WarpRow, CutscenesRow, AnimationRow, PricesRow, ScalingRow } : new[] { DifficultyRow, DetectorRow, ModeRow })
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
            // About 8 letters fit between the arrows at 0.75; a longer value shrinks to fit.
            float size = value.Length > 8 ? 0.75f * 8f / value.Length : 0.75f;
            Text("|center||size," + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + value, 2.6f, RowY[r]);
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
