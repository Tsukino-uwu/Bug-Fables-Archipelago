using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BugFablesAP
{
    // Dev-only measurement: saves the game's GUI sprite sheets (Resources "Sprites/GUI/gui" and "gui2", which make up
    // MainManager.guisprites, MainManager.cs:3195-3197) as PNGs, with a table of each guisprites index, its name and
    // its rectangle on its sheet, to pick art for the mod's own UI (the Warp button's icon, 2026-09-25). The files go
    // to the BepInEx folder, never the repo: they're the game's art.
    internal static class SpriteDump
    {
        internal static bool TryRun(ManualLogSource log)
        {
            if (MainManager.guisprites == null || MainManager.guisprites.Length == 0)
            {
                return false;
            }
            var table = new StringBuilder("index\tname\tsheet\tx\ty\tw\th\n");
            var saved = new System.Collections.Generic.HashSet<Texture2D>();
            for (int i = 0; i < MainManager.guisprites.Length; i++)
            {
                Sprite s = MainManager.guisprites[i];
                if (s == null)
                {
                    continue;
                }
                Rect r = s.textureRect;
                table.Append(i).Append('\t').Append(s.name).Append('\t').Append(s.texture.name).Append('\t')
                    .Append((int)r.x).Append('\t').Append((int)r.y).Append('\t').Append((int)r.width).Append('\t').Append((int)r.height).Append('\n');
                if (saved.Add(s.texture))
                {
                    Save(s.texture, Path.Combine(Paths.BepInExRootPath, "bugfablesap-sheet-" + s.texture.name + ".png"));
                }
            }
            File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "bugfablesap-guisprites.tsv"), table.ToString());
            log.LogInfo($"[dump] {MainManager.guisprites.Length} GUI sprites on {saved.Count} sheets -> bugfablesap-guisprites.tsv and bugfablesap-sheet-*.png");
            return true;
        }

        // The sheets aren't readable from script, so they're drawn into a render texture and read back from there.
        private static void Save(Texture2D texture, string path)
        {
            RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.Destroy(copy);
        }
    }
}
