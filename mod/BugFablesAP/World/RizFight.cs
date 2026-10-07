using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Riz at the Fishing Village's door (Event176) only turns the party back while Maki follows, and with the swamp
    // bridge kept up Maki never leaves. In a seed that says so, the scene's one follower check answers "no", so Riz
    // always offers his fight; Maki still fights alongside, and Riz then has more HP, never above his vanilla HP.
    internal static class RizFight
    {
        private const int RizScene = 176;
        private const float HpWithMaki = 1.6f;

        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        // While the scene's first step runs (inside StartEvent): its follower check answers "no".
        private static bool hideMaki;
        // Maki follows into this scene's fight: Riz's HP grows.
        private static bool withMaki;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (Hooks.Install(typeof(RizFight), "riz", "Riz turns the party back while Maki follows"))
            {
                log.LogInfo("[riz] installed on EventControl.StartEvent, MainManager.HasFollower and GetEnemyData");
            }
        }

        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
        [HarmonyPrefix]
        private static void BeforeStartEvent(int id)
        {
            withMaki = false;
            if (id != RizScene || MainManager.map == null
                || MainManager.map.mapid != MainManager.Maps.FarGrasslandsOutsideVillage)
            {
                return;
            }
            bool maki = MainManager.HasFollower(MainManager.AnimIDs.Maki);
            bool fights = randomizerOn != null && randomizerOn() && seed?.Invoke()?.RizFightWithFollower == true;
            log.LogInfo("[riz] Riz's scene, " + (maki ? "Maki following: " : "no Maki: ")
                + (!maki ? "the game's own choice" : fights
                    ? $"the fight offered, Maki helping, Riz's HP x{HpWithMaki}" : "left to the game (turned back)"));
            hideMaki = maki && fights;
            withMaki = hideMaki;
        }

        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
        [HarmonyPostfix]
        private static void AfterStartEvent()
        {
            hideMaki = false;
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.HasFollower))]
        [HarmonyPrefix]
        private static bool BeforeHasFollower(MainManager.AnimIDs followerid, ref bool __result)
        {
            if (!hideMaki || followerid != MainManager.AnimIDs.Maki)
            {
                return true;
            }
            __result = false;
            return false;
        }

        // After enemy scaling's own postfix, so the bonus is on the scaled HP: up to his vanilla HP at most (enemydata
        // column 1, as GetEnemyData reads it), and never below what scaling gave.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.GetEnemyData), typeof(int), typeof(bool), typeof(bool))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        private static void AfterGetEnemyData(int id, bool createentity, ref MainManager.BattleData __result)
        {
            if (!withMaki || !createentity || id != (int)MainManager.Enemies.Fisherman
                || MainManager.lastevent != RizScene)
            {
                return;
            }
            int vanilla = Convert.ToInt32(MainManager.enemydata[id, 1]);
            int hp = Math.Max(__result.maxhp, Math.Min((int)Math.Round(__result.maxhp * HpWithMaki), vanilla));
            log.LogInfo($"[riz] Maki helps against Riz: hp {__result.maxhp} -> {hp} (x{HpWithMaki}, "
                + $"at most {vanilla})");
            __result.hp = hp;
            __result.maxhp = hp;
            withMaki = false;
        }
    }
}
