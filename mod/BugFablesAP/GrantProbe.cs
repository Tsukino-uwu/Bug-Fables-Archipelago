using System.Collections.Generic;
using BepInEx.Logging;

namespace BugFablesAP
{
    // Dev-only measurement: what else happens when a key item arrives. It logs every key item added to
    // MainManager.instance.items[1], and every flip of flags / regionalflags / crystalbflags, each with the
    // frame and map, so a key-item grant can be matched to the flag that marks its location as done.
    //
    // Read-only: it copies arrays and compares; it never writes game state.
    // Cost per frame: comparing 750 + 100 + 50 bools plus the key-item list, and logging only on change.
    internal sealed class GrantProbe
    {
        private readonly ManualLogSource log;
        private bool[] flags;
        private bool[] regional;
        private bool[] crystal;
        private readonly Dictionary<int, int> keyItemCounts = new Dictionary<int, int>();
        private bool primed;
        private int frame;
        // Loading a save allocates new arrays of the same length (MainManager.cs:17274), so a length check
        // alone would report a whole save load as flag flips. Watch the array identity instead.
        private string lastBlocked = "";
        private bool[] flagsRef;
        private List<int> keyItemsRef;

        internal GrantProbe(ManualLogSource log)
        {
            this.log = log;
        }

        internal void Tick()
        {
            frame++;
            MainManager mm = MainManager.instance;
            string blocked = mm == null ? "MainManager.instance is null"
                : mm.flags == null ? "flags is null"
                : mm.items == null ? "items is null"
                : mm.items.Length < 2 ? $"items has {mm.items.Length} lists"
                : mm.items[1] == null ? "items[1] is null"
                : null;
            // A silent early return hid a whole play session once (2026-09-24). Say why, each time it changes.
            if (blocked != lastBlocked)
            {
                log.LogInfo($"[probe] frame {frame} {(blocked == null ? "reading the game" : "waiting: " + blocked)} map={Where()}");
                lastBlocked = blocked;
            }
            if (blocked != null)
            {
                primed = false;
                return;
            }

            if (primed && (!ReferenceEquals(mm.flags, flagsRef) || !ReferenceEquals(mm.items[1], keyItemsRef)))
            {
                primed = false;
            }

            if (!primed)
            {
                flagsRef = mm.flags;
                keyItemsRef = mm.items[1];
                // A new save or a load replaces the arrays: take a baseline silently, never report it as flips.
                flags = (bool[])mm.flags.Clone();
                regional = mm.regionalflags == null ? new bool[0] : (bool[])mm.regionalflags.Clone();
                crystal = mm.crystalbflags == null ? new bool[0] : (bool[])mm.crystalbflags.Clone();
                CountKeyItems(mm.items[1], keyItemCounts);
                primed = true;
                log.LogInfo($"[probe] baseline at frame {frame}: map={Where()} keyitems={mm.items[1].Count}");
                return;
            }

            if (mm.flags.Length != flags.Length)
            {
                log.LogInfo($"[probe] frame {frame} flags length {flags.Length} -> {mm.flags.Length}; re-baselining");
                primed = false;
                return;
            }

            Diff("flag", mm.flags, flags);
            if (mm.regionalflags != null && mm.regionalflags.Length == regional.Length)
            {
                Diff("regionalflag", mm.regionalflags, regional);
            }
            if (mm.crystalbflags != null && mm.crystalbflags.Length == crystal.Length)
            {
                Diff("crystalbflag", mm.crystalbflags, crystal);
            }

            var now = new Dictionary<int, int>();
            CountKeyItems(mm.items[1], now);
            foreach (KeyValuePair<int, int> entry in now)
            {
                keyItemCounts.TryGetValue(entry.Key, out int before);
                if (entry.Value > before)
                {
                    log.LogInfo($"[probe] frame {frame} KEYITEM +{entry.Value - before} id={entry.Key} "
                        + $"({(MainManager.Items)entry.Key}) map={Where()} event={mm.inevent} message={mm.message}");
                }
            }
            foreach (KeyValuePair<int, int> entry in keyItemCounts)
            {
                now.TryGetValue(entry.Key, out int after);
                if (after < entry.Value)
                {
                    log.LogInfo($"[probe] frame {frame} KEYITEM -{entry.Value - after} id={entry.Key} "
                        + $"({(MainManager.Items)entry.Key}) map={Where()}");
                }
            }
            keyItemCounts.Clear();
            foreach (KeyValuePair<int, int> entry in now)
            {
                keyItemCounts[entry.Key] = entry.Value;
            }
        }

        private void Diff(string kind, bool[] live, bool[] seen)
        {
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i] != seen[i])
                {
                    log.LogInfo($"[probe] frame {frame} {kind}[{i}] {seen[i]} -> {live[i]} map={Where()}");
                    seen[i] = live[i];
                }
            }
        }

        private static void CountKeyItems(List<int> list, Dictionary<int, int> into)
        {
            into.Clear();
            foreach (int id in list)
            {
                into.TryGetValue(id, out int n);
                into[id] = n + 1;
            }
        }

        private static string Where()
        {
            MapControl map = MainManager.map;
            return map == null ? "none" : $"{map.mapid}/{map.areaid}";
        }
    }
}
