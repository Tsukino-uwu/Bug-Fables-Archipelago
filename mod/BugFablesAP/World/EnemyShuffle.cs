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
            string[] donor = MoveTest ? Donor(LookTest) : null;
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
                if (donor != null)
                {
                    MoveLike(npc, donor);
                }
                changed++;
            }
            log.LogInfo($"[enemies] look test on {__instance.mapid}: {changed} map enemies now look like enemy {LookTest} "
                + $"(anim {anim}){(MoveTest ? donor != null ? $", moving like {donorFrom}" : ", no map enemy of it found to move like" : "")}; "
                + $"{puzzles} puzzle enemies kept");
        }

        // Dev only: with the look test, also copy the movement of a map enemy whose fight starts with that enemy.
        internal static bool MoveTest;

        // An entity row's layout, as MapControl.CreateEntities reads it.
        private const int BattleIdsAt = 166;
        private static int donorFor = -1;
        private static string[] donorRow;
        private static string donorFrom;

        private static string[] Donor(int enemy)
        {
            if (donorFor == enemy)
            {
                return donorRow;
            }
            donorFor = enemy;
            donorRow = null;
            donorFrom = null;
            foreach (MainManager.Maps map in Enum.GetValues(typeof(MainManager.Maps)))
            {
                if (map == MainManager.Maps.TestRoom)
                {
                    continue;
                }
                UnityEngine.TextAsset data = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Data/EntityData/" + (int)map);
                if (data == null)
                {
                    continue;
                }
                string[] lines = data.ToString().Split('\n');
                for (int i = 0; i < lines.Length - 1; i++)
                {
                    string[] f = lines[i].Split('}');
                    if (f.Length > BattleIdsAt + 1 && f[0] == "Enemy" && f[BattleIdsAt].Trim() != "0"
                        && int.TryParse(f[BattleIdsAt + 1].Trim(), out int first) && first == enemy)
                    {
                        donorRow = f;
                        donorFrom = map + ":" + i;
                        return donorRow;
                    }
                }
            }
            return null;
        }

        // The movement fields of a map row (MapControl.CreateEntities: behaviours, collider, speeds, radii, timers).
        private static void MoveLike(NPCControl npc, string[] f)
        {
            npc.behaviors = new[]
            {
                (NPCControl.ActionBehaviors)Enum.Parse(typeof(NPCControl.ActionBehaviors), f[2]),
                (NPCControl.ActionBehaviors)Enum.Parse(typeof(NPCControl.ActionBehaviors), f[3]),
            };
            EntityControl e = npc.entity;
            e.ccol.height = Convert.ToSingle(f[11]) / 2f;
            npc.colliderheight = Convert.ToSingle(f[11]);
            e.ccol.radius = Convert.ToSingle(f[12]);
            npc.radius = Convert.ToSingle(f[13]);
            npc.timer = Convert.ToSingle(f[14]);
            e.speed = Convert.ToSingle(f[15]);
            npc.actionfrequency = new[] { Convert.ToSingle(f[16]), Convert.ToSingle(f[17]) };
            npc.speedmultiplier = Convert.ToSingle(f[18]);
            npc.radiuslimit = Convert.ToSingle(f[19]);
            npc.wanderradius = Convert.ToSingle(f[20]);
            npc.teleportradius = Convert.ToSingle(f[21]);
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
