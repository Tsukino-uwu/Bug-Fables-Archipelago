using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Hold-ups waiting for a free moment (the user, 2026-09-25): a discovery just recorded shows what it found, and an
    // item received from another player is shown as the Item animation setting says (Off, Progression, All). They
    // wait for the same free moment the receiver waits for (ItemReceiver.Busy: no battle, scene, dialogue, menu or map
    // change) and play one at a time. Display only: the item itself is given by the receiver, never here.
    //
    // Bursts (the user tried 50 in a row at "All": about a minute of boxes): only the first waits for a settled moment,
    // the rest follow as soon as the previous box closes, and past BurstShown items from other players the rest of the
    // burst collapses into one "...and N more" box. Your own finds are never collapsed.
    internal static class HoldUps
    {
        private sealed class Entry
        {
            internal Action Show;
            internal bool FromOther;
        }

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static readonly List<Entry> waiting = new List<Entry>();
        // Frames to let a started hold-up open its text before the next may start.
        private static int settle;
        // Free frames in a row before the first of a burst plays: a chain of scenes and fights (the spider fights: scene,
        // fight, scene, fight, scene) can leave a free frame between two links (the user, 2026-09-25), and a hold-up
        // there would cut in. Within a burst, the next follows after NextFor.
        private const int FreeFor = 30;
        private const int NextFor = 3;
        private const int BurstShown = 3;
        private static int freeFrames;
        private static bool inBurst;
        private static int shownFromOthers;

        internal static void Init(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
        }

        internal static void FoundAt(long location, string what)
        {
            waiting.Add(new Entry { Show = () => ItemSwap.ShowFoundAt(location), FromOther = false });
            log.LogInfo($"[show] queued the hold-up for {what}");
        }

        internal static void Received(string name, Sprite sprite, Color? color, string article)
        {
            waiting.Add(new Entry { Show = () => ItemSwap.ShowHeldUp(name, sprite, color, article), FromOther = true });
            log.LogInfo($"[show] queued the hold-up for {name}");
        }

        internal static void Tick()
        {
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
                if (waiting.Count == 0 && !busy)
                {
                    // The burst is over once the queue is empty and the last box has closed.
                    inBurst = false;
                    shownFromOthers = 0;
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
            Entry next = waiting[0];
            if (next.FromOther && shownFromOthers >= BurstShown)
            {
                // The rest of the burst's items from other players in one box; own finds stay queued.
                int rest = waiting.Count(e => e.FromOther);
                waiting.RemoveAll(e => e.FromOther);
                log.LogInfo($"[show] {rest} more items from other players shown in one box");
                mm.StartCoroutine(MainManager.SetText($"|center|...and {rest} more item{(rest == 1 ? "" : "s")} from other players!",
                    dialogue: true, Vector3.zero, null, null));
                return;
            }
            waiting.RemoveAt(0);
            if (next.FromOther)
            {
                shownFromOthers++;
            }
            next.Show();
        }

        // A reload or a new seed: nothing carries over (the items themselves are already given).
        internal static void Clear()
        {
            waiting.Clear();
        }
    }
}
