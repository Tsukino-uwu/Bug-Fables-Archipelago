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
    //   unstick                 run the game's end-of-event cleanup, when a cutscene died and left you frozen
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
            // A queued warp waits until the player is free (no dialogue, cutscene or menu), instead of failing with
            // "not now" while the tester is busy (2026-09-24).
            bool warpWaits = queued.Count > 0 && (queued.Peek().StartsWith("loc") || queued.Peek().StartsWith("warp"))
                && (MainManager.player == null || MainManager.instance.inevent || MainManager.instance.message
                    || MainManager.instance.minipause || MainManager.instance.pause);
            if (queued.Count > 0 && pendingMap < 0 && !open && !warpWaits)
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
                    case "unstick": return Unstick();
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
            string skipped = SkipAutoEvents(map);
            pendingMap = (int)map;
            pendingFlag = flag;
            pendingSince = Time.realtimeSinceStartup;
            // The door warp: the game's own map transfer, to the entity's own start position when the map's entity
            // table has it (the user, 2026-09-24: origin-then-hop looked like two warps). The entity's own spot, not
            // a spot beside it: TransferMap ends by WALKING the party to its target and waits for that walk
            // (MainManager.cs:17610-17624), so a target over water never arrived, the transition never ended, and
            // the game kept respawning the party there (SnakemouthLake). An item rests on standable ground. FinishWarp
            // guards the item as soon as the map exists, and steps aside once the transition is over.
            Vector3? at = flag >= 0 ? StartPosition(map, flag) : null;
            guarded = false;
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, at.HasValue ? at.Value + Vector3.up * 0.5f : Vector3.zero));
            return "warping to " + map + skipped;
        }


        // A spot beside the entity with room for the party: no solid collider where the player would stand, and safe
        // ground below (not a hazard). Tries four sides at 2.5, then 1.5; null when none is safe.
        private static Vector3? ClearSpot(Vector3 at)
        {
            Vector3[] sides = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            foreach (float distance in new[] { 2.5f, 1.5f })
            {
                foreach (Vector3 side in sides)
                {
                    Vector3 spot = at + side * distance + Vector3.up * 0.5f;
                    bool blocked = Physics.OverlapSphere(spot + Vector3.up * 0.5f, 0.45f, ~0, QueryTriggerInteraction.Ignore)
                        .Any(c => MainManager.player == null || !c.transform.IsChildOf(MainManager.player.transform.root));
                    // Ground, and not a hazard: water counted as ground and put the party in the lake (the user,
                    // 2026-09-24). Hazards (water, spikes, pits) carry the game's Hazards component.
                    bool ground = Physics.Raycast(spot + Vector3.up, Vector3.down, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Collide)
                        && hit.collider.GetComponentInParent<Hazards>() == null && !hit.collider.isTrigger;
                    if (!blocked && ground)
                    {
                        return spot;
                    }
                }
            }
            return null;
        }

        // An entity's start position, read from the map's entity table as MapControl.CreateEntities does: fields 6-8
        // are its start position, field 194 its activationflag (MapControl.cs:1477-1640, EntityDump).
        private static Vector3? StartPosition(MainManager.Maps map, int flag)
        {
            TextAsset data = Resources.Load<TextAsset>("Data/EntityData/" + (int)map);
            if (data == null)
            {
                return null;
            }
            foreach (string line in data.ToString().Split('\n'))
            {
                string[] f = line.Split('}');
                if (f.Length > 194 && f[194].Trim() == flag.ToString())
                {
                    return new Vector3(Convert.ToSingle(f[6]), Convert.ToSingle(f[7]), Convert.ToSingle(f[8]));
                }
            }
            return null;
        }

        // A map's auto-start cutscenes (MapControl.autoevent, (flag, event)) run on arrival while their flag is off
        // (MapControl.cs:874-883). Arriving by warp, out of the story's order, one crashed and left the game stuck
        // "in an event" (2026-09-24, Event21 on SnakemouthUndergrondDoor). So a warp marks them seen first, as the map
        // itself does once they've run: skipping the cutscene, which is what a dev warp wants.
        private static string SkipAutoEvents(MainManager.Maps map)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Maps/" + map);
            MapControl control = prefab == null ? null : prefab.GetComponent<MapControl>();
            if (control == null || control.autoevent == null || control.autoevent.Length == 0)
            {
                return "";
            }
            var skipped = new List<string>();
            foreach (Vector2 pair in control.autoevent)
            {
                int flag = (int)pair.x;
                if (!MainManager.instance.flags[flag])
                {
                    MainManager.instance.flags[flag] = true;
                    skipped.Add($"event {(int)pair.y} (flag {flag})");
                }
            }
            return skipped.Count == 0 ? "" : "; skipped its auto-start " + string.Join(", ", skipped.ToArray());
        }

        // Gets the player moving again after a cutscene died half-way: the game's own end-of-event cleanup
        // (EventControl.EndEvent, private), which clears inevent and minipause and resets the player.
        private static string Unstick()
        {
            System.Reflection.MethodInfo end = HarmonyLib.AccessTools.Method(typeof(EventControl), "EndEvent", new[] { typeof(bool) });
            if (end == null)
            {
                return "EventControl.EndEvent not found";
            }
            end.Invoke(null, new object[] { false });
            if (MainManager.player != null)
            {
                MainManager.player.lockkeys = false;
                // A map transfer stuck walking to an unreachable target (see StartWarp): stop the walk and the
                // transition it holds open.
                MainManager.player.entity?.StopForceMove();
            }
            MainManager.roomtransition = false;
            pendingMap = -1;
            return "ran the game's end-of-event cleanup; inevent=" + MainManager.instance.inevent + ", minipause=" + MainManager.instance.minipause;
        }

        // Once the target map is up and the transfer is over, stand by the entity with the wanted flag, or else by
        // the first save point or door on the map.
        private static float unlockAt = -1f;
        private static bool guarded;

        private static void FinishWarp()
        {
            if (unlockAt > 0f && Time.realtimeSinceStartup >= unlockAt)
            {
                unlockAt = -1f;
                if (MainManager.player != null && !open)
                {
                    MainManager.player.lockkeys = false;
                }
            }
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
            if (map == null || (int)map.mapid != pendingMap || MainManager.player == null)
            {
                return;
            }
            // As soon as the map exists, even mid-transition: the party lands on the item's own spot, so keep it from
            // being taken until the step aside below (touchcooldown, which CheckItem waits out, NPCControl.cs:5608).
            if (!guarded && pendingFlag >= 0)
            {
                foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
                {
                    if (npc.activationflag == pendingFlag && npc.objecttype == NPCControl.ObjectTypes.Item)
                    {
                        npc.touchcooldown = 240f;
                        guarded = true;
                    }
                }
            }
            if (MainManager.roomtransition || MainManager.instance.intransition)
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
                // A fixed side put the party in walls and in the lake (the user, 2026-09-24, SnakemouthLake): stand on
                // the first side with room and safe ground; if there is none, at the map's save point (standable by
                // design) and say where the item is from there. The pickup's own touch cooldown (which CheckItem
                // waits out, NPCControl.cs:5608; counted down each frame, :2802) holds it about 1.5 s.
                Vector3? spot = ClearSpot(target.transform.position);
                if (!spot.HasValue)
                {
                    NPCControl save = entities.FirstOrDefault(e => e.objecttype == NPCControl.ObjectTypes.SavePoint);
                    if (save != null && save != target)
                    {
                        Vector3 offset = target.transform.position - save.transform.position;
                        where += $"; no safe spot beside it, so at the save point (the item is {offset.x:+0.0;-0.0} across, "
                            + $"{offset.z:+0.0;-0.0} deep, {offset.y:+0.0;-0.0} up from here)";
                        spot = save.transform.position + Vector3.up * 0.5f;
                    }
                }
                if (spot.HasValue)
                {
                    MainManager.player.transform.position = spot.Value;
                }
                if (target.objecttype == NPCControl.ObjectTypes.Item)
                {
                    target.touchcooldown = Mathf.Max(target.touchcooldown, 90f);
                }
                // And no walking for a second after arriving, so a key still held doesn't carry you into something
                // (the user, 2026-09-24).
                MainManager.player.lockkeys = true;
                unlockAt = Time.realtimeSinceStartup + 1f;
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
