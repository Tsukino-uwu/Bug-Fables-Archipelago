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
    //   warp <map> [flag|@name] go to a map (MainManager.Maps name or number); with a flag, stand by the entity
    //                           whose activationflag it is; with @name, beside the entity of that name
    //   spawn <item|key|medal> <id> [flag]   drop a pickup next to you; with a location's flag, it is that location
    //   flag <n> [on|off]       show or set flags[n]
    //   unstick                 run the game's end-of-event cleanup, when a cutscene died and left you frozen; also
    //                           takes the party off whatever the scene parked it on and lifts a leftover fade
    //   nudge <x> <y> <z>       shift the party by that much on this map
    //   onehit                  toggle: every hit on an enemy does at least 99 (off by default)
    //   infjump                 toggle: jump again in mid-air, to reach high places (off by default)
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

        // While a warp is in flight and for 1.5 s after arriving, touching a pickup does nothing: a warp lands on the
        // item's own spot, and a guard set after the map loaded lost the race (2026-09-24, the east Outskirts HP
        // Plus was taken on arrival). The touch starts in NPCControl.OnTriggerEnter (NPCControl.cs:4516); this skips
        // it for items only. The touch is an Enter, so step off and back on to take the item afterwards.
        private static HarmonyLib.Harmony harmony;
        private static float blockUntil = -1f;

        internal static void EnableGuard(string guid)
        {
            var enter = HarmonyLib.AccessTools.Method(typeof(NPCControl), "OnTriggerEnter");
            if (enter == null)
            {
                log.LogWarning("[dev] NPCControl.OnTriggerEnter not found: warps can't hold off pickups");
                return;
            }
            harmony = new HarmonyLib.Harmony(guid + ".devguard." + DateTime.UtcNow.Ticks);
            harmony.Patch(enter, prefix: new HarmonyLib.HarmonyMethod(typeof(DevConsole), nameof(HoldPickups)));
            // onehit: the damage every hit ends in, BattleControl.DoDamage(attacker, ref target, amount, property,
            // overrides, block) (BattleControl.cs:7283); the other overloads lead there.
            var damage = HarmonyLib.AccessTools.Method(typeof(BattleControl), "DoDamage", new[]
            {
                typeof(MainManager.BattleData?), typeof(MainManager.BattleData).MakeByRefType(), typeof(int),
                typeof(BattleControl.AttackProperty?), HarmonyLib.AccessTools.Inner(typeof(BattleControl), "DamageOverride").MakeArrayType(), typeof(bool),
            });
            if (damage == null)
            {
                log.LogWarning("[dev] BattleControl.DoDamage not found: onehit does nothing");
                return;
            }
            harmony.Patch(damage, prefix: new HarmonyLib.HarmonyMethod(typeof(DevConsole), nameof(OneHit)));
            // Every story event that starts, with what started it and where (the user, 2026-09-24: log what happens
            // while playing through Leif's joining). EventControl.StartEvent(id, caller) starts them all (:74).
            var start = HarmonyLib.AccessTools.Method(typeof(EventControl), nameof(EventControl.StartEvent), new[] { typeof(int), typeof(NPCControl) });
            if (start != null)
            {
                harmony.Patch(start, prefix: new HarmonyLib.HarmonyMethod(typeof(DevConsole), nameof(LogEvent)));
            }
        }

        private static void LogEvent(int id, NPCControl caller)
        {
            log.LogInfo($"[event] Event{id} starts on {MainManager.map?.mapid.ToString() ?? "?"}, started by "
                + (caller != null ? $"{caller.name} ({caller.objecttype})" : "the map or code"));
        }

        // Dev only (the user, 2026-09-24: fights are tedious to test through): while on, every hit on an enemy is at
        // least 99 before the game's own defence and the rest of the calculation. The party is untouched; the game's
        // own test is the target's "Player" tag (BattleControl.cs:7295). Kept in the config ([Debug] OneHit, off in the
        // code) so it survives reloads (the user, 2026-09-25: fights kept coming with the cheat gone after a reload);
        // "onehit" flips the setting.
        internal static BepInEx.Configuration.ConfigEntry<bool> OneHitSetting;
        private static bool oneHit => OneHitSetting != null && OneHitSetting.Value;

        // infjump: each press of the jump button in mid-air jumps again, through the game's own EntityControl.Jump
        // (the height and sound of a normal jump, EntityControl.cs:4598, PlayerControl.DoJump). The game's own jump
        // only fires on the ground (PlayerControl.cs:372), so the two never both act on one press. Off by default.
        // Kept in the config ([Debug] InfJump, off in the code) like OneHit (the user, 2026-09-25); "infjump" flips it.
        internal static BepInEx.Configuration.ConfigEntry<bool> InfJumpSetting;
        private static bool infJump => InfJumpSetting != null && InfJumpSetting.Value;

        private static void TickInfJump()
        {
            if (!infJump || open || MainManager.player == null || MainManager.player.entity == null || !MainManager.GetKey(4, hold: false))
            {
                return;
            }
            EntityControl e = MainManager.player.entity;
            bool free = MainManager.FreePlayer();
            // Not the game's jumpcooldown: a jump sets it to 30 frames, longer than the whole jump, so it never ran out in
            // mid-air (the log, 2026-09-24). A press is one frame, so each press jumps once anyway.
            bool jumps = free && !e.onground;
            // Say what the guard decided on every press (CLAUDE.md, "Log what a guard decided").
            log.LogInfo($"[dev] infjump: press, free {free}, onground {e.onground}, cooldown {e.jumpcooldown:0.0}, "
                + $"velocity y {e.rigid.velocity.y:0.0} -> {(jumps ? "jump" : "no jump")}");
            if (jumps)
            {
                e.Jump();
                e.PlaySoundSimple("Jump");
            }
        }

        private static void OneHit(ref MainManager.BattleData target, ref int damageammount)
        {
            if (oneHit && target.battleentity != null && !target.battleentity.CompareTag("Player"))
            {
                damageammount = Math.Max(damageammount, 99);
            }
        }

        internal static void DisableGuard()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static bool HoldPickups(NPCControl __instance)
        {
            bool holding = pendingMap >= 0 || Time.realtimeSinceStartup < blockUntil;
            return !(holding && __instance.objecttype == NPCControl.ObjectTypes.Item);
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
            TickInfJump();
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
                    case "nudge": return Nudge(parts);
                    case "items": return Items();
                    case "tree": return Tree();
                    case "addleif": return AddLeif();
                    case "holdup":
                        // A test of the hold-up for an item from another player: the Explorer Permit (key item 27), queued
                        // the way the receiver queues one, display only.
                        ItemSwap.DescribeOurs(ItemIds.Base + 27, ItemIds.KeyItemKind, out string name, out Sprite sprite, out Color? color);
                        HoldUps.Received(name + " from TestPlayer", sprite, color, ItemSwap.ArticleOf(ItemIds.Base + 27, ItemIds.KeyItemKind));
                        return "holdup queued: " + name + " from TestPlayer";
                    case "infjump":
                        if (InfJumpSetting == null)
                        {
                            return "infjump: no setting";
                        }
                        InfJumpSetting.Value = !InfJumpSetting.Value;
                        return "infjump " + (infJump ? "on: press jump in mid-air to jump again" : "off");
                    case "onehit":
                        if (OneHitSetting == null)
                        {
                            return "onehit: no setting";
                        }
                        OneHitSetting.Value = !OneHitSetting.Value;
                        return "onehit " + (oneHit ? "on: every hit on an enemy does at least 99" : "off");
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
            if (pickup.Flag < 0)
            {
                // A crystal berry or a respawning pickup has no flag to find it by.
                return $"location {id} has no flag of its own: use warp {pickup.Map} @<entity name>";
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
            // warp <map> @<name>: stand beside the entity with that name (a trigger has no flag of its own). Arrives at
            // the map's own spot first and steps beside it after the transition, so a trigger starts when you walk in,
            // not mid-warp (the user, 2026-09-24: Leif's joining trigger at the lake).
            if (parts.Length > 2 && parts[2].StartsWith("@"))
            {
                // The rest of the line: entity names can have spaces ("Crystal Berry").
                pendingName = string.Join(" ", parts.Skip(2).ToArray()).Substring(1);
                return StartWarp(map, -1) + " (to " + pendingName + ")";
            }
            int flag = parts.Length > 2 ? int.Parse(parts[2]) : -1;
            return StartWarp(map, flag);
        }

        private static string pendingName;

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
        internal static Vector3? ClearSpot(Vector3 at)
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
            // A cutscene that died half-way also leaves what it took: the camera pinned away from the player, the
            // map's camera limits removed, the music faded out (Event31 did all three, 2026-09-24: camera stuck, no
            // music). The game's own resets for each: ResetCamera (MainManager.cs:7398), MapControl.RestoreLimit
            // (MapControl.cs:1432), ChangeMusic() (the map's own music, MainManager.cs:4873).
            MainManager.ResetCamera(true);
            MainManager.map?.RestoreLimit(false);
            MainManager.ChangeMusic();
            // It can also leave the screen black and the party parked: the boat scene (Event107) fades out
            // (PlayTransition 4, which ends on a black dimmer, MainManager.cs Transition case 4 -> 0) and puts the party
            // on the boat with frozen physics before it moves; it threw halfway (2026-09-25: black screen, music on).
            // Its own ending undoes both: the party back to no parent and LockRigid(false), then a fade in (id 1, which
            // fades and removes the dimmer).
            int freed = 0;
            foreach (EntityControl member in MainManager.GetPartyEntities() ?? new EntityControl[0])
            {
                if (member != null && member.transform.parent != null)
                {
                    member.transform.parent = null;
                    member.LockRigid(false);
                    freed++;
                }
            }
            bool black = MainManager.instance.transitionobj != null && MainManager.instance.transitionobj.Length > 0
                && MainManager.instance.transitionobj[0] != null;
            if (black)
            {
                MainManager.PlayTransition(1, 0, 0.1f, Color.black);
            }
            return "ran the game's end-of-event cleanup and camera, limit and music resets; inevent=" + MainManager.instance.inevent
                + ", minipause=" + MainManager.instance.minipause + $"; freed {freed} party member(s); "
                + (black ? "faded the screen back in" : "no fade left on screen");
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
            NPCControl target = pendingName != null ? entities.FirstOrDefault(e => e.name == pendingName)
                : pendingFlag >= 0 ? entities.FirstOrDefault(e => e.activationflag == pendingFlag) : null;
            string where = target != null ? "by " + (pendingName ?? "the entity with flag " + pendingFlag) : null;
            if (pendingName != null && target == null)
            {
                where = $"(nothing named {pendingName} here)";
            }
            pendingName = null;
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
                    // No safe side (the lake's pillar, the bridge room's vines, 2026-09-24): stand on the entity's own
                    // spot, which it rests on, so the tester sees where it is (the user: land by the item, never just
                    // somewhere on the map); pickups are held off right after a warp.
                    spot = target.transform.position + Vector3.up * 0.5f;
                    where += "; no safe spot beside it, so on its own spot";
                }
                MainManager.player.transform.position = spot.Value;
                if (target.objecttype == NPCControl.ObjectTypes.Item)
                {
                    target.touchcooldown = Mathf.Max(target.touchcooldown, 90f);
                }
                // And no walking for a second after arriving, so a key still held doesn't carry you into something
                // (the user, 2026-09-24).
                MainManager.player.lockkeys = true;
                unlockAt = Time.realtimeSinceStartup + 1f;
                blockUntil = Time.realtimeSinceStartup + 1.5f;
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

        // Shifts the party by (x, y, z) on this map, e.g. off a pillar onto the bank (the user, 2026-09-24).
        private static string Nudge(string[] parts)
        {
            if (parts.Length < 3 || MainManager.player == null)
            {
                return "nudge <x> <y> <z>";
            }
            float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            float z = parts.Length > 3 ? float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 0f;
            MainManager.player.transform.position += new Vector3(x, y, z);
            MainManager.TeleportFollowers(true);
            return "moved to " + MainManager.player.transform.position;
        }

        // Every pickup that exists on the current map right now (the map's own entities, not the dump): kind, id,
        // activationflag, whether the game hides it, and its distance. Written to the log, since it can be long.
        // Logs the nearest pickup's whole object tree (the entity's root down): each object's path, whether it's active,
        // and what draws it, to see what a scene or the swap really leaves on screen (2026-09-25: a crystal berry model
        // kept showing over the seed's item after two guessed fixes).
        // Dev test (the user, 2026-09-25: rehearse an open start with Leif in the party before the trapdoor). The game adds a
        // member with ChangeParty; without fromscratch its copy loop never runs (for m < 0, MainManager.cs:3805) and the
        // party list comes out empty, which is why the 2026-09-24 try left Leif without a character. With fromscratch
        // every member is rebuilt from its defaults and the stat bonuses reapplied (ApplyStatBonus). ChangeParty only
        // reuses characters that exist, so SetPlayers(positions) (MainManager.cs:9416) then makes all three where the
        // party stands. Memory only until the game saves.
        private static string AddLeif()
        {
            MainManager mm = MainManager.instance;
            if (MainManager.player == null || mm.inevent || mm.message || MainManager.battle != null)
            {
                return "addleif: not now (no player, or an event, dialogue or battle)";
            }
            if (mm.playerdata.Any(p => p.trueid == 2))
            {
                return "addleif: Leif is already in the party";
            }
            Vector3 at = MainManager.player.transform.position;
            MainManager.ChangeParty(new[] { 0, 1, 2 }, true, true);
            var spots = new Vector3[mm.playerdata.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i] = at + new Vector3(-0.6f * i, 0f, 0.1f * i);
            }
            MainManager.SetPlayers(spots);
            return "addleif: party now " + string.Join(", ", mm.playerdata.Select(p => p.trueid.ToString()).ToArray())
                + $"; characters {mm.playerdata.Count(p => p.entity != null)} of {mm.playerdata.Length}";
        }

        private static string Tree()
        {
            if (MainManager.map == null || MainManager.player == null)
            {
                return "tree: no map or player";
            }
            Vector3 me = MainManager.player.transform.position;
            NPCControl nearest = MainManager.map.GetComponentsInChildren<NPCControl>(true)
                .Where(n => n.objecttype == NPCControl.ObjectTypes.Item && n.entity != null)
                .OrderBy(n => (n.entity.transform.position - me).sqrMagnitude).FirstOrDefault();
            if (nearest == null)
            {
                return "tree: no pickup on this map";
            }
            EntityControl e = nearest.entity;
            var sb = new System.Text.StringBuilder();
            sb.Append($"[dev] tree of {nearest.name} (animid {e.animid}, model {(e.model != null ? e.model.name : "none")}, spin {e.spin}, "
                + $"sprite {(e.sprite != null && e.sprite.sprite != null ? e.sprite.sprite.name : "none")}):");
            Walk(e.transform, e.transform, sb);
            log.LogInfo(sb.ToString());
            return "tree of " + nearest.name + " logged";
        }

        private static void Walk(Transform t, Transform root, System.Text.StringBuilder sb)
        {
            Renderer r = t.GetComponent<Renderer>();
            sb.Append("\n  ").Append(new string(' ', Depth(t, root) * 2)).Append(t.name)
              .Append(t.gameObject.activeSelf ? "" : " [inactive]")
              .Append(r != null ? $" <{r.GetType().Name}{(r.enabled ? "" : " disabled")}>" : "");
            foreach (Transform child in t)
            {
                Walk(child, root, sb);
            }
        }

        private static int Depth(Transform t, Transform root)
        {
            int d = 0;
            for (Transform at = t; at != null && at != root; at = at.parent)
            {
                d++;
            }
            return d;
        }

        private static string Items()
        {
            MapControl map = MainManager.map;
            if (map == null || MainManager.player == null)
            {
                return "not now: no map";
            }
            int n = 0;
            foreach (NPCControl npc in map.GetComponentsInChildren<NPCControl>(true))
            {
                if (npc.objecttype != NPCControl.ObjectTypes.Item || npc.entity == null)
                {
                    continue;
                }
                n++;
                float distance = Vector3.Distance(npc.transform.position, MainManager.player.transform.position);
                log.LogInfo($"[dev] item on {map.mapid}: {npc.name} kind {npc.entity.animid} id {npc.entity.animstate} flag {npc.activationflag} "
                    + $"hidden {npc.entity.iskill} active {npc.gameObject.activeInHierarchy} {distance:0.0} away at {npc.transform.position}");
            }
            return $"{n} pickups on {map.mapid} (listed in the log); entity data read from {map.readdatafromothermap}";
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
