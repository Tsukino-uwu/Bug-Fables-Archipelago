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
    // The entrance randomizer: after a map builds its entities, a door's data and vectordata[1..] are replaced by
    // another door's, read from that door's map's entity table. vectordata[0] (the walk in) and data[4] stay this
    // side's.
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

        // Dev only ([Debug] TestDoors); off in the release build, which never sets it.
        internal static string TestDoors = null;

        // "map/door" of every door rewritten so far, for the respawn-loop guard's log.
        private static readonly HashSet<string> rewritten = new HashSet<string>();

        internal static bool Rewrote(string map, string door) => rewritten.Contains(map + "/" + door);

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            if (Hooks.Install(typeof(Entities), "doors", "doors lead where the game has them"))
            {
                log.LogInfo("[doors] installed on MapControl.CreateEntities");
            }
            if (Hooks.Install(typeof(Taken), "doors", "the trackers never learn which doors were taken"))
            {
                log.LogInfo("[doors] installed on MainManager.TransferMap (doors taken)");
            }
        }

        // A door walked through with the doors shuffled, for the trackers (ApConnection.DoorTaken).
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.TransferMap), typeof(int), typeof(Vector3),
            typeof(Vector3), typeof(Vector3), typeof(NPCControl))]
        private static class Taken
        {
            [HarmonyPrefix]
            private static void BeforeTransfer(NPCControl caller)
            {
                MapControl map = MainManager.map;
                if (caller == null || map == null || caller.objecttype != NPCControl.ObjectTypes.DoorOtherMap
                    || randomizerOn == null || !randomizerOn() || connection?.DoorTargets == null
                    || connection.DoorTargets.Count == 0)
                {
                    return;
                }
                // Only for the trackers: a failure here must never stop the door itself.
                try
                {
                    // Another seed's save adds nothing to this slot's keys (as checks and shops are gated).
                    if (ItemReceiver.SaveMatchesSeed(connection, log) == false)
                    {
                        log.LogInfo($"[doors] {map.mapid}: {caller.name} not recorded, this save belongs to another seed");
                        return;
                    }
                    connection.DoorTaken(map.mapid + ": " + NameOf(map, caller));
                }
                catch (Exception e)
                {
                    log.LogWarning(
                        $"[doors] {map.mapid}: {caller.name} taken but not recorded: {e.GetBaseException().Message}");
                }
            }
        }

        // The door's name as door_targets and the apworld write it: "name#row" where its map has two doors of that name.
        private static string NameOf(MapControl map, NPCControl door)
        {
            int alike = map.GetComponentsInChildren<NPCControl>(true)
                .Count(n => n.name == door.name && n.objecttype == NPCControl.ObjectTypes.DoorOtherMap);
            if (alike < 2)
            {
                return door.name;
            }
            TextAsset names = Resources.Load<TextAsset>("Data/EntityData/Names/" + (int)map.mapid + "names");
            string[] lines = names == null ? new string[0] : names.ToString().Split('\n');
            for (int row = 0; row < lines.Length; row++)
            {
                if (lines[row].Trim() == door.name && Find(map, door.name + "#" + row) == door)
                {
                    return door.name + "#" + row;
                }
            }
            return door.name;
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
                    yield return new Target { Map = from[0].Trim(), Door = from[1].Trim(), LikeMap = like[0].Trim(),
                        LikeDoor = like[1].Trim() };
                }
            }
        }

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        private static class Entities
        {
            [HarmonyPostfix]
            private static void AfterCreate(MapControl __instance)
            {
                if (randomizerOn == null || !randomizerOn())
                {
                    return;
                }
                string map = __instance.mapid.ToString();
                foreach (Target t in Targets().Where(t => t.Map == map))
                {
                    NPCControl door = Find(__instance, t.Door);
                    if (door == null)
                    {
                        log.LogWarning($"[doors] {map}: no door {t.Door} to rewrite");
                        continue;
                    }
                    if (!Read(t.LikeMap, t.LikeDoor, out int[] data, out Vector3[] vectors, out float jump)
                        || data.Length == 0 || vectors.Length < 3)
                    {
                        log.LogWarning(
                            $"[doors] {map}: {t.Door} kept as it is ({t.LikeMap}/{t.LikeDoor} not readable as a door)");
                        continue;
                    }
                    // data[4] == 1: no walk into this door (a hole, a ladder), so it stays with this side.
                    int ownWalk = door.data != null && door.data.Length > 4 ? door.data[4] : 0;
                    var own = door.vectordata != null && door.vectordata.Length > 0 ? door.vectordata[0] : vectors[0];
                    if (data.Length > 4 || ownWalk != 0)
                    {
                        Array.Resize(ref data, Math.Max(data.Length, 5));
                        data[4] = ownWalk;
                    }
                    door.data = data;
                    door.vectordata = (Vector3[])vectors.Clone();
                    door.vectordata[0] = own;
                    // TransferMap reads the arrival jump from the door walked into (its entity's emoticonoffset.x).
                    if (door.entity != null)
                    {
                        door.entity.emoticonoffset =
                            new Vector3(jump, door.entity.emoticonoffset.y, door.entity.emoticonoffset.z);
                    }
                    rewritten.Add(map + "/" + t.Door);
                    log.LogInfo($"[doors] {map}: {t.Door} now leads where {t.LikeMap}/{t.LikeDoor} leads (map {(MainManager.Maps)data[0]}, appear {vectors[1]}, jump {jump}, own walk {ownWalk})");
                }
            }
        }

        // A door's name in door_targets: its entity name, or "name#row" where its map has two doors of that name, the
        // row being its line in the map's entity table.
        private static string Split(string door, out int row)
        {
            int hash = door.LastIndexOf('#');
            row = -1;
            if (hash > 0 && int.TryParse(door.Substring(hash + 1), out int parsed))
            {
                row = parsed;
                return door.Substring(0, hash);
            }
            return door;
        }

        // The live door: by name, or for a "name#row" door the one of that name standing on its row's starting spot (the
        // game makes entities in table order, but keeps no row; doors never move).
        private static NPCControl Find(MapControl map, string door)
        {
            string name = Split(door, out int row);
            List<NPCControl> named = map.GetComponentsInChildren<NPCControl>(true)
                .Where(n => n.name == name && n.objecttype == NPCControl.ObjectTypes.DoorOtherMap).ToList();
            if (row < 0)
            {
                return named.FirstOrDefault();
            }
            TextAsset table = Resources.Load<TextAsset>("Data/EntityData/" + (int)map.mapid);
            string[] lines = table == null ? new string[0] : table.ToString().Split('\n');
            string[] f = row < lines.Length ? lines[row].Split('}') : new string[0];
            if (f.Length <= 8)
            {
                return null;
            }
            var spot = new Vector3(Parse(f[6]), Parse(f[7]), Parse(f[8]));
            return named.OrderBy(n => (n.transform.position - spot).sqrMagnitude).FirstOrDefault();
        }

        private static bool Read(string mapName, string door, out int[] data, out Vector3[] vectors, out float jump)
        {
            string doorName = Split(door, out int row);
            data = new int[0];
            vectors = new Vector3[0];
            jump = 0f;
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
                if (nameLines[i].Trim() != doorName || (row >= 0 && i != row))
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
                jump = f.Length > 175 ? Parse(f[175]) : 0f;
                return true;
            }
            return false;
        }

        private static float Parse(string s) => float.Parse(s.Trim(), CultureInfo.InvariantCulture);
    }
}
