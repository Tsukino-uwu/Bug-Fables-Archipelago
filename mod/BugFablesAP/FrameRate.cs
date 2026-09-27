using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Frame rates above 60. The game moves its camera and its characters in FixedUpdate (50 steps a second), so a
    // higher rate only looks smoother when what is drawn is placed between the last two steps.
    internal static class FrameRate
    {
        private static ManualLogSource log;

        // The camera is drawn between its last two physics-step poses and put back after drawing, so the game's
        // own camera code never sees a changed value.
        internal static bool SmoothCamera;
        private static Vector3 prevPos, currPos;
        private static Quaternion prevRot, currRot;
        private static float lastFixedTime = -1f;
        private static bool drawnShifted;
        private static Vector3 savedLocalPos;
        private static Quaternion savedLocalRot;
        // A camera jump further than this in one step is a cut, not motion.
        private const float CutDistance = 3f;

        internal static void Enable(ManualLogSource logger)
        {
            log = logger;
            Camera.onPreCull += BeforeDraw;
            Camera.onPostRender += AfterDraw;
        }

        internal static void Disable()
        {
            Camera.onPreCull -= BeforeDraw;
            Camera.onPostRender -= AfterDraw;
        }

        // The console's "frames <seconds>": each frame's time and whether a garbage collection ran in it.
        private static float sampleUntil = -1f;
        private static readonly System.Collections.Generic.List<float> sampleTimes = new System.Collections.Generic.List<float>();
        private static readonly System.Collections.Generic.List<bool> sampleGc = new System.Collections.Generic.List<bool>();
        private static readonly System.Collections.Generic.List<float> sampleAt = new System.Collections.Generic.List<float>();
        private static int lastGcCount;
        private static readonly System.Diagnostics.Stopwatch drawWork = new System.Diagnostics.Stopwatch();
        private static float lastDrawMs;
        private static readonly System.Collections.Generic.List<float> sampleDraw = new System.Collections.Generic.List<float>();

        internal static string StartSample(float seconds)
        {
            sampleTimes.Clear();
            sampleGc.Clear();
            sampleAt.Clear();
            sampleDraw.Clear();
            lastGcCount = System.GC.CollectionCount(0);
            sampleUntil = Time.realtimeSinceStartup + seconds;
            return $"frames: sampling {seconds} s";
        }

        private static void Sample()
        {
            if (sampleUntil < 0f)
            {
                return;
            }
            int gc = System.GC.CollectionCount(0);
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
            var sorted = new System.Collections.Generic.List<float>(sampleTimes);
            sorted.Sort();
            float median = sorted[sorted.Count / 2];
            int slow = 0, slowWithGc = 0, gcs = 0;
            var worst = new System.Text.StringBuilder();
            for (int i = 0; i < sampleTimes.Count; i++)
            {
                gcs += sampleGc[i] ? 1 : 0;
                if (sampleTimes[i] > median * 2f)
                {
                    slow++;
                    slowWithGc += sampleGc[i] ? 1 : 0;
                    if (slow <= 40)
                    {
                        worst.Append($" {sampleTimes[i]:0.0}{(sampleGc[i] ? "(gc)" : "")}@{sampleAt[i]:0.00}[draw {sampleDraw[i]:0.0}]");
                    }
                }
            }
            log.LogInfo($"[dev] frames: {sampleTimes.Count} frames, median {median:0.00} ms, max {sorted[sorted.Count - 1]:0.0} ms, "
                + $"{slow} over twice the median ({slowWithGc} with a garbage collection), {gcs} collections in all; slow frames (ms):{worst}");
        }

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
            t.position = Vector3.Lerp(prevPos, currPos, alpha);
            t.rotation = Quaternion.Slerp(prevRot, currRot, alpha);
            drawnShifted = true;
        }

        private static void AfterDraw(Camera cam)
        {
            if (cam != null && cam == MainManager.MainCamera)
            {
                drawWork.Stop();
                lastDrawMs = (float)drawWork.Elapsed.TotalMilliseconds;
            }
            if (!drawnShifted || cam == null || cam != MainManager.MainCamera)
            {
                return;
            }
            cam.transform.localPosition = savedLocalPos;
            cam.transform.localRotation = savedLocalRot;
            drawnShifted = false;
        }
    }
}
