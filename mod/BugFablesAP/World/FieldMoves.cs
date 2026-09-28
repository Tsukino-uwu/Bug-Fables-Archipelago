using System;
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

        // From slot_data (shuffle_moves, shuffle_jump); false with no seed.
        internal static volatile bool MovesShuffled;
        internal static volatile bool JumpShuffled;

        internal static string Name(int id) => id == 0 ? "Beemerang Toss" : id == 1 ? "Horn Slash" : id == 2 ? "Freeze" : id == Jump ? "Jump" : "move " + id;

        internal static bool Locked(int id)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null)
            {
                return false;
            }
            bool shuffled = id == Jump ? JumpShuffled : MovesShuffled;
            // A move works once its key item (CustomItems) is in the bag, where the receiver puts it.
            return shuffled && MainManager.instance?.items != null && !MainManager.instance.items[1].Contains(CustomItems.MoveKeyItem(id));
        }

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            // DoActionTap only builds its coroutine, small enough to be inlined into its callers, where a patch never runs
            // (a prefix there never fired): the coroutine's own first step is gated instead.
            MethodInfo tap = AccessTools.Method(typeof(PlayerControl), "DoActionTap");
            MethodInfo tapStep = tap != null ? AccessTools.EnumeratorMoveNext(tap) : null;
            MethodInfo jump = AccessTools.Method(typeof(PlayerControl), "DoJump");
            if (tapStep == null || jump == null)
            {
                log.LogError($"[moves] NOT installed (DoActionTap's MoveNext {tapStep != null}, DoJump {jump != null}): moves and jump are never locked.");
                return;
            }
            tapState = AccessTools.Field(tapStep.DeclaringType, "<>1__state");
            tapOwner = AccessTools.Field(tapStep.DeclaringType, "<>4__this");
            if (!Hooks.Install(typeof(FieldMoves), "moves", "moves and jump are never locked"))
            {
                return;
            }
            log.LogInfo($"[moves] installed on PlayerControl.DoActionTap's first step (state field {tapState != null}, owner {tapOwner != null}) and DoJump");
        }

        private static readonly HashSet<string> reported = new HashSet<string>();

        // The game fires a tap on release and retries a held one every few frames, so the attack's buzz is played on the
        // press itself (Tick) and its refusals stay silent; the jump fires on the press and buzzes there.
        private static void Refuse(int id)
        {
            if (id == Jump)
            {
                MainManager.PlayBuzzer();
            }
            if (reported.Add(Name(id) + (MainManager.map != null ? MainManager.map.mapid.ToString() : "")))
            {
                log.LogInfo($"[moves] {Name(id)} pressed without its item: refused (buzzer)");
            }
        }

        internal static void Tick()
        {
            MainManager mm = MainManager.instance;
            if (mm?.playerdata == null || mm.playerdata.Length == 0 || MainManager.player == null || MainManager.battle != null
                || mm.pause || mm.minipause || mm.inevent || mm.message || MainManager.player.submarine)
            {
                return;
            }
            int move = mm.playerdata[0].animid;
            if (move >= 0 && move <= 2 && Locked(move) && MainManager.GetKey(5, hold: false))
            {
                MainManager.PlayBuzzer();
            }
        }

        private static FieldInfo tapState;
        private static FieldInfo tapOwner;

        // The tap's move is the leader's (playerdata[0].animid); the submarine's tap is its own and never locked. Only the
        // first step (state 0) is checked; a refused tap ends there, before it sets action or lockkeys.
        [HarmonyPatch(typeof(PlayerControl), "DoActionTap", MethodType.Enumerator)]
        [HarmonyPrefix]
        private static bool BeforeTapStep(object __instance, ref bool __result)
        {
            if (tapState == null || (int)tapState.GetValue(__instance) != 0)
            {
                return true;
            }
            MainManager mm = MainManager.instance;
            PlayerControl player = tapOwner?.GetValue(__instance) as PlayerControl;
            if (player == null || player.submarine || mm?.playerdata == null || mm.playerdata.Length == 0)
            {
                return true;
            }
            int move = mm.playerdata[0].animid;
            if (move < 0 || move > 2 || !Locked(move))
            {
                return true;
            }
            Refuse(move);
            tapState.SetValue(__instance, -1);
            __result = false;
            player.StartCoroutine(ClearActionRoutine(player));
            return false;
        }

        // The game clears actionroutine only at a tap's end; the caller stores the refused one after this step, and the hold
        // path starts a tap only while it's null. Cleared a frame later, as a finished tap leaves it.
        private static readonly FieldInfo actionRoutine = AccessTools.Field(typeof(PlayerControl), "actionroutine");

        private static System.Collections.IEnumerator ClearActionRoutine(PlayerControl player)
        {
            yield return null;
            if (player != null && !player.action)
            {
                actionRoutine?.SetValue(player, null);
            }
        }

        // Confirm next to a save crystal uses it instead of jumping (SaveCrystals).
        [HarmonyPatch(typeof(PlayerControl), "DoJump")]
        [HarmonyPrefix]
        private static bool BeforeJump()
        {
            if (SaveCrystals.TryUse())
            {
                return false;
            }
            if (!Locked(Jump))
            {
                return true;
            }
            Refuse(Jump);
            return false;
        }
    }
}
