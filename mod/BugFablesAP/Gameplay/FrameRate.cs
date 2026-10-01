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
            if (drawnShifted)
            {
                RestoreCamera(null);
            }
            bodies.Clear();
            scenery.Clear();
            if (active)
            {
                active = false;
                NoInterpolation();
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
                SmoothCamera = SmoothBodies = SmoothScenery = nowActive;
                if (nowActive)
                {
                    TrackAll();
                    TrackAllScenery();
                }
                else
                {
                    bodies.Clear();
                    scenery.Clear();
                }
                NoInterpolation();
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
            List<MethodBase> skipsFrames = Listed(FrameSkippers, m => Reads(m, (op, v) => v is MethodInfo mi
                && mi == FrameCountGetter));
            foreach (MethodBase m in skipsFrames)
            {
                Patch(m, transpiler: nameof(TranspileFrameSkip));
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
                + $"{countsFrames.Count} of {FrameCounters.Length} methods count frames, {skipsFrames.Count} of {FrameSkippers.Length} skip "
                + $"by the count, {fixedFramestep.Count} of {PhysicsFramestep.Length} "
                + $"physics-step methods read framestep, {blinkers.Count} of {Blinkers.Length} blink");
        }

        // Type.Method; a star marks a coroutine (its MoveNext is patched).
        private static readonly string[] FrameCounters =
        {
            "BattleControl.Update", "BattleControl.EnemyHeavyThrow*", "Caravan.LateUpdate", "EntityControl.Numb",
            "EntityControl.LateUpdate", "EntityControl.UpdateVelocity", "EntityControl.UpdateCollider",
                "EntityControl.UpdateEmoticon",
            "EntityControl.RefreshShadow", "EntityControl.OnTriggerStay", "Fader.LateUpdate", "FishAI.DoAI",
                "FishAI.UpdateDistance",
            "FishAI.UpdatePos", "FishingMain.Update", "HelpArrow.Update", "Hidder.LateUpdate", "LightFlicker.Update",
                "LightSorter.LateUpdate",
            "MapControl.LateUpdate", "NPCControl.Update", "NPCControl.LateUpdate", "PlayerControl.LateUpdate",
        };

        // The opposite test, `if (Time.frameCount % n == 0) return;`: the work is skipped on the counted frames.
        private static readonly string[] FrameSkippers = { "EntityControl.DoFollow" };

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
        static partial void TraceBodies(Camera cam);
        static partial void DrawStarted();
        static partial void DrawEnded();
        static partial void BeforeShifts();

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

        // Unity's rigidbody interpolation isn't used: it writes the drawn pose into the transform, and the game reads it
        // back (a platform's carrying, a frozen enemy's and Vi's per-frame writes: slow motion), and it doesn't smooth
        // a position written in a physics step (a conveyor). Every character has None and is drawn smoothed instead
        // (DrawBodies).
        [HarmonyPatch(typeof(EntityControl), "Start")]
        [HarmonyPostfix]
        private static void AfterEntityStart(EntityControl __instance)
        {
            if (active)
            {
                Track(__instance);
            }
        }

        // Shaky letters jump to a new random spot, and glitchy ones roll their swap, once per frame: at 240 a blur.
        // Between 1/60 s ticks both hold still; a shaky letter's position also overrides wavy, so wavy holds too.
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
        // (gravity) comes between, at 240 usually not, so the slide is cancelled at once and stops short. A cancel
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

        // What the player has in the game (it never sets interpolation); an earlier build of the row (hot reload) or the
        // console's "interp" may have changed it.
        private static void NoInterpolation()
        {
            foreach (EntityControl e in Resources.FindObjectsOfTypeAll<EntityControl>())
            {
                Rigidbody body = e.gameObject.scene.IsValid() ? e.rigid != null ? e.rigid : e.GetComponent<Rigidbody>()
                    : null;
                if (body != null && body.interpolation != RigidbodyInterpolation.None)
                {
                    body.interpolation = RigidbodyInterpolation.None;
                }
            }
        }

        // The camera is drawn between its last two physics-step poses and put back after drawing, so the game's
        // own camera code never sees a changed value.
        internal static bool SmoothCamera;
        private static Vector3 prevPos, currPos;
        private static Quaternion prevRot, currRot;
        private static float lastFixedTime = -1f;
        private static bool drawnShifted, cameraShifted;
        private static Vector3 savedLocalPos;
        private static Quaternion savedLocalRot;
        private static float savedTrueYaw;
        // A camera jump further than this in one step is a cut, not motion.
        private const float CutDistance = 3f;

        // Every character is drawn the way the camera is: set back by the share of its last physics step's move that
        // hasn't played yet, and put back after drawing, so the game only ever reads its true pose. The step's move is
        // its pose right after the step (AfterPhysics) against its pose at the last draw, when nothing else runs
        // before the step: walking, a conveyor, a knock. What the game writes every frame (a platform carrying it, Vi's
        // rise) is drawn as it is. The move is kept in the parent's space, so it turns with a turning platform, and
        // measured there when the parent stayed the same: a swing moved inside the step carries it (DrawScenery).
        internal static bool SmoothBodies;
        private sealed class Body
        {
            internal EntityControl Entity, CopyOf;
            internal Vector3 AtDraw, AfterStep, Move, Back, Saved, AtDrawLocal, AfterStepLocal;
            internal Transform MoveParent, SavedParent, DrawParent, StepParent;
            internal bool Drawn, Stepped;
        }
        private static readonly Dictionary<int, Body> bodies = new Dictionary<int, Body>();
        private static readonly List<Body> shiftedBodies = new List<Body>();
        private static readonly List<Body> copiers = new List<Body>();
        private static readonly List<int> goneBodies = new List<int>();

        // Whose position the game copies into this character every frame (EntityControl.Follow): Kabbu is put at Vi's
        // while she flies; a temporary follower at the last party member's in flight, or at the leader's while
        // digging. Drawn at the true copy, it would run ahead of the smoothed one it copies, so it takes its offset.
        private static EntityControl CopySource(EntityControl e)
        {
            MainManager game = MainManager.instance;
            PlayerControl p = MainManager.player;
            if (e.following == null || game == null || game.pause || game.overridefollower || e.overridefollow
                || e.dead || e.iskill || p == null || p.entity == null || e == p.entity)
            {
                return null;
            }
            bool digging = p.digging || p.startdig;
            if (e.tempfollower)
            {
                if (digging)
                {
                    return p.entity;
                }
                MainManager.BattleData[] party = game.playerdata;
                return p.flying && !p.shield && party != null && party.Length > 0 ? party[party.Length - 1].entity
                    : null;
            }
            bool inParty = e.following.CompareTag("Player") || e.following.CompareTag("PFollower");
            return inParty && !digging && p.flying && e.animid == 1 ? p.entity : null;
        }

        // A copy of a copy follows the chain (a temporary follower copying Kabbu).
        private static Vector3 BackOf(EntityControl e, int depth)
        {
            if (e == null || !bodies.TryGetValue(e.GetInstanceID(), out Body b))
            {
                return Vector3.zero;
            }
            return b.CopyOf != null && depth > 0 ? BackOf(b.CopyOf, depth - 1) : b.Back;
        }

        private static void Shift(Body b, Vector3 back)
        {
            if (!SmoothBodies || back == Vector3.zero)
            {
                return;
            }
            Transform t = b.Entity.transform;
            b.Saved = t.localPosition;
            b.SavedParent = t.parent;
            t.position = b.AtDraw - back;
            shiftedBodies.Add(b);
        }

        private static void Track(EntityControl entity)
        {
            int id = entity.GetInstanceID();
            if (!bodies.ContainsKey(id))
            {
                bodies[id] = new Body { Entity = entity };
            }
        }

        // Inactive ones included: a character whose Start ran before the row turned on gets no other chance.
        private static void TrackAll()
        {
            bodies.Clear();
            foreach (EntityControl e in Resources.FindObjectsOfTypeAll<EntityControl>())
            {
                if (e.gameObject.scene.IsValid())
                {
                    Track(e);
                }
            }
        }

        // The plugin runs this right after each physics step and its trigger messages.
        internal static void AfterPhysics()
        {
            if (!active)
            {
                return;
            }
            foreach (Body b in bodies.Values)
            {
                if (b.Entity != null && b.Drawn)
                {
                    Transform t = b.Entity.transform;
                    b.AfterStep = t.position;
                    b.AfterStepLocal = t.localPosition;
                    b.StepParent = t.parent;
                    b.Stepped = true;
                }
            }
            SampleScenery();
        }

        private static void DrawBodies(float alpha)
        {
            foreach (KeyValuePair<int, Body> pair in bodies)
            {
                Body b = pair.Value;
                if (b.Entity == null)
                {
                    goneBodies.Add(pair.Key);
                    continue;
                }
                Transform t = b.Entity.transform;
                if (!t.gameObject.activeInHierarchy)
                {
                    b.Drawn = b.Stepped = false;
                    b.Move = b.Back = Vector3.zero;
                    b.CopyOf = null;
                    continue;
                }
                Vector3 pos = t.position;
                Transform parent = t.parent;
                if (b.Stepped)
                {
                    bool sameParent = parent != null && b.DrawParent == parent && b.StepParent == parent;
                    Vector3 move = sameParent ? parent.TransformVector(b.AfterStepLocal - b.AtDrawLocal)
                        : b.AfterStep - b.AtDraw;
                    if (move.sqrMagnitude > CutDistance * CutDistance)
                    {
                        move = Vector3.zero;
                    }
                    b.Move = parent != null ? parent.InverseTransformVector(move) : move;
                    b.MoveParent = parent;
                    b.Stepped = false;
                }
                else if (parent != b.MoveParent)
                {
                    b.Move = Vector3.zero;
                }
                b.AtDraw = pos;
                b.AtDrawLocal = t.localPosition;
                b.DrawParent = parent;
                b.Drawn = true;
                b.Back = b.Move == Vector3.zero ? Vector3.zero
                    : (1f - alpha) * (parent != null ? parent.TransformVector(b.Move) : b.Move);
                b.CopyOf = b.Entity.following != null ? CopySource(b.Entity) : null;
                if (b.CopyOf != null)
                {
                    copiers.Add(b);
                }
                else
                {
                    Shift(b, b.Back);
                }
            }
            foreach (Body b in copiers)
            {
                Shift(b, BackOf(b.CopyOf, 3));
            }
            copiers.Clear();
            foreach (int id in goneBodies)
            {
                bodies.Remove(id);
            }
            goneBodies.Clear();
        }

        // A safety net: a pose still shifted when a physics step begins would be taken as true (slow motion again).
        internal static void BeforePhysics()
        {
            if (drawnShifted)
            {
                RestoreCamera("a physics step began");
            }
        }

        private static void BeforeDraw(Camera cam)
        {
            if (cam == null || cam != MainManager.MainCamera)
            {
                return;
            }
            Sample();
            Rates();
            DrawStarted();
            if (drawnShifted)
            {
                RestoreCamera("the main camera drew again");
            }
            if (!active)
            {
                return;
            }
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            BeforeShifts();
            DrawBodies(alpha);
            DrawScenery(alpha);
            drawnShifted = shiftedBodies.Count > 0 || shiftedScenery.Count > 0;
            if (drawnShifted)
            {
                lastCamera = LastCamera();
            }
            if (!SmoothCamera)
            {
                return;
            }
            Transform t = cam.transform;
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
            savedLocalPos = t.localPosition;
            savedLocalRot = t.localRotation;
            savedTrueYaw = t.eulerAngles.y;
            lastCamera = LastCamera();
            t.position = Vector3.Lerp(prevPos, currPos, alpha);
            t.rotation = Quaternion.Slerp(prevRot, currRot, alpha);
            drawnShifted = cameraShifted = true;
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
                TraceBodies(cam);
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
            if (cameraShifted && t != null)
            {
                t.localPosition = savedLocalPos;
                t.localRotation = savedLocalRot;
            }
            int wrongParent = 0;
            foreach (Body b in shiftedBodies)
            {
                if (b.Entity == null)
                {
                    continue;
                }
                if (b.Entity.transform.parent == b.SavedParent)
                {
                    b.Entity.transform.localPosition = b.Saved;
                }
                else
                {
                    wrongParent++;
                }
            }
            if (late != null && !lateLogged)
            {
                lateLogged = true;
                log.LogWarning($"[fps] still drawn-shifted when {late} (camera {cameraShifted}, bodies {shiftedBodies.Count}, scenery {shiftedScenery.Count}); put back (the last camera, {lastCamera?.name}, didn't draw)");
            }
            RestoreScenery();
            if (wrongParent > 0 && !parentLogged)
            {
                parentLogged = true;
                log.LogWarning($"[fps] {wrongParent} drawn-shifted bodies changed parent before being put back; left where the game put them");
            }
            shiftedBodies.Clear();
            drawnShifted = cameraShifted = false;
        }

        private static bool lateLogged, parentLogged;

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

        // For `if (Time.frameCount % n == 0) return;`, the opposite: on the frames in between, 0, which every n divides,
        // so they skip too and the work is done on the same ticks as at 60 (a follower's walk-or-brake, 30 a second).
        private static int FrameCountSkip()
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
            return tickFrame ? logicalFrame : 0;
        }

        private static IEnumerable<CodeInstruction> TranspileFrameCount(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, code => EditFrameCount(code, nameof(FrameCount)), "fps");

        private static IEnumerable<CodeInstruction> TranspileFrameSkip(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, code => EditFrameCount(code, nameof(FrameCountSkip)), "fps");

        private static IEnumerable<CodeInstruction> EditFrameCount(List<CodeInstruction> code, string helper)
        {
            List<CodeInstruction> reads = code.Where(i => i.Calls(FrameCountGetter)).ToList();
            MethodInfo counter = AccessTools.Method(typeof(FrameRate), helper)
                ?? throw new MissingMethodException(nameof(FrameRate), helper);
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
