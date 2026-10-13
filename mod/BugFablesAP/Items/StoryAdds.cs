using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // A key item a scene puts straight into the bag (items[1].Add, no giveitem): left out when the seed has that scene
    // as a location (slot_data location_added), its check sent by the scene's flag like any other.
    internal static class StoryAdds
    {
        // The Ancient Castle's Watcher (the Peculiar Gem).
        private static readonly string[] Scenes = { "Event117" };

        private static ManualLogSource log;
        private static Func<Dictionary<long, int[]>> seedAdded;

        internal static void Enable(ManualLogSource logger, Func<Dictionary<long, int[]>> added)
        {
            log = logger;
            seedAdded = added;
            Hooks.Install(typeof(Adds), "adds",
                "a scene that adds a key item straight to the bag would give it from the game, not the server");
        }

        private static class Adds
        {
            private static IEnumerable<MethodBase> TargetMethods() => Scenes.Select(scene =>
                (MethodBase)AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(EventControl), scene)
                    ?? throw new MissingMethodException(nameof(EventControl), scene)));

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "adds");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo add = AccessTools.Method(typeof(List<int>), nameof(List<int>.Add));
                MethodInfo keyAdd = AccessTools.Method(typeof(StoryAdds), nameof(KeyAdd))
                    ?? throw new MissingMethodException(nameof(StoryAdds), nameof(KeyAdd));
                // items[1].Add: the list loaded by ldc.i4.1 then ldelem.ref, one constant before the call.
                int[] at = Enumerable.Range(3, Math.Max(0, code.Count - 3))
                    .Where(i => code[i].Calls(add) && code[i - 2].opcode == OpCodes.Ldelem_Ref
                        && code[i - 3].opcode == OpCodes.Ldc_I4_1)
                    .ToArray();
                if (at.Length != 1)
                {
                    throw new InvalidOperationException($"{at.Length} key item adds where 1 was measured");
                }
                // Same stack shape (list, item); labels stay on the instruction.
                code[at[0]].opcode = OpCodes.Call;
                code[at[0]].operand = keyAdd;
                log.LogInfo("[adds] installed: a scene's key item add");
                return code;
            }
        }

        private static void KeyAdd(List<int> keyItems, int item)
        {
            Dictionary<long, int[]> added = seedAdded?.Invoke();
            if (added != null && added.Values.Any(a => a[0] == 1 && a[1] == item))
            {
                log.LogInfo($"[adds] key item {item} is a location's in this seed: left out of the bag");
                return;
            }
            keyItems.Add(item);
        }
    }
}
