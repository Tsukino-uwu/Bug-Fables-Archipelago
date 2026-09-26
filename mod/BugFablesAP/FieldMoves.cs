using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Shuffle Field Moves / Shuffle Jump: the leader's field attack (0 Beemerang, 1 Horn, 2 Ice) and the jump wait for
    // their item; a press before then plays the game's own buzzer. Only while Archipelago is on and the seed says so.
    internal static class FieldMoves
    {
        internal const int Jump = 3;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        private static Harmony harmony;

        // From slot_data (shuffle_moves, shuffle_jump); false with no seed.
        internal static volatile bool MovesShuffled;
        internal static volatile bool JumpShuffled;
        // The moves this save has been given, recomputed from the counted items every frame (ItemReceiver).
        private static readonly HashSet<int> received = new HashSet<int>();

        internal static void SetReceived(IEnumerable<int> ids)
        {
            received.Clear();
            received.UnionWith(ids);
        }

        internal static string Name(int id) => id == 0 ? "Beemerang" : id == 1 ? "Horn" : id == 2 ? "Ice" : id == Jump ? "Jump" : "move " + id;

        internal static bool Locked(int id)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null)
            {
                return false;
            }
            bool shuffled = id == Jump ? JumpShuffled : MovesShuffled;
            return shuffled && !received.Contains(id);
        }

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            MethodInfo tap = AccessTools.Method(typeof(PlayerControl), "DoActionTap");
            MethodInfo jump = AccessTools.Method(typeof(PlayerControl), "DoJump");
            if (tap == null || jump == null)
            {
                log.LogError($"[moves] NOT installed (DoActionTap {tap != null}, DoJump {jump != null}): moves and jump are never locked.");
                return;
            }
            harmony = new Harmony(guid + ".moves." + DateTime.UtcNow.Ticks);
            harmony.Patch(tap, prefix: new HarmonyMethod(typeof(FieldMoves), nameof(BeforeActionTap)));
            harmony.Patch(jump, prefix: new HarmonyMethod(typeof(FieldMoves), nameof(BeforeJump)));
            log.LogInfo("[moves] installed on PlayerControl.DoActionTap and DoJump");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static readonly HashSet<string> reported = new HashSet<string>();

        private static void Refuse(int id)
        {
            MainManager.PlayBuzzer();
            if (reported.Add(Name(id) + (MainManager.map != null ? MainManager.map.mapid.ToString() : "")))
            {
                log.LogInfo($"[moves] {Name(id)} pressed without its item: refused (buzzer)");
            }
        }

        private static IEnumerator Nothing()
        {
            yield break;
        }

        // The tap's move is the leader's (playerdata[0].animid); the submarine's tap is its own and never locked.
        private static bool BeforeActionTap(PlayerControl __instance, ref IEnumerator __result)
        {
            MainManager mm = MainManager.instance;
            if (__instance.submarine || mm?.playerdata == null || mm.playerdata.Length == 0)
            {
                return true;
            }
            int move = mm.playerdata[0].animid;
            if (move < 0 || move > 2 || !Locked(move))
            {
                return true;
            }
            Refuse(move);
            __result = Nothing();
            return false;
        }

        private static bool BeforeJump()
        {
            if (!Locked(Jump))
            {
                return true;
            }
            Refuse(Jump);
            return false;
        }
    }
}
