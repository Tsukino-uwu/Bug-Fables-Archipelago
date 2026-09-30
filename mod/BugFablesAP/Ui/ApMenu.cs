using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The Archipelago panel on the main menu, drawn with the game's own box and font, with real typing (paste
    // included). The title screen's input is suspended while it's open (StartMenu.canselect), so C, X, Z and V can be
    // typed.
    internal sealed partial class ApMenu : MonoBehaviour
    {
        private const int Address = 0, PortRow = 1, SlotRow = 2, PasswordRow = 3, ModeRow = 4, DeathLinkRow = 5,
            AchievementsRow = 6, NormalSavesRow = 7, Rows = 8;
        // The Quality of life page: the two buttons side by side on top, then the settings.
        private const int ButtonsRow = 0, FastTextRow = 1, WarpRow = 2, SkipConfirmRow = 3, CutscenesRow = 4,
            AnimationRow = 5, ColorsRow = 6, IconsRow = 7, BackgroundsRow = 8, DetectorRow = 9, SpyRow = 10,
            UncapRow = 11, QolRows = 12;
        // The Gameplay page: how the game plays, under the same two buttons.
        private const int DifficultyRow = 1, ScalingRow = 2, AttackRow = 3, CrystalsRow = 4, AutoSaveRow = 5,
            MedalPricesRow = 6, ExpRow = 7, BerryRow = 8, GameplayRows = 9;
        private enum Page { Main, Qol, Gameplay }
        private Page page;
        // On the buttons row: 0 Reset to defaults (where the cursor lands), 1 Disable all; confirming shows Yes / No
        // there (0 Yes, 1 No).
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
        private Func<string> status;

        private Transform box;
        private Transform help;
        private SpriteRenderer leaf;
        private SpriteRenderer dimmer;
        private Transform arrows;
        private readonly List<KeyValuePair<int, GameObject>> rowArrows = new List<KeyValuePair<int, GameObject>>();
        private GameObject scrollUp, scrollDown;
        private Transform pips;
        private Transform textRoot;
        private int row;
        // A settings page shows VisibleRows at a time from `top`, scrolled as the game's own lists are.
        private int top;
        private bool editing;
        private string edited;
        private string before;
        private int settleFrames;
        private string shownStatus;

        internal static void Show(ManualLogSource logger, StartMenu owner, ConfigEntry<string> server,
            ConfigEntry<string> port, ConfigEntry<string> slot, ConfigEntry<string> password, ConfigEntry<bool> mode,
            Func<string> status)
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
            TextPool.Reserve(log);
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
            box = MainManager.Create9Box(new Vector3(0f, -1f, 10f),
                new Vector2(13.5f, 7.25f), 1, BoxSort, Color.white, false);
            box.parent = transform;
            box.localPosition = new Vector3(0f, -1f, 0f);
            // Attached objects keep their world rotation: reset it, or they stay unturned where the GUI camera is
            // turned (in shops) and are seen edge-on, as nothing.
            box.localEulerAngles = Vector3.zero;
            help = MainManager.Create9Box(new Vector3(0f, 3.75f, 10f),
                new Vector2(12.5f, 2f), 4, HelpSort, Color.white, false);
            help.parent = transform;
            help.localPosition = new Vector3(0f, 3.75f, 0f);
            help.localEulerAngles = Vector3.zero;
            new GameObject("confirmbutton").AddComponent<ButtonSprite>().SetUp(4, -1, "Select / Edit",
                new Vector3(-4.5f, 0.25f), Vector3.one * 0.5f, ButtonSort, help);
            new GameObject("cancelbutton").AddComponent<ButtonSprite>().SetUp(5, -1, "Back",
                new Vector3(0.5f, 0.25f), Vector3.one * 0.5f, ButtonSort, help);
            if (!inGame)
            {
                new GameObject("enterbutton").AddComponent<ButtonSprite>().SetUp(9, -1, "Done typing",
                    new Vector3(-4.5f, -0.5f), Vector3.one * 0.5f, ButtonSort, help);
                MainManager.instance.StartCoroutine(MainManager.SetText(TextSort
                    + "|size,0.55|Ctrl+V paste   Ctrl+C copy", new Vector3(0.5f, -0.45f, 0f), help));
            }
            textRoot = new GameObject("text").transform;
            textRoot.parent = box;
            textRoot.localPosition = Vector3.zero;
            textRoot.localEulerAngles = Vector3.zero;
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
        // A settings page's rows are the game's Settings rows in the same box (MEASURED.md, a Settings row): its first
        // row's text at 2.55, 0.7 apart. Seven show, the help and status lines staying under them; more scroll.
        private const int VisibleRows = 7;
        private const float GameFirstRow = 2.55f, GameRowGap = 0.7f;
        private int PageRows => page == Page.Qol ? QolRows : page == Page.Gameplay ? GameplayRows : Rows;
        private float RowAt(int r) => page == Page.Main ? RowY[r] : GameFirstRow - (r - top) * GameRowGap;
        private bool Shown(int r) => page == Page.Main || (r >= top && r < top + VisibleRows);

        // The game's list rule (MainManager.UpdateList): the view moves only when the cursor passes its edge.
        private void Scroll()
        {
            if (page == Page.Main)
            {
                return;
            }
            if (row < top)
            {
                top = row;
            }
            else if (row >= top + VisibleRows)
            {
                top = row - VisibleRows + 1;
            }
            top = Mathf.Clamp(top, 0, Mathf.Max(0, PageRows - VisibleRows));
        }
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
            int rows = PageRows;
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
                        log.LogInfo("[apmenu] " + page + ": "
                            + (button == 0 ? "all reset to defaults" : "all disabled"));
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
                Scroll();
                MainManager.PlayScrollSound();
                Redraw();
            }
            else if (MainManager.GetKey(1, hold: false))
            {
                row = (row + 1) % rows;
                Scroll();
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
                    case DeathLinkRow:
                    case AchievementsRow:
                    case NormalSavesRow:
                        Step(row, 1);
                        Redraw();
                        break;
                }
            }
        }

        private void Redraw()
        {
            TextPool.Free(textRoot);
            if (arrows == null)
            {
                BuildArrows();
            }
            LayoutArrows();
            shownStatus = status();
            if (page == Page.Qol)
            {
                DrawButtons();
                Choice(FastTextRow, "Fast text", OnOff(QualityOfLife.FastText));
                Choice(WarpRow, "Travel", (QualityOfLife.Travel?.Value ?? "Both").ToUpperInvariant());
                Choice(SkipConfirmRow, "Skip confirm", (QualityOfLife.SkipConfirm?.Value ?? "Off").ToUpperInvariant());
                Choice(CutscenesRow, "Skip cutscenes", OnOff(QualityOfLife.SkipCutscenes));
                Choice(AnimationRow, "Item animation",
                    (QualityOfLife.ItemAnimation?.Value ?? "All").ToUpperInvariant());
                Choice(ColorsRow, "Item colors", (QualityOfLife.ItemColors?.Value ?? "Rarity").ToUpperInvariant());
                Choice(IconsRow, "Archipelago icon", QualityOfLife.IconMode == "OtherGames" ? "OTHER GAMES"
                    : QualityOfLife.IconMode == "AllPlayers" ? "ALL PLAYERS" : "OFF");
                Choice(BackgroundsRow, "Item backgrounds", OnOff(QualityOfLife.ItemBackgrounds));
                Choice(DetectorRow, "Detector", Detector == null || Detector.Value ? "ON" : "OFF");
                Choice(SpyRow, "Spy Specs", OnOff(QualityOfLife.SpySpecs));
                Label(UncapRow, "Uncap FPS");
                DrawPips(new[] { UncapRow }, new[] { Mathf.Max(0, Array.IndexOf(QualityOfLife.UncapValues,
                    QualityOfLife.UncapFps?.Value)) + 1 });
                Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
                Text("|center||size,0.5|Quality of life. Cancel goes back"
                    + (inGame ? " to Settings." : "."), 0f, StatusY);
                PlaceCursor();
                return;
            }
            if (page == Page.Gameplay)
            {
                DrawButtons();
                Choice(DifficultyRow, "Difficulty", (Difficulty?.Value ?? "Normal").ToUpperInvariant());
                Choice(ScalingRow, "Enemy scaling",
                    ScalingLabel(QualityOfLife.EnemyScalingMode?.Value ?? "PartyLevel"));
                Choice(AttackRow, "Attack boost", AttackBoost.Boost != null && AttackBoost.Boost.Value ? "+1" : "OFF");
                Choice(CrystalsRow, "Healing crystals", OnOff(SaveCrystals.AllHeal));
                Choice(AutoSaveRow, "Auto-save", OnOff(AutoSave.Enabled));
                Label(MedalPricesRow, "Medal prices");
                Label(ExpRow, "EXP multiplier");
                Label(BerryRow, "Berry multiplier");
                DrawPips(new[] { MedalPricesRow, ExpRow, BerryRow },
                    new[] { QualityOfLife.MedalPrices?.Value ?? QualityOfLife.FullPrice,
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
            Choice(DeathLinkRow, "DeathLink", OnOff(DeathLinkGame.Enabled));
            Choice(AchievementsRow, "Achievements", Achievements != null && Achievements.Value ? "ON" : "OFF");
            Choice(NormalSavesRow, "Use on normal saves", NormalSaves != null && NormalSaves.Value ? "ON" : "OFF");

            Text("|center||size,0.5|" + Describe(row), 0f, DescribeY);
            Text("|center||size,0.5|" + Safe(shownStatus), 0f, StatusY);
            leaf.transform.localPosition = new Vector3(LabelX + LeafOffset, RowAt(row) + LeafRise, 0f);
        }

        // Made once: prompts rebuilt on every redraw replayed their pop-in each time the cursor moved.
        private void BuildArrows()
        {
            arrows = new GameObject("arrows").transform;
            arrows.parent = box;
            arrows.localPosition = Vector3.zero;
            arrows.localEulerAngles = Vector3.zero;
            foreach (int r in page == Page.Qol ? new[] { FastTextRow, WarpRow, SkipConfirmRow, CutscenesRow,
                    AnimationRow, ColorsRow, IconsRow, BackgroundsRow, DetectorRow, SpyRow, UncapRow }
                : page == Page.Gameplay ? new[] { DifficultyRow, ScalingRow, AttackRow, CrystalsRow, AutoSaveRow,
                    MedalPricesRow, ExpRow, BerryRow }
                : new[] { ModeRow, DeathLinkRow, AchievementsRow, NormalSavesRow })
            {
                for (int side = 0; side < 2; side++)
                {
                    GameObject arrow = MainManager.NewUIObject("arrow" + r + side, arrows,
                        new Vector3(side == 0 ? LeftArrowX : RightArrowX, RowAt(r) + ArrowRise),
                        Vector3.one * RowArrowScale, MainManager.guisprites[1], ButtonSort);
                    arrow.transform.localEulerAngles = new Vector3(0f, 0f, side == 0 ? -90f : 90f);
                    arrow.layer = 5;
                    rowArrows.Add(new KeyValuePair<int, GameObject>(r, arrow));
                }
            }
            if (page != Page.Main)
            {
                scrollUp = ScrollArrow("scrollup", 180f);
                scrollDown = ScrollArrow("scrolldown", 0f);
            }
        }

        // The game's list arrows where its Settings screen puts them in the same box (MEASURED.md, the Settings list's
        // arrows): the top-right and bottom-right corners, guisprites[1] at 1.25, turned for up.
        private static readonly Vector3 ScrollUpAt = new Vector3(6.5f, 3f), ScrollDownAt = new Vector3(6.5f, -3.1f);

        private GameObject ScrollArrow(string name, float turn)
        {
            GameObject arrow = MainManager.NewUIObject(name, arrows, Vector3.zero, Vector3.one * 1.25f,
                MainManager.guisprites[1], ButtonSort);
            arrow.transform.localEulerAngles = new Vector3(0f, 0f, turn);
            arrow.layer = 5;
            return arrow;
        }

        // Each row's arrows follow the scroll; the list arrows show when rows are hidden above or below.
        private void LayoutArrows()
        {
            foreach (KeyValuePair<int, GameObject> pair in rowArrows)
            {
                bool shown = Shown(pair.Key);
                pair.Value.SetActive(shown);
                Vector3 at = pair.Value.transform.localPosition;
                pair.Value.transform.localPosition = new Vector3(at.x, RowAt(pair.Key) + ArrowRise, at.z);
            }
            if (scrollUp != null)
            {
                scrollUp.SetActive(top > 0);
                scrollUp.transform.localPosition = ScrollUpAt;
            }
            if (scrollDown != null)
            {
                scrollDown.SetActive(top + VisibleRows < PageRows);
                scrollDown.transform.localPosition = ScrollDownAt;
            }
        }

        // The volume rows' look (MainManager.ShowItemList, type 17): ten pips between the arrows, the lit ones the
        // yellow hexagon, at the game's own place and size.

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
                if (!Shown(rows[i]))
                {
                    continue;
                }
                for (int p = 0; p < Multipliers.Max; p++)
                {
                    bool on = p < lit[i];
                    GameObject pip = MainManager.NewUIObject("pip", pips,
                        new Vector3(GamePipX + 0.4f * p, RowAt(rows[i]) + ArrowRise),
                        Vector3.one * (on ? 1f / 3f : 1f / 4f),
                        MainManager.guisprites[on ? 42 : 59], ButtonSort + p);
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
            if (!Shown(ButtonsRow))
            {
                return;
            }
            Text("|size,0.8|" + (row == ButtonsRow && button == 0 ? "|color,1|" : "") + "Reset to defaults",
                LabelX, RowAt(ButtonsRow));
            Text("|center||size,0.8|" + (row == ButtonsRow && button == 1 ? "|color,1|" : "") + "Disable all",
                ValueCenter, RowAt(ButtonsRow));
        }

        private void PlaceCursor()
        {
            if (confirming)
            {
                DrawPopup();
                return;
            }
            ClosePopup();
            float leafX = row == ButtonsRow && button == 1 ? ValueCenter - DisableHalfWidth : LabelX;
            leaf.transform.localPosition = new Vector3(leafX + LeafOffset, RowAt(row) + LeafRise, 0f);
        }

        private void DrawPopup()
        {
            if (popup == null)
            {
                popup = MainManager.Create9Box(PopupAt + new Vector3(0f, 0f, 10f),
                    new Vector2(9f, 2.75f), 4, PopupSort, Color.white, false);
                popup.parent = transform;
                popup.localPosition = PopupAt;
                popup.localEulerAngles = Vector3.zero;
                popupText = new GameObject("popuptext").transform;
                popupText.parent = popup;
                popupText.localPosition = Vector3.zero;
                popupText.localEulerAngles = Vector3.zero;
            }
            TextPool.Free(popupText);
            string what = page == Page.Qol ? "Quality of life" : "Gameplay";
            string question = button == 0 ? "Put every " + what + " setting back to its default?"
                : "Turn every " + what + " setting off?";
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|center||size,0.55|" + question,
                new Vector3(0f, 0.45f, 0f), popupText));
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|size,0.8|"
                + (answer == 0 ? "|color,1|" : "") + "Yes", new Vector3(YesX, AnswerY, 0f), popupText));
            MainManager.instance.StartCoroutine(MainManager.SetText(PopupTextSort + "|size,0.8|"
                + (answer == 1 ? "|color,1|" : "") + "No", new Vector3(NoX, AnswerY, 0f), popupText));
            // The leaf lives under the panel's box: place it by world position on the picked answer.
            leaf.sortingOrder = PopupCursorSort;
            leaf.transform.position = popup.TransformPoint(new Vector3((answer == 0 ? YesX : NoX) + LeafOffset,
                AnswerY + PopupLeafRise, 0f));
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
        // A settings page's row is the game's Settings row (MEASURED.md, a Settings row): arrows at 1 on 0.4 and 5.4,
        // the value centred between them, ten pips from 1.1, 0.4 apart. The main page keeps its narrower row.
        private const float GameArrowLeftX = 0.4f, GameArrowRightX = 5.4f, GameValueX = 2.9f, GamePipX = 1.1f;
        private bool GameRow => page != Page.Main;
        private float LeftArrowX => GameRow ? GameArrowLeftX : ArrowLeftX;
        private float RightArrowX => GameRow ? GameArrowRightX : ArrowRightX;
        private float RowArrowScale => GameRow ? 1f : ArrowScale;
        private float ValueCenter => GameRow ? GameValueX : ValueCenterX;

        private void Choice(int r, string label, string value)
        {
            if (!Shown(r))
            {
                return;
            }
            Label(r, label);
            // About 8 letters fit between the main page's arrows at 0.75, 11 between the game's; longer shrinks to fit.
            int fits = GameRow ? 11 : 8;
            float size = value.Length > fits ? 0.75f * fits / value.Length : 0.75f;
            Text("|center||size," + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + value,
                ValueCenter, RowAt(r));
        }

        private void Label(int r, string label)
        {
            if (!Shown(r))
            {
                return;
            }
            string colour = editing && r == row ? "|color,1|" : "";
            // About 15 letters fit before the arrows at 0.8; a longer label shrinks to fit.
            float size = label.Length > 15 ? 0.8f * 15f / label.Length : 0.8f;
            Text("|size," + size.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + colour + label,
                LabelX, RowAt(r));
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
            Text("|size,0.65|" + (typing ? "|color,1|" : "") + shown + (typing ? "_" : ""), ValueX, RowAt(r));
        }

        private void Text(string text, float x, float y)
        {
            MainManager.instance.StartCoroutine(MainManager.SetText(TextSort + text, new Vector3(x, y, 0f), textRoot));
        }
    }
}
