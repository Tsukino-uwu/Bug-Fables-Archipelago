using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BugFablesAP
{
    // A line holding a server name, fitted to its box: one line when it fits, else two at the marked space, each
    // centred, and a line still too wide squashed sideways the way the game's own clamp does (|sizemulti,f,1|).
    internal static class TextFit
    {
        // The one space a line may break at. ServerText drops control characters, so no server string holds it.
        // A string, not a char: the release check can trace a string the compiler merges into the literals beside it.
        internal const string Break = "\u0001";
        private const string NameSlot = "|string,0|", ArticleSlot = "|string,1|";
        private const float Space = 0.3f;
        // The item-get box's text keeps this far in from each side of its sprite.
        private const float Margin = 0.6f;
        private const string Japanese =
            "\\p{IsHiragana}|\\p{IsKatakana}|\\p{IsKangxiRadicals}|\\p{IsCJKUnifiedIdeographs}";
        private const string Cyrillic = "\\p{IsCyrillic}";
        private const string Korean = "\\p{IsHangulJamo}|\\p{IsHangulSyllables}|\\p{IsHangulCompatibilityJamo}";

        internal static string Joined(string text) => text?.Replace(Break, " ");

        // The line with its article and name filled in and fitted to room; null leaves the game's line as it is.
        internal static string Fit(string line, string article, string name, float room, bool dialogue,
            out string told)
        {
            int at = line == null || name == null ? -1 : line.IndexOf(NameSlot);
            if (at < 0 || room <= 0f)
            {
                told = at < 0 ? "no name slot in the line, left as it is" : "no box to measure, left on one line";
                return null;
            }
            string before = line.Substring(0, at).Replace(ArticleSlot, article ?? "");
            string after = line.Substring(at + NameSlot.Length);
            string whole = before + Joined(name) + after;
            float width = Width(whole, dialogue);
            int cut = name.IndexOf(Break, StringComparison.Ordinal);
            if (width <= room)
            {
                told = $"{width:0.0} wide, room {room:0.0}: one line";
                return whole;
            }
            if (cut < 0)
            {
                told = $"{width:0.0} wide, room {room:0.0}: one line, squashed";
                return Squash(whole, width, room);
            }
            string first = before + name.Substring(0, cut), second = name.Substring(cut + Break.Length) + after;
            float firstWidth = Width(first, dialogue), secondWidth = Width(second, dialogue);
            float firstShown = Mathf.Min(firstWidth, room), secondShown = Mathf.Min(secondWidth, room);
            // The game centres the whole block by its widest line, so the shorter one is moved in by spaces.
            string pad = new string(' ', Mathf.RoundToInt(Mathf.Abs(firstShown - secondShown) / 2f / Space));
            told = $"{width:0.0} wide, room {room:0.0}: two lines {firstWidth:0.0} / {secondWidth:0.0}"
                + (firstWidth > room || secondWidth > room ? ", squashed" : "");
            return (firstShown < secondShown ? pad : "") + Squash(first, firstWidth, room) + "|line|"
                + (secondShown < firstShown ? pad : "") + Squash(second, secondWidth, room);
        }

        private static string Squash(string text, float width, float room) =>
            width <= room ? text
                : "|sizemulti," + (Mathf.Floor(room / width * 1000f) / 1000f).ToString("0.###",
                    CultureInfo.InvariantCulture) + ",1|" + text + "|size,1,1|";

        // The item-get's own box (Giveitem's "fauxmessage"), less a margin each side; 0 when it isn't there.
        internal static float HoldUpRoom()
        {
            Transform box = MainManager.GUICamera == null ? null : MainManager.GUICamera.transform.Find("fauxmessage");
            SpriteRenderer shown = box == null ? null : box.GetComponent<SpriteRenderer>();
            return shown == null || shown.sprite == null ? 0f
                : shown.sprite.bounds.size.x * box.localScale.x - 2f * Margin;
        }

        // As SetText lays letters out: the game's own advance per letter, 0.3 a space, nothing for a |command|; at a
        // Japanese, Russian or Korean letter the font changes and stays so.
        internal static float Width(string text, bool dialogue)
        {
            float width = 0f;
            int font = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '|')
                {
                    int end = text.IndexOf('|', i + 1);
                    i = end < 0 ? text.Length : end;
                }
                else if (c == ' ')
                {
                    width += Space;
                }
                else if (c != '\r')
                {
                    font = FontOf(c, font, dialogue);
                    width += MainManager.GetLetterOffset(c, font, 1f);
                }
            }
            return width;
        }

        private static int FontOf(char c, int font, bool dialogue)
        {
            int language = MainManager.languageid;
            bool asked = dialogue && !MainManager.instance.numberprompt;
            string letter = c.ToString();
            return (language == 3 && asked) || Regex.IsMatch(letter, Japanese) ? 3
                : (language == 6 && asked) || Regex.IsMatch(letter, Cyrillic) ? 4
                : (language == 5 && asked) || Regex.IsMatch(letter, Korean) ? 5
                : font;
        }
    }
}
