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
    // GlowTrigger (Start and LateUpdate) reads its material's "_Emission" colour without checking it has one; a map light
    // whose material lacks it makes Unity log an error. The colour is only ever written back to that same missing property, so a
    // material without it gets black instead, silently; each one is logged once.
    internal static class GlowGuard
    {
        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static ManualLogSource log;
        private static readonly HashSet<string> reported = new HashSet<string>();

        private static readonly MethodInfo GetColorByName = AccessTools.Method(typeof(Material), nameof(Material.GetColor), new[] { typeof(string) });

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            var start = AccessTools.Method(typeof(GlowTrigger), "Start");
            var late = AccessTools.Method(typeof(GlowTrigger), "LateUpdate");
            if (start == null || late == null || GetColorByName == null)
            {
                log.LogError($"[glow] NOT installed (GlowTrigger.Start {start != null}, LateUpdate {late != null}, "
                    + $"Material.GetColor {GetColorByName != null}); a light without a glow colour keeps logging an error.");
                return;
            }
            harmony = new Harmony(guid + ".glow." + DateTime.UtcNow.Ticks);
            harmony.Patch(start, transpiler: new HarmonyMethod(typeof(GlowGuard), nameof(Transpile)));
            harmony.Patch(late, transpiler: new HarmonyMethod(typeof(GlowGuard), nameof(Transpile)));
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
            foreach (CodeInstruction instruction in code.Where(i => i.Calls(GetColorByName)))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(GlowGuard), nameof(GetColor));
                replaced++;
            }
            log.LogInfo(replaced > 0 ? $"[glow] installed in a GlowTrigger method ({replaced} colour reads)"
                : "[glow] a GlowTrigger method reads no colour by name; nothing guarded there");
            return code;
        }

        // Same stack as Material.GetColor(string): the material, the property name.
        private static Color GetColor(Material material, string name)
        {
            if (randomizerOn == null || !randomizerOn() || material == null || material.HasProperty(name))
            {
                return material.GetColor(name);
            }
            if (reported.Add(material.name + "/" + name))
            {
                log.LogInfo($"[glow] {MainManager.map?.mapid}: material {material.name} has no '{name}'; read as black "
                    + "(the game would log an error)");
            }
            return Color.black;
        }
    }
}
