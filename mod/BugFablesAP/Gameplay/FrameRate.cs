using System;
using System.Collections;
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
    // The Quality of life row "Uncap FPS": a frame cap above the game's 30 or 60, with the game playing as it does at
    // 60. The game moves its camera and characters in FixedUpdate (50 steps a second), so motion is drawn between the
    // last two steps; what it counts in frames is held to 60 a second; what it scales by frame time inside a physics
    // step reads as it does at 60.
    internal static partial class FrameRate
    {
        private static ManualLogSource log;
        private static Harmony harmony;
        private static Func<bool> settingsOn;

        // Read once a frame (Tick): the hooks below run thousands of times a frame.
        private static bool active;
        private static int activeCap;
        private static bool installed, installFailed;

        internal static bool Active => active;

        // What this frame is worth in sixtieths of a second: 1 at 60 fps and below, inside a physics step, or with the
        // row off.
        internal static float Step => !active || Time.inFixedTimeStep ? 1f
            : Mathf.Min(1f, Time.unscaledDeltaTime * 60f);

        // Whether this frame starts a new 1/60 s (always, with the row off or inside a physics step).
        internal static bool OnTick
        {
            get
            {
                if (!active || Time.inFixedTimeStep)
                {
                    return true;
                }
                AdvanceClock();
                return tickFrame;
            }
        }

        internal static long Tick60
        {
            get
            {
                AdvanceClock();
                return clockTick;
            }
        }

        internal static int Cap
        {
            get
            {
                string v = QualityOfLife.UncapFps?.Value;
                int cap = v == "Monitor" ? Screen.currentResolution.refreshRate : v != null
                    && int.TryParse(v, out int n) ? n : 0;
                return cap > 60 ? cap : 0;
            }
        }

        internal static void Enable(ManualLogSource logger, Func<bool> settingsEnabled)
        {
            log = logger;
            settingsOn = settingsEnabled;
            Camera.onPreCull += BeforeDraw;
            Camera.onPostRender += AfterDraw;
            // For the method lists decided when installing; the fixed hooks are this class's attributed group.
            harmony = Hooks.Create("fps");
        }

        // Installed the first time the row is on, so with it off nothing of the game is patched.
        private static void EnsureInstalled()
        {
            if (installed || installFailed)
            {
                return;
            }
            try
            {
                Install();
                installed = true;
            }
            catch (Exception e)
            {
                log.LogError("[fps] NOT installed, Uncap FPS stays off: " + e);
                harmony.UnpatchSelf();
                installFailed = true;
            }
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
            harmony = null;
        }

        internal static void Tick()
        {
            if (drawnShifted)
            {
                RestoreCamera("the next frame's Update began");
            }
            int cap = settingsOn != null && settingsOn() ? Cap : 0;
            if (cap > 0)
            {
                EnsureInstalled();
                if (!installed)
                {
                    cap = 0;
                }
            }
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
            log.LogInfo(nowActive
                ? $"[fps] uncapped to {cap}: vSyncCount {QualitySettings.vSyncCount}, targetFrameRate "
                + $"{Application.targetFrameRate} (monitor {Screen.currentResolution.refreshRate} Hz); motion drawn between physics steps"
                : $"[fps] the game's own settings (FPS {MainManager.fps}, VSync {MainManager.vsync}): targetFrameRate "
                + $"{Application.targetFrameRate}, vSyncCount {QualitySettings.vSyncCount}");
        }

        // ---- Hooks installed once; each does nothing unless the row is on. ----

        private static void Install()
        {
            var watch = Stopwatch.StartNew();
            if (!Hooks.Install(typeof(FrameRate), "fps", "Uncap FPS stays off"))
            {
                throw new InvalidOperationException("its fixed hooks didn't install");
            }

            // The lists the console's "fpsscan" finds by reading every method of the game; each method's body is still
            // checked.
            List<MethodBase> countsFrames = Listed(FrameCounters, m => Reads(m, (op, v) => v is MethodInfo mi
                && mi == FrameCountGetter));
            foreach (MethodBase m in countsFrames)
            {
                Patch(m, transpiler: nameof(TranspileFrameCount));
            }
            List<MethodBase> fixedFramestep = Listed(PhysicsFramestep, m => Reads(m, (op, v) => op == OpCodes.Ldsfld
                && v is FieldInfo f && f == FramestepField));
            foreach (MethodBase m in fixedFramestep)
            {
                Patch(m, transpiler: nameof(TranspileFramestep));
            }
            List<MethodBase> blinkers = Listed(Blinkers, HasBlink);
            long coreMs = watch.ElapsedMilliseconds;
            FrameSites.Install(log, harmony, blinkers);
            log.LogInfo(
                $"[fps] installed in {watch.ElapsedMilliseconds} ms (sites {watch.ElapsedMilliseconds - coreMs}): "
                + $"{countsFrames.Count} of {FrameCounters.Length} methods count frames, {fixedFramestep.Count} of {PhysicsFramestep.Length} "
                + $"physics-step methods read framestep, {blinkers.Count} of {Blinkers.Length} blink");
        }

        // Type.Method; a star marks a coroutine (its MoveNext is patched).
        private static readonly string[] FrameCounters =
        {
            "BattleControl.Update", "BattleControl.EnemyHeavyThrow*", "Caravan.LateUpdate", "EntityControl.DoFollow",
                "EntityControl.Numb",
            "EntityControl.LateUpdate", "EntityControl.UpdateVelocity", "EntityControl.UpdateCollider",
                "EntityControl.UpdateEmoticon",
            "EntityControl.RefreshShadow", "EntityControl.OnTriggerStay", "Fader.LateUpdate", "FishAI.DoAI",
                "FishAI.UpdateDistance",
            "FishAI.UpdatePos", "FishingMain.Update", "HelpArrow.Update", "Hidder.LateUpdate", "LightFlicker.Update",
                "LightSorter.LateUpdate",
            "MapControl.LateUpdate", "NPCControl.Update", "NPCControl.LateUpdate", "PlayerControl.LateUpdate",
        };

        private static readonly string[] PhysicsFramestep = { "EntityControl.FixedUpdate",
            "PlayerControl.OnTriggerStay", "BattleControl.UpdateEntities" };

        private static readonly string[] Blinkers =
        {
            "NPCControl.Update", "BattleControl.DoAction*", "EntityControl.Update", "PauseMenu.Update",
                "Pips.ChangeRenderers",
            "EntityControl.ZaspWarp*", "EventControl.Event111*", "EventControl.ZaspWarp*", "EventControl.Event173*",
        };

        private static List<MethodBase> Listed(string[] names, Func<MethodBase, bool> check)
        {
            var found = new List<MethodBase>();
            foreach (string entry in names)
            {
                bool iter = entry.EndsWith("*");
                string[] parts = entry.TrimEnd('*').Split('.');
                Type type = typeof(MainManager).Assembly.GetType(parts[0]);
                var overloads = type == null ? new List<MethodInfo>() : type.GetMethods(BindingFlags.DeclaredOnly
                    | BindingFlags.Instance
                    | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == parts[1])
                    .ToList();
                var matched = overloads.Select(m => iter ? (MethodBase)AccessTools.EnumeratorMoveNext(m) : m)
                    .Where(m => m != null && check(m)).ToList();
                if (matched.Count == 0)
                {
                    log.LogWarning($"[fps] {entry} no longer found as expected: that correction is missing");
                }
                found.AddRange(matched);
            }
            return found;
        }

        private static bool Reads(MethodBase m, Func<OpCode, object, bool> match)
        {
            List<KeyValuePair<OpCode, object>> body = ReadBody(m, log, "fps");
            return body != null && body.Any(i => match(i.Key, i.Value));
        }

        private static string Name(MethodBase m) => m.DeclaringType?.Name + "." + m.Name;

        // The dev build's frame measurements (Dev/FrameRate.Dev.cs); in the release build these calls vanish.
        static partial void Sample();
        static partial void Rates();
        static partial void Trace(Camera cam);
        static partial void DrawStarted();
        static partial void DrawEnded();

        private static bool HasBlink(MethodBase m)
        {
            List<KeyValuePair<OpCode, object>> body = ReadBody(m, log, "fps");
            if (body == null || !body.Any(i => i.Value is MethodInfo sm && sm.Name == "set_enabled"
                && sm.DeclaringType == typeof(Renderer)))
            {
                return false;
            }
            List<CodeInstruction> ci = body.Select(i => new CodeInstruction(i.Key, i.Value)).ToList();
            return Enumerable.Range(0, ci.Count).Any(i => FrameSites.IsBlink(ci, i));
        }

        private static readonly HashSet<string> unreadableLogged = new HashSet<string>();

        // A method's IL, or null: none for an abstract, extern or runtime method, and a failed read logged once per
        // tag.
        internal static List<KeyValuePair<OpCode, object>> ReadBody(MethodBase m, ManualLogSource logger, string tag)
        {
            try
            {
                return m.GetMethodBody() == null ? null : PatchProcessor.ReadMethodBody(m).ToList();
            }
            catch (Exception e)
            {
                if (unreadableLogged.Add(tag))
                {
                    logger?.LogWarning(
                        $"[{tag}] couldn't read {m.DeclaringType?.Name}.{m.Name}: {e.GetBaseException().Message}"
                        + " (later failures not logged)");
                }
                return null;
            }
        }

        private static void Patch(MethodBase target, string prefix = null, string postfix = null,
            string transpiler = null)
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

        // A cap that divides the monitor's refresh rate is met with VSync (every refresh, every second one, ...):
        // without it frames tear, which shows as a blur on anything moving sideways. Any other cap is a limit with
        // VSync off. The target frame rate is the rate that results, which the game's tap bar reads.
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
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.ApplySettings))]
        [HarmonyPostfix]
        private static void AfterApplySettings()
        {
            if (active)
            {
                Enforce(activeCap);
            }
        }

        // ---- Motion drawn between physics steps. ----

        [HarmonyPatch(typeof(EntityControl), "Start")]
        [HarmonyPostfix]
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

        // A platform carries whoever stands on it as its child; interpolation drew them from their own physics poses
        // and held them back, like walking in mud (seen at 240 fps). Not interpolated while on one.
        [HarmonyPatch(typeof(GroundDetector), "OnTriggerStay")]
        [HarmonyPatch(typeof(GroundDetector), "OnTriggerExit")]
        [HarmonyPostfix]
        private static void AfterGround(GroundDetector __instance)
        {
            if (active && __instance.parent != null)
            {
                Interpolate(__instance.parent, __instance.platform != null);
            }
        }

        // One decision for both cases, so neither undoes the other: not interpolated on a platform, or while a frozen
        // enemy (the game writes a frozen enemy's position back every frame, from the drawn pose that trails the
        // physics one, so it dragged: slow after the first knock, fine with interpolation off).
        private static void Interpolate(EntityControl entity, bool onPlatform)
        {
            Rigidbody body = entity.rigid;
            if (body == null)
            {
                return;
            }
            NPCControl npc = entity.npcdata;
            bool frozen = npc != null && npc.entitytype == NPCControl.NPCType.Enemy && npc.freezecooldown > 0f;
            RigidbodyInterpolation wanted = onPlatform || frozen ? RigidbodyInterpolation.None
                : RigidbodyInterpolation.Interpolate;
            if (body.interpolation != wanted)
            {
                body.interpolation = wanted;
            }
        }

        // Shaky letters jump to a new random spot, and glitchy ones roll their swap, once per frame: at 240 a blur (the
        // user). Between 1/60 s ticks both hold still; a shaky letter's position also overrides wavy, so wavy holds
        // too.
        private const int Shaky = 1, Wavy = 2, Glitchy = 4;

        [HarmonyPatch(typeof(FontEffects), "Update")]
        [HarmonyPrefix]
        private static void BeforeFontEffects(FontEffects __instance, out int __state)
        {
            __state = 0;
            if (!active || OnTick || !(__instance.shaky || __instance.glitchy))
            {
                return;
            }
            if (__instance.shaky)
            {
                __state |= Shaky | (__instance.wavy ? Wavy : 0);
                __instance.shaky = __instance.wavy = false;
            }
            if (__instance.glitchy)
            {
                __state |= Glitchy;
                __instance.glitchy = false;
            }
        }

        [HarmonyPatch(typeof(FontEffects), "Update")]
        [HarmonyPostfix]
        private static void AfterFontEffects(FontEffects __instance, int __state)
        {
            if ((__state & Shaky) != 0)
            {
                __instance.shaky = true;
            }
            if ((__state & Wavy) != 0)
            {
                __instance.wavy = true;
            }
            if ((__state & Glitchy) != 0)
            {
                __instance.glitchy = true;
            }
        }

        // MainManager.ShakeObject (the bushes before the leaf gang's ambush, and many scenes) moves its object to a new
        // random offset every frame. The game's loop, with the offset kept between 1/60 s ticks.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShakeObject))]
        [HarmonyPrefix]
        private static bool BeforeShakeObject(Transform obj, Vector3 shake, float frametime, bool returntostart,
            ref IEnumerator __result)
        {
            if (!active)
            {
                return true;
            }
            __result = ShakeOnTicks(obj, shake, frametime, returntostart);
            return false;
        }

        private static IEnumerator ShakeOnTicks(Transform obj, Vector3 shake, float frametime, bool returntostart)
        {
            Vector3 p = obj.position;
            Vector3 offset = MainManager.RandomVector(shake);
            float a = 0f;
            do
            {
                if (OnTick)
                {
                    offset = MainManager.RandomVector(shake);
                }
                obj.position = p + offset;
                a += MainManager.TieFramerate(1f);
                yield return null;
            }
            while (a < frametime + 1f);
            if (returntostart)
            {
                obj.position = p;
            }
        }

        // A knocked ice block (a frozen enemy, a pushed rock) slides by icevel until a frame sees it with no vertical
        // speed, which reads as landed. The knock sets its speed flat and hops it a frame later: at 60 a physics step
        // (gravity) comes between, at 240 usually not, so the slide was cancelled at once and stopped short. A cancel
        // in a frame no physics step came before is undone; one right after a step (a real landing) stands.
        private static readonly AccessTools.FieldRef<NPCControl, Vector3> iceVelocity =
            AccessTools.FieldRefAccess<NPCControl, Vector3>("icevel");
        private static readonly Dictionary<int, float> lastPhysics = new Dictionary<int, float>();

        [HarmonyPatch(typeof(NPCControl), "Update")]
        [HarmonyPrefix]
        private static void BeforeNpcUpdate(NPCControl __instance, out Vector3 __state)
        {
            __state = Vector3.zero;
            if (!active || __instance.entity == null || __instance.entity.rigid == null)
            {
                return;
            }
            Interpolate(__instance.entity, __instance.entity.feet != null && __instance.entity.feet.platform != null);
            int id = __instance.GetInstanceID();
            if (lastPhysics.Count > 4096)
            {
                lastPhysics.Clear(); // ids of maps left behind
            }
            bool stepped = !lastPhysics.TryGetValue(id, out float seen) || seen != Time.fixedTime;
            lastPhysics[id] = Time.fixedTime;
            if (!stepped)
            {
                __state = iceVelocity(__instance);
            }
        }

        [HarmonyPatch(typeof(NPCControl), "Update")]
        [HarmonyPostfix]
        private static void AfterNpcUpdate(NPCControl __instance, Vector3 __state)
        {
            if (__state.sqrMagnitude < 0.01f || iceVelocity(__instance).sqrMagnitude >= 0.01f)
            {
                return;
            }
            iceVelocity(__instance) = __state;
            Rigidbody body = __instance.entity.rigid;
            body.velocity = new Vector3(__state.x, body.velocity.y, __state.z);
        }

        // EntityControl.ShakeSprite (a character's shake, as on a hit that does no damage): the same, its sprite.
        [HarmonyPatch(typeof(EntityControl), nameof(EntityControl.ShakeSprite), typeof(Vector3), typeof(float))]
        [HarmonyPrefix]
        private static bool BeforeShakeSprite(EntityControl __instance, Vector3 intensity, float frametimer,
            ref IEnumerator __result)
        {
            if (!active)
            {
                return true;
            }
            __result = ShakeSpriteOnTicks(__instance, intensity, frametimer);
            return false;
        }

        private static IEnumerator ShakeSpriteOnTicks(EntityControl entity, Vector3 intensity, float frametimer)
        {
            Vector3 startp = entity.spritetransform.localPosition;
            Vector3 offset = Vector3.zero;
            bool first = true;
            while (frametimer > 0f)
            {
                if (first || OnTick)
                {
                    offset = new Vector3(UnityEngine.Random.Range(0f - intensity.x, intensity.x),
                        UnityEngine.Random.Range(0f - intensity.y, intensity.y),
                        UnityEngine.Random.Range(0f - intensity.z, intensity.z));
                    first = false;
                }
                entity.spritetransform.localPosition = startp + offset;
                frametimer -= MainManager.framestep;
                yield return null;
            }
            entity.spritetransform.localPosition = startp + entity.extraoffset;
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
            Rates();
            DrawStarted();
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
                DrawEnded();
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

        private static readonly MethodInfo FrameCountGetter = AccessTools.PropertyGetter(typeof(Time),
            nameof(Time.frameCount));
        private static readonly FieldInfo FramestepField = AccessTools.Field(typeof(MainManager),
            nameof(MainManager.framestep));
        private static readonly FieldInfo VsyncField = AccessTools.Field(typeof(MainManager),
            nameof(MainManager.vsync));
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

        // For `Time.frameCount % n == 0`: the count of 1/60 s ticks on a tick's first frame; on the frames in between,
        // 1, which no n above 1 divides, so a check made "every n frames" is made every n sixtieths of a second.
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

        private static IEnumerable<CodeInstruction> TranspileFrameCount(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditFrameCount, "fps");

        private static IEnumerable<CodeInstruction> EditFrameCount(List<CodeInstruction> code)
        {
            List<CodeInstruction> reads = code.Where(i => i.Calls(FrameCountGetter)).ToList();
            MethodInfo counter = AccessTools.Method(typeof(FrameRate), nameof(FrameCount))
                ?? throw new MissingMethodException(nameof(FrameRate), nameof(FrameCount));
            reads.ForEach(i => i.operand = counter);
            return code;
        }

        // "Once every 1/60 s", which the game works out from the frame rate it asked for.
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.FrameDifference))]
        [HarmonyPrefix]
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
        [HarmonyPatch(typeof(MainManager), nameof(MainManager.TieFramerate))]
        [HarmonyPrefix]
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

        private static IEnumerable<CodeInstruction> TranspileFramestep(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditFramestep, "fps");

        private static IEnumerable<CodeInstruction> EditFramestep(List<CodeInstruction> code)
        {
            List<CodeInstruction> reads = code.Where(i => i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo f
                && f == FramestepField).ToList();
            MethodInfo framestep = AccessTools.Method(typeof(FrameRate), nameof(Framestep))
                ?? throw new MissingMethodException(nameof(FrameRate), nameof(Framestep));
            foreach (CodeInstruction i in reads)
            {
                i.opcode = OpCodes.Call;
                i.operand = framestep;
            }
            return code;
        }

        // The tapping-key command's fill per press uses the refresh rate when the game's own VSync is on; with the row
        // on, it reads the target frame rate, which Enforce sets to the rate that results.
        private static int GameVsync() => active ? 0 : MainManager.vsync;

        [HarmonyPatch(typeof(BattleControl), "DoCommand", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TranspileTapBar(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditTapBar, "fps");

        private static IEnumerable<CodeInstruction> EditTapBar(List<CodeInstruction> code)
        {
            List<CodeInstruction> reads = code.Where(i => i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo f
                && f == VsyncField).ToList();
            MethodInfo vsync = AccessTools.Method(typeof(FrameRate), nameof(GameVsync))
                ?? throw new MissingMethodException(nameof(FrameRate), nameof(GameVsync));
            foreach (CodeInstruction i in reads)
            {
                i.opcode = OpCodes.Call;
                i.operand = vsync;
            }
            log.LogInfo($"[fps] tap bar: {reads.Count} VSync reads in BattleControl.DoCommand");
            return code;
        }

    }
}
