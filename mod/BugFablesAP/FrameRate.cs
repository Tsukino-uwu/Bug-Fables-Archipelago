using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The Quality of life row "Uncap FPS": a frame cap above the game's 30 or 60, with the game playing as it does at 60.
    // The game moves its camera and characters in FixedUpdate (50 steps a second), so motion is drawn between the last two
    // steps; what it counts in frames is held to 60 a second; what it scales by frame time inside a physics step reads as
    // it does at 60.
    internal static class FrameRate
    {
        private static ManualLogSource log;
        private static Harmony harmony;
        private static Func<bool> settingsOn;

        // Read once a frame (Tick): the hooks below run thousands of times a frame.
        private static bool active;
        private static int activeCap;
        private static bool installed;

        internal static int Cap
        {
            get
            {
                string v = QualityOfLife.UncapFps?.Value;
                return v != null && int.TryParse(v, out int cap) && cap > 60 ? cap : 0;
            }
        }

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> settingsEnabled)
        {
            log = logger;
            settingsOn = settingsEnabled;
            Camera.onPreCull += BeforeDraw;
            Camera.onPostRender += AfterDraw;
            harmony = new Harmony(guid + ".fps." + DateTime.UtcNow.Ticks);
            try
            {
                Install();
            }
            catch (Exception e)
            {
                log.LogError("[fps] NOT installed, Uncap FPS stays off: " + e);
                harmony.UnpatchSelf();
                installed = false;
                return;
            }
            installed = true;
        }

        internal static void Disable()
        {
            Camera.onPreCull -= BeforeDraw;
            Camera.onPostRender -= AfterDraw;
            if (active)
            {
                active = false;
                SetInterpolation(false);
                MainManager.ApplySettings();
            }
            harmony?.UnpatchSelf();
            harmony = null;
        }

        internal static void Tick()
        {
            if (drawnShifted)
            {
                RestoreCamera("the next frame's Update began");
            }
            int cap = installed && settingsOn != null && settingsOn() ? Cap : 0;
            bool nowActive = cap > 0;
            if (nowActive)
            {
                Enforce(cap);
            }
            if (nowActive == active && cap == activeCap)
            {
                return;
            }
            bool wasActive = active;
            active = nowActive;
            activeCap = cap;
            if (wasActive != nowActive)
            {
                SmoothCamera = nowActive;
                SetInterpolation(nowActive);
                if (!nowActive)
                {
                    MainManager.ApplySettings();
                }
            }
            log.LogInfo(nowActive ? $"[fps] uncapped to {cap}: vSyncCount {QualitySettings.vSyncCount}, targetFrameRate "
                + $"{Application.targetFrameRate} (monitor {Screen.currentResolution.refreshRate} Hz); motion drawn between physics steps"
                : $"[fps] the game's own settings (FPS {MainManager.fps}, VSync {MainManager.vsync}): targetFrameRate "
                + $"{Application.targetFrameRate}, vSyncCount {QualitySettings.vSyncCount}");
        }

        // ---- Hooks installed once; each does nothing unless the row is on. ----

        private static void Install()
        {
            var watch = Stopwatch.StartNew();
            Patch(AccessTools.Method(typeof(MainManager), nameof(MainManager.ApplySettings)), postfix: nameof(AfterApplySettings));
            Patch(AccessTools.Method(typeof(EntityControl), "Start"), postfix: nameof(AfterEntityStart));
            Patch(AccessTools.Method(typeof(MainManager), nameof(MainManager.TieFramerate)), prefix: nameof(BeforeTieFramerate));
            Patch(AccessTools.Method(typeof(MainManager), nameof(MainManager.FrameDifference)), prefix: nameof(BeforeFrameDifference));
            MethodInfo doCommand = AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BattleControl), "DoCommand"));
            Patch(doCommand, transpiler: nameof(TranspileTapBar));

            // Every method that counts frames, and every method a physics step reaches that scales by framestep.
            Assembly game = typeof(MainManager).Assembly;
            var methods = new List<MethodBase>();
            foreach (Type type in AccessTools.GetTypesFromAssembly(game))
            {
                foreach (MethodInfo m in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!m.IsAbstract && !m.ContainsGenericParameters && m.GetMethodBody() != null)
                    {
                        methods.Add(m);
                    }
                }
            }
            var calls = new Dictionary<MethodBase, List<MethodBase>>();
            var countsFrames = new List<MethodBase>();
            var readsFramestep = new HashSet<MethodBase>();
            foreach (MethodBase m in methods)
            {
                List<KeyValuePair<OpCode, object>> code;
                try
                {
                    code = PatchProcessor.ReadMethodBody(m).ToList();
                }
                catch
                {
                    continue;
                }
                var callees = new List<MethodBase>();
                foreach (KeyValuePair<OpCode, object> i in code)
                {
                    if (i.Value is MethodInfo mi)
                    {
                        if (mi == FrameCountGetter)
                        {
                            countsFrames.Add(m);
                        }
                        else if (mi.DeclaringType != null && mi.DeclaringType.Assembly == game)
                        {
                            callees.Add(mi);
                        }
                    }
                    else if (i.Value is FieldInfo fi && fi == FramestepField && i.Key == OpCodes.Ldsfld)
                    {
                        readsFramestep.Add(m);
                    }
                }
                calls[m] = callees;
            }
            countsFrames = countsFrames.Distinct().Where(m => m.Name != nameof(MainManager.FrameDifference)).ToList();
            foreach (MethodBase m in countsFrames)
            {
                Patch(m, transpiler: nameof(TranspileFrameCount));
            }
            // A physics step: FixedUpdate and the collision and trigger messages, and what they call, three calls deep.
            var reached = new HashSet<MethodBase>();
            var frontier = methods.Where(m => m.Name == "FixedUpdate" || m.Name.StartsWith("OnTrigger") || m.Name.StartsWith("OnCollision")).ToList();
            for (int depth = 0; depth < 4 && frontier.Count > 0; depth++)
            {
                var next = new List<MethodBase>();
                foreach (MethodBase m in frontier)
                {
                    if (reached.Add(m) && calls.TryGetValue(m, out List<MethodBase> callees))
                    {
                        next.AddRange(callees);
                    }
                }
                frontier = next;
            }
            List<MethodBase> fixedFramestep = reached.Where(readsFramestep.Contains).ToList();
            foreach (MethodBase m in fixedFramestep)
            {
                Patch(m, transpiler: nameof(TranspileFramestep));
            }
            log.LogInfo($"[fps] installed in {watch.ElapsedMilliseconds} ms: {countsFrames.Count} methods count frames ("
                + string.Join(", ", countsFrames.Select(Name).ToArray()) + $"); {fixedFramestep.Count} physics-step methods read framestep ("
                + string.Join(", ", fixedFramestep.Select(Name).ToArray()) + ")");
        }

        private static string Name(MethodBase m) => m.DeclaringType?.Name + "." + m.Name;

        private static void Patch(MethodBase target, string prefix = null, string postfix = null, string transpiler = null)
        {
            if (target == null)
            {
                log.LogError($"[fps] a hook's method wasn't found ({prefix ?? postfix ?? transpiler}): Uncap FPS is missing that correction");
                return;
            }
            try
            {
                harmony.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(FrameRate), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(FrameRate), postfix),
                    transpiler: transpiler == null ? null : new HarmonyMethod(typeof(FrameRate), transpiler));
            }
            catch (Exception e)
            {
                log.LogError($"[fps] couldn't hook {Name(target)}: {e.GetBaseException().Message}");
            }
        }

        // A cap that divides the monitor's refresh rate is met with VSync (every refresh, every second one, ...): without it
        // frames tear, which shows as a blur on anything moving sideways. Any other cap is a limit with VSync off. The
        // target frame rate is the rate that results, which the game's tap bar reads.
        private static void Enforce(int cap)
        {
            int refresh = Screen.currentResolution.refreshRate;
            int sync = refresh <= 0 ? 0 : cap >= refresh ? 1 : refresh % cap == 0 ? Mathf.Min(refresh / cap, 4) : 0;
            int rate = sync > 0 ? refresh / sync : cap;
            if (QualitySettings.vSyncCount != sync)
            {
                QualitySettings.vSyncCount = sync;
            }
            if (Application.targetFrameRate != rate)
            {
                Application.targetFrameRate = rate;
            }
        }

        // The game's settings screen re-applies its own FPS and VSync; the row wins while it's on.
        private static void AfterApplySettings()
        {
            if (active)
            {
                Enforce(activeCap);
            }
        }

        // ---- Motion drawn between physics steps. ----

        private static void AfterEntityStart(EntityControl __instance)
        {
            if (active)
            {
                Rigidbody body = __instance.rigid != null ? __instance.rigid : __instance.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
        }

        private static void SetInterpolation(bool on)
        {
            foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>())
            {
                Rigidbody body = e.rigid != null ? e.rigid : e.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.interpolation = on ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                }
            }
        }

        // The camera is drawn between its last two physics-step poses and put back after drawing, so the game's
        // own camera code never sees a changed value.
        internal static bool SmoothCamera;
        private static Vector3 prevPos, currPos;
        private static Quaternion prevRot, currRot;
        private static float lastFixedTime = -1f;
        private static bool drawnShifted;
        private static Vector3 savedLocalPos;
        private static Quaternion savedLocalRot;
        private static float savedTrueYaw;
        // A camera jump further than this in one step is a cut, not motion.
        private const float CutDistance = 3f;

        private static void BeforeDraw(Camera cam)
        {
            if (cam == null || cam != MainManager.MainCamera)
            {
                return;
            }
            Sample();
            drawWork.Restart();
            if (!SmoothCamera)
            {
                return;
            }
            Transform t = cam.transform;
            if (drawnShifted)
            {
                RestoreCamera("the main camera drew again");
            }
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;
            if (Time.fixedTime != lastFixedTime)
            {
                lastFixedTime = Time.fixedTime;
                prevPos = currPos;
                prevRot = currRot;
                currPos = pos;
                currRot = rot;
                if ((currPos - prevPos).sqrMagnitude > CutDistance * CutDistance)
                {
                    prevPos = currPos;
                    prevRot = currRot;
                }
            }
            else if (pos != currPos || rot != currRot)
            {
                // Moved outside a physics step (a scene placing it): no motion to smooth.
                prevPos = currPos = pos;
                prevRot = currRot = rot;
            }
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            savedLocalPos = t.localPosition;
            savedLocalRot = t.localRotation;
            savedTrueYaw = t.eulerAngles.y;
            lastCamera = LastCamera();
            t.position = Vector3.Lerp(prevPos, currPos, alpha);
            t.rotation = Quaternion.Slerp(prevRot, currRot, alpha);
            drawnShifted = true;
            Trace(cam);
        }

        // The console's "trace [frames]": where the nearest NPC showing an emoticon, its emoticon and the player land on
        // screen in each drawn frame (x in pixels), to see which of them doesn't move with the smoothed camera.
        private static int traceLeft;
        private static readonly System.Text.StringBuilder traceLog = new System.Text.StringBuilder();

        internal static string StartTrace(int frames)
        {
            traceLeft = frames;
            traceLog.Length = 0;
            return $"trace: {frames} frames";
        }

        private static void Trace(Camera cam)
        {
            if (traceLeft <= 0 || MainManager.player == null)
            {
                return;
            }
            EntityControl npc = null;
            float best = float.MaxValue;
            foreach (EntityControl e in UnityEngine.Object.FindObjectsOfType<EntityControl>())
            {
                if (e.emoticon == null || e == MainManager.player.entity || e.emoticoncooldown <= 0f)
                {
                    continue;
                }
                float d = (e.transform.position - MainManager.player.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    npc = e;
                }
            }
            Rigidbody body = MainManager.player.entity != null ? MainManager.player.entity.rigid : null;
            if (npc == null || body == null || new Vector2(body.velocity.x, body.velocity.z).sqrMagnitude < 1f)
            {
                return;
            }
            Vector3 p = cam.WorldToScreenPoint(MainManager.player.transform.position);
            traceLog.Append($"\n  dt {Time.unscaledDeltaTime * 1000f:0.0} a {(Time.time - Time.fixedTime) / Time.fixedDeltaTime:0.00} player {p.x:0.0}");
            if (npc != null)
            {
                Vector3 n = cam.WorldToScreenPoint(npc.transform.position);
                Vector3 m = cam.WorldToScreenPoint(npc.emoticon.transform.position);
                Vector3 ms = npc.emoticonsprite != null ? cam.WorldToScreenPoint(npc.emoticonsprite.transform.position) : m;
                traceLog.Append($" npc {n.x:0.0} emoticon {m.x:0.0} sprite {ms.x:0.0},{ms.y:0.0} yaw cam {cam.transform.eulerAngles.y:0.00} "
                    + $"true {savedTrueYaw:0.00} bubble {npc.emoticon.transform.eulerAngles.y:0.00} rotater {npc.emoticon.transform.parent.eulerAngles.y:0.00} ({npc.name}, interp {(npc.rigid != null ? npc.rigid.interpolation.ToString() : "none")}, "
                    + $"emoticon parent {(npc.emoticon.transform.parent != null ? npc.emoticon.transform.parent.name : "none")})");
            }
            if (--traceLeft == 0)
            {
                log.LogInfo("[dev] trace:" + traceLog);
            }
        }

        // The main camera's children (3DGUI, where emoticons are drawn, and the HUD's GUICamera) draw after it and must
        // see the same pose, so it goes back only once the frame's last camera has drawn.
        private static Camera[] cameras = new Camera[8];
        private static Camera lastCamera;

        private static Camera LastCamera()
        {
            if (cameras.Length < Camera.allCamerasCount)
            {
                cameras = new Camera[Camera.allCamerasCount];
            }
            int count = Camera.GetAllCameras(cameras);
            Camera last = null;
            for (int i = 0; i < count; i++)
            {
                if (last == null || cameras[i].depth >= last.depth)
                {
                    last = cameras[i];
                }
            }
            return last;
        }

        private static void AfterDraw(Camera cam)
        {
            if (cam == null)
            {
                return;
            }
            if (cam == MainManager.MainCamera)
            {
                drawWork.Stop();
                lastDrawMs = (float)drawWork.Elapsed.TotalMilliseconds;
            }
            if (!drawnShifted || (cam != lastCamera && lastCamera != null))
            {
                return;
            }
            RestoreCamera(null);
        }

        private static void RestoreCamera(string late)
        {
            Transform t = MainManager.MainCamera != null ? MainManager.MainCamera.transform : null;
            if (t != null)
            {
                t.localPosition = savedLocalPos;
                t.localRotation = savedLocalRot;
            }
            drawnShifted = false;
            if (late != null && !lateLogged)
            {
                lateLogged = true;
                log.LogWarning($"[fps] the camera was still drawn-shifted when {late}; put back (the last camera, {lastCamera?.name}, didn't draw)");
            }
        }

        private static bool lateLogged;

        // ---- What the game counts in frames, held to 60 a second. ----

        private static readonly MethodInfo FrameCountGetter = AccessTools.PropertyGetter(typeof(Time), nameof(Time.frameCount));
        private static readonly FieldInfo FramestepField = AccessTools.Field(typeof(MainManager), nameof(MainManager.framestep));
        private static readonly FieldInfo VsyncField = AccessTools.Field(typeof(MainManager), nameof(MainManager.vsync));
        private static int clockFrame = -1;
        private static long clockTick = -1;
        private static int logicalFrame;
        private static bool tickFrame;

        // A frame is a "tick" when a new 1/60 s has begun since the last one (every frame, below 60 fps).
        private static void AdvanceClock()
        {
            int frame = Time.frameCount;
            if (frame == clockFrame)
            {
                return;
            }
            clockFrame = frame;
            long tick = (long)(Time.unscaledTime * 60f);
            tickFrame = tick != clockTick;
            if (tickFrame)
            {
                clockTick = tick;
                logicalFrame++;
            }
        }

        // For `Time.frameCount % n == 0`: the count of 1/60 s ticks on a tick's first frame; on the frames in between, 1,
        // which no n above 1 divides, so a check made "every n frames" is made every n sixtieths of a second.
        private static int FrameCount()
        {
            if (!active)
            {
                return Time.frameCount;
            }
            if (Time.inFixedTimeStep)
            {
                return logicalFrame;
            }
            AdvanceClock();
            return tickFrame ? logicalFrame : 1;
        }

        private static IEnumerable<CodeInstruction> TranspileFrameCount(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction i in instructions)
            {
                if (i.Calls(FrameCountGetter))
                {
                    i.operand = AccessTools.Method(typeof(FrameRate), nameof(FrameCount));
                }
                yield return i;
            }
        }

        // "Once every 1/60 s", which the game works out from the frame rate it asked for.
        private static bool BeforeFrameDifference(ref bool __result)
        {
            if (!active)
            {
                return true;
            }
            AdvanceClock();
            __result = tickFrame;
            return false;
        }

        // ---- Frame time inside a physics step reads as it does at 60. ----

        // TieFramerate scales by the render frame time, even when called from a 50 Hz physics step.
        private static bool BeforeTieFramerate(float value, ref float __result)
        {
            if (!active || !Time.inFixedTimeStep)
            {
                return true;
            }
            __result = value;
            return false;
        }

        private static float Framestep() => active && Time.inFixedTimeStep ? 1f : MainManager.framestep;

        private static IEnumerable<CodeInstruction> TranspileFramestep(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo f && f == FramestepField)
                {
                    i.opcode = OpCodes.Call;
                    i.operand = AccessTools.Method(typeof(FrameRate), nameof(Framestep));
                }
                yield return i;
            }
        }

        // The tapping-key command's fill per press uses the refresh rate when the game's own VSync is on; with the row on,
        // it reads the target frame rate, which Enforce sets to the rate that results.
        private static int GameVsync() => active ? 0 : MainManager.vsync;

        private static IEnumerable<CodeInstruction> TranspileTapBar(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo f && f == VsyncField)
                {
                    i.opcode = OpCodes.Call;
                    i.operand = AccessTools.Method(typeof(FrameRate), nameof(GameVsync));
                    replaced++;
                }
                yield return i;
            }
            log.LogInfo($"[fps] tap bar: {replaced} VSync reads in BattleControl.DoCommand");
        }

        // ---- The console's "frames <seconds>": each frame's time, and whether a garbage collection ran. ----

        private static float sampleUntil = -1f;
        private static readonly List<float> sampleTimes = new List<float>();
        private static readonly List<bool> sampleGc = new List<bool>();
        private static readonly List<float> sampleAt = new List<float>();
        private static readonly List<float> sampleDraw = new List<float>();
        private static int lastGcCount;
        private static readonly Stopwatch drawWork = new Stopwatch();
        private static float lastDrawMs;

        internal static string StartSample(float seconds)
        {
            sampleTimes.Clear();
            sampleGc.Clear();
            sampleAt.Clear();
            sampleDraw.Clear();
            lastGcCount = GC.CollectionCount(0);
            sampleUntil = Time.realtimeSinceStartup + seconds;
            return $"frames: sampling {seconds} s";
        }

        private static void Sample()
        {
            if (sampleUntil < 0f)
            {
                return;
            }
            int gc = GC.CollectionCount(0);
            sampleTimes.Add(Time.unscaledDeltaTime * 1000f);
            sampleGc.Add(gc != lastGcCount);
            sampleAt.Add(Time.realtimeSinceStartup);
            sampleDraw.Add(lastDrawMs);
            lastGcCount = gc;
            if (Time.realtimeSinceStartup < sampleUntil)
            {
                return;
            }
            sampleUntil = -1f;
            var sorted = new List<float>(sampleTimes);
            sorted.Sort();
            float median = sorted[sorted.Count / 2];
            int slow = 0, gcs = 0;
            var worst = new System.Text.StringBuilder();
            for (int i = 0; i < sampleTimes.Count; i++)
            {
                gcs += sampleGc[i] ? 1 : 0;
                if (sampleTimes[i] > median * 2f)
                {
                    slow++;
                    if (slow <= 40)
                    {
                        worst.Append($" {sampleTimes[i]:0.0}{(sampleGc[i] ? "(gc)" : "")}@{sampleAt[i]:0.00}[draw {sampleDraw[i]:0.0}]");
                    }
                }
            }
            log.LogInfo($"[dev] frames: {sampleTimes.Count} frames, median {median:0.00} ms, max {sorted[sorted.Count - 1]:0.0} ms, "
                + $"{slow} over twice the median, {gcs} collections; slow frames (ms, (gc) marks the frame before the stall):{worst}");
        }
    }
}
