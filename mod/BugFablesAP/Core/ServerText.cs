using System.Globalization;
using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;

namespace BugFablesAP
{
    // Every string the server or another player's game decides passes through here before the game shows or saves it.
    // The game runs any |command| inside text it shows (flags, money, items, map changes), and a save splits on
    // "|SPLIT|", the not sign (U+00AC) and line breaks. Those are dropped, with control and format characters, and a
    // string is kept to MaxLength, so whatever a name holds can only ever be read.
    internal static class ServerText
    {
        internal const int MaxLength = 100;

        internal static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            var clean = new StringBuilder(System.Math.Min(text.Length, MaxLength));
            foreach (char c in text)
            {
                if (c == '|' || c == (char)0x00AC || char.IsControl(c)
                    || char.GetUnicodeCategory(c) == UnicodeCategory.Format)
                {
                    continue;
                }
                if (clean.Length == MaxLength)
                {
                    break;
                }
                clean.Append(c);
            }
            return clean.ToString();
        }

        internal static string ShownPlayer(this ItemInfo item) => Clean(item.Player?.Name);

        internal static string ShownItem(this ItemInfo item) => Clean(item.ItemDisplayName);

        internal static string ShownGame(this ItemInfo item) => Clean(item.ItemGame);

        internal static string ShownLocation(this ItemInfo item) => Clean(item.LocationDisplayName);

        internal static string SeedOf(ArchipelagoSession session) => Clean(session?.RoomState.Seed);
    }
}
