using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // A goal flag stays on only when one of its own events sets it (slot_data's goal_flags). Set any other way (a borrowed
    // flag, a path nobody found), it is turned back off before the goal is counted, a map is built or a file is saved.
    internal static class GoalGuard
    {
        private static ManualLogSource log;
        private static ApConnection connection;
        private static Func<bool> randomizerOn;
        // The flags array last checked: the game makes a new one for each file loaded or started, taken as it is.
        private static bool[] checkedFlags;
        private static readonly HashSet<int> kept = new HashSet<int>();
        private static readonly HashSet<int> allowed = new HashSet<int>();
        private static readonly HashSet<string> warned = new HashSet<string>();

        internal static void Enable(ManualLogSource logger, ApConnection conn, Func<bool> on)
        {
            log = logger;
            connection = conn;
            randomizerOn = on;
            Hooks.Install(typeof(GoalGuard), "goal-guard",
                "a goal flag set the wrong way is still caught each frame, but not right before a save or a map's build");
        }

        // The dev console's flag command: a goal flag it turns on is kept, as if its event had set it.
        internal static void Allow(int flag)
        {
            bool[] flags = MainManager.instance?.flags;
            if (flags != null && flag >= 0 && flag < flags.Length && !flags[flag])
            {
                allowed.Add(flag);
            }
        }

        internal static void Check(string at)
        {
            MainManager mm = MainManager.instance;
            List<ApConnection.GoalFlag> goals = connection?.GoalFlags;
            if (mm?.flags == null || goals == null || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            bool[] flags = mm.flags;
            if (!ReferenceEquals(flags, checkedFlags))
            {
                checkedFlags = flags;
                kept.Clear();
                allowed.Clear();
                warned.Clear();
                kept.UnionWith(goals.Select(g => g.Flag).Where(f => f >= 0 && f < flags.Length && flags[f]));
                log.LogInfo("[goal-guard] a file loaded or started: its goal flags taken as they are ("
                    + (kept.Count == 0 ? "none on" : "on: " + string.Join(", ", kept.Select(f => f.ToString()).ToArray()))
                    + ")");
                return;
            }
            foreach (ApConnection.GoalFlag goal in goals)
            {
                int flag = goal.Flag;
                if (flag < 0 || flag >= flags.Length)
                {
                    continue;
                }
                if (!flags[flag])
                {
                    kept.Remove(flag);
                    continue;
                }
                if (kept.Contains(flag))
                {
                    continue;
                }
                int ev = MainManager.lastevent;
                if (allowed.Remove(flag))
                {
                    kept.Add(flag);
                    log.LogInfo($"[goal-guard] flag {flag} set by the dev console on {Where()}: kept");
                }
                else if (mm.inevent && goal.Events.Contains(ev))
                {
                    kept.Add(flag);
                    log.LogInfo($"[goal-guard] flag {flag} set by Event{ev}, one of its own, on {Where()}: kept");
                }
                else
                {
                    flags[flag] = false;
                    // Once per flag, room and event per file: a writer that sets it every frame would flood the log.
                    if (warned.Add($"{flag}|{Where()}|{ev}"))
                    {
                        log.LogWarning($"[goal-guard] flag {flag} turned on outside its events ("
                            + string.Join(", ", goal.Events.Select(e => "Event" + e).ToArray())
                            + $") on {Where()}, seen at {at} (last event {ev}, in an event: {mm.inevent}): turned back off");
                    }
                }
            }
        }

        private static string Where()
        {
            MapControl map = MainManager.map;
            return map == null ? "no map" : $"{map.mapid}/{map.areaid}";
        }

        [HarmonyPatch(typeof(MainManager), nameof(MainManager.SaveFile), typeof(UnityEngine.Vector3?))]
        [HarmonyPrefix]
        private static void BeforeSave()
        {
            Safely("a save");
        }

        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPrefix]
        private static void BeforeMapBuild()
        {
            Safely("a map's build");
        }

        // Inside the game's own save and map build: a throw here must never stop either.
        private static void Safely(string at)
        {
            try
            {
                Check(at);
            }
            catch (Exception e)
            {
                log?.LogError($"[goal-guard] the check before {at} threw: {e}");
            }
        }
    }
}
