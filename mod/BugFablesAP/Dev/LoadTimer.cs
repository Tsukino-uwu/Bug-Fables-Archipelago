using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Dev only ([Debug] LoadTimer): each room change, from TransferMap to the screen back and then to control back, with
    // the game's LoadMap and CreateEntities and the mod's own share: its patches on that path, the helpers its
    // transpilers call there, and its per-frame ticks while the change runs.
    internal static class LoadTimer
    {
        private static ManualLogSource log;
        private static readonly Stopwatch total = new Stopwatch();
        private static bool timing;
        private static string fromMap;
        private static long screenBackTicks = -1, blackTicks = -1, loadStartTicks = -1, loadEndTicks = -1;
        private static long loadMapTicks, createTicks, tickTicks;
        private static int frames;
        private static readonly Dictionary<string, long> modTicks = new Dictionary<string, long>();
        private static readonly Dictionary<string, int> modCalls = new Dictionary<string, int>();

        private static string owner;
        private static bool installed;

        // Installed on the first tick, once the mod's own hooks are all in, so it can find them.
        internal static void Enable(ManualLogSource logger, string guid)
        {
            log = logger;
            owner = guid;
        }

        private static void Install()
        {
            installed = true;
            string guid = owner;
            Harmony harmony = Hooks.Create("loadtime");
            HarmonyMethod Own(string name) => new HarmonyMethod(AccessTools.Method(typeof(LoadTimer), name));
            List<MethodBase> transfer = AccessTools.GetDeclaredMethods(typeof(MainManager))
                .Where(m => m.Name == nameof(MainManager.TransferMap)).Cast<MethodBase>().ToList();
            List<MethodBase> loadMap = AccessTools.GetDeclaredMethods(typeof(MainManager))
                .Where(m => m.Name == nameof(MainManager.LoadMap)).Cast<MethodBase>().ToList();
            MethodBase create = AccessTools.Method(typeof(MapControl), "CreateEntities");
            foreach (MethodBase m in transfer)
            {
                harmony.Patch(m, prefix: Own(nameof(TransferStart)));
            }
            // LoadMap(int) only for its time: the other two call it.
            MethodBase loadById = AccessTools.Method(typeof(MainManager), nameof(MainManager.LoadMap), new[] { typeof(int) });
            harmony.Patch(loadById, prefix: Own(nameof(Begin)), postfix: Own(nameof(EndLoadMap)));
            harmony.Patch(create, prefix: Own(nameof(Begin)), postfix: Own(nameof(EndCreate)));
            foreach (MethodBase m in loadMap)
            {
                harmony.Patch(m, prefix: Own(nameof(LoadStarts)), postfix: Own(nameof(LoadEnds)));
            }

            // The mod's own work on that path. Patching a patch method times it each time its target runs.
            var load = new List<MethodBase>(loadMap) { create,
                AccessTools.Method(typeof(MainManager), nameof(MainManager.CheckIfCanExist)),
                AccessTools.Method(typeof(NPCControl), "Start"), AccessTools.Method(typeof(MapControl), "Start") };
            var mine = new HashSet<MethodInfo>();
            foreach (MethodBase target in load.Where(t => t != null))
            {
                Patches info = Harmony.GetPatchInfo(target);
                if (info == null)
                {
                    continue;
                }
                foreach (Patch p in info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers)
                    .Where(p => p.owner.StartsWith(guid) && !p.owner.Contains(".loadtime.")))
                {
                    mine.Add(p.PatchMethod);
                }
            }
            mine.Add(AccessTools.Method(typeof(DoorRows), "Apply"));
            mine.Add(AccessTools.Method(typeof(DayNight), "AddCopies"));
            foreach (MethodInfo m in mine.Where(m => m != null))
            {
                harmony.Patch(m, prefix: Own(nameof(Begin)), postfix: Own(nameof(EndMod)));
            }
            foreach (string tick in new[] { "Update", "LateUpdate", "FixedUpdate" })
            {
                MethodInfo m = AccessTools.Method(typeof(Plugin), tick);
                if (m != null)
                {
                    harmony.Patch(m, prefix: Own(nameof(Begin)), postfix: Own(nameof(EndTick)));
                }
            }
            log.LogInfo($"[loadtime] on: TransferMap x{transfer.Count}, LoadMap x{loadMap.Count}, CreateEntities, and {mine.Count} of the mod's methods on that path");
        }

        private static void TransferStart()
        {
            if (timing)
            {
                return;
            }
            timing = true;
            fromMap = MainManager.map != null ? MainManager.map.mapid.ToString() : "?";
            screenBackTicks = blackTicks = loadStartTicks = loadEndTicks = -1;
            loadMapTicks = createTicks = tickTicks = 0;
            frames = 0;
            modTicks.Clear();
            modCalls.Clear();
            total.Reset();
            total.Start();
        }

        private static void Begin(out long __state) => __state = Stopwatch.GetTimestamp();

        private static void LoadStarts()
        {
            if (timing && loadStartTicks < 0)
            {
                loadStartTicks = total.ElapsedTicks;
            }
        }

        private static void LoadEnds()
        {
            if (timing)
            {
                loadEndTicks = total.ElapsedTicks;
            }
        }

        private static void EndLoadMap(long __state)
        {
            if (timing)
            {
                loadMapTicks += Stopwatch.GetTimestamp() - __state;
            }
        }

        private static void EndCreate(long __state)
        {
            if (timing)
            {
                createTicks += Stopwatch.GetTimestamp() - __state;
            }
        }

        private static void EndMod(MethodBase __originalMethod, long __state)
        {
            if (!timing || __originalMethod == null)
            {
                return;
            }
            string name = __originalMethod.DeclaringType?.Name + "." + __originalMethod.Name;
            modTicks.TryGetValue(name, out long t);
            modTicks[name] = t + Stopwatch.GetTimestamp() - __state;
            modCalls.TryGetValue(name, out int n);
            modCalls[name] = n + 1;
        }

        private static void EndTick(long __state)
        {
            if (timing)
            {
                tickTicks += Stopwatch.GetTimestamp() - __state;
            }
        }

        private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        // Each frame: the screen is back once the room transfer and its fade are over, control once the walk in ends.
        internal static void Tick()
        {
            if (owner != null && !installed)
            {
                Install();
            }
            if (!timing)
            {
                return;
            }
            frames++;
            MainManager mm = MainManager.instance;
            // The black: the transition sprite at full alpha, from the fade out's end to the fade in's start.
            UnityEngine.SpriteRenderer fade = mm != null && mm.transitionobj != null && mm.transitionobj.Length > 0
                && mm.transitionobj[0] != null ? mm.transitionobj[0].GetComponent<UnityEngine.SpriteRenderer>() : null;
            float alpha = fade != null ? fade.color.a : 0f;
            if (blackTicks < 0 && alpha >= 0.9f)
            {
                blackTicks = total.ElapsedTicks;
            }
            if (blackTicks >= 0 && screenBackTicks < 0 && loadEndTicks >= 0 && alpha < 0.9f)
            {
                screenBackTicks = total.ElapsedTicks;
            }
            bool controlBack = mm != null && !MainManager.roomtransition && !mm.minipause && MainManager.player != null;
            if (!controlBack && total.ElapsedMilliseconds <= 60000)
            {
                return;
            }
            total.Stop();
            timing = false;
            double mod = modTicks.Values.Sum(Ms);
            string top = string.Join(", ", modTicks.OrderByDescending(k => k.Value).Take(8)
                .Select(k => $"{k.Key} {Ms(k.Value):0.0} ({modCalls[k.Key]}x)"));
            string to = MainManager.map != null ? MainManager.map.mapid.ToString() : "?";
            double At(long ticks) => ticks < 0 ? double.NaN : ticks * 1000.0 / Stopwatch.Frequency;
            double black = At(blackTicks), load = At(loadStartTicks), back = At(screenBackTicks);
            double end = total.Elapsed.TotalMilliseconds;
            log.LogInfo($"[loadtime] {fromMap} -> {to}: walk in and fade out {black:0} ms; black {back - black:0} ms (LoadMap from {load - black:0} ms in, {Ms(loadMapTicks):0} ms, CreateEntities {Ms(createTicks):0.0} ms); walk in after {end - back:0} ms; total {end:0} ms. The mod: load hooks {mod:0.0} ms, per-frame ticks {Ms(tickTicks):0.0} ms over {frames} frames; top: {top}");
        }
    }
}
