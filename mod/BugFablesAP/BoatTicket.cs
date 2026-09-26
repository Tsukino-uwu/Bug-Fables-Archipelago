using System;
using BepInEx.Logging;
using HarmonyLib;

namespace BugFablesAP
{
    // The pier sailor sails to Metal Island only for the Boat Ticket (the mod's own key item, CustomItems.cs); the trip
    // is free and the ticket stays. His lines as the user approved them; he checks the ticket where the game checked the
    // fare (lines 16 and 19), and a missing ticket gets his old "no money" reply. Every line of his, the first included,
    // comes through GetDialogueText.
    internal static class BoatTicket
    {
        private const string PierMap = "BugariaPier";
        private const string Offer = "Hm. You look disappointingly poor. But I'll ask out of decency...|next|"
            + "Would you fancy traveling to |color,1|Metal Island|color,0|? Show me your ticket.|goto,15,keep|";

        private static Func<bool> randomizerOn;
        private static Harmony harmony;
        private static ManualLogSource log;

        internal static void Enable(ManualLogSource logger, string guid, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            var getLine = AccessTools.Method(typeof(MainManager), nameof(MainManager.GetDialogueText), new[] { typeof(int) });
            if (getLine == null)
            {
                log.LogError("[boat] NOT installed: MainManager.GetDialogueText(int) wasn't found; the sailor asks for berries.");
                return;
            }
            harmony = new Harmony(guid + ".boat." + DateTime.UtcNow.Ticks);
            harmony.Patch(getLine, postfix: new HarmonyMethod(typeof(BoatTicket), nameof(AfterGetLine)));
            log.LogInfo("[boat] installed on MainManager.GetDialogueText");
        }

        internal static void Disable()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }

        private static bool HasTicket() => MainManager.instance?.items != null && MainManager.instance.items[1].Contains(CustomItems.BoatTicket);

        private static void AfterGetLine(int id, ref string __result)
        {
            if (__result == null || MainManager.map == null || MainManager.map.mapid.ToString() != PierMap
                || randomizerOn == null || !randomizerOn())
            {
                return;
            }
            bool ticket = HasTicket();
            string line;
            switch (id)
            {
                case 3:
                case 18:
                    // The first talk, and the card Masters' discount: the same offer, no fare.
                    line = Offer;
                    break;
                case 15:
                    line = "|prompt,map,3,2,16,17,@Let's go!,@" + (ticket ? "Not yet!" : "I lost my ticket!") + "|";
                    break;
                case 16:
                case 19:
                    line = ticket ? "|goto,21|" : "|goto,20|";
                    log.LogInfo(ticket ? "[boat] the ticket shown: sailing" : "[boat] no ticket: the sailor refuses");
                    break;
                case 20:
                    line = "What?! No ticket, no trip! Get out of here!";
                    break;
                case 21:
                    line = "...Ticket's in order. Hop on! Our destination: |color,1|Metal Island|color,0|!|break||flag,419,true||event,107|";
                    break;
                default:
                    return;
            }
            __result = line;
        }
    }
}
