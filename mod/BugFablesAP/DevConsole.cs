using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only (Debug.DevConsole): an F9 console to reach a location fast, through the game's own functions.
    // It can leave a save in a state the story never makes. Commands: agent_docs/development.md.
    internal static class DevConsole
    {
        private const long LocationIdBase = 7_720_000;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static bool open;
        private static string line = "";
        private static string lastResult = "F9: dev console. loc <n> | warp <map> [flag] | spawn <item|key|medal> <id> [flag] | flag <n> [on|off]";

        private static int pendingMap = -1;
        private static int pendingFlag = -1;
        private static float pendingSince;

        internal static void Init(ManualLogSource logger, ApConnection conn)
        {
            log = logger;
            connection = conn;
        }

        // Pickups ignore touches during a warp and ~1.5 s after: a warp lands on the item's own spot.
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
            // onehit: every hit ends in this DoDamage overload.
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

        // Kept in the config so it survives reloads; "onehit" flips it.
        internal static BepInEx.Configuration.ConfigEntry<bool> OneHitSetting;
        private static bool oneHit => OneHitSetting != null && OneHitSetting.Value;

        // infjump: the game's own jump fires only on the ground, so one press never jumps twice.
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
            // Not jumpcooldown: it outlasts the whole jump, so it never runs out in mid-air.
            bool jumps = free && !e.onground;
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

        // Debug.DevCommandFile: commands from outside the game, polled twice a second and run one at a time.
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
            // A queued warp waits for the player to be free rather than failing with "not now".
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
                    // Freeze as an item-get does: lockkeys stops movement, minipause the rest (C and X are confirm/cancel).
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
                    case "enemylook":
                        // A visual test: reloads the current map with every ordinary map enemy looking like one enemy.
                        if (parts.Length < 2 || (parts[1] != "off" && !int.TryParse(parts[1], out _)))
                        {
                            return "enemylook <enemy id|off>";
                        }
                        EnemyShuffle.LookTest = parts[1] == "off" ? -1 : int.Parse(parts[1]);
                        return StartWarp(MainManager.map.mapid, -1) + $" (enemy look {parts[1]})";
                    case "nudge": return Nudge(parts);
                    case "items": return Items();
                    case "tree": return Tree();
                    case "script": return Script(parts);
                    case "pos":
                    {
                        // Start position from the map's entity table (fields 6-8).
                        if (parts.Length < 3 || !Enum.TryParse(parts[1], true, out MainManager.Maps posMap))
                        {
                            return "pos <map> <entity index>...";
                        }
                        TextAsset table = Resources.Load<TextAsset>("Data/EntityData/" + (int)posMap);
                        string[] rows = table == null ? new string[0] : table.ToString().Split('\n');
                        var posLog = new System.Text.StringBuilder("[dev] pos " + posMap + ":");
                        foreach (string indexText in parts.Skip(2))
                        {
                            int index = int.Parse(indexText);
                            string[] f = index < rows.Length ? rows[index].Split('}') : new string[0];
                            posLog.Append(" #").Append(index).Append(f.Length > 8 ? $"=({f[6]}, {f[7]}, {f[8]})" : "=?");
                        }
                        log.LogInfo(posLog.ToString());
                        return "pos logged";
                    }
                    case "berries":
                        // As the game's money script command does: clamped to 0-999. Test files only.
                        if (parts.Length < 2 || !int.TryParse(parts[1], out int berries))
                        {
                            return "berries <n>";
                        }
                        MainManager.instance.money = Mathf.Clamp(MainManager.instance.money + berries, 0, 999);
                        MainManager.instance.showmoney = 1f;
                        return "berries now " + MainManager.instance.money;
                    case "line":
                        if (parts.Length < 3)
                        {
                            return "line <map> <n> [n...]";
                        }
                        TextAsset lineTable = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid + "/Maps/" + parts[1]);
                        if (lineTable == null)
                        {
                            return "line: no dialogue table for " + parts[1];
                        }
                        string[] lineRows = lineTable.ToString().Replace("\r\n", "\n").Split('\n');
                        var lineLog = new System.Text.StringBuilder("[dev] lines of " + parts[1] + ":");
                        foreach (string n in parts.Skip(2))
                        {
                            int at = int.Parse(n);
                            lineLog.Append("\n  ").Append(at).Append(": ").Append(at < lineRows.Length ? lineRows[at] : "(none)");
                        }
                        log.LogInfo(lineLog.ToString());
                        return "lines logged";
                    case "prices":
                        var priceLog = new System.Text.StringBuilder("[dev] prices:");
                        foreach (string idText in parts.Skip(1))
                        {
                            int medal = int.Parse(idText);
                            priceLog.Append(" ").Append(medal).Append("=").Append(MainManager.badgedata[medal, 5]).Append("b/")
                                .Append(MainManager.badgedata[medal, 7]).Append("c");
                        }
                        log.LogInfo(priceLog.ToString());
                        return "prices logged";
                    case "gui":
                        if (MainManager.GUICamera == null)
                        {
                            return "gui: no GUI camera";
                        }
                        var guiLog = new System.Text.StringBuilder("[dev] gui:");
                        foreach (Transform top in MainManager.GUICamera.transform)
                        {
                            Renderer tr = top.GetComponent<Renderer>();
                            guiLog.Append("\n  ").Append(top.name).Append(top.gameObject.activeSelf ? "" : " [inactive]")
                                .Append(tr != null ? " <" + tr.GetType().Name + ">" : "").Append(" children ").Append(top.childCount);
                        }
                        log.LogInfo(guiLog.ToString());
                        return "gui logged";
                    case "addleif": return AddLeif();
                    case "follower":
                    {
                        // Adds a follower as the story does (e.g. Maki is 46).
                        if (parts.Length < 2 || !int.TryParse(parts[1], out int followerId))
                        {
                            return "follower <animid>";
                        }
                        MainManager.instance.extrafollowers.Add(followerId);
                        MainManager.AddFollower(null, followerId);
                        return $"follower {followerId} added; followers now {string.Join(",", MainManager.instance.extrafollowers.Select(f => f.ToString()).ToArray())}";
                    }
                    case "who":
                    {
                        // Every character drawn as a party member (animid 0 Vi, 1 Kabbu, 2 Leif).
                        var whoLog = new System.Text.StringBuilder("[dev] who:");
                        foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>().Where(e => e.animid >= 0 && e.animid <= 2))
                        {
                            whoLog.Append($"\n  {e.name} animid {e.animid} at {e.transform.position}, parent {(e.transform.parent != null ? e.transform.parent.name : "none")}, "
                                + $"tag {e.tag}, following {(e.following != null ? e.following.name : "none")}, tempfollower {e.tempfollower}, "
                                + $"playerentity {e.playerentity}, npcdata {(e.npcdata != null ? e.npcdata.name : "none")}, active {e.gameObject.activeInHierarchy}");
                        }
                        log.LogInfo(whoLog.ToString());
                        return "who logged";
                    }
                    case "cam":
                    {
                        MainManager m = MainManager.instance;
                        Transform target = m.camtarget;
                        string targetName = target == null ? (ReferenceEquals(target, null) ? "none" : "DESTROYED") : target.name;
                        string camLog = $"[dev] cam: target {targetName}, player {(MainManager.player != null ? MainManager.player.name + " at " + MainManager.player.transform.position : "none")}, "
                            + $"camera at {MainManager.MainCamera.transform.position}, camtargetpos {m.camtargetpos}, offset {m.camoffset}, offset2 {m.camoffset2}, "
                            + $"angle {m.camangleoffset}, speed {m.camspeed}, insideid {m.insideid}, limits {MainManager.map?.camlimitpos} / {MainManager.map?.camlimitneg}, "
                            + $"party {string.Join(",", m.playerdata.Select(p => p.trueid + (p.entity != null ? ":" + p.entity.name : ":no entity")).ToArray())}, "
                            + $"inevent {m.inevent}, minipause {m.minipause}";
                        log.LogInfo(camLog);
                        return "cam logged";
                    }
                    case "addmember":
                        return parts.Length > 1 && int.TryParse(parts[1], out int member) && member >= 0 && member <= 2
                            ? "addmember: " + PartyMembers.Add(member)
                            : "addmember <0 Vi | 1 Kabbu | 2 Leif>";
                    case "holdup":
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
                // Taken already: clear its flag so the map creates it again.
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
            // @<name>: land at the map's origin, then step beside the named entity, so a trigger starts on walking in, not mid-warp.
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
            // Warp onto the entity's own spot, never beside it: TransferMap waits for a walk to its target, which never ends
            // over water. FinishWarp guards the item, then steps aside once the transition is over.
            Vector3? at = flag >= 0 ? StartPosition(map, flag) : null;
            guarded = false;
            MainManager.instance.StartCoroutine(MainManager.TransferMap((int)map, at.HasValue ? at.Value + Vector3.up * 0.5f : Vector3.zero));
            return "warping to " + map + skipped;
        }


        // A spot beside the entity with room for the party and safe ground below; null when none is.
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
                    // Water raycasts as ground: hazards (water, spikes, pits) carry the Hazards component.
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

        // Entity table: fields 6-8 are the start position, field 194 the activationflag.
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

        // Auto-start cutscenes run on arrival while their flag is off; out of story order they can crash, so a warp
        // marks them seen first.
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

        // The game's private end-of-event cleanup: clears inevent and minipause and resets the player.
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
                // A transfer stuck walking to an unreachable target: stop the walk and the transition it holds open.
                MainManager.player.entity?.StopForceMove();
            }
            MainManager.roomtransition = false;
            pendingMap = -1;
            // A dead cutscene can leave the camera pinned, the limits removed and the music faded out.
            MainManager.ResetCamera(true);
            MainManager.map?.RestoreLimit(false);
            MainManager.ChangeMusic();
            // It can also leave a black dimmer and the party parented to scenery with frozen physics (the boat scene).
            int freed = 0;
            foreach (EntityControl member in MainManager.GetPartyEntities() ?? new EntityControl[0])
            {
                if (member == null)
                {
                    continue;
                }
                if (member.transform.parent != null)
                {
                    member.transform.parent = null;
                    freed++;
                }
                // And gravity off with a forced animation (the trapdoor scene).
                member.LockRigid(false);
                if (member.rigid != null)
                {
                    member.rigid.useGravity = true;
                }
                member.overrideanim = false;
                member.animstate = 0;
                member.StopForceMove();
            }
            // A dialogue that died mid-line leaves message set, freezing the player: undo it as the game's dialogue end does.
            MainManager mmd = MainManager.instance;
            bool talking = mmd.message || mmd.waitinput || mmd.prompt;
            mmd.message = false;
            mmd.waitinput = false;
            mmd.prompt = false;
            mmd.inlist = false;
            mmd.minipause = false;
            mmd.overridefollower = false;
            System.Reflection.FieldInfo boxField = HarmonyLib.AccessTools.Field(typeof(MainManager), "textbox");
            if (boxField != null && boxField.GetValue(boxField.IsStatic ? null : mmd) is Transform box && box != null)
            {
                DialogueAnim anim = box.GetComponent<DialogueAnim>();
                if (anim != null)
                {
                    anim.shrink = true;
                }
                UnityEngine.Object.Destroy(box.gameObject, 1f);
            }
            // That field holds only the letters; the speech box is maintextbox.
            if (MainManager.maintextbox != null)
            {
                DialogueAnim boxAnim = MainManager.maintextbox.GetComponent<DialogueAnim>();
                if (boxAnim != null)
                {
                    boxAnim.shrink = true;
                }
                UnityEngine.Object.Destroy(MainManager.maintextbox, 1f);
            }
            // An orphan Textbox(Clone) can remain under the GUI camera even so; with no dialogue running, remove it.
            int boxes = 0;
            if (MainManager.GUICamera != null)
            {
                foreach (Transform child in MainManager.GUICamera.transform)
                {
                    if (child.name == "Textbox(Clone)")
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                        boxes++;
                    }
                }
            }
            if (MainManager.player != null && MainManager.player.entity != null && MainManager.player.entity.rigid != null)
            {
                MainManager.player.entity.rigid.constraints = RigidbodyConstraints.FreezeRotation;
            }
            bool black = MainManager.instance.transitionobj != null && MainManager.instance.transitionobj.Length > 0
                && MainManager.instance.transitionobj[0] != null;
            if (black)
            {
                MainManager.PlayTransition(1, 0, 0.1f, Color.black);
            }
            return "ran the game's end-of-event cleanup and camera, limit and music resets; inevent=" + MainManager.instance.inevent
                + ", minipause=" + MainManager.instance.minipause + (talking ? "; closed a dead dialogue" : "")
                + (boxes > 0 ? $"; removed {boxes} leftover speech box(es)" : "")
                + $"; freed {freed} party member(s); "
                + (black ? "faded the screen back in" : "no fade left on screen");
        }

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
            // As soon as the map exists: the party lands on the item, so hold its touch until the step aside.
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
                // First side with room and safe ground; the touch cooldown holds the pickup about 1.5 s.
                Vector3? spot = ClearSpot(target.transform.position);
                if (!spot.HasValue)
                {
                    // No safe side: stand on the entity's own spot, so the tester sees where it is.
                    spot = target.transform.position + Vector3.up * 0.5f;
                    where += "; no safe spot beside it, so on its own spot";
                }
                MainManager.player.transform.position = spot.Value;
                if (target.objecttype == NPCControl.ObjectTypes.Item)
                {
                    target.touchcooldown = Mathf.Max(target.touchcooldown, 90f);
                }
                // No walking for a second after arriving, so a held key doesn't carry the party into something.
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

        // ChangeParty needs fromscratch (else its copy loop never runs and the party list comes out empty); then
        // SetPlayers makes the characters. Memory only until the game saves.
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

        private static string Script(string[] parts)
        {
            if (parts.Length < 2)
            {
                return "script <map>";
            }
            TextAsset asset = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid + "/Maps/" + parts[1]);
            if (asset == null)
            {
                return "script: no dialogue table for " + parts[1];
            }
            string[] rows = asset.ToString().Replace("\r\n", "\n").Split('\n');
            var sb = new System.Text.StringBuilder("[dev] script " + parts[1] + ":");
            var token = new System.Text.RegularExpressions.Regex(@"\|([a-zA-Z]+)((?:,[^|]*)?)\|");
            for (int i = 0; i < rows.Length; i++)
            {
                var tokens = token.Matches(rows[i]).Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value.ToLowerInvariant() + m.Groups[2].Value).ToArray();
                if (tokens.Length > 0)
                {
                    sb.Append("\n  ").Append(i).Append(": ").Append(string.Join(" ", tokens));
                }
            }
            log.LogInfo(sb.ToString());
            return "script of " + parts[1] + " logged";
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
