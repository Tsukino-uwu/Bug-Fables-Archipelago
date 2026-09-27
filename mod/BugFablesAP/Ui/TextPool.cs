using UnityEngine;

namespace BugFablesAP
{
    // Clears text drawn by MainManager.SetText. The game's own DestroyText frees the letters of its 500-letter pool
    // while stepping forward through the children it moves away, so every other letter stays taken until the frame
    // ends; a redraw in the same frame then runs the pool dry and letters go missing. This frees every one.
    internal static class TextPool
    {
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
