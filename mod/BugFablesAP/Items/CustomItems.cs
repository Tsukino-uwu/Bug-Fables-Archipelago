using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

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

        // The submarine: the game's own name for it (the Termite King's), and a look lent until one is chosen on screen.
        internal const int Submarine = 212;
        private const int SubmarineLooksLike = 159;
        private const string SubmarineName = "Subaquatic Maritime Neotransport";
        private const string SubmarineDescription = "It is impossible for it to sink! ...Probably.";
        // The game draws a list's rows unfitted, and this name is wider than the Key Items list: while a list is
        // built it is narrowed as the game narrows a long-worded language's rows (0.7), back to full size after it,
        // since a description header ("name - Worth...") follows it.
        private const string NarrowSubmarine = "|sizemulti,0.7,1|" + SubmarineName + "|sizemulti,1.4286,1|";

        // The Progressive Boat is never in the bag: each copy gives the Boat Ticket, then the submarine (NextBoat). Its
        // row only names and draws it where the item is shown, as one look for both copies.
        internal const int ProgressiveBoat = 213;
        private const string ProgressiveName = "Progressive Boat";
        private const string ProgressiveDescription = "The Boat Ticket, then the Subaquatic Maritime Neotransport.";

        // The key item a Progressive Boat copy gives: the ticket first, then the submarine.
        internal static int NextBoat(List<int> bag) =>
            bag.Contains(BoatTicket) ? Submarine : BoatTicket;

        // Field moves (Shuffle Field Moves / Shuffle Jump) as key items, so the bag shows which ones work:
        // 201 Beemerang, 202 Horn, 203 Ice (each its member's party icon), 204 Jump (the Archipelago icon, the whole
        // party's).
        internal const int FirstMove = 201;
        internal static int MoveKeyItem(int move) => FirstMove + move;

        private static ManualLogSource log;
        private static Func<bool> randomizerOn;

        internal static void Enable(ManualLogSource logger, Func<bool> randomizerEnabled)
        {
            log = logger;
            randomizerOn = randomizerEnabled;
            if (Hooks.Install(typeof(ListHooks), "items", "the submarine's name runs past the Key Items list"))
            {
                log.LogInfo("[items] installed on MainManager.ShowItemList (the submarine's name narrowed in lists)");
            }
        }

        private static class ListHooks
        {
            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList))]
            [HarmonyPrefix]
            private static void BeforeList(out bool __state)
            {
                __state = MainManager.itemdata != null && MainManager.itemdata[0, Submarine, 0] == SubmarineName;
                if (__state)
                {
                    MainManager.itemdata[0, Submarine, 0] = NarrowSubmarine;
                }
            }

            [HarmonyPatch(typeof(MainManager), nameof(MainManager.ShowItemList))]
            [HarmonyFinalizer]
            private static Exception AfterList(Exception __exception, bool __state)
            {
                if (__state && MainManager.itemdata != null)
                {
                    MainManager.itemdata[0, Submarine, 0] = SubmarineName;
                }
                return __exception;
            }
        }

        internal static void Tick()
        {
            if (randomizerOn == null || !randomizerOn() || MainManager.itemdata == null
                || MainManager.itemsprites == null)
            {
                return;
            }
            AddMoves();
            Add(BoatTicket, TicketName, TicketDescription, TicketLooksLike);
            Add(Submarine, SubmarineName, SubmarineDescription, SubmarineLooksLike);
            Add(ProgressiveBoat, ProgressiveName, ProgressiveDescription, TicketLooksLike);
        }

        private static void Add(int id, string name, string description, int looksLike)
        {
            if (MainManager.itemdata[0, id, 0] == name)
            {
                return;
            }
            // Its fields as the card's (price, article, item data), then its own name and description (field 2 is the
            // one the menus show; field 1 holds "Desc" for key items, as the game's own do).
            for (int field = 0; field < MainManager.itemdata.GetLength(2); field++)
            {
                MainManager.itemdata[0, id, field] = MainManager.itemdata[0, TicketLooksLike, field];
            }
            MainManager.itemdata[0, id, 0] = name;
            MainManager.itemdata[0, id, 2] = description;
            MainManager.itemsprites[0, id] = MainManager.itemsprites[0, looksLike];
            log.LogInfo($"[items] the {name} added as item {id}, looking like item {looksLike}");
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
                current &= MainManager.itemdata[0, ability.Key, 0]
                    == Abilities.SkillText(ability.Skill, 0, ability.Name);
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
                // The game's own name and field description (what it does outside battle); its battle skill comes
                // with it.
                MainManager.itemdata[0, ability.Key, 0] = Abilities.SkillText(ability.Skill, 0, ability.Name);
                MainManager.itemdata[0, ability.Key, 2] = Abilities.SkillText(ability.Skill, 1,
                    PartyMembers.Name(ability.Member) + " can use " + ability.Name + ".");
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
            log.LogInfo(
                $"[items] the field moves and abilities added as key items {MoveKeyItem(0)}-{Abilities.Shield}");
        }
    }
}
