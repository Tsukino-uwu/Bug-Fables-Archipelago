using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Queued hold-ups (display only; the receiver gives the item), played one at a time when the player is free.
    // In a burst only the first waits for a settled moment; holding skip speeds up the item-get's fixed pauses.
    internal static class HoldUps
    {
        private sealed class Entry
        {
            internal Action Show;
        }

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static readonly List<Entry> waiting = new List<Entry>();
        private static int settle;
        // A chain of scenes and fights can leave a lone free frame between links: the first of a burst waits for FreeFor.
        private const int FreeFor = 30;
        private const int NextFor = 3;
        private static int freeFrames;
        private static bool inBurst;
        private static bool showing;
        private static bool speeding;
        private const float HoldSpeed = 4f;

        internal static void Init(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
        }

        internal static void FoundAt(long location, string what)
        {
            waiting.Add(new Entry { Show = () => ItemSwap.ShowFoundAt(location) });
            log.LogInfo($"[show] queued the hold-up for {what}");
        }

        internal static void Received(string name, Sprite sprite, Color? color, string article)
        {
            waiting.Add(new Entry { Show = () => ItemSwap.ShowHeldUp(name, sprite, color, article) });
            log.LogInfo($"[show] queued the hold-up for {name}");
        }

        internal static void Tick()
        {
            Speed();
            if (settle > 0)
            {
                settle--;
                return;
            }
            MainManager mm = MainManager.instance;
            bool busy = mm == null || ItemReceiver.Busy(mm) != null;
            if (waiting.Count == 0 || busy || randomizerOn == null || !randomizerOn())
            {
                freeFrames = 0;
                if (!busy)
                {
                    showing = false;
                    if (waiting.Count == 0)
                    {
                        inBurst = false;
                    }
                }
                return;
            }
            if (++freeFrames < (inBurst ? NextFor : FreeFor))
            {
                return;
            }
            freeFrames = 0;
            inBurst = true;
            settle = 5;
            showing = true;
            Entry next = waiting[0];
            waiting.RemoveAt(0);
            next.Show();
        }

        // Only a speed this set is undone, so a speed-up by the game or the mod elsewhere is left alone.
        private static void Speed()
        {
            bool want = showing && MainManager.instance != null && MainManager.instance.message && MainManager.GetKey(5, hold: true);
            if (want && !speeding)
            {
                speeding = true;
                Time.timeScale = HoldSpeed;
            }
            else if (!want && speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
            }
        }

        internal static void Clear()
        {
            waiting.Clear();
            if (speeding)
            {
                speeding = false;
                Time.timeScale = 1f;
            }
        }
    }
}
