using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Shuffle Field Moves / Shuffle Jump: the leader's field attack (0 Beemerang, 1 Horn, 2 Ice) and the jump wait for
    // their item; a press before then plays the game's own buzzer. Only while Archipelago is on and the seed says so.
    internal static class FieldMoves
    {
        internal const int Jump = 3;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        // From slot_data's options (shuffle_field_moves, shuffle_jump); false with no seed.
        private static Func<SeedData> seed;
        internal static bool MovesShuffled => seed?.Invoke()?.MovesShuffled ?? false;
        internal static bool JumpShuffled => seed?.Invoke()?.JumpShuffled ?? false;

        internal static string Name(int id) => id == 0 ? "Beemerang Toss" : id == 1 ? "Horn Slash" : id == 2 ? "Freeze"
            : id == Jump ? "Jump" : "move " + id;

        internal static bool Locked(int id)
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.map == null)
            {
                return false;
            }
            bool shuffled = id == Jump ? JumpShuffled : MovesShuffled;
            // A move works once its key item (CustomItems) is in the bag, where the receiver puts it.
            return shuffled && MainManager.instance?.items != null
                && !MainManager.instance.items[1].Contains(CustomItems.MoveKeyItem(id));
        }

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            // DoActionTap only builds its coroutine, small enough to be inlined into its callers, where a patch never
            // runs (a prefix there never fired): the coroutine's own first step is gated instead.
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
            Hooks.Install(typeof(WormGame), "moves", "the Wacka Worm game starts without Vi or her Beemerang");
            Hooks.Install(typeof(HornLock), "moves",
                "once the Dash is learned, Kabbu's tap does all the Horn Slash does without its item");
        }

        // Without the Horn Slash the Dash only moves and the Horn Dash only breaks boulders: Kabbu's hitboxes carry no
        // horn tag and the first press swings nothing; a dashing Horn Dash's hitbox is the game's own only on a boulder.
        private static class HornLock
        {
            private static readonly AccessTools.FieldRef<PlayerControl, BoxCollider> hitbox =
                AccessTools.FieldRefAccess<PlayerControl, BoxCollider>("tbox");

            [HarmonyPatch(typeof(PlayerControl), "DoActionTap", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> TranspileTap(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, EditTap, "moves");

            // Each `ldstr "BeetleHorn"/"BeetleDash"; callvirt set_tag` gets HornTag between; Kabbu's swing (its
            // animstate 100 just before the "Cut" sound) gets SlashAnim, the sprite's turn after it becomes SlashTurn and
            // the sound's PlaySound SlashSound.
            private static IEnumerable<CodeInstruction> EditTap(List<CodeInstruction> code)
            {
                List<int> tags = Enumerable.Range(0, Math.Max(0, code.Count - 1))
                    .Where(i => code[i].opcode == OpCodes.Ldstr && (code[i].operand as string == "BeetleHorn"
                        || code[i].operand as string == "BeetleDash")
                        && code[i + 1].operand is MethodInfo setter && setter.Name == "set_tag")
                    .ToList();
                MethodInfo play = AccessTools.Method(typeof(MainManager), nameof(MainManager.PlaySound),
                    new[] { typeof(string), typeof(int), typeof(float), typeof(float) });
                int cut = code.FindIndex(c => c.opcode == OpCodes.Ldstr && c.operand as string == "Cut");
                int sound = cut < 0 ? -1 : code.FindIndex(cut, c => c.Calls(play));
                FieldInfo animstate = AccessTools.Field(typeof(EntityControl), nameof(EntityControl.animstate));
                int swing = cut < 0 ? -1 : code.FindLastIndex(cut, c => c.StoresField(animstate));
                bool swingIs100 = swing > 0 && code[swing - 1].opcode == OpCodes.Ldc_I4_S
                    && Convert.ToInt32(code[swing - 1].operand) == 100;
                MethodInfo setAngles = AccessTools.PropertySetter(typeof(Transform), nameof(Transform.localEulerAngles));
                int turn = swing < 0 ? -1 : code.FindIndex(swing, c => c.Calls(setAngles));
                bool turnBeforeSound = turn > swing && turn < cut;
                if (tags.Count != 3 || sound < 0 || sound - cut > 6 || !swingIs100 || !turnBeforeSound)
                {
                    log.LogError($"[moves] DoActionTap: {tags.Count} of 3 horn tags, the swing's sound {sound >= 0}, "
                        + $"its animation {swingIs100}, its turn {turnBeforeSound}, which differs from what was "
                        + "measured: left as it is, so once the Dash is learned, Kabbu's tap does all the Horn Slash "
                        + "does without its item");
                    return code;
                }
                MethodInfo slashSound = AccessTools.Method(typeof(FieldMoves), nameof(SlashSound));
                MethodInfo slashAnim = AccessTools.Method(typeof(FieldMoves), nameof(SlashAnim));
                MethodInfo slashTurn = AccessTools.Method(typeof(FieldMoves), nameof(SlashTurn));
                code[sound] = new CodeInstruction(OpCodes.Call, slashSound).MoveLabelsFrom(code[sound]);
                code[turn] = new CodeInstruction(OpCodes.Call, slashTurn).MoveLabelsFrom(code[turn]);
                code.Insert(swing, new CodeInstruction(OpCodes.Call, slashAnim));
                MethodInfo hornTag = AccessTools.Method(typeof(FieldMoves), nameof(HornTag));
                foreach (int i in tags.Select(i => i > swing ? i + 1 : i).OrderByDescending(i => i))
                {
                    code.Insert(i + 1, new CodeInstruction(OpCodes.Call, hornTag));
                }
                log.LogInfo("[moves] installed in DoActionTap: 3 of 3 horn tags, Kabbu's swing, its turn and its sound");
                return code;
            }

            // The game's own boulder branch runs as written (BreakRock, which keeps the Dash going), then the tag goes
            // back. A dashing hitbox is untagged only by HornTag, so a Horn Slash received mid-Dash still breaks it.
            [HarmonyPatch(typeof(NPCControl), "OnTriggerEnter")]
            [HarmonyPrefix]
            private static void BeforeTrigger(NPCControl __instance, Collider other, out bool __state)
            {
                __state = false;
                PlayerControl player = MainManager.player;
                if (__instance.objecttype != NPCControl.ObjectTypes.BreakableRock || other == null || player == null
                    || !player.dashing || !ReferenceEquals(other, hitbox(player)) || !other.CompareTag("Untagged")
                    || !Abilities.Learned(MainManager.instance.flags, 39))
                {
                    return;
                }
                other.tag = "BeetleDash";
                __state = true;
            }

            [HarmonyPatch(typeof(NPCControl), "OnTriggerEnter")]
            [HarmonyFinalizer]
            private static Exception AfterTrigger(Exception __exception, Collider other, bool __state)
            {
                if (__state && other != null)
                {
                    other.tag = "Untagged";
                }
                return __exception;
            }
        }

        // Called from DoActionTap's own code (HornLock): a hitbox's tag, Kabbu's swing, its turn and its sound, each
        // nothing while the Horn Slash is locked.
        public static string HornTag(string tag) => Locked(1) ? "Untagged" : tag;

        public static int SlashAnim(int anim) => Locked(1) ? 0 : anim;

        public static void SlashTurn(Transform sprite, Vector3 angles)
        {
            if (!Locked(1))
            {
                sprite.localEulerAngles = angles;
            }
        }

        public static AudioSource SlashSound(string clip, int id, float pitch, float volume) =>
            Locked(1) ? null : MainManager.PlaySound(clip, id, pitch, volume);

        // The Wacka Worm game (Event54, the festival's and Whack Farms') is Vi throwing the Beemerang: without her in
        // the party or the move, it doesn't start, and Whack Farms' fee, already paid by its line, is given back.
        private static class WormGame
        {
            private const int Event = 54;
            private const int Fee = 10;

            [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent), typeof(int), typeof(NPCControl))]
            [HarmonyPrefix]
            private static bool BeforeStartEvent(int id)
            {
                MainManager mm = MainManager.instance;
                if (id != Event || randomizerOn == null || !randomizerOn() || MainManager.map == null
                    || mm.playerdata == null)
                {
                    return true;
                }
                bool vi = mm.playerdata.Any(p => p.trueid == 0);
                bool beemerang = !Locked(0);
                if (vi && beemerang)
                {
                    return true;
                }
                bool farms = MainManager.map.mapid == MainManager.Maps.GoldenSMinigame;
                if (farms)
                {
                    mm.money = Mathf.Clamp(mm.money + Fee, 0, 999);
                }
                log.LogInfo($"[moves] Wacka Worm refused on {MainManager.map.mapid}: Vi {(vi ? "in" : "not in")} the party, "
                    + $"the Beemerang {(beemerang ? "usable" : "locked")}{(farms ? $"; the {Fee}-berry fee given back" : "")}");
                mm.StartCoroutine(Release());
                return false;
            }

            // A line's |event| leaves minipause and overridefollower for the scene to clear, and skips EndOfMessage:
            // with no scene, done here once the message closes.
            private static IEnumerator Release()
            {
                MainManager mm = MainManager.instance;
                while (mm.message)
                {
                    yield return null;
                }
                mm.minipause = false;
                mm.overridefollower = false;
                MainManager.EndOfMessage();
                log.LogInfo("[moves] Wacka Worm refused: the line's end done (minipause off)");
            }
        }

        private static readonly HashSet<string> reported = new HashSet<string>();

        // The game fires a tap on release and retries a held one every few frames, so the attack's buzz is played on
        // the press itself (Tick) and its refusals stay silent; the jump fires on the press and buzzes there.
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
            // A press during a tap (the Dash's second press) or a Dash (the one that ends it) isn't a new move.
            if (mm?.playerdata == null || mm.playerdata.Length == 0 || MainManager.player == null
                || MainManager.battle != null || mm.pause || mm.minipause || mm.inevent || mm.message
                || MainManager.player.submarine || MainManager.player.action || MainManager.player.dashing)
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

        // The tap's move is the leader's (playerdata[0].animid); the submarine's tap is its own and never locked. Only
        // the first step (state 0) is checked; a refused tap ends there, before it sets action or lockkeys.
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
            // Without the Horn Slash, Kabbu's double tap still starts the Dash once it's learned (HornLock).
            if (move == 1 && Abilities.Learned(mm.flags, 699))
            {
                return true;
            }
            Refuse(move);
            tapState.SetValue(__instance, -1);
            __result = false;
            player.StartCoroutine(ClearActionRoutine(player));
            return false;
        }

        // The game clears actionroutine only at a tap's end; the caller stores the refused one after this step, and the
        // hold path starts Kabbu's or Leif's tap only while it's null. Cleared a frame later, as a finished tap leaves
        // it.
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
