using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class FxArt
    {
        public const string AdditiveShader = "Shaders/Additive";
        public const string PieceShader = "Shaders/Piece";
        public const int ChunkTiles = 2;

        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Material Additive => Material("additive", AdditiveShader, null);
        public static Material Piece => Material("piece", PieceShader, null);

        public static Material ParticleAdditive(Sprite sprite) => Material("add:" + sprite.texture.name, AdditiveShader, sprite.texture);
        public static Material ParticleBlend(Sprite sprite) => Material("mix:" + sprite.texture.name, null, sprite.texture);

        static Material Material(string key, string shaderPath, Texture texture)
        {
            if (Materials.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(shaderPath == null ? Shader.Find("Sprites/Default") : LoadShader(shaderPath)) { name = key };
            if (texture != null) m.mainTexture = texture;
            Materials[key] = m;
            return m;
        }

        public static Shader LoadShader(string path)
        {
            var shader = Resources.Load<Shader>(path);
            if (shader != null) return shader;
            Debug.LogError("[EmberThrone] Shader missing: Resources/" + path);
            return Shader.Find("Sprites/Default");
        }

        public static void Preload()
        {
            Glow();
            Core();
            Flare();
            Shockwave();
            Smoke();
            Chunks();
            Confetti();
            Rays();
            Bolt();
        }

        static float Edge(float r) => 1f - Art.Smooth(0.82f, 1f, r);

        static float Sq(float x) => x * x;

        public static Sprite Glow()
        {
            return Art.Get("fx_glow", () => Art.Bake("fx_glow", p =>
            {
                var c = Color.white;
                c.a = Mathf.Exp(-p.sqrMagnitude * 4.2f) * Edge(p.magnitude);
                return c;
            }));
        }

        public static Sprite Core()
        {
            return Art.Get("fx_core", () => Art.Bake("fx_core", p =>
            {
                float r2 = p.sqrMagnitude;
                var c = Color.white;
                c.a = Mathf.Clamp01(Mathf.Exp(-r2 * 18f) + Mathf.Exp(-r2 * 3.6f) * 0.45f) * Edge(p.magnitude);
                return c;
            }));
        }

        public static Sprite Flare()
        {
            return Art.Get("fx_flare", () => Art.Bake("fx_flare", p =>
            {
                float ax = Mathf.Abs(p.x), ay = Mathf.Abs(p.y);
                float rays = Mathf.Exp(-ay * 26f) * Mathf.Exp(-ax * 3.2f) + Mathf.Exp(-ax * 26f) * Mathf.Exp(-ay * 3.2f);
                float halo = Mathf.Exp(-p.sqrMagnitude * 12f) * 0.9f;
                var c = Color.white;
                c.a = Mathf.Clamp01(rays + halo) * Edge(Mathf.Max(ax, ay));
                return c;
            }));
        }

        public static Sprite Shockwave()
        {
            return Art.Get("fx_shockwave", () => Art.Bake("fx_shockwave", p =>
            {
                float r = p.magnitude;
                float band = Mathf.Exp(-Sq((r - 0.82f) / 0.065f));
                float inner = 0.2f * Art.Smooth(0.25f, 0.82f, r) * (1f - Art.Smooth(0.82f, 0.9f, r));
                var c = Color.white;
                c.a = Mathf.Clamp01(band + inner);
                return c;
            }));
        }

        public static Sprite Smoke()
        {
            return Art.Get("fx_smoke", () => Art.Bake("fx_smoke", p =>
            {
                float n = Fbm(p * 2.2f);
                float r = p.magnitude + (n - 0.5f) * 0.55f;
                float shade = Mathf.Lerp(0.78f, 1f, n);
                var c = new Color(shade, shade, shade, 1f);
                c.a = (1f - Art.Smooth(0.28f, 0.9f, r)) * (0.7f + 0.3f * n) * Edge(p.magnitude);
                return c;
            }));
        }

        static float Fbm(Vector2 p)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < 4; i++)
            {
                sum += amp * Mathf.PerlinNoise(p.x + 37.2f, p.y + 91.7f);
                norm += amp;
                p *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        static readonly Vector2[][] ChunkShapes =
        {
            new[] { new Vector2(0f, 0.8f), new Vector2(0.7f, -0.55f), new Vector2(-0.7f, -0.45f) },
            new[] { new Vector2(-0.6f, 0.5f), new Vector2(0.5f, 0.7f), new Vector2(0.7f, -0.4f), new Vector2(-0.4f, -0.7f) },
            new[] { new Vector2(0f, 0.8f), new Vector2(0.7f, 0.25f), new Vector2(0.45f, -0.65f), new Vector2(-0.5f, -0.6f), new Vector2(-0.75f, 0.2f) },
            new[] { new Vector2(-0.8f, 0.1f), new Vector2(0.2f, 0.55f), new Vector2(0.8f, -0.05f), new Vector2(-0.1f, -0.5f) },
        };

        public static Sprite Chunks()
        {
            return Art.Get("fx_chunks", () => Art.Bake("fx_chunks", p =>
            {
                float u = (p.x + 1f) * 0.5f * ChunkTiles, v = (p.y + 1f) * 0.5f * ChunkTiles;
                int tx = Mathf.Min(ChunkTiles - 1, (int)u), ty = Mathf.Min(ChunkTiles - 1, (int)v);
                var q = new Vector2((u - tx) * 2f - 1f, (v - ty) * 2f - 1f);
                float d = Art.SdPolygon(q, ChunkShapes[ty * ChunkTiles + tx]) - 0.06f;
                float facet = Art.Smooth(-0.15f, 0.15f, (q.y - q.x) * 0.7f);
                float rim = Art.Smooth(-0.2f, -0.04f, d);
                float shade = Mathf.Lerp(0.62f, 1f, facet) + 0.2f * rim * facet;
                var c = new Color(shade, shade, shade, 1f);
                c.a = Art.Coverage(d, Art.HelperSize / ChunkTiles);
                return c;
            }));
        }

        public static Sprite Confetti()
        {
            return Art.Get("fx_confetti", () => Art.Bake("fx_confetti", p =>
            {
                float shade = Mathf.Lerp(0.82f, 1f, Art.Smooth(-0.5f, 0.5f, p.y));
                var c = new Color(shade, shade, shade, 1f);
                c.a = Art.Coverage(Art.SdRoundBox(p, 0.86f, 0.52f, 0.12f), 64);
                return c;
            }, 64));
        }

        public static Sprite Rays()
        {
            return Art.Get("fx_rays", () => Art.Bake("fx_rays", p =>
            {
                float angle = Mathf.Atan2(p.y, p.x);
                float r = p.magnitude;
                float ray = Art.Smooth(0.5f, 0.95f, 0.5f + 0.5f * Mathf.Sin(angle * 10f)) * (0.55f + 0.45f * Mathf.Sin(angle * 3f + 1.3f));
                float fall = (1f - Art.Smooth(0.1f, 1f, r)) * Art.Smooth(0.02f, 0.16f, r);
                var c = Color.white;
                c.a = Mathf.Clamp01(ray * fall);
                return c;
            }));
        }

        public static Sprite Bolt()
        {
            return Art.Get("fx_bolt", () => Art.Bake("fx_bolt", p =>
            {
                var c = Color.white;
                c.a = Mathf.Exp(-Sq(p.y * 3.2f)) * (1f - Art.Smooth(0.86f, 1f, Mathf.Abs(p.x)));
                return c;
            }, 64));
        }
    }
}
