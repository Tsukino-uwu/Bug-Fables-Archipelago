using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Gameplay page's EXP and berry multipliers, 1x to 10x (a pip each, like the volume bars).
    // EXP: each defeated enemy's share, after the game's own per-enemy caps; the game still caps a battle at a level's worth.
    // Berries: only those picked up in the world (lying there or dropped after a fight), never berries from a check.
    internal static class Multipliers
    {
        internal const int Min = 1, Max = 10;
        internal static ConfigEntry<int> Exp;
        internal static ConfigEntry<int> Berries;

        private static Func<bool> settingsOn;
        private static ManualLogSource log;

        internal static void Enable(ManualLogSource logger, ConfigFile config, Func<bool> on)
        {
            log = logger;
            settingsOn = on;
            Exp = config.Bind("Gameplay", "ExpMultiplier", 1, new ConfigDescription(
                "EXP from each defeated enemy times this, 1 to 10. The game still caps one battle at a level's worth. "
                + "Switch it on the Gameplay page.", new AcceptableValueRange<int>(Min, Max)));
            Berries = config.Bind("Gameplay", "BerryMultiplier", 1, new ConfigDescription(
                "Berries picked up in the world (lying there or dropped after a fight) times this, 1 to 10; never berries "
                + "that come from a check. Switch it on the Gameplay page.", new AcceptableValueRange<int>(Min, Max)));
            if (Hooks.Install(typeof(Multipliers), "mult", "the multipliers do nothing"))
            {
                log.LogInfo("[mult] installed on BattleControl.GetEXP and NPCControl.BerryBounce's MoveNext");
            }
        }

        internal static void StepBy(ConfigEntry<int> entry, int by, int min = Min, int max = Max)
        {
            entry.Value = UnityEngine.Mathf.Clamp(entry.Value + by, min, max);
            log.LogInfo("[apmenu] " + entry.Definition.Key + ": " + entry.Value + "x");
        }

        private static bool On(ConfigEntry<int> entry) => entry != null && entry.Value > 1 && settingsOn != null && settingsOn();

        // A hard rematch (flag 166), which the game holds to 5 EXP.
        [HarmonyPatch(typeof(BattleControl), "GetEXP", typeof(int), typeof(bool), typeof(MainManager.Enemies))]
        [HarmonyPostfix]
        private static void AfterGetExp(ref int __result)
        {
            if (__result <= 0 || !On(Exp) || MainManager.instance.flags[GameFlags.HardRematch])
            {
                return;
            }
            int was = __result;
            __result = was * Exp.Value;
            log.LogInfo($"[mult] EXP {was} -> {__result} ({Exp.Value}x)");
        }

        // BerryBounce starts right after a berry pickup has added its 1, 5 or 20, and only then; its first step runs at once.
        // The iterator's own MoveNext: the tiny BerryBounce() stub is inlined into its caller, so a patch there never runs.
        [HarmonyPatch(typeof(NPCControl), "BerryBounce", MethodType.Enumerator)]
        [HarmonyPrefix]
        private static void BeforeBerryStep(object __instance)
        {
            if (!On(Berries))
            {
                return;
            }
            var step = Traverse.Create(__instance);
            if (step.Field("<>1__state").GetValue<int>() != 0)
            {
                return;
            }
            NPCControl npc = step.Field("<>4__this").GetValue<NPCControl>();
            if (npc?.entity == null)
            {
                return;
            }
            int value;
            switch ((MainManager.Items)npc.entity.animstate)
            {
                case MainManager.Items.MoneySmall: value = 1; break;
                case MainManager.Items.MoneyMedium: value = 5; break;
                case MainManager.Items.MoneyBig: value = 20; break;
                default: return;
            }
            MainManager.instance.money = UnityEngine.Mathf.Clamp(MainManager.instance.money + value * (Berries.Value - 1), 0, 999);
            log.LogInfo($"[mult] berries {value} -> {value * Berries.Value} ({Berries.Value}x)");
        }
    }
}
