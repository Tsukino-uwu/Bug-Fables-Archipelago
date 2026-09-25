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
        private static Harmony harmony;
        private static ManualLogSource log;
        private static readonly HashSet<string> reported = new HashSet<string>();

        private static readonly MethodInfo CrossFade = AccessTools.Method(typeof(Animator), nameof(Animator.CrossFadeInFixedTime),
            new[] { typeof(string), typeof(float) });

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            var setAnim = AccessTools.Method(typeof(EntityControl), nameof(EntityControl.SetAnim), new[] { typeof(string), typeof(bool) });
            if (setAnim == null || CrossFade == null)
            {
                log.LogError($"[anim] NOT installed (SetAnim {setAnim != null}, CrossFadeInFixedTime {CrossFade != null}); "
                    + "missing animation states keep warning.");
                return;
            }
            harmony = new Harmony(guid + ".anim." + DateTime.UtcNow.Ticks);
            harmony.Patch(setAnim, transpiler: new HarmonyMethod(typeof(AnimGuard), nameof(Transpile)));
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            int replaced = 0;
            foreach (CodeInstruction instruction in code.Where(i => i.Calls(CrossFade)))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(AnimGuard), nameof(Play));
                replaced++;
            }
            if (replaced == 2)
            {
                log.LogInfo("[anim] installed in EntityControl.SetAnim (both plays)");
            }
            else
            {
                log.LogWarning($"[anim] EntityControl.SetAnim has {replaced} plays, not 2; the guard covers those it found.");
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
            string controller = anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "none";
            if (reported.Add(controller + "/" + state))
            {
                log.LogInfo($"[anim] {anim.gameObject.name} ({controller}) has no state '{state}'; skipped (the game would warn and play nothing)");
            }
        }
    }
}
