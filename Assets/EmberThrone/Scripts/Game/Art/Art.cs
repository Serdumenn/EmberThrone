using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class Palette
    {
        public static readonly Color Background = Hex("1A1220");
        public static readonly Color BoardBack = Hex("221650");
        public static readonly Color BoardGlow = new Color(1f, 1f, 1f, 0.85f);
        public static readonly Color TileLight = new Color(236f / 255f, 244f / 255f, 1f, 0.93f);
        public static readonly Color TileDark = new Color(208f / 255f, 224f / 255f, 1f, 0.93f);
        public static readonly Color Gold = Hex("FFD24A");
        public static readonly Color Select = Hex("7AD83A");
        public const float CoverAlpha = 0.82f;

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

    public static class Art
    {
        const int HelperSize = 128;
        const string BoardFolder = "Board/";

        const float PieceCells = 1.1f;
        const float FloorCells = 1.08f;
        const float CoverCells = 1.12f;
        const float BackdropPixelsPerUnit = 100f;

        static readonly string[] GemFiles = { "piece_R", "piece_G", "piece_B", "piece_Y", "piece_P", "piece_O" };

        delegate float Sdf(Vector2 p);
        delegate Color PixelFn(Vector2 p);

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static Material _material;

        public static Material SpriteMaterial => _material != null ? _material : (_material = new Material(Shader.Find("Sprites/Default")));

        public static void Preload()
        {
            for (int color = 0; color < GemColor.Count; color++) Gem(color);
            Rocket(true);
            Rocket(false);
            Bomb();
            Homing();
            ColorBomb();
            Collectible();
            for (int hp = 1; hp <= 3; hp++)
            {
                Box(hp);
                Cover(hp);
                Floor(hp);
            }
            Backdrop();
            Tile();
            CellBack();
            Glow();
            Selection();
            Ring();
            Disc();
            Chevron();
            Sparkle();
            Shard();
            Splinter();
        }

        public static Sprite Gem(int color) => Render(GemFiles[Mathf.Clamp(color, 0, GemFiles.Length - 1)]);

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
                case PieceKind.Collectible: return Collectible();
                default: return Disc();
            }
        }

        public static Sprite Rocket(bool horizontal) => Render(horizontal ? "sp_rocket" : "sp_rocket_v");
        public static Sprite Bomb() => Render("sp_bomb");
        public static Sprite Homing() => Render("sp_homing");
        public static Sprite ColorBomb() => Render("sp_orb");

        public static Sprite Box(int hp) => Render("ob_box" + Mathf.Clamp(hp, 1, 3));

        public static Sprite Collectible() => Render("ob_lantern");

        public static Sprite Cover(int hp) => Render("ob_ice", CoverCells);

        public static Sprite Floor(int hp) => Render(hp >= 2 ? "ob_floor2" : "ob_floor1", FloorCells);

        public static Sprite Backdrop()
        {
            return Get("bg_game", () =>
            {
                var tex = Load("bg_game");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), BackdropPixelsPerUnit, 0, SpriteMeshType.FullRect);
            });
        }

        static Sprite Render(string file, float cells = PieceCells)
        {
            return Get(file, () =>
            {
                var tex = Load(file);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / cells, 0, SpriteMeshType.FullRect);
            });
        }

        static Texture2D Load(string file)
        {
            var tex = Resources.Load<Texture2D>(BoardFolder + file);
            if (tex == null) throw new MissingReferenceException("[EmberThrone] Missing board art: Resources/" + BoardFolder + file);
            return tex;
        }

        public static Sprite Tile()
        {
            return Get("tile", () => Bake("tile", p =>
            {
                float d = SdRoundBox(p, 0.93f, 0.93f, 0.37f);
                float lowerEdge = Smooth(-0.78f, -0.84f, p.y);
                var c = Color.Lerp(Color.white, new Color(0.24f, 0.31f, 0.63f), 0.18f * lowerEdge);
                c.a = Coverage(d);
                return c;
            }));
        }

        public static Sprite CellBack()
        {
            return Get("back", () => Bake("back", p =>
            {
                var c = Color.white;
                c.a = Coverage(SdRoundBox(p, 0.98f, 0.98f, 0.42f));
                return c;
            }));
        }

        public static Sprite Glow()
        {
            return Get("glow", () => Bake("glow", p =>
            {
                var c = Color.white;
                c.a = 1f - Smooth(-0.12f, 0f, SdRoundBox(p, 0.92f, 0.92f, 0.4f));
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

        public static Sprite Sparkle()
        {
            return Get("sparkle", () =>
            {
                var star = Star(4, 0.95f, 0.16f);
                return Bake("sparkle", p =>
                {
                    float core = Coverage(SdPolygon(p, star), 64);
                    float halo = Mathf.Exp(-p.sqrMagnitude * 9f) * 0.55f;
                    var c = Color.white;
                    c.a = Mathf.Clamp01(core + halo);
                    return c;
                }, 64);
            });
        }

        public static Sprite Shard()
        {
            return Get("shard", () =>
            {
                var tri = new[] { new Vector2(0f, 0.8f), new Vector2(0.62f, -0.5f), new Vector2(-0.62f, -0.5f) };
                return Bake("shard", p =>
                {
                    float d = SdPolygon(p, tri) - 0.08f;
                    var c = Color.Lerp(Color.white, new Color(0.78f, 0.78f, 0.78f), Smooth(0.1f, -0.3f, p.y - p.x * 0.4f) * 0.6f);
                    c.a = Coverage(d, 64);
                    return c;
                }, 64);
            });
        }

        public static Sprite Splinter()
        {
            return Get("splinter", () => Bake("splinter", p =>
            {
                var c = Color.Lerp(Color.white, new Color(0.7f, 0.7f, 0.7f), Smooth(0f, -0.2f, p.y));
                c.a = Coverage(SdRoundBox(p, 0.9f, 0.24f, 0.2f), 64);
                return c;
            }, 64));
        }

        static Vector2[] Star(int points, float outer, float inner)
        {
            var v = new Vector2[points * 2];
            for (int i = 0; i < v.Length; i++)
            {
                float a = (90f + i * 180f / points) * Mathf.Deg2Rad;
                v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? outer : inner);
            }
            return v;
        }

        static float SdRoundBox(Vector2 p, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(p.x) - hx + r;
            float qy = Mathf.Abs(p.y) - hy + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
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

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Coverage(float d, int size = HelperSize)
        {
            float aa = 2.4f / size;
            return 1f - Smooth(-aa, aa, d);
        }

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            Cache[key] = s;
            return s;
        }

        static Sprite Bake(string name, PixelFn fn, int size = HelperSize)
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
