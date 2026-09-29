using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Gameplay page's Attack boost: Off / +1, off by default. +1 on each hit a party member lands, where and when
    // the game adds its own +1 for the member in front; the save's attack stat is never touched, only the
    // medals screen's attack number shows the +1.
    internal static class AttackBoost
    {
        internal static ConfigEntry<bool> Boost;

        private static Func<bool> settingsOn;
        private static ManualLogSource log;
        private static AccessTools.FieldRef<BattleControl, bool> demoMode;
        private static AccessTools.FieldRef<PauseMenu, DynamicFont[]> statText;
        private static AccessTools.FieldRef<PauseMenu, int> menuOption;
        // PauseMenu.windowid of the medals screen, whose second stat line is the chosen member's attack.
        private const int MedalsWindow = 2;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on)
        {
            log = logger;
            settingsOn = on;
            Boost = config.Bind("Gameplay", "AttackBoost", false,
                "On: each hit a party member lands does 1 more damage, as if their attack were 1 higher. The save's stats "
                + "don't change. Switch it on the Gameplay page.");
            var demo = AccessTools.Field(typeof(BattleControl), "demomode");
            if (demo == null)
            {
                log.LogError("[boost] NOT installed (BattleControl.demomode not found); the attack boost does nothing.");
                return;
            }
            demoMode = AccessTools.FieldRefAccess<BattleControl, bool>(demo);
            if (!Hooks.Install(typeof(AttackBoost), "boost", "the attack boost does nothing"))
            {
                return;
            }
            var texts = AccessTools.Field(typeof(PauseMenu), "dynamictext");
            var picked = AccessTools.Field(typeof(PauseMenu), "option");
            if (texts == null || picked == null)
            {
                log.LogWarning($"[boost] the medals screen won't show the +1 (dynamictext {texts != null}, option {picked != null})");
            }
            else
            {
                statText = AccessTools.FieldRefAccess<PauseMenu, DynamicFont[]>(texts);
                menuOption = AccessTools.FieldRefAccess<PauseMenu, int>(picked);
                if (!Hooks.Install(typeof(Stats), "boost", "the medals screen won't show the +1"))
                {
                    statText = null;
                }
            }
            log.LogInfo("[boost] installed on BattleControl.CalculateBaseDamage"
                + (statText != null ? " and PauseMenu.UpdateDynamicText" : ""));
        }

        // The game's own player bonuses skip Raw hits and the demo battle; so does this one.
        [HarmonyPatch(typeof(BattleControl), "CalculateBaseDamage")]
        [HarmonyPrefix]
        private static void BeforeBaseDamage(BattleControl __instance, MainManager.BattleData? attacker,
            ref int basevalue,
            BattleControl.AttackProperty? property)
        {
            if (Boost == null || !Boost.Value || settingsOn == null || !settingsOn() || attacker == null
                || attacker.Value.battleentity == null || !attacker.Value.battleentity.CompareTag("Player")
                || property == BattleControl.AttackProperty.Raw || demoMode(__instance))
            {
                return;
            }
            basevalue++;
        }

        // The game rewrites the medals screen's stats every frame; the attack line then shows the boosted value.
        private static class Stats
        {
            [HarmonyPatch(typeof(PauseMenu), "UpdateDynamicText")]
            [HarmonyPostfix]
            private static void AfterStatText(PauseMenu __instance)
            {
                if (Boost == null || !Boost.Value || settingsOn == null || !settingsOn()
                    || __instance.windowid != MedalsWindow)
                {
                    return;
                }
                DynamicFont[] text = statText(__instance);
                int member = menuOption(__instance);
                if (text == null || text.Length < 6 || text[1] == null || member < 0
                    || member >= MainManager.instance.playerdata.Length)
                {
                    return;
                }
                text[1].text = (MainManager.instance.playerdata[member].atk + 1).ToString().PadLeft(2, '0');
            }
        }
    }
}
