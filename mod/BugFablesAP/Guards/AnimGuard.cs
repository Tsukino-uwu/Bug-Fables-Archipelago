using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // A character asked for an animation state its controller lacks makes Unity warn twice ("State could not be
    // found", "Invalid Layer Index '-1'") and play nothing. SetAnim's plays check the state first, so nothing changes
    // on screen and the log stays clean; each missing state is logged once per controller.
    internal static class AnimGuard
    {
        private static Func<bool> randomizerOn;
        private static ManualLogSource log;
        private static readonly HashSet<string> reported = new HashSet<string>();

        private static readonly MethodInfo CrossFade = AccessTools.Method(typeof(Animator),
            nameof(Animator.CrossFadeInFixedTime),
            new[] { typeof(string), typeof(float) });

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            if (CrossFade == null)
            {
                log.LogError("[anim] NOT installed (Animator.CrossFadeInFixedTime not found); missing animation states keep warning.");
                return;
            }
            if (!Hooks.Install(typeof(AnimGuard), "anim", "missing animation states keep warning"))
            {
                return;
            }
            if (Hooks.Install(typeof(DirectPlays), "anim", "direct plays aren't guarded"))
            {
                log.LogInfo("[anim] installed on Animator.Play(string, int, float)");
            }
        }

        // False skips a play of a state the animator lacks on the asked layer (or on any, for -1).
        private static class DirectPlays
        {
            // The game's direct anim.Play("name") calls (battles, events, menus) all end in this overload.
            [HarmonyPatch(typeof(Animator), nameof(Animator.Play), typeof(string), typeof(int), typeof(float))]
            [HarmonyPrefix]
            private static bool BeforePlay(Animator __instance, string stateName, int layer)
            {
                if (randomizerOn == null || !randomizerOn() || __instance == null || __instance.layerCount == 0)
                {
                    return true;
                }
                int hash = Animator.StringToHash(stateName);
                bool found = layer >= 0 && layer < __instance.layerCount ? __instance.HasState(layer, hash)
                    : HasStateOnAnyLayer(__instance, hash);
                if (!found)
                {
                    Report(__instance, stateName);
                }
                return found;
            }
        }

        private static void Report(Animator anim, string state)
        {
            string controller = anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "none";
            if (reported.Add(controller + "/" + state))
            {
                log.LogInfo($"[anim] {anim.gameObject.name} ({controller}) has no state '{state}'; skipped (the game would warn and play nothing)");
            }
        }

        [HarmonyPatch(typeof(EntityControl), nameof(EntityControl.SetAnim), typeof(string), typeof(bool))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditSetAnim, "anim");

        private static IEnumerable<CodeInstruction> EditSetAnim(List<CodeInstruction> code)
        {
            List<CodeInstruction> plays = code.Where(i => i.Calls(CrossFade)).ToList();
            MethodInfo guarded = AccessTools.Method(typeof(AnimGuard), nameof(Play))
                ?? throw new MissingMethodException(nameof(AnimGuard), nameof(Play));
            foreach (CodeInstruction instruction in plays)
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = guarded;
            }
            if (plays.Count == 2)
            {
                log.LogInfo("[anim] installed in EntityControl.SetAnim (both plays)");
            }
            else
            {
                log.LogWarning(
                    $"[anim] EntityControl.SetAnim has {plays.Count} plays, not 2; the guard covers those it found.");
            }
            return code;
        }

        // The game passes no layer (-1, any), so a state on any layer counts.
        private static bool HasStateOnAnyLayer(Animator anim, int hash)
        {
            // An animator that isn't ready reports no layers: let the game's own call handle it, as before.
            if (anim.layerCount == 0)
            {
                return true;
            }
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                if (anim.HasState(layer, hash))
                {
                    return true;
                }
            }
            return false;
        }

        // Same stack as Animator.CrossFadeInFixedTime(string, float): the animator, the state, the duration.
        private static void Play(Animator anim, string state, float duration)
        {
            if (randomizerOn == null || !randomizerOn() || HasStateOnAnyLayer(anim, Animator.StringToHash(state)))
            {
                anim.CrossFadeInFixedTime(state, duration);
                return;
            }
            Report(anim, state);
        }
    }
}
