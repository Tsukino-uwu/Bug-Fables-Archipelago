using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The Gameplay page's Attack boost: Off / +1, off by default. +1 on each hit a party member lands, where and when the
    // game adds its own +1 for the member in front (mod guide, step 27); the save's attack stat is never touched.
    internal static class AttackBoost
    {
        internal static ConfigEntry<bool> Boost;

        private static Func<bool> settingsOn;
        private static Harmony harmony;
        private static ManualLogSource log;
        private static AccessTools.FieldRef<BattleControl, bool> demoMode;

        internal static void Enable(ManualLogSource logger, string guid, ConfigFile config, Func<bool> on)
        {
            log = logger;
            settingsOn = on;
            Boost = config.Bind("Gameplay", "AttackBoost", false,
                "On: each hit a party member lands does 1 more damage, as if their attack were 1 higher. The save's stats "
                + "don't change. Switch it on the Gameplay page.");
            var damage = AccessTools.Method(typeof(BattleControl), "CalculateBaseDamage");
            var demo = AccessTools.Field(typeof(BattleControl), "demomode");
            if (damage == null || demo == null)
            {
                log.LogError($"[boost] NOT installed (BattleControl.CalculateBaseDamage {damage != null}, demomode {demo != null}); "
                    + "the attack boost does nothing.");
                return;
            }
            demoMode = AccessTools.FieldRefAccess<BattleControl, bool>(demo);
            harmony = new Harmony(guid + ".boost." + DateTime.UtcNow.Ticks);
            harmony.Patch(damage, prefix: new HarmonyMethod(typeof(AttackBoost), nameof(BeforeBaseDamage)));
            log.LogInfo("[boost] installed on BattleControl.CalculateBaseDamage");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        // The game's own player bonuses skip Raw hits and the demo battle; so does this one.
        private static void BeforeBaseDamage(BattleControl __instance, MainManager.BattleData? attacker, ref int basevalue,
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
    }
}
