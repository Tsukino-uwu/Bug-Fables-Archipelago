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
        private const int Address = 0, PortRow = 1, SlotRow = 2, PasswordRow = 3, ModeRow = 4, AchievementsRow = 5, NormalSavesRow = 6,
            Rows = 7;
        // The Quality of life page: the two buttons side by side on top, then the settings.
        private const int ButtonsRow = 0, FastTextRow = 1, WarpRow = 2, SkipConfirmRow = 3, CutscenesRow = 4, AnimationRow = 5, DetectorRow = 6,
            QolRows = 7;
        // The Gameplay page: how the game plays, under the same two buttons.
        private const int DifficultyRow = 1, ScalingRow = 2, MedalPricesRow = 3, ExpRow = 4, BerryRow = 5, GameplayRows = 6;
        private enum Page { Main, Qol, Gameplay }
        private Page page;
        // On the buttons row: 0 Reset to defaults (where the cursor lands), 1 Disable all; confirming shows Yes / No there (0 Yes, 1 No).
        private int button;
        private bool confirming;
        private int answer;
        // The Yes / No box over the page while confirming.
        private Transform popup;
        private Transform popupText;

        internal static readonly string[] Difficulties = { "Normal", "Hard", "Hardest" };
        internal static ConfigEntry<string> Difficulty;
        internal static ConfigEntry<bool> Detector;
        internal static ConfigEntry<bool> Achievements;
        internal static ConfigEntry<bool> NormalSaves;

        internal static ApMenu Open;
        // Opened from the pause menu's Settings: only the Quality of life or Gameplay page, over a hidden pause menu.
        private bool inGame;

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
        private Transform pips;
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

        // From the pause menu's Settings (InGameSettings): one settings page, no connection page.
        internal static void ShowInGame(ManualLogSource logger, bool gameplay)
        {
            if (Open != null)
            {
                return;
            }
            log = logger;
            var go = new GameObject("ArchipelagoMenu");
            ApMenu menu = go.AddComponent<ApMenu>();
            menu.inGame = true;
            menu.status = () => "";
            menu.page = gameplay ? Page.Gameplay : Page.Qol;
            menu.row = ButtonsRow;
            Open = menu;
            menu.Build();
        }

        private void Build()
        {
            if (inGame)
            {
                // Only the Settings screen's two boxes (its list lives inside them) are hidden: the pause menu's own
                // background stays, as going from the pause menu to Settings does. Its input is held by InGameSettings.
                SetSettingsBoxes(false);
                if (MainManager.instance.cursor != null)
                {
                    MainManager.instance.cursor.enabled = false;
                }
            }
            else
            {
                Traverse.Create(owner).Field("canselect").SetValue(false);
                // Hide the title screen under the panel, as the game does for the file select.
                SetTitleVisible(false);
            }
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
            if (!inGame)
            {
                new GameObject("enterbutton").AddComponent<ButtonSprite>().SetUp(9, -1, "Done typing", new Vector3(-4.5f, -0.5f), Vector3.one * 0.5f, ButtonSort, help);
                MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + "|size,0.55|Ctrl+V paste   Ctrl+C copy", new Vector3(0.5f, -0.45f, 0f), help));
            }
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
            if (inGame)
            {
                // Back to the Settings list as it was; the cooldown keeps the closing press from acting there too.
                SetSettingsBoxes(true);
                if (MainManager.instance.cursor != null)
                {
                    MainManager.instance.cursor.enabled = true;
                }
                MainManager.instance.inputcooldown = 10f;
                Open = null;
                Destroy(gameObject);
                log.LogInfo("[apmenu] closed (back to Settings)");
                return;
            }
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
        // Centred on a row's text, as the Settings screen's leaf is; the Yes / No box's answers sit differently.
        private const float LeafRise = 0.05f, PopupLeafRise = 0.15f;
        private const float ValueX = -1.9f;

        private static void SetSettingsBoxes(bool visible)
        {
            if (MainManager.pausemenu == null)
            {
                return;
            }
            DialogueAnim[] boxes = Traverse.Create(MainManager.pausemenu).Field("boxes").GetValue<DialogueAnim[]>();
            foreach (DialogueAnim settingsBox in boxes ?? new DialogueAnim[0])
            {
                if (settingsBox != null)
                {
                    settingsBox.gameObject.SetActive(visible);
                }
            }
        }

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
                if (dimmer != null && !inGame)
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
            int rows = page == Page.Qol ? QolRows : page == Page.Gameplay ? GameplayRows : Rows;
            bool confirm = MainManager.GetKey(4, hold: false) || Input.GetKeyDown(KeyCode.Return);
            bool sideways = MainManager.GetKey(2, hold: false) || MainManager.GetKey(3, hold: false);
            bool cancel = MainManager.GetKey(5, hold: false) || Input.GetKeyDown(KeyCode.Escape);
            if (confirming)
            {
                if (sideways)
                {
                    answer = 1 - answer;
                    MainManager.PlayScrollSound();
                    DrawPopup(); // the page under it is unchanged
                }
                else if (confirm)
                {
                    confirming = false;
                    if (answer == 0)
                    {
                        MainManager.PlaySound("Confirm", -1);
                        if (page == Page.Qol)
                        {
                            if (button == 0)
                            {
                                QualityOfLife.ResetAll();
                            }
                            else
                            {
                                QualityOfLife.DisableAll();
                            }
                        }
                        else
                        {
                            GameplayAll(reset: button == 0);
                        }
                        log.LogInfo("[apmenu] " + page + ": " + (button == 0 ? "all reset to defaults" : "all disabled"));
                    }
                    else
                    {
                        MainManager.PlaySound("Cancel", 10);
                    }
                    Redraw();
                }
                else if (cancel)
                {
                    confirming = false;
                    MainManager.PlaySound("Cancel", 10);
                    Redraw();
                }
                return;
            }
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
            else if (page != Page.Main && row == ButtonsRow && sideways)
            {
                button = 1 - button;
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (IsChoice(row) && sideways)
            {
                Step(row, MainManager.GetKey(3, hold: false) ? 1 : -1);
                Redraw();
            }
            else if (cancel)
            {
                MainManager.PlaySound("Cancel", 10);
                Close();
            }
            else if (confirm && page != Page.Main && row == ButtonsRow)
            {
                // No is chosen first, so a stray press never wipes the settings.
                MainManager.PlaySound("Confirm", -1);
                confirming = true;
                answer = 1;
                Redraw();
            }
            else if (confirm && page != Page.Main)
            {
                Step(row, 1);
                Redraw();
            }
            else if (confirm)
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
                    case AchievementsRow:
                    case NormalSavesRow:
                        Step(row, 1);
                        Redraw();
                        break;
                }
            }
        }

        private string Describe(int r)
        {
            if (page == Page.Qol)
            {
                switch (r)
                {
                    case ButtonsRow:
                        return confirming ? "" : button == 0 ? "Puts every setting on this page back to its default." : "Turns every setting on this page off.";
                    case FastTextRow: return "Dialogue text is instant, but still requires a button press to proceed.";
                    case WarpRow:
                        switch (QualityOfLife.Travel?.Value)
                        {
                            case "Off": return "No travel buttons in the pause menu.";
                            case "Warp": return "A pause menu button back to where the game started.";
                            case "Map": return "A pause menu map: pick an area you've been to and travel there.";
                            default: return "Both pause menu buttons: Warp to the start, and Map travel.";
                        }
                    case SkipConfirmRow:
                        switch (QualityOfLife.SkipConfirm?.Value)
                        {
                            case "Warp": return "Warp goes at once, without asking Yes / No first.";
                            case "Map": return "Map travel goes at once, without asking Yes / No first.";
                            case "Both": return "Warp and Map travel go at once, without asking Yes / No first.";
                            default: return "Warp and Map travel ask Yes / No first.";
                        }
                    case CutscenesRow: return "Skips the intro and scenes you don't need to watch.";
                    case AnimationRow:
                        switch (QualityOfLife.ItemAnimation?.Value)
                        {
                            case "Progression": return "Only items from others that unlock something are held up.";
                            case "Off": return "Items from others arrive without being held up.";
                            default: return "Every item from another player is held up as it arrives.";
                        }
                    case DetectorRow: return "Acts like the Detector medal is always equipped, to find hidden items.";
                    default: return "";
                }
            }
            if (page == Page.Gameplay)
            {
                switch (r)
                {
                    case ButtonsRow:
                        return confirming ? "" : button == 0 ? "Puts every setting on this page back to its default." : "Turns every setting on this page off.";
                    case DifficultyRow:
                        switch (Difficulty?.Value)
                        {
                            case "Hard": return "As if the Hard Mode medal were on: tougher enemies. Checks stay the same.";
                            case "Hardest": return "As the HARDEST code: toughest enemies. Checks stay the same.";
                            default: return "Enemies as the game makes them. Checks stay the same.";
                        }
                    case ScalingRow:
                        switch (QualityOfLife.EnemyScaling?.Value)
                        {
                            case "Off": return "Enemies keep their own stats, as in vanilla.";
                            case "Artifacts": return "Enemies grow with artifacts found; levelling ahead makes it easier.";
                            default: return "Enemies match your level, so every area plays fair in any order.";
                        }
                    case MedalPricesRow:
                    {
                        int tenths = QualityOfLife.MedalPrices?.Value ?? QualityOfLife.FullPrice;
                        return tenths >= QualityOfLife.FullPrice ? "Medal shops charge their normal price."
                            : tenths <= 0 ? "Medal shops charge nothing."
                            : "Medal shops charge " + tenths * 10 + "% of their price.";
                    }
                    case ExpRow:
                        return Multipliers.Exp == null || Multipliers.Exp.Value <= 1 ? "Enemies give their normal EXP."
                            : "Enemies give " + Multipliers.Exp.Value + "x EXP. A battle still gives at most a level's worth.";
                    case BerryRow:
                        return Multipliers.Berries == null || Multipliers.Berries.Value <= 1 ? "Berries you pick up count as usual."
                            : "Berries you pick up count " + Multipliers.Berries.Value + "x. Never berries that come from a check.";
                    default: return "";
                }
            }
            switch (r)
            {
                case Address: return "The server the room is hosted on, e.g. archipelago.gg.";
                case PortRow: return "The room's port, e.g. 38281.";
                case SlotRow: return "Your player slot name.";
                case PasswordRow: return "The room's password, if it has one.";
                case ModeRow: return "Turns Archipelago on or off. While on, normal saves are never touched.";
                case AchievementsRow:
                    return Achievements != null && Achievements.Value
                        ? "Steam achievements unlock as usual. This only affects Steam, not Archipelago."
                        : "Steam achievements aren't unlocked while Archipelago is on. Only affects Steam.";
                case NormalSavesRow:
                    return NormalSaves != null && NormalSaves.Value
                        ? "Quality of life and Gameplay also apply with Archipelago off."
                        : "Quality of life and Gameplay apply only with Archipelago on.";
                default: return "";
            }
        }

        // The settings screen's value-change sound; the main menu has no pause menu, so the global sound volume.
        private static void ChangeSound()
        {
            MainManager.PlaySound(Resources.Load<AudioClip>("Audio/Sounds/Confirm0"), 10, 1f, 1f);
            MainManager.sounds[10].volume = MainManager.pausemenu != null ? MainManager.pausemenu.svolume : MainManager.soundvolume;
        }

        private bool IsChoice(int r) => page == Page.Main ? r == ModeRow || r == AchievementsRow || r == NormalSavesRow : r != ButtonsRow;

        private static void Cycle(ConfigEntry<string> entry, string[] values, int by)
        {
            int at = Array.IndexOf(values, entry.Value);
            entry.Value = values[((at < 0 ? 0 : at) + by + values.Length) % values.Length];
            log.LogInfo("[apmenu] " + entry.Definition.Key + ": " + entry.Value);
        }

        private void Step(int r, int by)
        {
            ChangeSound();
            if (page == Page.Qol)
            {
                if (r == AnimationRow && QualityOfLife.ItemAnimation != null)
                {
                    Cycle(QualityOfLife.ItemAnimation, QualityOfLife.ItemAnimations, by);
                }
                else if (r == WarpRow && QualityOfLife.Travel != null)
                {
                    Cycle(QualityOfLife.Travel, QualityOfLife.TravelValues, by);
                }
                else if (r == SkipConfirmRow && QualityOfLife.SkipConfirm != null)
                {
                    Cycle(QualityOfLife.SkipConfirm, QualityOfLife.TravelValues, by);
                }
                else
                {
                    ConfigEntry<bool> setting = QolSetting(r);
                    if (setting != null)
                    {
                        setting.Value = !setting.Value;
                        log.LogInfo("[apmenu] " + setting.Definition.Key + ": " + (setting.Value ? "On" : "Off"));
                    }
                }
            }
            else if (page == Page.Gameplay)
            {
                if (r == DifficultyRow && Difficulty != null)
                {
                    Cycle(Difficulty, Difficulties, by);
                }
                else if (r == ScalingRow && QualityOfLife.EnemyScaling != null)
                {
                    Cycle(QualityOfLife.EnemyScaling, EnemyScaling.Modes, by);
                }
                else if (r == MedalPricesRow && QualityOfLife.MedalPrices != null)
                {
                    Multipliers.StepBy(QualityOfLife.MedalPrices, by, 0, QualityOfLife.FullPrice);
                }
                else if (r == ExpRow && Multipliers.Exp != null)
                {
                    Multipliers.StepBy(Multipliers.Exp, by);
                }
                else if (r == BerryRow && Multipliers.Berries != null)
                {
                    Multipliers.StepBy(Multipliers.Berries, by);
                }
            }
            else if (r == ModeRow)
            {
                MenuToggle.SetMode(owner, !mode.Value);
            }
            else if (r == AchievementsRow && Achievements != null)
            {
                Achievements.Value = !Achievements.Value;
                log.LogInfo("[apmenu] Achievements: " + (Achievements.Value ? "On" : "Off"));
            }
            else if (r == NormalSavesRow && NormalSaves != null)
            {
                NormalSaves.Value = !NormalSaves.Value;
                log.LogInfo("[apmenu] Use on normal saves: " + (NormalSaves.Value ? "On" : "Off"));
            }
        }

        private static ConfigEntry<bool> QolSetting(int r) =>
            r == FastTextRow ? QualityOfLife.FastText
            : r == CutscenesRow ? QualityOfLife.SkipCutscenes
            : r == DetectorRow ? Detector
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
            TextPool.Free(textRoot);
            if (arrows == null)
            {
                BuildArrows();
            }
            shownStatus = status();
            if (page == Page.Qol)
            {
                DrawButtons();
                Choice(FastTextRow, "Fast text", OnOff(QualityOfLife.FastText));
                Choice(WarpRow, "Travel", (QualityOfLife.Travel?.Value ?? "Both").ToUpperInvariant());
                Choice(SkipConfirmRow, "Skip confirm", (QualityOfLife.SkipConfirm?.Value ?? "Off").ToUpperInvariant());
                Choice(CutscenesRow, "Skip cutscenes", OnOff(QualityOfLife.SkipCutscenes));
                Choice(AnimationRow, "Item animation", (QualityOfLife.ItemAnimation?.Value ?? "All").ToUpperInvariant());
                Choice(DetectorRow, "Detector", Detector == null || Detector.Value ? "ON" : "OFF");
                Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
                Text("|center||size,0.5|Quality of life. Cancel goes back" + (inGame ? " to Settings." : "."), 0f, StatusY);
                PlaceCursor();
                return;
            }
            if (page == Page.Gameplay)
            {
                DrawButtons();
                Choice(DifficultyRow, "Difficulty", (Difficulty?.Value ?? "Normal").ToUpperInvariant());
                Choice(ScalingRow, "Enemy scaling", ScalingLabel(QualityOfLife.EnemyScaling?.Value ?? "PartyLevel"));
                Label(MedalPricesRow, "Medal prices");
                Label(ExpRow, "EXP multiplier");
                Label(BerryRow, "Berry multiplier");
                DrawPips(new[] { MedalPricesRow, ExpRow, BerryRow }, new[] { QualityOfLife.MedalPrices?.Value ?? QualityOfLife.FullPrice,
                    Multipliers.Exp?.Value ?? 1, Multipliers.Berries?.Value ?? 1 });
                Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
                Text("|center||size,0.5|Gameplay. Cancel goes back" + (inGame ? " to Settings." : "."), 0f, StatusY);
                PlaceCursor();
                return;
            }
            string pw = editing && row == PasswordRow ? edited : new string('*', password.Value.Length);
            Row(Address, "Address", editing && row == Address ? edited : server.Value);
            Row(PortRow, "Port", editing && row == PortRow ? edited : port.Value);
            Row(SlotRow, "Slot", editing && row == SlotRow ? edited : slot.Value);
            Row(PasswordRow, "Password", pw);

            Choice(ModeRow, "Archipelago", mode.Value ? "ENABLED" : "DISABLED");
            Choice(AchievementsRow, "Achievements", Achievements != null && Achievements.Value ? "ON" : "OFF");
            Choice(NormalSavesRow, "Use on normal saves", NormalSaves != null && NormalSaves.Value ? "ON" : "OFF");

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
            foreach (int r in page == Page.Qol ? new[] { FastTextRow, WarpRow, SkipConfirmRow, CutscenesRow, AnimationRow, DetectorRow }
                : page == Page.Gameplay ? new[] { DifficultyRow, ScalingRow, MedalPricesRow, ExpRow, BerryRow } : new[] { ModeRow, AchievementsRow, NormalSavesRow })
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

        // The volume rows' look (MainManager.ShowItemList, type 17): ten pips between the arrows, the lit ones the yellow
        // hexagon, scaled from the Settings row's spacing to this panel's narrower one.
        private const float PipScale = 0.68f;

        private void DrawPips(int[] rows, int[] lit)
        {
            if (pips != null)
            {
                Destroy(pips.gameObject);
            }
            pips = new GameObject("pips").transform;
            pips.parent = box;
            pips.localPosition = Vector3.zero;
            pips.localEulerAngles = Vector3.zero;
            for (int i = 0; i < rows.Length; i++)
            {
                for (int p = 0; p < Multipliers.Max; p++)
                {
                    bool on = p < lit[i];
                    GameObject pip = MainManager.NewUIObject("pip", pips,
                        new Vector3(ArrowLeftX + (0.7f + 0.4f * p) * PipScale, RowY[rows[i]] + ArrowRise),
                        Vector3.one * (on ? 1f / 3f : 1f / 4f) * PipScale, MainManager.guisprites[on ? 42 : 59], ButtonSort + p);
                    if (on)
                    {
                        pip.GetComponent<SpriteRenderer>().color = Color.yellow;
                    }
                    pip.layer = 5;
                }
            }
        }

        // The Yes / No box: the controls box type, over the page, its text and the leaf above it.
        private const int PopupSort = 30, PopupCursorSort = 45;
        private const string PopupTextSort = "|sort,40|";
        private static readonly Vector3 PopupAt = new Vector3(0f, -0.25f, 0f);
        private const float YesX = -2.1f, NoX = 1.1f, AnswerY = -0.45f;

        // The two buttons side by side at the top of a settings page; confirming one opens the Yes / No box.
        private void DrawButtons()
        {
            Text("|size,0.8|" + (row == ButtonsRow && button == 0 ? "|color,1|" : "") + "Reset to defaults", LabelX, RowY[ButtonsRow]);
            Text("|center||size,0.8|" + (row == ButtonsRow && button == 1 ? "|color,1|" : "") + "Disable all", ValueCenterX, RowY[ButtonsRow]);
        }

        private void PlaceCursor()
        {
            if (confirming)
            {
                DrawPopup();
                return;
            }
            ClosePopup();
            float leafX = row == ButtonsRow && button == 1 ? ValueCenterX - DisableHalfWidth : LabelX;
            leaf.transform.localPosition = new Vector3(leafX + LeafOffset, RowY[row] + LeafRise, 0f);
        }

        // The Gameplay page's two buttons: every row to its plain value, or back to its default.
        private static void GameplayAll(bool reset)
        {
            foreach (ConfigEntryBase setting in new ConfigEntryBase[] { Difficulty, QualityOfLife.EnemyScaling, QualityOfLife.MedalPrices, Multipliers.Exp, Multipliers.Berries })
            {
                if (setting != null && reset)
                {
                    setting.BoxedValue = setting.DefaultValue;
                }
            }
            if (reset)
            {
                return;
            }
            if (Difficulty != null)
            {
                Difficulty.Value = "Normal";
            }
            if (QualityOfLife.EnemyScaling != null)
            {
                QualityOfLife.EnemyScaling.Value = "Off";
            }
            if (QualityOfLife.MedalPrices != null)
            {
                QualityOfLife.MedalPrices.Value = QualityOfLife.FullPrice;
            }
            foreach (ConfigEntry<int> multiplier in new[] { Multipliers.Exp, Multipliers.Berries })
            {
                if (multiplier != null)
                {
                    multiplier.Value = 1;
                }
            }
        }

        private void DrawPopup()
        {
            if (popup == null)
            {
                popup = MainManager.Create9Box(PopupAt + new Vector3(0f, 0f, 10f), new Vector2(9f, 2.75f), 4, PopupSort, Color.white, false);
                popup.parent = transform;
                popup.localPosition = PopupAt;
                popupText = new GameObject("popuptext").transform;
                popupText.parent = popup;
                popupText.localPosition = Vector3.zero;
            }
            TextPool.Free(popupText);
            string what = page == Page.Qol ? "Quality of life" : "Gameplay";
            string question = button == 0 ? "Put every " + what + " setting back to its default?" : "Turn every " + what + " setting off?";
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|center||size,0.55|" + question, new Vector3(0f, 0.45f, 0f), popupText));
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|size,0.8|" + (answer == 0 ? "|color,1|" : "") + "Yes", new Vector3(YesX, AnswerY, 0f), popupText));
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|size,0.8|" + (answer == 1 ? "|color,1|" : "") + "No", new Vector3(NoX, AnswerY, 0f), popupText));
            // The leaf lives under the panel's box: place it by world position on the picked answer.
            leaf.sortingOrder = PopupCursorSort;
            leaf.transform.position = popup.TransformPoint(new Vector3((answer == 0 ? YesX : NoX) + LeafOffset, AnswerY + PopupLeafRise, 0f));
        }

        private void ClosePopup()
        {
            if (popup != null)
            {
                Destroy(popup.gameObject);
                popup = null;
                popupText = null;
            }
            leaf.sortingOrder = CursorSort;
        }

        // The second button's label, centred over the values like them; its half width (about 2.3 at 0.8, measured on
        // screen) puts the leaf at its left edge.
        private const float ValueCenterX = 2.6f, DisableHalfWidth = 1.15f;
        private const float ArrowLeftX = 0.9f, ArrowRightX = 4.3f, ArrowRise = 0.15f, ArrowScale = 0.75f;

        private void Choice(int r, string label, string value)
        {
            Label(r, label);
            // About 8 letters fit between the arrows at 0.75; a longer value shrinks to fit.
            float size = value.Length > 8 ? 0.75f * 8f / value.Length : 0.75f;
            Text("|center||size," + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + value, ValueCenterX, RowY[r]);
        }

        private void Label(int r, string label)
        {
            string colour = editing && r == row ? "|color,1|" : "";
            // About 15 letters fit before the arrows at 0.8; a longer label shrinks to fit.
            float size = label.Length > 15 ? 0.8f * 15f / label.Length : 0.8f;
            Text("|size," + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + colour + label, LabelX, RowY[r]);
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
