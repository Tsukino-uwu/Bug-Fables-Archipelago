using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // The Spy Cards tournament (Event210) opens the deck builder, which offers only cards of enemies spied on and
    // throws on an empty boss list, freezing the party. In the game nobody reaches it without a deck; a seed opens the
    // Auditorium from the start, so without one the judge says so instead of offering it (the fee handed back if the
    // tournament still starts).
    internal static class CardDeckGuard
    {
        private const int Tournament = 210;
        private const int EntryFee = 25;
        private const string NoDeck = "Come back when you have a deck worth playing.";

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;
        // Data/CardData rows as LoadCardData reads them: field 2 the enemy, field 4 the type (2 mini-boss, 3 boss).
        private static List<KeyValuePair<int, int>> cards;

        internal static void Enable(ManualLogSource logger, Func<bool> on)
        {
            log = logger;
            randomizerOn = on;
            Hooks.Install(typeof(Start), "cards", "the Spy Cards tournament without a deck freezes the party");
            Hooks.Install(typeof(Judge), "cards", "the judge offers the tournament without a deck");
        }

        // A deck takes a boss card, two mini-boss cards and the rest from the cards spied on (librarystuff[1, enemy]).
        internal static bool CanBuildDeck(out string why)
        {
            if (cards == null)
            {
                cards = new List<KeyValuePair<int, int>>();
                foreach (string row in Resources.Load<TextAsset>("Data/CardData").ToString().Split('\n'))
                {
                    string[] fields = row.Split(',');
                    if (fields.Length > 4)
                    {
                        cards.Add(new KeyValuePair<int, int>(Convert.ToInt32(fields[2]), Convert.ToInt32(fields[4])));
                    }
                }
            }
            int boss = 0, miniboss = 0, other = 0;
            foreach (KeyValuePair<int, int> card in cards)
            {
                if (!MainManager.instance.librarystuff[1, card.Key])
                {
                    continue;
                }
                if (card.Value == 3)
                {
                    boss++;
                }
                else if (card.Value == 2)
                {
                    miniboss++;
                }
                else
                {
                    other++;
                }
            }
            why = $"{boss} boss, {miniboss} mini-boss and {other} other cards spied";
            return boss > 0 && miniboss > 1 && other > 0;
        }

        [HarmonyPatch(typeof(EventControl), nameof(EventControl.StartEvent))]
        private static class Start
        {
            [HarmonyPrefix]
            private static bool Before(int id)
            {
                if (id != Tournament || randomizerOn == null || !randomizerOn()
                    || MainManager.CurrentMap() != MainManager.Maps.MetalIslandAuditorium)
                {
                    return true;
                }
                if (CanBuildDeck(out string why))
                {
                    log.LogInfo($"[cards] the tournament starts: {why}");
                    return true;
                }
                MainManager mm = MainManager.instance;
                mm.money = Mathf.Clamp(mm.money + EntryFee, 0, 999);
                mm.showmoney = 1f;
                mm.StartCoroutine(MainManager.SetText(NoDeck, dialogue: true, Vector3.zero, null, null));
                log.LogInfo($"[cards] the tournament refused, the fee handed back: {why}");
                return false;
            }
        }

        // The judge's question (line 5, then the fee and Event210 on yes) replaced by the no-deck line.
        [HarmonyPatch(typeof(NPCControl), nameof(NPCControl.GetDialogue))]
        private static class Judge
        {
            [HarmonyPostfix]
            private static void After(NPCControl __instance, ref string __result)
            {
                if (__instance.name != "judge" || randomizerOn == null || !randomizerOn()
                    || MainManager.CurrentMap() != MainManager.Maps.MetalIslandAuditorium
                    || CanBuildDeck(out string why))
                {
                    return;
                }
                __result = NoDeck;
                log.LogInfo($"[cards] the judge holds the tournament back: {why}");
            }
        }
    }
}
