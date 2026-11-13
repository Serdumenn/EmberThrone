using UnityEngine;

namespace EmberThrone.Game
{
    public enum BoardTone
    {
        Navy,
        Dusk,
        Light,
    }

    public static class BoardSkin
    {
        public const int PixelsPerCell = 64;
        public const float Margin = 0.5f;
        const float Offset = 0.15f;
        const float FrameWidth = 0.11f;
        const float Round = 0.25f;
        const float ShadowDrop = 0.1f;
        const float ShadowSoft = 0.24f;
        const float SeamWidth = 0.016f;
        const float InnerShade = 0.22f;
        const int Big = 1 << 14;

        public static BoardTone Tone { get; set; } = BoardTone.Light;

        struct Theme
        {
            public Color CellA, CellB, Gap, Seam, Shade, Shadow;
        }

        static readonly Color FrameDark = Palette.Hex("7A400C");
        static readonly Color FrameMid = Palette.Hex("EBA834");
        static readonly Color FrameLight = Palette.Hex("FFF2BF");
        static readonly Vector3 LightDir = new Vector3(-0.35f, 0.62f, 0.7f).normalized;

        static Theme ThemeOf(BoardTone tone)
        {
            switch (tone)
            {
                case BoardTone.Navy:
                    return new Theme
                    {
                        CellA = Palette.Hex("34459A"), CellB = Palette.Hex("2F4092"), Gap = Palette.Hex("27367F"),
                        Seam = Palette.Hex("1F2C6C"), Shade = Palette.Hex("151E55"), Shadow = new Color(0.02f, 0.02f, 0.08f, 0.6f),
                    };
                case BoardTone.Dusk:
                    return new Theme
                    {
                        CellA = Palette.Hex("6670B6"), CellB = Palette.Hex("5F69AE"), Gap = Palette.Hex("545E9F"),
                        Seam = Palette.Hex("4A538F"), Shade = Palette.Hex("39407A"), Shadow = new Color(0.02f, 0.02f, 0.08f, 0.6f),
                    };
                default:
                    return new Theme
                    {
                        CellA = Palette.Hex("CDD0EE"), CellB = Palette.Hex("C4C8E8"), Gap = Palette.Hex("B9BEE0"),
                        Seam = Palette.Hex("A7ACD6"), Shade = Palette.Hex("8C93C4"), Shadow = new Color(0.03f, 0.02f, 0.1f, 0.62f),
                    };
            }
        }

        public static Texture2D Bake(bool[] exists, int w, int h, BoardTone tone)
        {
            var theme = ThemeOf(tone);
            int p = PixelsPerCell;
            int m = Mathf.CeilToInt(Margin * p);
            int tw = w * p + 2 * m;
            int th = h * p + 2 * m;
            int n = tw * th;

            var cell = new bool[n];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                    cell[y * tw + x] = Exists(exists, w, h, Floor(x - m, p), Floor(y - m, p));
            var toCells = Distance(cell, tw, th);

            float grow = (Offset + Round) * p;
            var outside = new bool[n];
            for (int i = 0; i < n; i++) outside[i] = toCells[i] > grow;
            var toOutside = Distance(outside, tw, th);

            var sd = new float[n];
            for (int i = 0; i < n; i++)
                sd[i] = (toCells[i] <= grow ? Round * p - toOutside[i] : Round * p + toCells[i] - grow) / p;

            int drop = Mathf.RoundToInt(ShadowDrop * p);
            var pixels = new Color32[n];
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int i = y * tw + x;
                    float s = sd[i];
                    float cast = sd[Mathf.Min(th - 1, y + drop) * tw + x];
                    var under = theme.Shadow;
                    under.a *= 1f - Smooth(0f, ShadowSoft, cast);

                    float cover = Mathf.Clamp01(0.5f - s * p);
                    if (cover <= 0f)
                    {
                        pixels[i] = under;
                        continue;
                    }

                    float inFrame = Mathf.Clamp01((s + FrameWidth) * p + 0.5f);
                    var top = inFrame >= 1f ? FrameColor(s, Normal(sd, tw, th, x, y))
                        : FillColor(theme, exists, w, h, (x - m + 0.5f) / p, (y - m + 0.5f) / p, s);
                    if (inFrame > 0f && inFrame < 1f)
                        top = Color.Lerp(FillColor(theme, exists, w, h, (x - m + 0.5f) / p, (y - m + 0.5f) / p, s),
                            FrameColor(s, Normal(sd, tw, th, x, y)), inFrame);
                    top.a = cover;
                    pixels[i] = Over(top, under);
                }
            }

