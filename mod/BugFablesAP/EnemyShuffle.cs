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
            var create = AccessTools.Method(typeof(MapControl), "CreateEntities");
            if (create == null)
            {
                log.LogWarning("[enemies] MapControl.CreateEntities wasn't found; the dev look test does nothing.");
                return;
            }
            harmony.Patch(create, postfix: new HarmonyMethod(typeof(EnemyShuffle), nameof(AfterCreate)));
        }

        // Dev only (console `enemylook`): every ordinary map enemy looks like this enemy id; -1 off.
        internal static int LookTest = -1;

        // Dev only (console `enemyfight`): every map fight is this list of enemy ids; null off.
        internal static int[] FightTest;

        // After the map builds its entities and before their Start, which sets up the model from animid.
        private static void AfterCreate(MapControl __instance)
        {
            if (LookTest < 0 || randomizerOn == null || !randomizerOn() || MainManager.enemydata == null
                || LookTest >= MainManager.enemydata.GetLength(0))
            {
                return;
            }
            int anim = Convert.ToInt32(MainManager.enemydata[LookTest, 0]);
            int changed = 0, puzzles = 0;
            foreach (NPCControl npc in __instance.GetComponentsInChildren<NPCControl>(true))
            {
                if (npc.entitytype != NPCControl.NPCType.Enemy || npc.entity == null)
                {
                    continue;
                }
                // A respawning puzzle enemy keeps its own look.
                if (npc.eventid > 0)
                {
                    puzzles++;
                    continue;
                }
                npc.entity.animid = anim;
                changed++;
            }
            log.LogInfo($"[enemies] look test on {__instance.mapid}: {changed} map enemies now look like enemy {LookTest} "
                + $"(anim {anim}); {puzzles} puzzle enemies kept");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static void BeforeBattle(ref int[] enemyids, NPCControl calledfrom)
        {
            if (FightTest != null && calledfrom != null && calledfrom.entitytype == NPCControl.NPCType.Enemy
                && randomizerOn != null && randomizerOn())
            {
                log.LogInfo($"[enemies] fight test: {string.Join(" ", enemyids)} -> {string.Join(" ", FightTest)}");
                enemyids = (int[])FightTest.Clone();
                return;
            }
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
