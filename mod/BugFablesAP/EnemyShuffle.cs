using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Enemy Shuffle: a map enemy starts the fight the seed gave it (slot_data enemy_swaps, keyed "map:entity index").
    internal static class EnemyShuffle
    {
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static ManualLogSource log;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> randomizerEnabled)
        {
            log = logger;
            connection = conn;
            randomizerOn = randomizerEnabled;
            var method = AccessTools.Method(typeof(BattleControl), nameof(BattleControl.StartBattle),
                new[] { typeof(int[]), typeof(int), typeof(int), typeof(string), typeof(NPCControl), typeof(bool) });
            if (method == null)
            {
                log.LogError("[enemies] NOT installed: BattleControl.StartBattle wasn't found; fights stay the game's own.");
                return;
            }
            harmony = new Harmony(guid + ".enemies." + DateTime.UtcNow.Ticks);
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(EnemyShuffle), nameof(BeforeBattle)));
            log.LogInfo("[enemies] installed on BattleControl.StartBattle");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void BeforeBattle(ref int[] enemyids, NPCControl calledfrom)
        {
            Dictionary<string, int[]> swaps = connection?.EnemySwaps;
            if (calledfrom == null || calledfrom.entitytype != NPCControl.NPCType.Enemy || swaps == null || swaps.Count == 0
                || randomizerOn == null || !randomizerOn() || MainManager.map == null)
            {
                return;
            }
            string key = MainManager.map.mapid + ":" + calledfrom.mapid;
            if (!swaps.TryGetValue(key, out int[] fight))
            {
                log.LogInfo($"[enemies] {key}: not in the seed's list, its own fight ({string.Join(" ", enemyids)})");
                return;
            }
            // A copy: the game's own EnemyCheck rewrites the array it's given.
            log.LogInfo($"[enemies] {key}: {string.Join(" ", enemyids)} -> {string.Join(" ", fight)}");
            enemyids = (int[])fight.Clone();
        }
    }
}
