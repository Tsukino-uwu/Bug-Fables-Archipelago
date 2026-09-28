namespace BugFablesAP
{
    // The game's own save slots the mod reads or writes, by what the game uses them for (MEASURED.md).
    internal static class GameFlags
    {
        internal const int TrapdoorFall = 14;
        // Set as the Explorer Permit's event (Event16), the opening, ends.
        internal const int PermitEvent = 15;
        internal const int LeifJoined = 16;
        // Leif follows after the spider scene, not yet in the party.
        internal const int LeifFollows = 27;
        internal const int FirstBossBeaten = 41;
        // The rematch machine's hard option, inside a hologram fight (flag 162).
        internal const int HardRematch = 166;
        internal const int NoExp = 613;
        // Set on every new game.
        internal const int NewGame = 691;
    }

    internal static class GameVars
    {
        internal const int ShopPrice = 1;
        // The crystal berry shop's currency.
        internal const int CrystalBerries = 14;
    }

    internal static class GameStrings
    {
        // Read by the "You got" box.
        internal const int ItemName = 0;
        internal const int ItemArticle = 1;
    }
}
