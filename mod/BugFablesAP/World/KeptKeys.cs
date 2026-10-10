using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Key items never used up (slot_data kept_keys: the Factory Pass): the lock routine (Event59) leaves the shown key
    // in the bag instead of its one removal. All after it stays the game's: the lock's flag and scene, the pump room's
    // scanner counting each show. Only in Event59: a global Remove patch would never end Event193's Prison Key sweep.
    internal static class KeptKeys
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            Hooks.Install(typeof(KeptKeys), "keys",
                "every lock takes its key: a seed's one Factory Pass is gone at the first lock");
        }

        [HarmonyPatch(typeof(EventControl), "Event59", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, Edit, "keys");

        // The shown key's removal, `callvirt List<int>.Remove`, becomes `call Take`: the same stack in and out.
        private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
        {
            MethodInfo remove = AccessTools.Method(typeof(List<int>), nameof(List<int>.Remove));
            int[] removals = Enumerable.Range(0, code.Count).Where(i => code[i].Calls(remove)).ToArray();
            if (removals.Length != 1)
            {
                throw new InvalidOperationException($"Event59 has {removals.Length} key removals where 1 was "
                    + "measured; every lock takes its key: a seed's one Factory Pass is gone at the first lock");
            }
            MethodInfo take = AccessTools.Method(typeof(KeptKeys), nameof(Take))
                ?? throw new MissingMethodException(nameof(KeptKeys), nameof(Take));
            code[removals[0]].opcode = OpCodes.Call;
            code[removals[0]].operand = take;
            log.LogInfo($"[keys] installed in EventControl.Event59 (instruction {removals[0]})");
            return code;
        }

        // The game's bag.Remove(item), unless this seed keeps the item: then the bag is left as it is.
        public static bool Take(List<int> bag, int item)
        {
            string map = MainManager.map != null ? MainManager.map.mapid.ToString() : "?";
            bool on = randomizerOn != null && randomizerOn();
            SeedData data = on ? seed?.Invoke() : null;
            if (data?.KeptKeys != null && data.KeptKeys.Contains(item))
            {
                bool held = bag.Contains(item);
                log?.LogInfo($"[keys] {map}: key item {item} kept in the bag (this seed's kept_keys); "
                    + $"in the bag: {held}");
                return held;
            }
            string why = !on ? "Archipelago off" : data == null ? "no seed"
                : data.KeptKeys == null ? "a seed without kept_keys" : "not a kept key";
            log?.LogInfo($"[keys] {map}: key item {item} taken by the lock ({why})");
            return bag.Remove(item);
        }
    }
}
