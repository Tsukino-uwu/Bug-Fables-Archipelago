namespace BugFablesAP
{
    // How an Archipelago item id maps to the game's, the apworld's rule (data_tables.py, item_id): ITEM_ID_BASE plus
    // the game's id, plus MEDAL_ID_OFFSET for a medal, since medal ids (MainManager.BadgeTypes) overlap item ids
    // (MainManager.Items). The kind comes from slot_data's item_kinds.
    internal static class ItemIds
    {
        internal const long Base = 7_710_000;
        internal const int MedalOffset = 1_000;
        internal const int KeyItemKind = 1;
        internal const int MedalKind = 2;
        // Berries (money): handed out by the same giveitem as items, type -1; as an item, the game id is the amount.
        internal const int MoneyOffset = 2_000;
        internal const int MoneyKind = 3;
        // Crystal berries: a counted currency (flagvar[14], the crystal berry shop's counter); one item, game id 0.
        internal const int CrystalOffset = 3_000;
        internal const int CrystalKind = 4;

        internal static int GameId(long itemId, int kind)
        {
            int offset = kind == MedalKind ? MedalOffset : kind == MoneyKind ? MoneyOffset : kind == CrystalKind ? CrystalOffset : 0;
            return (int)(itemId - Base - offset);
        }

        // The berry sprite the game's own giveitem shows for an amount (MainManager.cs:11506).
        internal static UnityEngine.Sprite BerrySprite(int amount)
        {
            return MainManager.itemsprites[0, amount >= 20 ? 186 : amount < 5 ? 6 : 7];
        }
    }
}
