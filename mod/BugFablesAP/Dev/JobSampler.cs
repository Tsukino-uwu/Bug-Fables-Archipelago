using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BugFablesAP
{
    // The console's "jobs <seconds>": each guarded per-frame job's time and the managed memory it grew by, summed over
    // the sample, to find what feeds the garbage collector (documentation step 25's measurement). With Debug.GcWatch,
    // the same per job, summed between collections, and one line per collection.
    internal static class JobSampler
    {
        private static float until = -1f;
        private static int frames;
        private static readonly Dictionary<string, (double Ms, long Bytes, int Runs)> totals =
            new Dictionary<string, (double, long, int)>();
        private static readonly Stopwatch watch = new Stopwatch();
        private static long before;

        internal static bool Active => until > 0f;
        internal static bool Watching;

        private const float CloseGapSeconds = 10f;
        private static readonly Dictionary<string, long> sinceGc = new Dictionary<string, long>();
        private static int watchFrames;
        private static int lastCount = -1;
        private static float lastGcAt = -1f;
        private static long lastHeap;
        private static long madeSince;

        internal static string Start(float seconds)
        {
            totals.Clear();
            frames = 0;
            until = Time.realtimeSinceStartup + seconds;
            return $"jobs: measuring {seconds} s";
        }

        internal static void JobStart()
        {
            before = GC.GetTotalMemory(false);
            watch.Restart();
        }

        internal static void JobEnd(string name)
        {
            watch.Stop();
            long grew = Math.Max(0, GC.GetTotalMemory(false) - before);
            if (Active && name != "tick")
            {
                totals.TryGetValue(name, out var t);
                totals[name] = (t.Ms + watch.Elapsed.TotalMilliseconds, t.Bytes + grew, t.Runs + 1);
            }
            // "tick" wraps every job, so it would count them twice.
            if (Watching && name != "tick")
            {
                sinceGc.TryGetValue(name, out long b);
                sinceGc[name] = b + grew;
            }
        }

        // Once a frame, after the tick: ends the sample and logs its table.
        internal static string FrameEnd()
        {
            if (!Active)
            {
                return null;
            }
            frames++;
            if (Time.realtimeSinceStartup < until)
            {
                return null;
            }
            until = -1f;
            int n = Math.Max(1, frames);
            string table = string.Join(", ", totals.OrderByDescending(t => t.Value.Bytes).Select(t =>
                $"{t.Key} {t.Value.Bytes / n / 1024.0:0.0} KB/frame {t.Value.Ms / n:0.000} ms"));
            return $"jobs: {frames} frames, {GC.CollectionCount(0)} collections so far; per frame: {table}";
        }

        // Once a frame, after the tick: the heap's growth since the last frame, and on a new collection its line.
        internal static void WatchFrameEnd(BepInEx.Logging.ManualLogSource log)
        {
            if (!Watching)
            {
                lastCount = -1;
                return;
            }
            long heap = GC.GetTotalMemory(false);
            int count = GC.CollectionCount(0);
            float now = Time.realtimeSinceStartup;
            if (lastCount < 0)
            {
                lastCount = count;
                lastGcAt = now;
                lastHeap = heap;
                return;
            }
            watchFrames++;
            if (heap > lastHeap)
            {
                madeSince += heap - lastHeap;
            }
            lastHeap = heap;
            if (count == lastCount)
            {
                return;
            }
            float gap = now - lastGcAt;
            int n = Math.Max(1, watchFrames);
            string top = string.Join(", ", sinceGc.OrderByDescending(j => j.Value).Take(3)
                .Select(j => $"{j.Key} {j.Value / n / 1024.0:0.0}"));
            double mb = madeSince / 1048576.0;
            string line = $"[gc] collection #{count}: {gap:0.0} s since the last, frame "
                + $"{Time.unscaledDeltaTime * 1000f:0.0} ms; {mb:0.0} MB made since ({mb / Math.Max(0.01f, gap):0.0} "
                + $"MB/s); top makers (KB/frame): {top}";
            if (gap < CloseGapSeconds)
            {
                log.LogWarning(line + $" - under {CloseGapSeconds} s apart: something makes garbage every frame");
            }
            else
            {
                log.LogInfo(line);
            }
            lastCount = count;
            lastGcAt = now;
            madeSince = 0;
            watchFrames = 0;
            sinceGc.Clear();
        }
    }
}
