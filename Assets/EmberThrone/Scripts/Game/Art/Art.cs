using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class Palette
    {
        public static readonly Color Background = Hex("1A1220");
        public static readonly Color BoardFrame = Hex("120C17");
        public static readonly Color CellLight = Hex("3B2C42");
        public static readonly Color CellDark = Hex("322539");
        public static readonly Color Panel = Hex("2B2033");
        public static readonly Color PanelLight = Hex("443350");
        public static readonly Color Accent = Hex("F2842B");
        public static readonly Color Good = Hex("4CC96A");
        public static readonly Color Text = Hex("FFF6EA");
        public static readonly Color TextDim = Hex("BBA9C4");
        public static readonly Color Warning = Hex("FFB347");
        public static readonly Color Danger = Hex("FF5A4D");
        public static readonly Color Dim = new Color(0.05f, 0.03f, 0.07f, 0.72f);
        public static readonly Color Gold = Hex("FFD24A");
        public static readonly Color StarEmpty = Hex("4A3B52");

        // Index = GemColor. Every color also has its own shape (UX-001 ACC-004: never color alone).
        static readonly Color[] Gems =
        {
            Hex("E8463A"), Hex("45C05A"), Hex("3B8EF0"), Hex("F7C531"), Hex("A45CE6"), Hex("F2842B"),
        };

        public static Color Gem(int color) => color >= 0 && color < Gems.Length ? Gems[color] : Color.white;

        public static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var c);
            return c;
        }
    }

    /// <summary>
    /// Placeholder art generated at runtime from signed distance functions: no image files needed,
    /// and the project stays diffable until the real art arrives in Phase 2.
    /// Sprites are one world unit wide, i.e. exactly one board cell.
    /// </summary>
    public static class Art
    {
        const int PieceSize = 128;

        delegate float Sdf(Vector2 p);
        delegate Color PixelFn(Vector2 p);

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Vector2 LightDir = new Vector2(-0.5f, 0.85f).normalized;

        // ---------------------------------------------------------------- public sprites

        /// <summary>Bakes every sprite at startup, so the first special or effect never stalls a move.</summary>
        public static void Preload()
        {
            for (int color = 0; color < GemColor.Count; color++) Gem(color);
            Rocket(true);
            Rocket(false);
            Bomb();
            Homing();
            ColorBomb();
            Crown();
            for (int hp = 1; hp <= 9; hp++) Box(hp);
            for (int hp = 1; hp <= 3; hp++)
            {
                Cover(hp);
                Floor(hp);
            }
            Tile();
            Selection();
            Ring();
            Disc();
            Chevron();
        }

        public static Sprite Gem(int color)
        {
            return Get("gem" + color, () =>
            {
                var sdf = GemShape(color);
                var c = Palette.Gem(color);
                return Bake("gem" + color, p => Shade(sdf, c, p));
            });
        }

        public static Sprite Piece(PieceKind kind, int color, int hp)
        {
            switch (kind)
            {
                case PieceKind.Gem: return Gem(color);
                case PieceKind.RocketH: return Rocket(true);
                case PieceKind.RocketV: return Rocket(false);
                case PieceKind.Bomb: return Bomb();
                case PieceKind.Homing: return Homing();
                case PieceKind.ColorBomb: return ColorBomb();
                case PieceKind.Box: return Box(hp);
                case PieceKind.Collectible: return Crown();
                default: return Disc();
            }
        }

        public static Sprite Rocket(bool horizontal)
        {
            string key = horizontal ? "rocketH" : "rocketV";
            return Get(key, () =>
            {
                var leftHead = new[] { new Vector2(-0.8f, 0f), new Vector2(-0.38f, 0.42f), new Vector2(-0.38f, -0.42f) };
                var rightHead = new[] { new Vector2(0.8f, 0f), new Vector2(0.38f, -0.42f), new Vector2(0.38f, 0.42f) };
                Sdf body = p =>
                {
                    float shaft = SdSegment(p, new Vector2(-0.34f, 0f), new Vector2(0.34f, 0f), 0.2f);
                    float left = SdPolygon(p, leftHead) - 0.03f;
                    float right = SdPolygon(p, rightHead) - 0.03f;
                    return Mathf.Min(shaft, Mathf.Min(left, right));
                };
                var cream = Palette.Hex("FFE2B0");
                var band = Palette.Hex("E8463A");
                return Bake(key, raw =>
                {
                    var p = horizontal ? raw : new Vector2(raw.y, raw.x);
                    var c = Shade(body, cream, p);
                    float dot = SdCircle(p, 0.15f);
                    return c.a > 0f ? Over(c, band, Coverage(dot)) : c;
                });
            });
        }

        public static Sprite Bomb()
        {
            return Get("bomb", () =>
            {
                Sdf ball = p => SdCircle(p - new Vector2(-0.05f, -0.1f), 0.64f);
                Sdf fuse = p => SdSegment(p, new Vector2(0.28f, 0.42f), new Vector2(0.48f, 0.62f), 0.08f);
                var sparkStar = Star(4, 0.26f, 0.08f, 0f);
                Sdf spark = p => SdPolygon(p - new Vector2(0.56f, 0.72f), sparkStar);
                var iron = Palette.Hex("3A3F52");
                var rope = Palette.Hex("B08A5A");
                var flame = Palette.Hex("FFD24A");
                return Bake("bomb", p =>
                {
                    var c = Shade(fuse, rope, p);
                    c = Over(c, Shade(ball, iron, p));
                    return Over(c, flame, Coverage(spark(p)));
                });
            });
        }

        public static Sprite Homing()
        {
            return Get("homing", () =>
            {
                var plane = new[]
                {
                    new Vector2(0.76f, 0.76f), new Vector2(-0.72f, 0.2f), new Vector2(-0.14f, 0.02f),
                    new Vector2(-0.02f, -0.14f), new Vector2(0.2f, -0.72f),
                };
                Sdf sdf = p => SdPolygon(p, plane) - 0.04f;
                var c = Palette.Hex("6FE3F2");
                return Bake("homing", p => Shade(sdf, c, p));
            });
        }

        public static Sprite ColorBomb()
        {
            return Get("colorbomb", () =>
            {
                Sdf sdf = p => SdCircle(p, 0.74f);
                return Bake("colorbomb", p =>
                {
                    float angle = Mathf.Atan2(p.y, p.x) / (2f * Mathf.PI) + 0.5f;
                    var hue = Color.HSVToRGB(Mathf.Repeat(angle + p.magnitude * 0.25f, 1f), 0.85f, 1f);
                    var c = Shade(sdf, hue, p);
                    if (c.a <= 0f) return c;
                    float core = 1f - Smooth(0.05f, 0.34f, p.magnitude);
                    return Over(c, Color.white, core);
                });
            });
        }

        public static Sprite Box(int hp)
        {
            hp = Mathf.Clamp(hp, 1, 9);
            string key = "box" + hp;
            return Get(key, () =>
            {
                Sdf sdf = p => SdRoundBox(p, 0.82f, 0.82f, 0.12f);
                var wood = Color.Lerp(Palette.Hex("C48A4E"), Palette.Hex("6E4A32"), (hp - 1) / 3f);
                var dark = wood * 0.55f;
                dark.a = 1f;
                return Bake(key, p =>
                {
                    var c = Shade(sdf, wood, p);
                    if (c.a <= 0f) return c;
                    float plank = Mathf.Min(Mathf.Abs(p.y - 0.27f), Mathf.Abs(p.y + 0.27f));
                    c = Over(c, dark, 1f - Smooth(0.015f, 0.04f, plank));
                    float frame = Mathf.Abs(SdRoundBox(p, 0.66f, 0.66f, 0.06f));
                    c = Over(c, dark, (1f - Smooth(0.02f, 0.05f, frame)) * 0.8f);
                    // pips show the hits left (ACC-004: readable without relying on color)
                    if (hp >= 2)
                    {
                        float pip = 1f;
                        for (int i = 0; i < hp; i++)
                        {
                            float x = (i - (hp - 1) * 0.5f) * 0.26f;
                            pip = Mathf.Min(pip, SdCircle(p - new Vector2(x, 0f), 0.1f));
                        }
                        c = Over(c, Palette.Text, Coverage(pip));
                    }
                    return c;
                });
            });
        }

        public static Sprite Crown()
        {
            return Get("crown", () =>
            {
                var crown = new[]
                {
                    new Vector2(-0.68f, -0.52f), new Vector2(-0.76f, 0.34f), new Vector2(-0.34f, -0.02f),
                    new Vector2(0f, 0.62f), new Vector2(0.34f, -0.02f), new Vector2(0.76f, 0.34f), new Vector2(0.68f, -0.52f),
                };
                Sdf sdf = p => SdPolygon(p, crown) - 0.04f;
                var gold = Palette.Gold;
                var jewel = Palette.Hex("E8463A");
                return Bake("crown", p =>
                {
                    var c = Shade(sdf, gold, p);
                    if (c.a <= 0f) return c;
                    return Over(c, jewel, Coverage(SdCircle(p - new Vector2(0f, -0.26f), 0.13f)));
                });
            });
        }

        public static Sprite Cover(int hp)
        {
            hp = Mathf.Clamp(hp, 1, 3);
            string key = "cover" + hp;
            return Get(key, () =>
            {
                var ice = Palette.Hex("A6E4FF");
                float baseAlpha = 0.34f + 0.18f * (hp - 1);
                return Bake(key, p =>
                {
                    float d = SdRoundBox(p, 0.94f, 0.94f, 0.14f);
                    float a = Coverage(d);
                    if (a <= 0f) return Color.clear;
                    float edge = Smooth(-0.14f, -0.02f, d);
                    float streak = Mathf.PingPong((p.x + p.y) * 1.6f + 10f, 1f);
                    float gloss = (1f - Smooth(0.0f, 0.1f, Mathf.Abs(streak - 0.5f))) * 0.3f;
                    var c = Color.Lerp(ice, Color.white, Mathf.Max(edge * 0.7f, gloss));
                    c.a = a * Mathf.Clamp01(baseAlpha + edge * 0.45f + gloss);
                    return c;
                });
            });
        }

        public static Sprite Floor(int hp)
        {
            hp = Mathf.Clamp(hp, 1, 3);
            string key = "floor" + hp;
            return Get(key, () =>
            {
                var tint = hp == 1 ? Palette.Hex("3F8F6B") : Palette.Hex("7A5BD0");
                return Bake(key, p =>
                {
                    float a = Coverage(SdRoundBox(p, 0.97f, 0.97f, 0.14f));
                    if (a <= 0f) return Color.clear;
                    float speck = Mathf.Sin(p.x * 13f) * Mathf.Sin(p.y * 11f);
                    var c = Color.Lerp(tint, tint * 1.25f, speck * 0.5f + 0.5f);
                    c.a = a;
                    return c;
                });
            });
        }

        public static Sprite Tile()
        {
            return Get("tile", () => Bake("tile", p =>
            {
                var c = Color.white;
                c.a = Coverage(SdRoundBox(p, 0.97f, 0.97f, 0.14f));
                return c;
            }));
        }

        public static Sprite Selection()
        {
            return Get("selection", () => Bake("selection", p =>
            {
                var c = Color.white;
                c.a = 1f - Smooth(0.06f, 0.1f, Mathf.Abs(SdRoundBox(p, 0.9f, 0.9f, 0.16f)));
                return c;
            }));
        }

        public static Sprite Ring()
        {
            return Get("ring", () => Bake("ring", p =>
            {
                var c = Color.white;
                c.a = 1f - Smooth(0.03f, 0.12f, Mathf.Abs(p.magnitude - 0.84f));
                return c;
            }));
        }

        public static Sprite Disc()
        {
            return Get("disc", () => Bake("disc", p =>
            {
                var c = Color.white;
                c.a = 1f - Smooth(0.55f, 1f, p.magnitude);
                return c;
            }, 64));
        }

        public static Sprite Chevron()
        {
            return Get("chevron", () =>
            {
                var v = new[]
                {
                    new Vector2(-0.7f, 0.5f), new Vector2(-0.34f, 0.5f), new Vector2(0f, 0.02f),
                    new Vector2(0.34f, 0.5f), new Vector2(0.7f, 0.5f), new Vector2(0f, -0.5f),
                };
                return Bake("chevron", p =>
                {
                    var c = Color.white;
                    c.a = Coverage(SdPolygon(p, v));
                    return c;
                }, 64);
            });
        }

        public static Sprite Check()
        {
            return Get("check", () => Bake("check", p =>
            {
                float d = Mathf.Min(
                    SdSegment(p, new Vector2(-0.56f, -0.04f), new Vector2(-0.14f, -0.46f), 0.13f),
                    SdSegment(p, new Vector2(-0.14f, -0.46f), new Vector2(0.62f, 0.44f), 0.13f));
                var c = Color.white;
                c.a = Coverage(d, 64);
                return c;
            }, 64));
        }

        public static Sprite StarIcon()
        {
            return Get("star", () =>
            {
                var v = Star(5, 0.92f, 0.42f, 90f);
                return Bake("star", p =>
                {
                    var c = Color.white;
                    c.a = Coverage(SdPolygon(p, v) - 0.04f);
                    return c;
                });
            });
        }

        /// <summary>White rounded rectangle for 9-sliced UI panels (radius in canvas units).</summary>
        public static Sprite RoundedPanel()
        {
            return Get("panel", () =>
            {
                const int size = 96;
                const float radius = 0.5f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "panel",
                };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                        var c = Color.white;
                        c.a = Coverage(SdRoundBox(p, 0.98f, 0.98f, radius), size);
                        px[y * size + x] = c;
                    }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                var border = new Vector4(30, 30, 30, 30);
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            });
        }

        // ---------------------------------------------------------------- shapes

        static Sdf GemShape(int color)
        {
            switch (color)
            {
                case GemColor.Red:
                    return p => SdCircle(p, 0.74f);
                case GemColor.Green:
                {
                    var hex = Ngon(6, 0.72f, 90f);
                    return p => SdPolygon(p, hex) - 0.06f;
                }
                case GemColor.Blue:
                {
                    var diamond = new[]
                    {
                        new Vector2(0f, 0.78f), new Vector2(0.66f, 0f), new Vector2(0f, -0.78f), new Vector2(-0.66f, 0f),
                    };
                    return p => SdPolygon(p, diamond) - 0.06f;
                }
                case GemColor.Yellow:
                {
                    var star = Star(5, 0.82f, 0.44f, 90f);
                    return p => SdPolygon(p, star) - 0.05f;
                }
                case GemColor.Purple:
                    return p => SdRoundBox(p, 0.68f, 0.68f, 0.2f);
                default:
                {
                    var tri = Ngon(3, 0.74f, 90f);
                    return p => SdPolygon(p - new Vector2(0f, -0.14f), tri) - 0.1f;
                }
            }
        }

        static Vector2[] Ngon(int n, float r, float rotationDeg)
        {
            var v = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = (rotationDeg + i * 360f / n) * Mathf.Deg2Rad;
                v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return v;
        }

        static Vector2[] Star(int points, float outer, float inner, float rotationDeg)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (rotationDeg + i * 180f / points) * Mathf.Deg2Rad;
                v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? outer : inner);
            }
            return v;
        }

        // ---------------------------------------------------------------- distance functions

        static float SdCircle(Vector2 p, float r) => p.magnitude - r;

        static float SdRoundBox(Vector2 p, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(p.x) - hx + r;
            float qy = Mathf.Abs(p.y) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static float SdSegment(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        static float SdPolygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                float t = Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                var b = w - e * t;
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        // ---------------------------------------------------------------- shading

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Coverage(float d, int size = PieceSize)
        {
            float aa = 2.4f / size;
            return 1f - Smooth(-aa, aa, d);
        }

        static Color Over(Color under, Color over, float amount = -1f)
        {
            float a = amount >= 0f ? amount : over.a;
            var c = Color.Lerp(under, over, a);
            c.a = Mathf.Max(under.a, a);
            return c;
        }

        /// <summary>Candy look: outline, bevel lit from the upper left, and a soft gloss spot.</summary>
        static Color Shade(Sdf sdf, Color baseColor, Vector2 p)
        {
            float d = sdf(p);
            float a = Coverage(d);
            var outline = baseColor * 0.42f;
            // Transparent pixels keep the outline color so filtering does not add a black fringe.
            if (a <= 0f) return new Color(outline.r, outline.g, outline.b, 0f);

            const float e = 0.02f;
            var g = new Vector2(sdf(p + new Vector2(e, 0f)) - sdf(p - new Vector2(e, 0f)),
                sdf(p + new Vector2(0f, e)) - sdf(p - new Vector2(0f, e)));
            float light = g.sqrMagnitude > 1e-8f ? Vector2.Dot(g.normalized, LightDir) : 0f;
            float bevel = Mathf.Clamp01(1f + d / 0.24f);
            bevel *= bevel;

            var c = baseColor * (1f + 0.14f * p.y);
            c = light > 0f
                ? Color.Lerp(c, Color.white, bevel * light * 0.55f)
                : Color.Lerp(c, Color.black, bevel * -light * 0.38f);

            c = Color.Lerp(c, outline, Smooth(-0.075f, -0.04f, d));

            float gx = p.x + 0.24f, gy = p.y - 0.38f;
            float gloss = Mathf.Exp(-(gx * gx * 10f + gy * gy * 24f));
            c = Color.Lerp(c, Color.white, gloss * 0.5f * (d < -0.08f ? 1f : 0f));
            c.a = a;
            return c;
        }

        // ---------------------------------------------------------------- baking

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        static Sprite Bake(string name, PixelFn fn, int size = PieceSize)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = name,
            };
            var px = new Color32[size * size];
            var v = Vector2.zero;
            for (int y = 0; y < size; y++)
            {
                v.y = (y + 0.5f) / size * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    v.x = (x + 0.5f) / size * 2f - 1f;
                    px[y * size + x] = fn(v);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }
    }
}
