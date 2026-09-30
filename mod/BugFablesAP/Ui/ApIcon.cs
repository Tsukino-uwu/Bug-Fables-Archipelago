using UnityEngine;

namespace BugFablesAP
{
    // Archipelago's logo, six overlapping circles in its colours, drawn in code (no game or Archipelago art copied):
    // flat circles with the gaps between them and a rim round the flower in black, like the game's outlined item
    // sprites.
    internal static class ApIcon
    {
        // The logo's colours, in drawing order: each later circle sits on the ones before.
        private static readonly string[] Colors = { "C97682", "EEE391", "75C275", "767EBD", "CA94C2", "D9A07D" };
        // Where each sits, in degrees round the centre, the same order: top, upper left, upper right, lower left, lower
        // right, bottom.
        private static readonly float[] Angles = { 90f, 152f, 28f, 208f, 332f, 270f };
        // Shares of the icon's half width, from the logo: circle radius, distance from the centre (leaving the middle
        // open), the gap cut round a circle, and the outline round the whole flower.
        private const float Radius = 0.39f, Distance = 0.6f, Gap = 0.05f, Rim = 0.07f;
        private const int Size = 128, Samples = 3;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        // Sized like an item sprite, for a hold-up or a pickup.
        internal static Sprite Get() => Get(Rim, Color.black);

        // Dev (console `shelflook`): another outline, to compare.
        internal static Sprite Get(float rimShare, Color outline)
        {
            string key = rimShare + "/" + outline;
            if (sprites.TryGetValue(key, out Sprite made))
            {
                return made;
            }
            float half = Size / 2f, scale = half / (Radius + Distance + rimShare);
            var centres = new Vector2[Colors.Length];
            var fills = new Color[Colors.Length];
            for (int i = 0; i < Colors.Length; i++)
            {
                float a = Angles[i] * Mathf.Deg2Rad;
                centres[i] = new Vector2(half + Mathf.Cos(a) * Distance * scale,
                    half + Mathf.Sin(a) * Distance * scale);
                int rgb = System.Convert.ToInt32(Colors[i], 16);
                fills[i] = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
            }
            float r = Radius * scale, gap = Gap * scale, rim = rimShare * scale;
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // A few samples per pixel smooth every edge; summed premultiplied by coverage, then back to
                    // straight alpha.
                    Color sum = Color.clear;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            sum += At(new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples), centres, fills,
                                r, gap, rim, outline);
                        }
                    }
                    Color c = sum / (Samples * Samples);
                    pixels[y * Size + x] = c.a > 0f ? new Color(c.r / c.a, c.g / c.a, c.b / c.a, c.a) : Color.clear;
                }
            }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels);
            texture.Apply();
            Vector3 item = MainManager.itemsprites[0, 0].bounds.size;
            made = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f),
                Size / Mathf.Max(item.x, item.y));
            sprites[key] = made;
            return made;
        }

        // The topmost circle at p decides: its colour, or the outline inside its gap; past every circle, the rim.
        private static Color At(Vector2 p, Vector2[] centres, Color[] fills, float r, float gap, float rim,
            Color outline)
        {
            for (int i = centres.Length - 1; i >= 0; i--)
            {
                float d = Vector2.Distance(p, centres[i]);
                if (d <= r)
                {
                    return fills[i];
                }
                if (d <= r + gap)
                {
                    return outline;
                }
            }
            foreach (Vector2 centre in centres)
            {
                if (Vector2.Distance(p, centre) <= r + rim)
                {
                    return outline;
                }
            }
            return Color.clear;
        }
    }
}
