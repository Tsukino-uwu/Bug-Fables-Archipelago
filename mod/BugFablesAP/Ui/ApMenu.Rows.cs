using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    internal sealed partial class ApMenu
    {
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
                    case CutscenesRow: return "Skips or speeds up scenes you don't need to watch.";
                    case AnimationRow:
                        switch (QualityOfLife.ItemAnimation?.Value)
                        {
                            case "Progression": return "Only items from others that unlock something are held up.";
                            case "Off": return "Items from others arrive without being held up.";
                            default: return "Every item from another player is held up as it arrives.";
                        }
                    case ColorsRow:
                        return QualityOfLife.RarityColors ? "Items coloured like loot: green, blue, purple, red for traps."
                            : QualityOfLife.ApColors ? "Items coloured as in Archipelago's own client."
                            : "The game's own colours.";
                    case IconsRow:
                        switch (QualityOfLife.IconMode)
                        {
                            case "AllPlayers": return "Every item that isn't yours shows the Archipelago icon.";
                            case "Off": return "Items that aren't yours look like the game's own item there.";
                            default: return "Other games' items show the Archipelago icon.";
                        }
                    case BackgroundsRow:
                        return QualityOfLife.ItemBackgrounds == null || QualityOfLife.ItemBackgrounds.Value
                            ? "Items show how important they are before you take them."
                            : "Items show no backdrop until you take them.";
                    case DetectorRow: return "Acts like the Detector medal is always equipped, to find hidden items.";
                    case UncapRow:
                        return FrameRate.Cap == 0 ? "Experimental. Off: the game's own FPS setting (30 or 60)."
                            : $"Experimental. {FrameRate.Cap} FPS, smooth motion; plays as at 60.";
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
                        switch (QualityOfLife.EnemyScalingMode?.Value)
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
                else if (r == ColorsRow && QualityOfLife.ItemColors != null)
                {
                    Cycle(QualityOfLife.ItemColors, QualityOfLife.ItemColorValues, by);
                }
                else if (r == IconsRow && QualityOfLife.ItemIcons != null)
                {
                    Cycle(QualityOfLife.ItemIcons, QualityOfLife.ItemIconValues, by);
                }
                else if (r == WarpRow && QualityOfLife.Travel != null)
                {
                    Cycle(QualityOfLife.Travel, QualityOfLife.TravelValues, by);
                }
                else if (r == SkipConfirmRow && QualityOfLife.SkipConfirm != null)
                {
                    Cycle(QualityOfLife.SkipConfirm, QualityOfLife.TravelValues, by);
                }
                else if (r == UncapRow && QualityOfLife.UncapFps != null)
                {
                    Cycle(QualityOfLife.UncapFps, QualityOfLife.UncapValues, by);
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
                else if (r == ScalingRow && QualityOfLife.EnemyScalingMode != null)
                {
                    Cycle(QualityOfLife.EnemyScalingMode, EnemyScaling.Modes, by);
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
            : r == BackgroundsRow ? QualityOfLife.ItemBackgrounds
            : r == DetectorRow ? Detector
            : null;

        private static string OnOff(ConfigEntry<bool> setting) => setting != null && setting.Value ? "ON" : "OFF";

        private static string ScalingLabel(string value) => value == "PartyLevel" ? "PARTY LEVEL" : value.ToUpperInvariant();

        // The Gameplay page's two buttons: every row to its plain value, or back to its default.
        private static void GameplayAll(bool reset)
        {
            foreach (ConfigEntryBase setting in new ConfigEntryBase[] { Difficulty, QualityOfLife.EnemyScalingMode, QualityOfLife.MedalPrices, Multipliers.Exp, Multipliers.Berries })
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
            if (QualityOfLife.EnemyScalingMode != null)
            {
                QualityOfLife.EnemyScalingMode.Value = "Off";
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
    }
}
