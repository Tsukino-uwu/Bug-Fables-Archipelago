using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only (Debug.DevConsole, off by default): a one-line command console on F9, to get to a location fast
    // instead of playing there. Every change goes through the game's own functions: MainManager.TransferMap (the
    // door warp), EntityControl.CreateItem (the pickup dig spots create), flags[] writes as the game's |flag|
    // command does. Nothing here is for a real game: it can put a save in a state the story never makes.
    //
    //   loc <n>                 go to location n's map and stand by its pickup (n = the apworld's location id,
    //                           e.g. 5, or the full 77200xx); a taken pickup's flag is cleared first so it's back
    //   warp <map> [flag]       go to a map (MainManager.Maps name or number); with a flag, stand by the entity
    //                           whose activationflag it is
    //   spawn <item|key|medal> <id> [flag]   drop a pickup next to you; with a location's flag, it is that location
    //   flag <n> [on|off]       show or set flags[n]
    internal static class DevConsole
    {
        private const long LocationIdBase = 7_720_000;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static bool open;
        private static string line = "";
        private static string lastResult = "F9: dev console. loc <n> | warp <map> [flag] | spawn <item|key|medal> <id> [flag] | flag <n> [on|off]";

        // A warp in flight: once the target map is loaded, stand by the entity with this activationflag.
        private static int pendingMap = -1;
        private static int pendingFlag = -1;
        private static float pendingSince;

        internal static void Init(ManualLogSource logger, ApConnection conn)
        {
            log = logger;
            connection = conn;
        }

        // Dev only (Debug.DevCommandFile): a text file the console also reads, so a developer outside the game can run
        // commands while the tester watches. Checked twice a second; its lines are queued and the file emptied.
        // Commands run one at a time, the next only once a warp has arrived.
        internal static string CommandFile;
        private static readonly Queue<string> queued = new Queue<string>();
        private static float lastPoll;

        private static void PollFile()
        {
            if (string.IsNullOrEmpty(CommandFile) || Time.realtimeSinceStartup - lastPoll < 0.5f)
            {
                return;
            }
            lastPoll = Time.realtimeSinceStartup;
            try
            {
                if (!System.IO.File.Exists(CommandFile))
                {
                    return;
                }
                string[] lines = System.IO.File.ReadAllLines(CommandFile);
                if (lines.Length == 0)
                {
                    return;
                }
                System.IO.File.WriteAllText(CommandFile, "");
                foreach (string l in lines.Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith("#")))
                {
                    queued.Enqueue(l);
                }
            }
            catch (Exception e)
            {
                log.LogWarning("[dev] command file: " + e.Message);
            }
        }

        internal static void Tick(bool enabled)
        {
            if (!enabled)
            {
                if (open)
                {
                    Close();
                }
                return;
            }
            FinishWarp();
            PollFile();
            if (queued.Count > 0 && pendingMap < 0 && !open)
            {
                string command = queued.Dequeue();
                lastResult = Run(command);
                shownAt = Time.realtimeSinceStartup;
                log.LogInfo($"[dev] (file) {command} -> {lastResult}");
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (open)
                {
                    Close();
                }
                else if (MainManager.player != null && !MainManager.instance.minipause && !MainManager.instance.message)
                {
                    open = true;
                    line = "";
                    // Freeze the player while typing, the way the game freezes it during an item-get: lockkeys stops
                    // movement and actions, minipause the rest (letters like C and X are the game's confirm/cancel).
                    MainManager.player.lockkeys = true;
                    MainManager.instance.minipause = true;
                }
                return;
            }
            if (!open)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    line = line.Length > 0 ? line.Substring(0, line.Length - 1) : line;
                }
                else if (c == '\n' || c == '\r')
                {
                    string command = line.Trim();
                    Close();
                    if (command.Length > 0)
                    {
                        lastResult = Run(command);
                        log.LogInfo($"[dev] {command} -> {lastResult}");
                    }
                    return;
                }
                else if (!char.IsControl(c))
                {
                    line += c;
                }
            }
        }

        internal static void Draw(bool enabled)
        {
            // Open, or for a few seconds after a command answered.
            if (!enabled || (!open && (shownAt < 0f || Time.realtimeSinceStartup - shownAt > 6f)))
            {
                return;
            }
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 16, wordWrap = true };
            string text = open ? "> " + line + "_" : lastResult;
            GUI.Box(new Rect(10, Screen.height - 70, Screen.width - 20, 60), text, style);
        }

        private static float shownAt = -1f;

        private static void Close()
        {
            open = false;
            shownAt = Time.realtimeSinceStartup;
            if (MainManager.player != null)
            {
                MainManager.player.lockkeys = false;
            }
            if (MainManager.instance != null)
            {
                MainManager.instance.minipause = false;
            }
        }

        private static string Run(string command)
        {
            string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                switch (parts[0].ToLowerInvariant())
                {
                    case "loc": return Loc(parts);
                    case "warp": return Warp(parts);
                    case "spawn": return Spawn(parts);
                    case "flag": return Flag(parts);
                    default: return "unknown command: " + parts[0];
                }
            }
            catch (Exception e)
            {
                return "failed: " + e.GetType().Name + ": " + e.Message;
            }
        }

        private static string Loc(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "loc <n>";
            }
            long id = long.Parse(parts[1]);
            if (id < LocationIdBase)
            {
                id += LocationIdBase;
            }
            Dictionary<long, ApConnection.Pickup> pickups = connection.LocationPickups;
            if (pickups == null || !pickups.TryGetValue(id, out ApConnection.Pickup pickup))
            {
                return $"location {id} isn't a pickup in this seed (or not connected yet)";
            }
            MainManager.Maps map = (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), pickup.Map);
            if (MainManager.instance.flags[pickup.Flag])
            {
                // Taken already: clear its flag so the map creates it again (the check stays sent on the server).
                MainManager.instance.flags[pickup.Flag] = false;
            }
            return StartWarp(map, pickup.Flag) + $" (location {id}, flag {pickup.Flag})";
        }

        private static string Warp(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "warp <map> [flag]";
            }
            MainManager.Maps map = int.TryParse(parts[1], out int number)
                ? (MainManager.Maps)number
                : (MainManager.Maps)Enum.Parse(typeof(MainManager.Maps), parts[1], true);
            int flag = parts.Length > 2 ? int.Parse(parts[2]) : -1;
            return StartWarp(map, flag);
        }

        private static string StartWarp(MainManager.Maps map, int flag)
        {
            if (MainManager.player == null || MainManager.instance.inevent || MainManager.instance.message)
            {
                return "not now: no player, or an event or dialogue is running";
            }
            pendingMap = (int)map;
            pendingFlag = flag;
            pendingSince = Time.realtimeSinceStartup;
            // The door warp: the game's own map transfer. Position zero, then FinishWarp moves the party to a
            // known spot on the new map (a map's origin can be inside a wall).
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, Vector3.zero));
            return "warping to " + map;
        }

        // Once the target map is up and the transfer is over, stand by the entity with the wanted flag, or else by
        // the first save point or door on the map.
        private static void FinishWarp()
        {
            if (pendingMap < 0)
            {
                return;
            }
            if (Time.realtimeSinceStartup - pendingSince > 20f)
            {
                lastResult = "warp: the map never finished loading";
                shownAt = Time.realtimeSinceStartup;
                pendingMap = -1;
                return;
            }
            MapControl map = MainManager.map;
            if (map == null || (int)map.mapid != pendingMap || MainManager.roomtransition || MainManager.instance.intransition
                || MainManager.player == null)
            {
                return;
            }
            List<NPCControl> entities = map.GetComponentsInChildren<NPCControl>(true).ToList();
            NPCControl target = pendingFlag >= 0 ? entities.FirstOrDefault(e => e.activationflag == pendingFlag) : null;
            string where = target != null ? "by the entity with flag " + pendingFlag : null;
            if (target == null)
            {
                target = entities.FirstOrDefault(e => e.objecttype == NPCControl.ObjectTypes.SavePoint)
                    ?? entities.FirstOrDefault(e => e.objecttype == NPCControl.ObjectTypes.DoorOtherMap);
                where = target != null ? "by " + target.name + (pendingFlag >= 0 ? $" (nothing with flag {pendingFlag} here)" : "") : "at the map's origin";
            }
            if (target != null)
            {
                // Beside it, not on it: standing on a pickup would take it before you look.
                MainManager.player.transform.position = target.transform.position + new Vector3(1.5f, 0.5f, 0f);
                MainManager.TeleportFollowers(true);
            }
            lastResult = "arrived on " + map.mapid + ", " + where;
            shownAt = Time.realtimeSinceStartup;
            log.LogInfo("[dev] " + lastResult);
            pendingMap = -1;
        }

        private static string Spawn(string[] parts)
        {
            if (parts.Length < 3)
            {
                return "spawn <item|key|medal> <id> [flag]";
            }
            int kind = parts[1].ToLowerInvariant() == "medal" ? 2 : parts[1].ToLowerInvariant() == "key" ? 1 : 0;
            int id = int.Parse(parts[2]);
            int flag = parts.Length > 3 ? int.Parse(parts[3]) : -1;
            if (MainManager.player == null || MainManager.map == null)
            {
                return "not now: no player";
            }
            Vector3 at = MainManager.player.transform.position + new Vector3(1.5f, 1f, 0f);
            NPCControl item = EntityControl.CreateItem(at, kind, id, Vector3.zero, -1);
            item.activationflag = flag;
            if (flag >= 0)
            {
                MainManager.instance.flags[flag] = false;
            }
            return $"spawned {parts[1]} {id}" + (flag >= 0 ? $" with flag {flag}" : "") + " next to you";
        }

        private static string Flag(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "flag <n> [on|off]";
            }
            int n = int.Parse(parts[1]);
            if (parts.Length > 2)
            {
                MainManager.instance.flags[n] = parts[2].ToLowerInvariant() == "on" || parts[2] == "true" || parts[2] == "1";
            }
            return $"flags[{n}] = {MainManager.instance.flags[n]}";
        }
    }
}
