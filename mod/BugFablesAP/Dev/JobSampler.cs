using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BugFablesAP
{
    // The console's "jobs <seconds>": each guarded per-frame job's time and the managed memory it grew by, summed over
    // the sample, to find what feeds the garbage collector (documentation step 25's measurement).
    internal static class JobSampler
    {
        private static float until = -1f;
        private static int frames;
        private static readonly Dictionary<string, (double Ms, long Bytes, int Runs)> totals =
            new Dictionary<string, (double, long, int)>();
        private static readonly Stopwatch watch = new Stopwatch();
        private static long before;

        internal static bool Active => until > 0f;

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
            totals.TryGetValue(name, out var t);
            totals[name] = (t.Ms + watch.Elapsed.TotalMilliseconds, t.Bytes + grew, t.Runs + 1);
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
    }
}
