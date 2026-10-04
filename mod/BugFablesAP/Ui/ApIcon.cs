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
        private const int Size = 128, Samples = 3;
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> sprites =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        // A look, in shares of the icon's half width: the outline round the flower, the gap cut round a circle, the
        // outline round the open middle, the circles' radius and their distance from the centre.
        internal sealed class Look
        {
            internal readonly float Rim, Gap, Middle, Radius, Distance;

            internal Look(float rim, float gap, float middle, float radius, float distance)
            {
                Rim = rim;
                Gap = gap;
                Middle = middle;
                Radius = radius;
                Distance = distance;
            }

            public override string ToString() =>
                $"rim {Rim}, gap {Gap}, middle {Middle}, radius {Radius}, distance {Distance}";
        }

        // In use: outlines as heavy as the game's medals, the circles spread so the middle stays open. The first look,
        // thin-lined, is kept to go back to.
        internal static readonly Look Current = new Look(0.22f, 0.16f, 0.07f, 0.39f, 0.7f);
        internal static readonly Look First = new Look(0.07f, 0.05f, 0.07f, 0.39f, 0.6f);

        // Sized like an item sprite, for a hold-up or a pickup.
        internal static Sprite Get() => Get(Current, Color.black);

        // Dev (console `shelflook`): any look, to compare.
        internal static Sprite Get(Look look, Color outline)
        {
            float rimShare = look.Rim, gapShare = look.Gap, middleShare = look.Middle, radiusShare = look.Radius,
                distanceShare = look.Distance;
            string key = look + "/" + outline;
            if (sprites.TryGetValue(key, out Sprite made))
            {
                return made;
            }
            float half = Size / 2f, scale = half / (radiusShare + distanceShare + rimShare);
            var centres = new Vector2[Colors.Length];
            var fills = new Color[Colors.Length];
            for (int i = 0; i < Colors.Length; i++)
            {
                float a = Angles[i] * Mathf.Deg2Rad;
                centres[i] = new Vector2(half + Mathf.Cos(a) * distanceShare * scale,
                    half + Mathf.Sin(a) * distanceShare * scale);
                int rgb = System.Convert.ToInt32(Colors[i], 16);
                fills[i] = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
            }
            float r = radiusShare * scale, gap = gapShare * scale, rim = rimShare * scale, mid = middleShare * scale;
            float ring = distanceShare * scale;
            var middle = new Vector2(half, half);
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
                            var p = new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples);
                            // Inside the ring of centres is the open middle, outlined by its own share.
                            sum += At(p, centres, fills, r, gap,
                                Vector2.Distance(p, middle) < ring ? mid : rim, outline);
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
