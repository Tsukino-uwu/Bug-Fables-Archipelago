using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The ant tunnels' miners (Event48, one scene for every far end) dig for free in a seed that says so: each area's
    // price, which the scene stores in flagvar[0] and both shows and charges from there, reads as 0.
    internal static class AntTunnels
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        private static int prices;

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (Hooks.Install(typeof(AntTunnels), "tunnels", "the miners still charge their price"))
            {
                log.LogInfo($"[tunnels] installed in EventControl.Event48 ({prices} of 6 prices)");
            }
        }

        private static int Price(int price)
        {
            bool free = randomizerOn != null && randomizerOn() && seed?.Invoke()?.FreeAntTunnels == true;
            log?.LogInfo($"[tunnels] the miner's price {price}: {(free ? "free in this seed" : "charged")}");
            return free ? 0 : price;
        }

        [HarmonyPatch(typeof(EventControl), "Event48", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions) =>
            Hooks.Safe(instructions, Edit, "tunnels");

        // flagvar[0] = price: ldfld flagvar, ldc.i4.0, the price, stelem.i4; the price goes through Price first.
        private static IEnumerable<CodeInstruction> Edit(List<CodeInstruction> code)
        {
            FieldInfo flagvar = AccessTools.Field(typeof(MainManager), nameof(MainManager.flagvar));
            List<int> stores = Enumerable.Range(3, Math.Max(code.Count - 3, 0))
                .Where(i => code[i].opcode == OpCodes.Stelem_I4 && code[i - 2].LoadsConstant(0)
                    && code[i - 3].LoadsField(flagvar) && code[i - 1].LoadsConstant())
                .ToList();
            MethodInfo price = AccessTools.Method(typeof(AntTunnels), nameof(Price));
            for (int k = stores.Count - 1; k >= 0; k--)
            {
                code.Insert(stores[k], new CodeInstruction(OpCodes.Call, price));
            }
            prices = stores.Count;
            return code;
        }
    }
}