            var tex = new Texture2D(tw, th, TextureFormat.RGBA32, true)
            {
                name = "BoardSkin",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };
            tex.SetPixels32(pixels);
            tex.Apply(true, true);
            return tex;
        }

        public static Sprite SpriteOf(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PixelsPerCell, 0, SpriteMeshType.FullRect);

        static Color FrameColor(float s, Vector2 normal)
        {
            float t = Mathf.Clamp01(-s / FrameWidth);
            float side = Mathf.Cos(t * Mathf.PI);
            var n = new Vector3(normal.x * side, normal.y * side, Mathf.Sin(t * Mathf.PI)).normalized;
            float light = Mathf.Clamp01(Vector3.Dot(n, LightDir));
            var c = Color.Lerp(FrameDark, FrameMid, Smooth(0.05f, 0.7f, light));
            c = Color.Lerp(c, FrameLight, Smooth(0.78f, 1f, light));
            float shine = Mathf.Pow(light, 24f) * 0.45f;
            c += new Color(shine, shine, shine, 0f);
            float edge = Mathf.Min(t, 1f - t);
            c *= Mathf.Lerp(0.7f, 1f, Smooth(0f, 0.16f, edge));
            c.a = 1f;
            return c;
        }

        static Color FillColor(Theme theme, bool[] exists, int w, int h, float fx, float fy, float s)
        {
            int cx = Mathf.FloorToInt(fx);
            int cy = Mathf.FloorToInt(fy);
            Color c;
            if (Exists(exists, w, h, cx, cy))
            {
                c = ((cx + cy) & 1) == 0 ? theme.CellA : theme.CellB;
                float lx = fx - cx, ly = fy - cy;
                float seam = 1f;
                if (Exists(exists, w, h, cx - 1, cy)) seam = Mathf.Min(seam, lx);
                if (Exists(exists, w, h, cx + 1, cy)) seam = Mathf.Min(seam, 1f - lx);
                if (Exists(exists, w, h, cx, cy - 1)) seam = Mathf.Min(seam, ly);
                if (Exists(exists, w, h, cx, cy + 1)) seam = Mathf.Min(seam, 1f - ly);
                c = Color.Lerp(c, theme.Seam, (1f - Smooth(SeamWidth * 0.5f, SeamWidth * 1.5f, seam)) * 0.6f);
                c *= 1f + 0.035f * (ly - 0.5f);
            }
            else
            {
                c = theme.Gap;
            }
            float near = 1f - Smooth(0f, InnerShade, -s - FrameWidth);
            c = Color.Lerp(c, theme.Shade, near * 0.55f);
            c.a = 1f;
            return c;
        }

        static Vector2 Normal(float[] sd, int w, int h, int x, int y)
        {
            float dx = sd[y * w + Mathf.Min(w - 1, x + 1)] - sd[y * w + Mathf.Max(0, x - 1)];
            float dy = sd[Mathf.Min(h - 1, y + 1) * w + x] - sd[Mathf.Max(0, y - 1) * w + x];
            var g = new Vector2(dx, dy);
            return g.sqrMagnitude > 1e-12f ? g.normalized : Vector2.up;
        }

        static Color32 Over(Color top, Color under)
        {
            float a = top.a + under.a * (1f - top.a);
            if (a <= 0f) return new Color32(0, 0, 0, 0);
            float k = under.a * (1f - top.a);
            var c = new Color(
                (top.r * top.a + under.r * k) / a,
                (top.g * top.a + under.g * k) / a,
                (top.b * top.a + under.b * k) / a,
                a);
            return c;
        }

        static float[] Distance(bool[] zero, int w, int h)
        {
            int n = w * h;
            var ox = new int[n];
            var oy = new int[n];
            for (int i = 0; i < n; i++)
            {
                ox[i] = zero[i] ? 0 : Big;
                oy[i] = zero[i] ? 0 : Big;
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Pull(ox, oy, w, h, x, y, -1, 0);
                    Pull(ox, oy, w, h, x, y, 0, -1);
                    Pull(ox, oy, w, h, x, y, -1, -1);
                    Pull(ox, oy, w, h, x, y, 1, -1);
                }
                for (int x = w - 1; x >= 0; x--) Pull(ox, oy, w, h, x, y, 1, 0);
            }
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = w - 1; x >= 0; x--)
                {
                    Pull(ox, oy, w, h, x, y, 1, 0);
                    Pull(ox, oy, w, h, x, y, 0, 1);
                    Pull(ox, oy, w, h, x, y, 1, 1);
                    Pull(ox, oy, w, h, x, y, -1, 1);
                }
                for (int x = 0; x < w; x++) Pull(ox, oy, w, h, x, y, -1, 0);
            }

            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Sqrt((float)ox[i] * ox[i] + (float)oy[i] * oy[i]);
            return d;
        }

        static void Pull(int[] ox, int[] oy, int w, int h, int x, int y, int dx, int dy)
        {
            int qx = x + dx, qy = y + dy;
            if (qx < 0 || qy < 0 || qx >= w || qy >= h) return;
            int i = y * w + x, q = qy * w + qx;
            if (ox[q] >= Big) return;
            int cx = ox[q] + dx, cy = oy[q] + dy;
            long now = (long)ox[i] * ox[i] + (long)oy[i] * oy[i];
            long cand = (long)cx * cx + (long)cy * cy;
            if (cand < now)
            {
                ox[i] = cx;
                oy[i] = cy;
            }
        }

        static bool Exists(bool[] exists, int w, int h, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && exists[y * w + x];

        static int Floor(int v, int p) => v >= 0 ? v / p : -1;

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }
    }
}
