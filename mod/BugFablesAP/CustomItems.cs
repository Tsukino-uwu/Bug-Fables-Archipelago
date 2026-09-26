using System;
using BepInEx.Logging;

namespace BugFablesAP
{
    // The mod's own items (custom gates): entries after the game's last item in its item table (string[1, 256, 7]) and
    // sprite table, each lending an existing sprite, with its own name and description. Re-applied whenever the game
    // reloads its table (a language change), and only while Archipelago is enabled.
    internal static class CustomItems
    {
        // The Boat Ticket (Next 21): the Platinum Card's look, its own words.
        internal const int BoatTicket = 200;
        private const int TicketLooksLike = 176;
        private const string TicketName = "Boat Ticket";
        private const string TicketDescription = "A boat ticket. Maybe we should visit the pier.";

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
        }

        internal static void Tick()
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.itemdata == null || MainManager.itemsprites == null
                || MainManager.itemdata[0, BoatTicket, 0] == TicketName)
            {
                return;
            }
            // Its fields as the card's (price, article, item data), then its own name and description (field 2 is the one
            // the menus show; field 1 holds "Desc" for key items, as the game's own do).
            for (int field = 0; field < MainManager.itemdata.GetLength(2); field++)
            {
                MainManager.itemdata[0, BoatTicket, field] = MainManager.itemdata[0, TicketLooksLike, field];
            }
            MainManager.itemdata[0, BoatTicket, 0] = TicketName;
            MainManager.itemdata[0, BoatTicket, 2] = TicketDescription;
            MainManager.itemsprites[0, BoatTicket] = MainManager.itemsprites[0, TicketLooksLike];
            log.LogInfo($"[items] the {TicketName} added as item {BoatTicket}, looking like item {TicketLooksLike}");
        }
    }
}
