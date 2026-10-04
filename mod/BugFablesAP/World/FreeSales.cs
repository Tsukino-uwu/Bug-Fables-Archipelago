using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // A seller the seed makes free (slot_data free_sales): as their map's lines load, the price commands in the listed
    // lines (checkmoney, money) become 0, and so does the price where the lines write it ("150 berries").
    internal static class FreeSales
    {
        private static ManualLogSource log;
        private static Func<SeedData> seed;
        private static Func<bool> randomizerOn;
        private static readonly Regex CheckMoney = new Regex(@"\|checkmoney,(\d+),");
        private static readonly Regex Money = new Regex(@"\|money,-(\d+)\|");

        internal static void Enable(ManualLogSource logger, Func<SeedData> seedData, Func<bool> on)
        {
            log = logger;
            seed = seedData;
            randomizerOn = on;
            if (Hooks.Install(typeof(FreeSales), "sales", "free sellers still charge their price"))
            {
                log.LogInfo("[sales] installed on MapControl.CreateEntities");
            }
        }

        // The lines are read just before CreateEntities, so they're in place before anyone can talk.
        [HarmonyPatch(typeof(MapControl), "CreateEntities")]
        [HarmonyPrefix]
        private static void BeforeCreate(MapControl __instance)
        {
            Dictionary<string, int[]> sales = randomizerOn != null && randomizerOn() ? seed?.Invoke()?.FreeSales : null;
            string map = __instance.mapid.ToString();
            if (sales == null || !sales.TryGetValue(map, out int[] lines) || __instance.dialogues == null)
            {
                return;
            }
            int[] valid = lines.Where(l => l >= 0 && l < __instance.dialogues.Length).ToArray();
            var prices = new HashSet<string>(valid.SelectMany(l => CheckMoney.Matches(__instance.dialogues[l])
                .Cast<Match>().Concat(Money.Matches(__instance.dialogues[l]).Cast<Match>()))
                .Select(m => m.Groups[1].Value));
            foreach (int line in valid)
            {
                string text = CheckMoney.Replace(__instance.dialogues[line], "|checkmoney,0,");
                text = Money.Replace(text, "|money,-0|");
                foreach (string price in prices)
                {
                    text = Regex.Replace(text, @"\b" + price + @"(?= berr)", "0");
                }
                __instance.dialogues[line] = text;
            }
            log.LogInfo($"[sales] {map}: lines {string.Join(", ", valid.Select(l => l.ToString()).ToArray())} free "
                + $"(price {string.Join("/", prices.ToArray())} to 0)");
        }
    }
}
