using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Dev only (Debug.DevConsole): an F9 console to reach a location fast, through the game's own functions.
    // It can leave a save in a state the story never makes. Commands: agent_docs/development.md.
    internal static partial class DevConsole
    {
        private const long LocationIdBase = 7_720_000;

        private static ManualLogSource log;
        private static ApConnection connection;
        private static bool open;
        private static string line = "";
        private static string lastResult =
            "F9: dev console. loc <n> | warp <map> [flag] | spawn <item|key|medal> <id> [flag] | flag <n> [on|off]";

        private static int pendingMap = -1;
        private static int pendingFlag = -1;
        private static float pendingSince;

        internal static void Init(ManualLogSource logger, ApConnection conn)
        {
            log = logger;
            connection = conn;
        }

        // Pickups ignore touches during a warp and ~1.5 s after: a warp lands on the item's own spot.
        private static float blockUntil = -1f;

        internal static void EnableGuard(ManualLogSource logger)
        {
            log = logger;
            if (Hooks.Install(typeof(PickupHold), "dev", "warps can't hold off pickups")
                && Hooks.Install(typeof(OneHitHook), "dev", "onehit does nothing"))
            {
                Hooks.Install(typeof(EventLog), "dev", "started events aren't logged");
            }
        }

        private static class EventLog
        {
            [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
            [HarmonyPrefix]
            private static void LogEvent(int id, NPCControl caller)
            {
                log.LogInfo($"[event] Event{id} starts on {MainManager.map?.mapid.ToString() ?? "?"}, started by "
                    + (caller != null ? $"{caller.name} ({caller.objecttype})" : "the map or code"));
            }
        }

        // Kept in the config so it survives reloads; "onehit" flips it.
        internal static BepInEx.Configuration.ConfigEntry<bool> OneHitSetting;
        private static bool oneHit => OneHitSetting != null && OneHitSetting.Value;

        // infjump: the game's own jump fires only on the ground, so one press never jumps twice.
        internal static BepInEx.Configuration.ConfigEntry<bool> InfJumpSetting;
        private static bool infJump => InfJumpSetting != null && InfJumpSetting.Value;
        internal static BepInEx.Configuration.ConfigEntry<bool> InfBerriesSetting;

        // Berries to the game's cap (999) once per save played, when its first map loads. A refill on every drop hid
        // purchases from the item shops, which see a purchase as the berries going down.
        private static bool berriesTopped;

        private static void TickInfBerries()
        {
            MainManager mm = MainManager.instance;
            if (MainManager.map == null)
            {
                berriesTopped = false; // the title screen: the next save gets its top-up
                return;
            }
            if (InfBerriesSetting == null || !InfBerriesSetting.Value || mm == null || berriesTopped)
            {
                return;
            }
            berriesTopped = true;
            mm.money = 999;
        }

        private static void TickInfJump()
        {
            if (!infJump || open || MainManager.player == null || MainManager.player.entity == null
                || !MainManager.GetKey(4, hold: false))
            {
                return;
            }
            EntityControl e = MainManager.player.entity;
            // Not jumpcooldown: it outlasts the whole jump, so it never runs out in mid-air.
            if (MainManager.FreePlayer() && !e.onground)
            {
                e.Jump();
                e.PlaySoundSimple("Jump");
            }
        }

        private static class OneHitHook
        {
            // onehit: every hit ends in this DoDamage overload, whose parameters include a private nested type.
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(BattleControl), "DoDamage", new[]
            {
                typeof(MainManager.BattleData?), typeof(MainManager.BattleData).MakeByRefType(), typeof(int),
                typeof(BattleControl.AttackProperty?), AccessTools.Inner(typeof(BattleControl), "DamageOverride")
                    .MakeArrayType(), typeof(bool),
            });

            [HarmonyPrefix]
            private static void OneHit(ref MainManager.BattleData target, ref int damageammount)
            {
                if (oneHit && target.battleentity != null && !target.battleentity.CompareTag("Player"))
                {
                    damageammount = Math.Max(damageammount, 99);
                }
            }
        }

        private static class PickupHold
        {
            [HarmonyPatch(typeof(NPCControl), "OnTriggerEnter")]
            [HarmonyPrefix]
            private static bool HoldPickups(NPCControl __instance)
            {
                bool holding = pendingMap >= 0 || Time.realtimeSinceStartup < blockUntil;
                return !(holding && __instance.objecttype == NPCControl.ObjectTypes.Item);
            }
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
            KeepMemberLooks();
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
            TickInfBerries();
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
                    // Freeze as an item-get does: lockkeys stops movement, minipause the rest (C and X are
                    // confirm/cancel).
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
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 16,
                wordWrap = true };
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
                    case "discovery": return Discovery(parts);
                    case "textsearch": return TextSearch(parts);
                    case "unstick": return Unstick();
                    case "take":
                    {
                        // As the game's own |removeitem,kind,id| does: items[kind].Remove(id). Test files only.
                        int kind = parts.Length > 1 && parts[1] == "key" ? 1 : parts.Length > 1 && parts[1] == "item"
                            ? 0 : -1;
                        if (kind < 0 || parts.Length < 3 || !int.TryParse(parts[2], out int takeId))
                        {
                            return "take <item|key> <id>";
                        }
                        return MainManager.instance.items[kind].Remove(takeId) ? $"took {parts[1]} {takeId}"
                            : $"no {parts[1]} {takeId} to take";
                    }
                    case "warpicon": return WarpButton.SetIcon(parts.Length > 1 ? parts[1] : "");
                    case "warpcolor": return WarpButton.SetColour(parts.Length > 1 ? parts[1] : "");
                    case "heal":
                        // The game's own full heal (HP and TP, the whole party), as the rematch machine uses.
                        MainManager.Heal();
                        return "party healed";
                    case "killall":
                    {
                        // HP to 0 only: the battle's own CheckDead, after the next action, ends them the game's way.
                        BattleControl battle = MainManager.battle;
                        if (battle == null || battle.enemydata == null)
                        {
                            return "killall: not in a battle";
                        }
                        int set = 0;
                        for (int i = 0; i < battle.enemydata.Length; i++)
                        {
                            if (battle.enemydata[i].hp > 0)
                            {
                                battle.enemydata[i].hp = 0;
                                set++;
                            }
                        }
                        return $"killall: {set} enemies at 0 HP; they fall after the next action";
                    }
                    case "enemylook":
                        // A visual test: reloads the current map with every ordinary map enemy looking like one enemy.
                        if (parts.Length < 2 || (parts[1] != "off" && !int.TryParse(parts[1], out _)))
                        {
                            return "enemylook <enemy id|off> [move]";
                        }
                        EnemyShuffle.LookTest = parts[1] == "off" ? -1 : int.Parse(parts[1]);
                        EnemyShuffle.MoveTest = parts.Length > 2 && parts[2] == "move";
                        return StartWarp(MainManager.map.mapid, -1) + $" (enemy look {parts[1]})";
                    case "enemyfight":
                        // A test: every map fight starts with these enemy ids.
                        if (parts.Length < 2)
                        {
                            return "enemyfight <enemy id> [id...] | off";
                        }
                        EnemyShuffle.FightTest = parts[1] == "off" ? null : parts.Skip(1).Select(int.Parse).ToArray();
                        return parts[1] == "off" ? "map fights back to the seed's" : "map fights now: "
                            + string.Join(" ", parts.Skip(1).ToArray());
                    case "nudge": return Nudge(parts);
                    case "items": return Items();
                    case "tree": return Tree();
                    case "solids": return Solids();
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
                            posLog.Append(" #").Append(index).Append(f.Length > 8 ? $"=({f[6]}, {f[7]}, {f[8]})"
                                : "=?");
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
                        TextAsset lineTable = Resources.Load<TextAsset>("Data/Dialogues" + MainManager.languageid
                            + "/Maps/" + parts[1]);
                        if (lineTable == null)
                        {
                            return "line: no dialogue table for " + parts[1];
                        }
                        string[] lineRows = lineTable.ToString().Replace("\r\n", "\n").Split('\n');
                        var lineLog = new System.Text.StringBuilder("[dev] lines of " + parts[1] + ":");
                        foreach (string n in parts.Skip(2))
                        {
                            int at = int.Parse(n);
                            lineLog.Append("\n  ").Append(at).Append(": ").Append(at < lineRows.Length ? lineRows[at]
                                : "(none)");
                        }
                        log.LogInfo(lineLog.ToString());
                        return "lines logged";
                    case "prices":
                        var priceLog = new System.Text.StringBuilder("[dev] prices:");
                        foreach (string idText in parts.Skip(1))
                        {
                            int medal = int.Parse(idText);
                            priceLog.Append(" ").Append(medal).Append("=").Append(MainManager.badgedata[medal, 5])
                                .Append("b/")
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
                            guiLog.Append("\n  ").Append(top.name)
                                .Append(top.gameObject.activeSelf ? "" : " [inactive]")
                                .Append(tr != null ? " <" + tr.GetType().Name + ">" : "").Append(" children ")
                                .Append(top.childCount);
                        }
                        log.LogInfo(guiLog.ToString());
                        return "gui logged";
                    case "display":
                    {
                        // The inputs of the game's own frame-rate code (MainManager's vSyncCount and targetFrameRate
                        // lines).
                        Resolution cur = Screen.currentResolution;
                        string display =
                            $"display: current {cur.width}x{cur.height} @ {cur.refreshRate} Hz, window {Screen.width}x{Screen.height}, "
                            + $"fullscreen {Screen.fullScreen} ({Screen.fullScreenMode}); game settings fps {MainManager.fps}, vsync {MainManager.vsync}; "
                            + $"Unity vSyncCount {QualitySettings.vSyncCount}, targetFrameRate {Application.targetFrameRate}; "
                            + $"measured {1f / Time.smoothDeltaTime:0.0} fps; fixedDeltaTime {Time.fixedDeltaTime}, "
                            + $"player rigidbody interpolation {(MainManager.player != null && MainManager.player.entity != null && MainManager.player.entity.rigid != null ? MainManager.player.entity.rigid.interpolation.ToString() : "no player")}, "
                            + $"characters tracked {FrameRate.SmoothedCount} (bodylerp {(FrameRate.SmoothBodies ? "on" : "off")}), "
                            + $"Physics.autoSyncTransforms {Physics.autoSyncTransforms}";
                        log.LogInfo("[dev] " + display);
                        return display;
                    }
                    case "fps":
                    {
                        // A look at a frame cap for this session only; the game's own settings put theirs back when
                        // applied.
                        if (parts.Length < 2 || !int.TryParse(parts[1], out int cap))
                        {
                            return "fps <cap, -1 uncapped>";
                        }
                        QualitySettings.vSyncCount = 0;
                        Application.targetFrameRate = cap;
                        return $"fps: vSyncCount 0, targetFrameRate {Application.targetFrameRate}";
                    }
                    case "frames":
                        return FrameRate.StartSample(parts.Length > 1 && float.TryParse(parts[1], out float secs) ? secs
                            : 5f);
                    case "trace":
                        return FrameRate.StartTrace(parts.Length > 1 && int.TryParse(parts[1], out int traceFrames)
                            ? traceFrames : 40);
                    case "il":
                    {
                        // il <Type> <Method> [iter]: the method's IL (its iterator's MoveNext with "iter"), to write a
                        // patch against.
                        if (parts.Length < 3)
                        {
                            return "il <Type> <Method> [iter]";
                        }
                        Type type = typeof(MainManager).Assembly.GetType(parts[1]);
                        if (type == null)
                        {
                            return $"il: type {parts[1]} not found";
                        }
                        int dumped = 0;
                        foreach (System.Reflection.MethodInfo overload in type.GetMethods(
                            System.Reflection.BindingFlags.DeclaredOnly
                            | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
                            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                            .Where(m => m.Name == parts[2]))
                        {
                            System.Reflection.MethodInfo method = parts.Length > 3 && parts[3] == "iter"
                                ? HarmonyLib.AccessTools.EnumeratorMoveNext(overload) : overload;
                            if (method == null)
                            {
                                continue;
                            }
                            var ilLog =
                                new System.Text.StringBuilder(
                                    $"[dev] il {parts[1]}.{parts[2]}{overload.GetParameters().Length}:");
                            int index = 0;
                            foreach (KeyValuePair<System.Reflection.Emit.OpCode, object> ins
                                in HarmonyLib.PatchProcessor.ReadMethodBody(method))
                            {
                                string operand = ins.Value is System.Reflection.MemberInfo mi ? mi.DeclaringType?.Name
                                    + "." + mi.Name
                                    : ins.Value is float f ? f.ToString("R") + "f" : ins.Value?.ToString() ?? "";
                                ilLog.Append("\n  ").Append(index++).Append(' ').Append(ins.Key.Name).Append(' ')
                                    .Append(operand);
                            }
                            log.LogInfo(ilLog.ToString());
                            dumped++;
                        }
                        return $"il: {dumped} overloads logged";
                    }
                    case "fpsscan":
                        return FrameRate.Scan();
                    case "rates":
                        return FrameRate.StartRates(parts.Length > 1 && float.TryParse(parts[1], out float rateSecs)
                            ? rateSecs : 5f);
                    case "cams":
                    {
                        var camLog =
                            new System.Text.StringBuilder(
                                $"[dev] cams: QualitySettings.antiAliasing {QualitySettings.antiAliasing}, "
                                + $"downsample {MainManager.downsample}, runInBackground {Application.runInBackground}, lowtexture {MainManager.lowtexture}, "
                                + $"masterTextureLimit {QualitySettings.masterTextureLimit}, anisotropic {QualitySettings.anisotropicFiltering}:");
                        foreach (Camera c in Camera.allCameras)
                        {
                            camLog.Append($"\n  {c.name} depth {c.depth} enabled {c.enabled} parent {(c.transform.parent != null ? c.transform.parent.name : "none")} "
                                + $"mask {c.cullingMask} clear {c.clearFlags} main {c == MainManager.MainCamera} guicam {c == MainManager.GUICamera} "
                                + $"path {c.renderingPath}/{c.actualRenderingPath} msaa {c.allowMSAA} hdr {c.allowHDR} rect {c.rect} "
                                + $"target {(c.targetTexture != null ? c.targetTexture.width + "x" + c.targetTexture.height + " aa" + c.targetTexture.antiAliasing : "screen")} "
                                + $"effects {string.Join(",", System.Array.ConvertAll(c.GetComponents<MonoBehaviour>(), m => m.GetType().Name + (m.enabled ? "" : "(off)")))}");
                        }
                        Transform quad = MainManager.GUICamera != null && MainManager.GUICamera.transform.childCount > 0
                            ? MainManager.GUICamera.transform.GetChild(0) : null;
                        Renderer quadRenderer = quad != null ? quad.GetComponentInChildren<MeshRenderer>(true) : null;
                        camLog.Append($"\n  render-scale quad: {(quad != null ? quad.name + " active " + quad.gameObject.activeSelf : "none")}, "
                            + $"shader {(quadRenderer != null ? quadRenderer.sharedMaterial.shader.name : "none")}");
                        log.LogInfo(camLog.ToString());
                        return "cams logged";
                    }
                    case "camlerp":
                        FrameRate.SmoothCamera = parts.Length > 1 && parts[1] == "on";
                        return "camlerp: " + (FrameRate.SmoothCamera ? "on" : "off");
                    case "bodylerp":
                        FrameRate.SmoothBodies = parts.Length > 1 && parts[1] == "on";
                        return "bodylerp: " + (FrameRate.SmoothBodies ? "on" : "off")
                            + $" ({FrameRate.SmoothedCount} characters tracked)";
                    case "bodytrace":
                        return FrameRate.StartBodyTrace(parts.Length > 1 && int.TryParse(parts[1], out int bodyFrames)
                            ? bodyFrames : 120);
                    case "interp":
                    {
                        bool interpOn = parts.Length > 1 && parts[1] == "on";
                        int changed = 0;
                        foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>())
                        {
                            if (e.rigid != null)
                            {
                                e.rigid.interpolation = interpOn ? RigidbodyInterpolation.Interpolate
                                    : RigidbodyInterpolation.None;
                                changed++;
                            }
                        }
                        return $"interp: {(interpOn ? "Interpolate" : "None")} on {changed} rigidbodies";
                    }
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
                    case "radii":
                    {
                        // Each entity's talk radius (npcdata.radius, entity data column 13) and its distance now.
                        var radiiLog = new System.Text.StringBuilder("[dev] radii:");
                        foreach (NPCControl n in UnityEngine.Object.FindObjectsOfType<NPCControl>()
                            .Where(n => n.gameObject.activeInHierarchy && MainManager.player != null)
                            .OrderBy(n => MainManager.GetDistance(n.transform.position,
                                MainManager.player.transform.position, ignoreY: false)))
                        {
                            float d = MainManager.GetDistance(n.transform.position, MainManager.player.transform.position,
                                ignoreY: false);
                            radiiLog.Append($"\n  {n.name} {n.entitytype}/{n.objecttype} radius {n.radius} distance {d:0.00}");
                        }
                        log.LogInfo(radiiLog.ToString());
                        return "radii logged";
                    }
                    case "who":
                    {
                        // Every character drawn as a party member (animid 0 Vi, 1 Kabbu, 2 Leif).
                        var whoLog = new System.Text.StringBuilder("[dev] who:");
                        foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>()
                            .Where(e => e.animid >= 0 && e.animid <= 2))
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
                        string targetName = target == null ? (ReferenceEquals(target, null) ? "none" : "DESTROYED")
                            : target.name;
                        string camLog =
                            $"[dev] cam: target {targetName}, player {(MainManager.player != null ? MainManager.player.name + " at " + MainManager.player.transform.position : "none")}, "
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
                    case "removemember":
                        return parts.Length > 1 && int.TryParse(parts[1], out int gone) && gone >= 0 && gone <= 2
                            ? "removemember: " + PartyMembers.Remove(gone)
                            : "removemember <0 Vi | 1 Kabbu | 2 Leif>";
                    case "holdup":
                    {
                        // holdup [member n | ap | long]: the permit, party member n (0 Vi, 1 Kabbu, 2 Leif), the drawn
                        // Archipelago icon, or lines too wide for the box.
                        if (parts.Length > 1 && parts[1] == "long")
                        {
                            // The line seen running off the box, a longer one, and ServerText's longest name with a
                            // player and alone (no place to break: squashed).
                            Color plum = new Color(0xAF / 255f, 0x99 / 255f, 0xEF / 255f);
                            string longest = string.Join(" ", Enumerable.Repeat("Extremely Long Item Name", 5).ToArray())
                                .Substring(0, ServerText.MaxLength);
                            HoldUps.Received(ItemSwap.FromText("Poison Resistance Medal",
                                Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude, "BugTester"), ApIcon.Get(),
                                plum, "a");
                            HoldUps.Received(ItemSwap.FromText("Progressive Grappling Hook Upgrade",
                                Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement, "AVeryLongPlayerName"),
                                ApIcon.Get(), plum, "a");
                            HoldUps.Received(ItemSwap.FromText(longest,
                                Archipelago.MultiClient.Net.Enums.ItemFlags.None, "BugTester"), ApIcon.Get(), plum, "");
                            HoldUps.Received(ItemSwap.ClassText(longest,
                                Archipelago.MultiClient.Net.Enums.ItemFlags.None), ApIcon.Get(), plum, "");
                            return "holdup queued: four lines too wide for the box";
                        }
                        if (parts.Length > 1 && parts[1] == "ap")
                        {
                            // The drawn icon on two of the class backdrops a real item gets (ItemSwap.Describe's plum
                            // and cyan).
                            foreach (Color backdrop in new[] { new Color(0xAF / 255f, 0x99 / 255f, 0xEF / 255f),
                                new Color(0f, 0xEE / 255f, 0xEE / 255f) })
                            {
                                HoldUps.Received(ItemSwap.FromText("Archipelago icon",
                                    Archipelago.MultiClient.Net.Enums.ItemFlags.None, "TestPlayer"),
                                    ApIcon.Get(), backdrop, "an");
                            }
                            return "holdup queued: the Archipelago icon";
                        }
                        bool asMember = parts.Length > 2 && parts[1] == "member";
                        long held = asMember ? ItemIds.Base + ItemIds.MemberOffset + int.Parse(parts[2]) : ItemIds.Base
                            + 27;
                        int heldKind = asMember ? ItemIds.MemberKind : ItemIds.KeyItemKind;
                        ItemSwap.DescribeOurs(held, heldKind, out string name, out Sprite sprite, out Color? color);
                        HoldUps.Received(ItemSwap.FromText(name,
                            Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement, "TestPlayer"), sprite, color,
                            ItemSwap.ArticleOf(held, heldKind));
                        return "holdup queued: " + name + " from TestPlayer";
                    }
                    case "colortry":
                    {
                        // colortry <hex...>: a trap's "You got" line per colour, each added after ours for the test
                        // only.
                        HoldUps.AddApColors();
                        ItemSwap.DescribeOurs(ItemIds.Base + 27, ItemIds.KeyItemKind, out string permit,
                            out Sprite permitSprite, out Color? permitColor);
                        var tried = new List<Color>(MainManager.instance.textcolors);
                        foreach (string hex in parts.Skip(1))
                        {
                            int rgb = Convert.ToInt32(hex, 16);
                            tried.Add(new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f,
                                (rgb & 0xFF) / 255f));
                            HoldUps.Received($"|color,{tried.Count - 1}|trap {hex}|color,0| from "
                                + ItemSwap.PlayerText("TestPlayer"),
                                permitSprite, permitColor, "a");
                        }
                        MainManager.instance.textcolors = tried.ToArray();
                        return $"colortry: {parts.Length - 1} hold-ups queued";
                    }
                    case "shelflook":
                    {
                        // shelflook <location id> <white|black> <rim share> | shelflook off: a shop slot shows the
                        // drawn icon so.
                        if (parts.Length > 1 && parts[1] == "off")
                        {
                            ItemSwap.DevLooks.Clear();
                            return "shelflook: every slot back to the seed's item";
                        }
                        if (parts.Length < 4)
                        {
                            return "shelflook <location id> <white|black> <rim share> | shelflook off";
                        }
                        long at = LocationIdBase + long.Parse(parts[1]);
                        ItemSwap.DevLooks[at] =
                            ApIcon.Get(float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                            parts[2] == "white" ? Color.white : Color.black);
                        return $"shelflook: location {at} shows the icon, {parts[2]} outline {parts[3]}";
                    }
                    case "iteminfo":
                    {
                        // Every item entity on the map: its sprite, pivot, size and the offsets that place it (shelf
                        // heights).
                        Sprite star = MainManager.guisprites[85];
                        var info = new System.Text.StringBuilder("[dev] items on " + MainManager.map.mapid
                            + $" (starburst {star.name} pivot {star.pivot} rect {star.rect.size} bounds c{star.bounds.center} e{star.bounds.extents}):");
                        foreach (NPCControl npc in MainManager.map.GetComponentsInChildren<NPCControl>(true))
                        {
                            EntityControl e = npc.entity;
                            if (e == null || e.sprite == null || (npc.objecttype != NPCControl.ObjectTypes.Item
                                && e.sprite.sprite == null))
                            {
                                continue;
                            }
                            Sprite sp = e.sprite.sprite;
                            Transform mark = e.sprite.transform.Find("apback");
                            info.Append($"\n  {npc.name} ({npc.objecttype}, animid {e.animid}, state {e.animstate}) at {e.transform.position}: sprite "
                                + (sp == null ? "none"
                                : $"{sp.name} pivot {sp.pivot} rect {sp.rect.size} bounds c{sp.bounds.center} e{sp.bounds.extents}")
                                + $"; spritetransform {(e.spritetransform != null ? e.spritetransform.localPosition.ToString() : "none")}"
                                + $"; sprite local {e.sprite.transform.localPosition} parent {(e.sprite.transform.parent != null ? e.sprite.transform.parent.name : "none")}"
                                + (mark != null ? $"; apback {mark.localPosition}" : ""));
                        }
                        log.LogInfo(info.ToString());
                        return "item info logged";
                    }
                    case "markcolor":
                    {
                        // markcolor <progression|useful|trap|filler> <hex>: a class's starburst colour, live.
                        string[] classes = { "progression", "useful", "trap", "filler" };
                        int which = parts.Length > 2 ? Array.IndexOf(classes, parts[1]) : -1;
                        if (which < 0)
                        {
                            return "markcolor <progression|useful|trap|filler> <hex> (now "
                                + string.Join(" ", ItemSwap.ClassColors.Select(c => c.ToString("X6")).ToArray()) + ")";
                        }
                        ItemSwap.ClassColors[which] = Convert.ToInt32(parts[2], 16);
                        return $"markcolor: {classes[which]} now {parts[2]}";
                    }
                    case "hide":
                    {
                        // hide <entity name>: switch an entity on this map off until the map reloads (nothing saved).
                        if (parts.Length < 2 || MainManager.map == null)
                        {
                            return "hide <entity name>";
                        }
                        NPCControl found = MainManager.map.GetComponentsInChildren<NPCControl>(true)
                            .FirstOrDefault(n => n.name == parts[1]);
                        if (found == null)
                        {
                            return "hide: no " + parts[1] + " on this map";
                        }
                        found.gameObject.SetActive(false);
                        return "hidden until the map reloads: " + parts[1];
                    }
                    case "markclass":
                    {
                        // markclass <entity name> <progression|useful|trap|filler> | markclass off: force a slot's
                        // backdrop class.
                        string[] classes = { "progression", "useful", "trap", "filler" };
                        if (parts.Length > 1 && parts[1] == "off")
                        {
                            ItemSwap.DevClasses.Clear();
                            return "markclass: every backdrop its own class again";
                        }
                        int which = parts.Length > 2 ? Array.IndexOf(classes, parts[2]) : -1;
                        if (which < 0)
                        {
                            return "markclass <entity name> <progression|useful|trap|filler> | markclass off";
                        }
                        ItemSwap.DevClasses[parts[1]] = which;
                        return $"markclass: {parts[1]} drawn as {classes[which]}";
                    }
                    case "mark":
                    {
                        // mark <scale> <raise>: the backdrop behind a check's item, live.
                        if (parts.Length < 3)
                        {
                            return $"mark <scale> <raise> (now {ItemSwap.MarkScale} {ItemSwap.MarkRaise})";
                        }
                        ItemSwap.MarkScale = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                        ItemSwap.MarkRaise = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        return $"mark now scale {ItemSwap.MarkScale}, raise {ItemSwap.MarkRaise}";
                    }
                    case "letters":
                    {
                        // The game's 500-letter text pool: how many are taken (text set), and by which text holder.
                        var pool = (TextMesh[])HarmonyLib.AccessTools.Field(typeof(MainManager), "letterpool")
                            .GetValue(null);
                        var holders = new Dictionary<string, int>();
                        int taken = 0;
                        foreach (TextMesh letter in pool)
                        {
                            if (letter == null || letter.text == "")
                            {
                                continue;
                            }
                            taken++;
                            // Holder names carry their whole text (the font preloader's is every glyph the game has),
                            // and BepInEx's console broke writing that: only the owner's name, cut short and plain.
                            Transform t = letter.transform.parent;
                            string owner = t == null ? "(none)" : t.parent != null ? t.parent.name : t.name;
                            owner = new string(owner.Where(c => c >= ' ' && c < 127).Take(40).ToArray());
                            string path = owner + (t != null && !t.gameObject.activeInHierarchy ? " [hidden]" : "");
                            holders[path] = holders.TryGetValue(path, out int n) ? n + 1 : 1;
                        }
                        log.LogInfo($"[dev] letters: {taken} of {pool.Length} taken; "
                            + string.Join("; ", holders.OrderByDescending(h => h.Value)
                            .Select(h => h.Key + " " + h.Value).ToArray()));
                        return $"letters: {taken} of {pool.Length} taken";
                    }
                    case "menuinfo":
                    {
                        string menu = ApMenu.Open == null ? "no Archipelago panel open" : ApMenu.Open.TextReport();
                        log.LogInfo("[dev] menuinfo: " + menu);
                        return "menuinfo logged";
                    }
                    case "palette":
                        log.LogInfo("[dev] text colours: " + string.Join(", ", MainManager.instance.textcolors
                            .Select((c, i) => i + " " + ColorUtility.ToHtmlStringRGB(c)).ToArray()));
                        return "palette logged";
                    case "articles":
                    {
                        // The found-item line's article: the default (menutext[125]) and each item's own (itemdata[0,
                        // id, 3]).
                        var articles = new System.Text.StringBuilder("[dev] default article '"
                            + MainManager.menutext[125] + "'");
                        foreach (string n in parts.Skip(1))
                        {
                            int id = int.Parse(n);
                            articles.Append(
                                $"; item {id} {MainManager.itemdata[0, id, 0]}: '{MainManager.itemdata[0, id, 3]}'");
                        }
                        // A pickup's "You found" line (menutext 2) and Giveitem's "You got" lines (106, and 110 for the
                        // other case).
                        articles.Append($"; menutext[2] '{MainManager.menutext[2]}'; menutext[106] '{MainManager.menutext[106]}'; menutext[110] '{MainManager.menutext[110]}'");
                        log.LogInfo(articles.ToString());
                        return "articles logged";
                    }
                    case "infberries":
                        if (InfBerriesSetting == null)
                        {
                            return "infberries: no setting";
                        }
                        InfBerriesSetting.Value = !InfBerriesSetting.Value;
                        return "infberries " + (InfBerriesSetting.Value ? "on: berries stay at 999" : "off");
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
    }
}
