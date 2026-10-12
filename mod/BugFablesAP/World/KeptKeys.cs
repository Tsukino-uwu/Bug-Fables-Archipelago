using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // Key items never used up (slot_data kept_keys: the Factory Pass, the Prison Key): the lock routine (Event59) leaves
    // the shown key in the bag instead of its one removal, and fills the pump room's three-slot scanner at once. All
    // after it stays the game's: the lock's flag and scene. The bridge scene (Event193) empties the bag of Prison Keys
    // in its own loop and marks three Prison Key spots taken: with the key kept, the loop spares it and the spots stay.
    internal static class KeptKeys
    {
        // flagvar slot of the pump room's scanner: the keys shown so far, opening at 3.
        private const int ScannerCount = 24;
        private const int PrisonKey = 161;
        // The Prison Key spots the bridge scene marks taken: the Pier's Wasp Driller, Electric Cells' and the Gym's.
        private static readonly int[] PrisonKeySpots = { 584, 545, 551 };
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
            Hooks.Install(typeof(BridgeScene), "keys",
                "the bridge scene takes the seed's one Prison Key and marks three of its spots taken unchecked");
            Hooks.Install(typeof(KeyJoin), "keys",
                "both Sand Castle Key halves' spots done would give the key from the game, not the server");
        }

        // Event109 joins the Heaven and Earth Keys into the Sand Castle Key once both their spots' flags (301, 302)
        // are set; with the key an item, the read of 301 answers false, so the join never runs.
        private static class KeyJoin
        {
            [HarmonyPatch(typeof(EventControl), "Event109", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "keys");

            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                int[] reads = Enumerable.Range(0, code.Count - 1)
                    .Where(i => code[i].LoadsConstant(301) && code[i + 1].opcode == OpCodes.Ldelem_U1)
                    .ToArray();
                if (reads.Length != 1)
                {
                    throw new InvalidOperationException($"Event109 has {reads.Length} reads of flag 301 where 1 was "
                        + "measured");
                }
                code.Insert(reads[0] + 2, new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(KeptKeys), nameof(JoinAllowed))));
                log.LogInfo("[keys] installed in EventControl.Event109 (the Sand Castle Key's join)");
                return code;
            }
        }

        // The game's flags[301], unless the seed has the Sand Castle Key as an item.
        public static bool JoinAllowed(bool earthKeyDone)
        {
            SeedData data = randomizerOn != null && randomizerOn() ? seed?.Invoke() : null;
            if (data == null || !data.SandCastleKeyItem)
            {
                return earthKeyDone;
            }
            log?.LogInfo("[keys] the Heaven and Earth Keys' join kept away: the Sand Castle Key comes from the server");
            return false;
        }

        private static bool Kept(int item)
        {
            SeedData data = randomizerOn != null && randomizerOn() ? seed?.Invoke() : null;
            return data?.KeptKeys != null && data.KeptKeys.Contains(item);
        }

        private static class BridgeScene
        {
            [HarmonyPatch(typeof(EventControl), "Event193", MethodType.Enumerator)]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
                Hooks.Safe(instructions, Edit, "keys");

            // The sweep's `callvirt List<int>.Contains` on 161 becomes `call StillToSweep`, and the three key spots'
            // `stelem.i1` become `call SetFlag`: the same stack in and out.
            private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
            {
                MethodInfo contains = AccessTools.Method(typeof(List<int>), nameof(List<int>.Contains));
                int[] sweeps = Enumerable.Range(1, code.Count - 1)
                    .Where(i => code[i].Calls(contains) && code[i - 1].LoadsConstant(PrisonKey)).ToArray();
                int[] spots = Enumerable.Range(2, code.Count - 2)
                    .Where(i => code[i].opcode == OpCodes.Stelem_I1 && code[i - 1].LoadsConstant(1)
                        && PrisonKeySpots.Any(flag => code[i - 2].LoadsConstant(flag))).ToArray();
                if (sweeps.Length != 1 || spots.Length != PrisonKeySpots.Length)
                {
                    throw new InvalidOperationException($"Event193 has {sweeps.Length} Prison Key sweeps and "
                        + $"{spots.Length} key spot flags where 1 and {PrisonKeySpots.Length} were measured");
                }
                code[sweeps[0]].opcode = OpCodes.Call;
                code[sweeps[0]].operand = AccessTools.Method(typeof(KeptKeys), nameof(StillToSweep));
                foreach (int i in spots)
                {
                    code[i].opcode = OpCodes.Call;
                    code[i].operand = AccessTools.Method(typeof(KeptKeys), nameof(SetFlag));
                }
                log.LogInfo($"[keys] installed in EventControl.Event193 (the sweep at {sweeps[0]}, the key spots at "
                    + string.Join(", ", spots.Select(i => i.ToString()).ToArray()) + ")");
                return code;
            }
        }

        // The sweep's loop test: a kept key ends it at once, left in the bag.
        public static bool StillToSweep(List<int> bag, int item)
        {
            if (!bag.Contains(item))
            {
                return false;
            }
            if (Kept(item))
            {
                log?.LogInfo($"[keys] the bridge scene's sweep leaves key item {item} in the bag (this seed's kept_keys)");
                return false;
            }
            return true;
        }

        // The game's flags[index] = value, unless the Prison Key is kept: then its spots stay until taken.
        public static void SetFlag(bool[] flags, int index, bool value)
        {
            if (Kept(PrisonKey))
            {
                log?.LogInfo($"[keys] the bridge scene leaves flag {index} as it is ({flags[index]}): a Prison Key "
                    + "spot stays until taken");
                return;
            }
            flags[index] = value;
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
                // The pump room's scanner counts a key per slot and opens at the third: one kept key fills all three,
                // the count set to two before the game's own increment.
                if (MainManager.map != null && MainManager.map.mapid == MainManager.Maps.FactoryProcessingPump)
                {
                    int[] vars = MainManager.instance.flagvar;
                    int before = vars[ScannerCount];
                    vars[ScannerCount] = Math.Max(before, 2);
                    log?.LogInfo($"[keys] {map}: the scanner's count {before} raised to {vars[ScannerCount]}, the "
                        + "game's own increment makes the third slot");
                }
                return held;
            }
            string why = !on ? "Archipelago off" : data == null ? "no seed"
                : data.KeptKeys == null ? "a seed without kept_keys" : "not a kept key";
            log?.LogInfo($"[keys] {map}: key item {item} taken by the lock ({why})");
            return bag.Remove(item);
        }
    }
}
