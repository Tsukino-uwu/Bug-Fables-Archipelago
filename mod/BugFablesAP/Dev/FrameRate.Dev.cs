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
    internal static partial class FrameRate
    {
        // For the patch dump: the hooks are installed as when the row turns on; each does nothing while it's off.
        internal static void InstallForDump() => EnsureInstalled();

        // The console's "fpsscan": reads every method of the game and logs any that counts frames, scales by framestep
        // inside a physics step, or blinks per frame, and isn't in the lists above (or is listed and no longer does).
        internal static string Scan()
        {
            var watch = Stopwatch.StartNew();
            Assembly game = typeof(MainManager).Assembly;
            var methods = new List<MethodBase>();
            foreach (Type type in AccessTools.GetTypesFromAssembly(game))
            {
                foreach (MethodInfo m in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance
                    | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!m.IsAbstract && !m.ContainsGenericParameters && m.GetMethodBody() != null)
                    {
                        methods.Add(m);
                    }
                }
            }
            var calls = new Dictionary<MethodBase, List<MethodBase>>();
            var countsFrames = new HashSet<string>();
            var readsFramestep = new HashSet<MethodBase>();
            var blinks = new HashSet<string>();
            foreach (MethodBase m in methods)
            {
                List<KeyValuePair<OpCode, object>> code = ReadBody(m, log, "fpsscan");
                if (code == null)
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
                            countsFrames.Add(ListName(m));
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
                if (HasBlink(m))
                {
                    blinks.Add(ListName(m));
                }
            }
            countsFrames.Remove("MainManager.FrameDifference");
            var reached = new HashSet<MethodBase>();
            var frontier = methods.Where(m => m.Name == "FixedUpdate" || m.Name.StartsWith("OnTrigger")
                || m.Name.StartsWith("OnCollision")).ToList();
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
            var physics = new HashSet<string>(reached.Where(readsFramestep.Contains).Select(ListName));
            string result =
                $"fpsscan: {methods.Count} methods in {watch.ElapsedMilliseconds} ms. Frame counts {Diff(countsFrames, FrameCounters.Concat(FrameSkippers).ToArray())}. "
                + $"Physics framestep {Diff(physics, PhysicsFramestep)}. Blinks {Diff(blinks, Blinkers)}.";
            log.LogInfo("[dev] " + result);
            return result;
        }

        private static string Diff(HashSet<string> seen, string[] listed)
        {
            string[] missing = seen.Except(listed).ToArray();
            string[] stale = listed.Except(seen).ToArray();
            return "missing from the list: " + (missing.Length > 0 ? string.Join(", ", missing) : "none")
                + "; listed but not seen: " + (stale.Length > 0 ? string.Join(", ", stale) : "none");
        }

        // As the lists name a method: a coroutine's MoveNext is its outer type's method with a star.
        private static string ListName(MethodBase m)
        {
            Type t = m.DeclaringType;
            if (t != null && t.Name.StartsWith("<") && t.DeclaringType != null)
            {
                return t.DeclaringType.Name + "." + t.Name.Substring(1, t.Name.IndexOf('>') - 1) + "*";
            }
            return t?.Name + "." + m.Name;
        }


        // The console's "trace [frames]": where the nearest NPC showing an emoticon, its emoticon and the player land
        // on screen in each drawn frame (x in pixels), to see which of them doesn't move with the smoothed camera.
        private static int traceLeft;
        private static readonly System.Text.StringBuilder traceLog = new System.Text.StringBuilder();

        internal static string StartTrace(int frames)
        {
            traceLeft = frames;
            traceLog.Length = 0;
            return $"trace: {frames} frames";
        }

        static partial void Trace(Camera cam)
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
                Vector3 ms = npc.emoticonsprite != null ? cam.WorldToScreenPoint(npc.emoticonsprite.transform.position)
                    : m;
                traceLog.Append($" npc {n.x:0.0} emoticon {m.x:0.0} sprite {ms.x:0.0},{ms.y:0.0} yaw cam {cam.transform.eulerAngles.y:0.00} "
                    + $"true {savedTrueYaw:0.00} bubble {npc.emoticon.transform.eulerAngles.y:0.00} rotater {npc.emoticon.transform.parent.eulerAngles.y:0.00} ({npc.name}, interp {(npc.rigid != null ? npc.rigid.interpolation.ToString() : "none")}, "
                    + $"emoticon parent {(npc.emoticon.transform.parent != null ? npc.emoticon.transform.parent.name : "none")})");
            }
            if (--traceLeft == 0)
            {
                log.LogInfo("[dev] trace:" + traceLog);
            }
        }

        // ---- The console's "bodytrace [frames]": the leader's and the first follower's place on screen each drawn
        // frame, as drawn and at their true pose, and the spread of its frame-to-frame change: at a steady walk, drawn
        // smoothed, it should hold still.

        internal static int SmoothedCount => bodies.Count;

        private sealed class Traced
        {
            internal readonly List<float> Drawn = new List<float>(), True = new List<float>();
        }

        private static int bodyTraceLeft;
        private static float traceFixedTime = -1f;
        private static Vector3 traceLastLeader;
        private static readonly System.Text.StringBuilder bodyTraceLog = new System.Text.StringBuilder();
        private static readonly Traced tracedLeader = new Traced(), tracedFollower = new Traced();

        internal static string StartBodyTrace(int frames)
        {
            bodyTraceLeft = frames;
            bodyTraceLog.Length = 0;
            foreach (Traced t in new[] { tracedLeader, tracedFollower })
            {
                t.Drawn.Clear();
                t.True.Clear();
            }
            return $"bodytrace: {frames} frames, from when the leader or a follower moves";
        }

        private static Vector3 TruePos(EntityControl e)
        {
            return bodies.TryGetValue(e.GetInstanceID(), out Body b) && shiftedBodies.Contains(b)
                ? b.SavedParent != null ? b.SavedParent.TransformPoint(b.Saved) : b.Saved
                : e.transform.position;
        }

        private static string Place(Camera cam, EntityControl e, Traced into)
        {
            Vector3 truePos = TruePos(e);
            float dx = cam.WorldToScreenPoint(e.transform.position).x;
            float tx = cam.WorldToScreenPoint(truePos).x;
            into.Drawn.Add(dx);
            into.True.Add(tx);
            float move = bodies.TryGetValue(e.GetInstanceID(), out Body b) ? b.Move.magnitude : -1f;
            return $"drawn x {dx:0.0} true x {tx:0.0} move {move:0.000} vel {(e.rigid != null ? e.rigid.velocity.magnitude : 0f):0.00}"
                + $"{(e.forcemove ? " walking" : "")}";
        }

        static partial void TraceBodies(Camera cam)
        {
            if (bodyTraceLeft <= 0 || MainManager.player == null || MainManager.player.entity == null)
            {
                return;
            }
            EntityControl leader = MainManager.player.entity;
            MainManager.BattleData[] party = MainManager.instance.playerdata;
            EntityControl follower = party != null && party.Length > 1 && party[1].entity != leader ? party[1].entity
                : party != null && party.Length > 0 && party[0].entity != leader ? party[0].entity : null;
            Vector3 leaderPos = TruePos(leader);
            bool leaderMoved = (leaderPos - traceLastLeader).sqrMagnitude > 1e-6f;
            traceLastLeader = leaderPos;
            // Armed until something moves, so the trace covers a walk, not the wait for one.
            if (tracedLeader.Drawn.Count == 0 && !leaderMoved && (leader.rigid == null
                || leader.rigid.velocity.sqrMagnitude < 0.25f) && (follower == null || follower.rigid == null
                || follower.rigid.velocity.sqrMagnitude < 0.25f))
            {
                return;
            }
            Collider on = MainManager.player.standingon;
            bool stepped = Time.fixedTime != traceFixedTime;
            traceFixedTime = Time.fixedTime;
            bodyTraceLog.Append($"\n  dt {Time.unscaledDeltaTime * 1000f:0.0} a {(Time.time - Time.fixedTime) / Time.fixedDeltaTime:0.00} "
                + $"step {(stepped ? 1 : 0)} | leader {Place(cam, leader, tracedLeader)}"
                + $" on {(on != null ? on.name + "/" + on.tag : "none")} parent {(leader.transform.parent != null ? leader.transform.parent.name : "none")}"
                + (follower != null ? $" | {follower.name} {Place(cam, follower, tracedFollower)}" : ""));
            if (--bodyTraceLeft == 0)
            {
                log.LogInfo($"[dev] bodytrace (bodylerp {(SmoothBodies ? "on" : "off")}, camlerp {(SmoothCamera ? "on" : "off")}): "
                    + $"leader drawn {Spread(tracedLeader.Drawn)}, true {Spread(tracedLeader.True)}; "
                    + $"{(follower != null ? follower.name : "no follower")} drawn {Spread(tracedFollower.Drawn)}, true {Spread(tracedFollower.True)}"
                    + bodyTraceLog);
            }
        }

        private static string Spread(List<float> xs)
        {
            if (xs.Count < 3)
            {
                return "too few frames";
            }
            var steps = new List<float>();
            for (int i = 1; i < xs.Count; i++)
            {
                steps.Add(xs[i] - xs[i - 1]);
            }
            float mean = steps.Average();
            float sd = Mathf.Sqrt(steps.Select(s => (s - mean) * (s - mean)).Average());
            return $"mean step {mean:0.00} px, spread {sd:0.00} px";
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

        static partial void DrawStarted()
        {
            drawWork.Restart();
        }

        static partial void DrawEnded()
        {
            drawWork.Stop();
            lastDrawMs = (float)drawWork.Elapsed.TotalMilliseconds;
        }

        // The console's "rates <seconds>": per second, how many sixtieths the frames were worth (Step) and how many
        // frames started a new sixtieth (OnTick). Both 60 means everything built on them runs as at 60 fps.
        private static float ratesUntil = -1f, ratesStart, stepSum;
        private static int ratesFrames, tickCount, ratesLastFrame = -1;

        internal static string StartRates(float seconds)
        {
            ratesUntil = Time.realtimeSinceStartup + seconds;
            ratesStart = Time.realtimeSinceStartup;
            stepSum = 0f;
            ratesFrames = tickCount = 0;
            return $"rates: measuring {seconds} s";
        }

        static partial void Rates()
        {
            if (ratesUntil < 0f || Time.frameCount == ratesLastFrame)
            {
                return;
            }
            ratesLastFrame = Time.frameCount;
            ratesFrames++;
            stepSum += Step;
            tickCount += OnTick ? 1 : 0;
            if (Time.realtimeSinceStartup < ratesUntil)
            {
                return;
            }
            float span = Time.realtimeSinceStartup - ratesStart;
            ratesUntil = -1f;
            log.LogInfo($"[dev] rates over {span:0.00} s (row {(active ? activeCap.ToString() : "off")}): {ratesFrames / span:0.0} frames/s, "
                + $"frame worth {stepSum / span:0.00} sixtieths/s, new-sixtieth frames {tickCount / span:0.00}/s (60 and 60 = as at 60 fps)");
        }

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

        static partial void Sample()
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
