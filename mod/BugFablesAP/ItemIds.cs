namespace BugFablesAP
{
    // Archipelago item id = Base + game id + a per-kind offset; must match the apworld's item_id (data_tables.py).
    internal static class ItemIds
    {
        internal const long Base = 7_710_000;
        internal const int MedalOffset = 1_000;
        internal const int KeyItemKind = 1;
        internal const int MedalKind = 2;
        // Berries: game id is the amount.
        internal const int MoneyOffset = 2_000;
        internal const int MoneyKind = 3;
        // Crystal berries: a counter (flagvar[14]), one item, game id 0.
        internal const int CrystalOffset = 3_000;
        internal const int CrystalKind = 4;

        internal static int GameId(long itemId, int kind)
        {
            int offset = kind == MedalKind ? MedalOffset : kind == MoneyKind ? MoneyOffset : kind == CrystalKind ? CrystalOffset : 0;
            return (int)(itemId - Base - offset);
        }

        // The game's own giveitem sprite choice by amount.
        internal static UnityEngine.Sprite BerrySprite(int amount)
        {
            return MainManager.itemsprites[0, amount >= 20 ? 186 : amount < 5 ? 6 : 7];
        }
    }
}
