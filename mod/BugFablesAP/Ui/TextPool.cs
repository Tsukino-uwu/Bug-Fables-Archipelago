using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BugFablesAP
{
    // Clears text drawn by MainManager.SetText. The game's own DestroyText frees the letters of its 500-letter pool
    // while stepping forward through the children it moves away, so every other letter stays taken until the frame
    // ends; a redraw in the same frame then runs the pool dry and letters go missing. This frees every one.
    internal static class TextPool
    {
        // A panel page on top of a screen that keeps its own text (Settings, hidden, and the main menu) needs more
        // than 500 letters.
        internal const int PanelLetters = 1000;
        private static readonly FieldInfo pool = AccessTools.Field(typeof(MainManager), "letterpool");

        // The game's GetEmptyLetter makes a letter (NewLetter) for any empty slot, so a longer array is filled by the
        // game itself, only as letters are needed.
        internal static void Reserve(ManualLogSource log)
        {
            TextMesh[] letters = pool?.GetValue(null) as TextMesh[];
            if (letters == null)
            {
                log?.LogError("[text] the game's letter pool wasn't found; a long panel page may lose its last letters");
                return;
            }
            if (letters.Length >= PanelLetters)
            {
                return;
            }
            var grown = new TextMesh[PanelLetters];
            System.Array.Copy(letters, grown, letters.Length);
            pool.SetValue(null, grown);
            log?.LogInfo($"[text] letter pool {letters.Length} -> {PanelLetters} slots (the game fills the new ones)");
        }

        internal static void Free(Transform parent)
        {
            if (parent == null)
            {
                return;
            }
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform holder = parent.GetChild(i);
                if (!holder.CompareTag("Text"))
                {
                    continue;
                }
                for (int j = holder.childCount - 1; j >= 0; j--)
                {
                    TextMesh letter = holder.GetChild(j).GetComponent<TextMesh>();
                    if (letter == null || !letter.CompareTag("Letter"))
                    {
                        continue;
                    }
                    letter.text = "";
                    FontEffects effects = letter.GetComponent<FontEffects>();
                    if (effects != null)
                    {
                        Object.Destroy(effects);
                    }
                    letter.transform.parent = MainManager.instance.transform;
                    letter.tag = "Untagged";
                }
                Object.Destroy(holder.gameObject);
            }
        }
    }
}
