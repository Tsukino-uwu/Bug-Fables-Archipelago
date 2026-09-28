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
        private static ManualLogSource log;
        private static readonly HashSet<string> reported = new HashSet<string>();

        private static readonly MethodInfo GetColorByName = AccessTools.Method(typeof(Material), nameof(Material.GetColor), new[] { typeof(string) });

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            if (GetColorByName == null)
            {
                log.LogError("[glow] NOT installed (Material.GetColor not found); a light without a glow colour keeps logging an error.");
                return;
            }
            Hooks.Install(typeof(GlowGuard), "glow", "a light without a glow colour keeps logging an error");
        }

        [HarmonyPatch(typeof(GlowTrigger), "Start")]
        [HarmonyPatch(typeof(GlowTrigger), "LateUpdate")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, EditColourReads, "glow");

        private static IEnumerable<CodeInstruction> EditColourReads(List<CodeInstruction> code)
        {
            List<CodeInstruction> reads = code.Where(i => i.Calls(GetColorByName)).ToList();
            MethodInfo guarded = AccessTools.Method(typeof(GlowGuard), nameof(GetColor))
                ?? throw new MissingMethodException(nameof(GlowGuard), nameof(GetColor));
            foreach (CodeInstruction instruction in reads)
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = guarded;
            }
            log.LogInfo(reads.Count > 0 ? $"[glow] installed in a GlowTrigger method ({reads.Count} colour reads)"
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
