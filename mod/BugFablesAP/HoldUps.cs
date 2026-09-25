using System;
using System.Collections.Generic;
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
    // the rest follow as soon as the previous box closes, every item still gets its own box (a "...and N more" summary
    // felt off to the user), and holding the skip button runs the game faster while the mod's hold-up is on screen, so
    // the item-get's fixed pauses (WaitForSeconds in Giveitem) pass quickly too.
    internal static class HoldUps
    {
        private sealed class Entry
        {
            internal Action Show;
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
        private static int freeFrames;
        private static bool inBurst;
        // Frames since the mod's last hold-up started, while its box may still be up; and whether this sped the game up.
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
                        // The burst is over once the queue is empty and the last box has closed.
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

        // While the mod's own hold-up is on screen and the skip button is held, the game runs faster; otherwise normal.
        // Only a speed this set is undone, so a scene the game or the mod speeds up itself is left alone.
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

        // A reload or a new seed: nothing carries over (the items themselves are already given).
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
