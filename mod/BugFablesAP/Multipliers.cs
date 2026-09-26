using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Gameplay page's EXP and berry multipliers, 1x to 10x (a pip each, like the volume bars).
    // EXP: each defeated enemy's share, after the game's own per-enemy caps; the game still caps a battle at a level's worth.
    // Berries: only those picked up in the world (lying there or dropped after a fight), never a check's.
    internal static class Multipliers
    {
        internal const int Min = 1, Max = 10;
        internal static ConfigEntry<int> Exp;
        internal static ConfigEntry<int> Berries;

        private static Func<bool> settingsOn;
        private static Harmony harmony;
        private static ManualLogSource log;

        internal static void Enable(ManualLogSource logger, string guid, ConfigFile config, Func<bool> on)
        {
            log = logger;
            settingsOn = on;
            Exp = config.Bind("Gameplay", "ExpMultiplier", 1, new ConfigDescription(
                "EXP from each defeated enemy times this, 1 to 10. The game still caps one battle at a level's worth. "
                + "Switch it on the Gameplay page.", new AcceptableValueRange<int>(Min, Max)));
            Berries = config.Bind("Gameplay", "BerryMultiplier", 1, new ConfigDescription(
                "Berries picked up in the world (lying there or dropped after a fight) times this, 1 to 10; never a check's "
                + "berries. Switch it on the Gameplay page.", new AcceptableValueRange<int>(Min, Max)));
            var exp = AccessTools.Method(typeof(BattleControl), "GetEXP", new[] { typeof(int), typeof(bool), typeof(MainManager.Enemies) });
            var berry = AccessTools.Method(typeof(NPCControl), "BerryBounce");
            if (exp == null || berry == null)
            {
                log.LogError($"[mult] NOT installed (BattleControl.GetEXP {exp != null}, NPCControl.BerryBounce {berry != null}); "
                    + "the multipliers do nothing.");
                return;
            }
            harmony = new Harmony(guid + ".mult." + DateTime.UtcNow.Ticks);
            harmony.Patch(exp, postfix: new HarmonyMethod(typeof(Multipliers), nameof(AfterGetExp)));
            harmony.Patch(berry, prefix: new HarmonyMethod(typeof(Multipliers), nameof(BeforeBerryBounce)));
            log.LogInfo("[mult] installed on BattleControl.GetEXP and NPCControl.BerryBounce");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        internal static void StepBy(ConfigEntry<int> entry, int by)
        {
            entry.Value = UnityEngine.Mathf.Clamp(entry.Value + by, Min, Max);
            log.LogInfo("[apmenu] " + entry.Definition.Key + ": " + entry.Value + "x");
        }

        private static bool On(ConfigEntry<int> entry) => entry != null && entry.Value > 1 && settingsOn != null && settingsOn();

        // flag 166: the hologram fights, which the game holds to 5 EXP.
        private static void AfterGetExp(ref int __result)
        {
            if (__result <= 0 || !On(Exp) || MainManager.instance.flags[166])
            {
                return;
            }
            int was = __result;
            __result = was * Exp.Value;
            log.LogInfo($"[mult] EXP {was} -> {__result} ({Exp.Value}x)");
        }

        // Called right after a berry pickup has added its 1, 5 or 20, and only then.
        private static void BeforeBerryBounce(NPCControl __instance)
        {
            if (!On(Berries) || __instance?.entity == null)
            {
                return;
            }
            int value;
            switch ((MainManager.Items)__instance.entity.animstate)
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
