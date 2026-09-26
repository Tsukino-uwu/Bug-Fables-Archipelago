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

        // Archipelago's text client colours (NetUtils.py: another player yellow; progression plum, useful slate blue, trap
        // salmon, filler cyan), darkened for the near-white text box (the user's picks), after the game's own text colours
        // (10 in its scene, not the code's 7). Offsets from ApBase.
        internal const int Player = 0, Progression = 1, Useful = 2, Trap = 3, Filler = 4;
        private static readonly string[] apColors = { "B8860B", "8A63D2", "4A6BD8", "E9573F", "008B8B" };
        internal static int ApBase = -1;

        private static Color FromHex(string hex)
        {
            int rgb = Convert.ToInt32(hex, 16);
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        }

        // The array outlives a hot reload: our colours are found again by the first of them, and the list redone.
        internal static void AddApColors()
        {
            MainManager mm = MainManager.instance;
            if (mm == null || mm.textcolors == null || (ApBase >= 0 && mm.textcolors.Length == ApBase + apColors.Length))
            {
                return;
            }
            Color first = FromHex(apColors[0]);
            int own = Array.FindIndex(mm.textcolors, c => Mathf.Approximately(c.r, first.r) && Mathf.Approximately(c.g, first.g)
                && Mathf.Approximately(c.b, first.b));
            ApBase = own >= 0 ? own : mm.textcolors.Length;
            var colors = new List<Color>(mm.textcolors);
            colors.RemoveRange(ApBase, colors.Count - ApBase);
            foreach (string hex in apColors)
            {
                colors.Add(FromHex(hex));
            }
            mm.textcolors = colors.ToArray();
            log.LogInfo($"[show] Archipelago's text colours at index {ApBase} on");
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
