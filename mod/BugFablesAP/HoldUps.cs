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
    internal static class HoldUps
    {
        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static readonly Queue<Action> waiting = new Queue<Action>();
        // Frames to let a started hold-up open its text before the next may start.
        private static int settle;
        // Free frames in a row before one plays: a chain of scenes and fights (the spider fights: scene, fight, scene,
        // fight, scene) can leave a free frame between two links (the user, 2026-09-25), and a hold-up there would cut in.
        private const int FreeFor = 30;
        private static int freeFrames;

        internal static void Init(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
        }

        internal static void FoundAt(long location, string what)
        {
            waiting.Enqueue(() => ItemSwap.ShowFoundAt(location));
            log.LogInfo($"[show] queued the hold-up for {what}");
        }

        internal static void Received(string name, Sprite sprite, Color? color, string article)
        {
            waiting.Enqueue(() => ItemSwap.ShowHeldUp(name, sprite, color, article));
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
            if (waiting.Count == 0 || mm == null || randomizerOn == null || !randomizerOn() || ItemReceiver.Busy(mm) != null)
            {
                freeFrames = 0;
                return;
            }
            if (++freeFrames < FreeFor)
            {
                return;
            }
            freeFrames = 0;
            waiting.Dequeue()();
            settle = 30;
        }

        // A reload or a new seed: nothing carries over (the items themselves are already given).
        internal static void Clear()
        {
            waiting.Clear();
        }
    }
}
