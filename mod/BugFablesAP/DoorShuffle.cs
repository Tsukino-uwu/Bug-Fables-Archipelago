using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The entrance randomizer (the user, 2026-09-25: every door, experimental until its logic is done). A door to another
    // map is an entity whose data says where it leads: walking in calls TransferMap(data[0], vectordata[0], vectordata[1],
    // vectordata[2]) (NPCControl.cs:5458-5461): the target map, the walk on this side, where the party appears, where it
    // then walks. A shuffled door "leads where another door leads": after the map builds its entities, the door's data
    // and its vectordata from [1] on are replaced by the other door's, read from that door's own map's entity table
    // (Data/EntityData/<map>, fields split by '}', at the positions MapControl.CreateEntities reads, MapControl.cs:1540-1566:
    // the data count at 60, the vectordata count at 71) and its names table (Data/EntityData/Names/<map>names, one name
    // per entity, MapControl.cs:1454). Its own vectordata[0], the walk into it on this side, stays. The list comes from
    // slot_data (door_targets); a dev setting (Debug.TestDoors) can add pairs by hand.
    internal static class DoorShuffle
    {
        internal sealed class Target
        {
            internal string Map;
            internal string Door;
            internal string LikeMap;
            internal string LikeDoor;
        }

        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // Dev only: "Map/Door=LikeMap/LikeDoor;..." (Debug.TestDoors).
        internal static string TestDoors;

        internal static void Enable(ManualLogSource logger, string guid, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            MethodInfo create = AccessTools.Method(typeof(MapControl), "CreateEntities");
            if (create == null)
            {
                log.LogError("[doors] MapControl.CreateEntities not found: doors lead where the game has them.");
                return;
            }
            harmony = new Harmony(guid + ".doors." + DateTime.UtcNow.Ticks);
            harmony.Patch(create, postfix: new HarmonyMethod(typeof(DoorShuffle), nameof(AfterCreate)));
            log.LogInfo("[doors] installed on MapControl.CreateEntities");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static IEnumerable<Target> Targets()
        {
            if (connection?.DoorTargets != null)
            {
                foreach (Target t in connection.DoorTargets)
                {
                    yield return t;
                }
            }
            if (string.IsNullOrEmpty(TestDoors))
            {
                yield break;
            }
            foreach (string pair in TestDoors.Split(';'))
            {
                string[] sides = pair.Split('=');
                string[] from = sides[0].Split('/');
                string[] like = sides.Length > 1 ? sides[1].Split('/') : new string[0];
                if (from.Length == 2 && like.Length == 2)
                {
                    yield return new Target { Map = from[0].Trim(), Door = from[1].Trim(), LikeMap = like[0].Trim(), LikeDoor = like[1].Trim() };
                }
            }
        }

        private static void AfterCreate(MapControl __instance)
        {
            if (randomizerOn == null || !randomizerOn())
            {
                return;
            }
            string map = __instance.mapid.ToString();
            foreach (Target t in Targets().Where(t => t.Map == map))
            {
                NPCControl door = __instance.GetComponentsInChildren<NPCControl>(true)
                    .FirstOrDefault(n => n.name == t.Door && n.objecttype == NPCControl.ObjectTypes.DoorOtherMap);
                if (door == null)
                {
                    log.LogWarning($"[doors] {map}: no door {t.Door} to rewrite");
                    continue;
                }
                if (!Read(t.LikeMap, t.LikeDoor, out int[] data, out Vector3[] vectors) || data.Length == 0 || vectors.Length < 3)
                {
                    log.LogWarning($"[doors] {map}: {t.Door} kept as it is ({t.LikeMap}/{t.LikeDoor} not readable as a door)");
                    continue;
                }
                var own = door.vectordata != null && door.vectordata.Length > 0 ? door.vectordata[0] : vectors[0];
                door.data = data;
                door.vectordata = (Vector3[])vectors.Clone();
                door.vectordata[0] = own;
                log.LogInfo($"[doors] {map}: {t.Door} now leads where {t.LikeMap}/{t.LikeDoor} leads (map {(MainManager.Maps)data[0]}, appear {vectors[1]})");
            }
        }

        // A door's data and vectordata from its map's entity table, found by name.
        private static bool Read(string mapName, string doorName, out int[] data, out Vector3[] vectors)
        {
            data = new int[0];
            vectors = new Vector3[0];
            MainManager.Maps map;
            try
            {
                map = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), mapName, true);
            }
            catch (ArgumentException)
            {
                return false;
            }
            TextAsset table = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
            TextAsset names = Resources.Load<TextAsset>("Data/EntityData/Names/" + (int)map + "names");
            if (table == null || names == null)
            {
                return false;
            }
            string[] lines = table.ToString().Split('\n');
            string[] nameLines = names.ToString().Split('\n');
            for (int i = 0; i < lines.Length - 1 && i < nameLines.Length; i++)
            {
                if (nameLines[i].Trim() != doorName)
                {
                    continue;
                }
                string[] f = lines[i].Split('}');
                if (f.Length < 81 || f[1].Trim() != "DoorOtherMap")
                {
                    return false;
                }
                int n = int.Parse(f[60].Trim());
                data = new int[n];
                for (int k = 0; k < n; k++)
                {
                    data[k] = int.Parse(f[61 + k].Trim());
                }
                int m = int.Parse(f[71].Trim());
                vectors = new Vector3[m];
                for (int k = 0; k < m; k++)
                {
                    vectors[k] = new Vector3(Parse(f[72 + k * 3]), Parse(f[73 + k * 3]), Parse(f[74 + k * 3]));
                }
                return true;
            }
            return false;
        }

        private static float Parse(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
    }
}
