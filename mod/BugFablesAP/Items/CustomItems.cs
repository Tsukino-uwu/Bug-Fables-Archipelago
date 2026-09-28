using System;
using BepInEx.Logging;

namespace BugFablesAP
{
    // The mod's own items (custom gates): entries after the game's last item in its item table (string[1, 256, 7]) and
    // sprite table, each lending an existing sprite, with its own name and description. Re-applied whenever the game
    // reloads its table (a language change), and only while Archipelago is enabled.
    internal static class CustomItems
    {
        // The Boat Ticket: the Platinum Card's look, its own words.
        internal const int BoatTicket = 200;
        private const int TicketLooksLike = 176;
        private const string TicketName = "Boat Ticket";
        private const string TicketDescription = "A boat ticket. Maybe we should visit the pier.";

        // Field moves (Shuffle Field Moves / Shuffle Jump) as key items, so the bag shows which ones work: 201 Beemerang,
        // 202 Horn, 203 Ice (each its member's party icon), 204 Jump (the Archipelago icon, the whole party's).
        internal const int FirstMove = 201;
        internal static int MoveKeyItem(int move) => FirstMove + move;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
        }

        internal static void Tick()
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.itemdata == null || MainManager.itemsprites == null)
            {
                return;
            }
            AddMoves();
            if (MainManager.itemdata[0, BoatTicket, 0] == TicketName)
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

        private static void AddMoves()
        {
            bool current = true;
            for (int move = 0; move <= FieldMoves.Jump; move++)
            {
                current &= MainManager.itemdata[0, MoveKeyItem(move), 0] == FieldMoves.Name(move);
            }
            foreach (var ability in Abilities.Keys)
            {
                current &= MainManager.itemdata[0, ability.Key, 0] == Abilities.SkillText(ability.Skill, 0, ability.Name);
            }
            if (current || MainManager.instance?.charcolor == null)
            {
                return;
            }
            // The learned abilities (Abilities.cs), each with its member's icon.
            foreach (var ability in Abilities.Keys)
            {
                for (int field = 0; field < MainManager.itemdata.GetLength(2); field++)
                {
                    MainManager.itemdata[0, ability.Key, field] = MainManager.itemdata[0, TicketLooksLike, field];
                }
                // The game's own name and field description (what it does outside battle); its battle skill comes with it.
                MainManager.itemdata[0, ability.Key, 0] = Abilities.SkillText(ability.Skill, 0, ability.Name);
                MainManager.itemdata[0, ability.Key, 2] = Abilities.SkillText(ability.Skill, 1, PartyMembers.Name(ability.Member) + " can use " + ability.Name + ".");
                MainManager.itemsprites[0, ability.Key] = ItemSwap.MemberSprite(ability.Member);
            }
            for (int move = 0; move <= FieldMoves.Jump; move++)
            {
                int id = MoveKeyItem(move);
                for (int field = 0; field < MainManager.itemdata.GetLength(2); field++)
                {
                    MainManager.itemdata[0, id, field] = MainManager.itemdata[0, TicketLooksLike, field];
                }
                MainManager.itemdata[0, id, 0] = FieldMoves.Name(move);
                MainManager.itemdata[0, id, 2] = ItemSwap.MoveDescription(move);
                MainManager.itemsprites[0, id] = move == FieldMoves.Jump ? ApIcon.Get() : ItemSwap.MemberSprite(move);
            }
            log.LogInfo($"[items] the field moves and abilities added as key items {MoveKeyItem(0)}-{Abilities.Shield}");
        }
    }
}
