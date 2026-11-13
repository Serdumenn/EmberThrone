using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class Fracture
    {
        public struct Piece
        {
            public Sprite Sprite;
            public Vector2 Offset;
        }

        const float Ring = 0.3f;
        const float RingJitter = 0.08f;
        const float CenterJitter = 0.06f;

        static readonly Dictionary<Sprite, Dictionary<int, Piece[]>> Cache = new Dictionary<Sprite, Dictionary<int, Piece[]>>();

        public static Piece[] Of(Sprite source, int count)
        {
            if (source == null || count < 2) return new Piece[0];
            if (!Cache.TryGetValue(source, out var byCount))
            {
                byCount = new Dictionary<int, Piece[]>();
                Cache[source] = byCount;
            }
            if (byCount.TryGetValue(count, out var cached) && (cached.Length == 0 || cached[0].Sprite != null)) return cached;

            var rect = source.rect;
            float w = rect.width, h = rect.height;
            var rng = new System.Random(count * 7919 + Mathf.RoundToInt(w) * 31 + Mathf.RoundToInt(h));
            var seeds = new Vector2[count];
            seeds[0] = new Vector2(w * (0.5f + Jitter(rng, CenterJitter)), h * (0.5f + Jitter(rng, CenterJitter)));
            for (int i = 1; i < count; i++)
            {
                float a = (i - 1 + 0.6f * (float)rng.NextDouble()) / (count - 1) * Mathf.PI * 2f;
                float r = Ring + Jitter(rng, RingJitter);
                seeds[i] = new Vector2(w * (0.5f + r * Mathf.Cos(a)), h * (0.5f + r * Mathf.Sin(a)));
            }

            var pieces = new List<Piece>(count);
            for (int i = 0; i < count; i++)
            {
                var poly = new List<Vector2> { new Vector2(0f, 0f), new Vector2(w, 0f), new Vector2(w, h), new Vector2(0f, h) };
                for (int j = 0; j < count && poly.Count >= 3; j++)
                    if (j != i) poly = Clip(poly, seeds[i], seeds[j]);
                if (poly.Count >= 3) pieces.Add(Build(source, poly));
            }
            var result = pieces.ToArray();
            byCount[count] = result;
            return result;
        }

        static float Jitter(System.Random rng, float amount) => ((float)rng.NextDouble() * 2f - 1f) * amount;

        static List<Vector2> Clip(List<Vector2> poly, Vector2 keep, Vector2 other)
        {
            var mid = (keep + other) * 0.5f;
            var normal = other - keep;
            var result = new List<Vector2>(poly.Count + 1);
            for (int k = 0; k < poly.Count; k++)
            {
                var a = poly[k];
                var b = poly[(k + 1) % poly.Count];
                float da = Vector2.Dot(a - mid, normal);
                float db = Vector2.Dot(b - mid, normal);
                if (da <= 0f) result.Add(a);
                if ((da < 0f && db > 0f) || (da > 0f && db < 0f)) result.Add(Vector2.Lerp(a, b, da / (da - db)));
            }
            return result;
        }

        static Piece Build(Sprite source, List<Vector2> poly)
        {
            var rect = source.rect;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            var centroid = Vector2.zero;
            foreach (var p in poly)
            {
                minX = Mathf.Min(minX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
                centroid += p;
            }
            centroid /= poly.Count;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(minX), 0, (int)rect.width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(minY), 0, (int)rect.height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(maxX), x0 + 1, (int)rect.width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY), y0 + 1, (int)rect.height);
            var sub = new Rect(rect.x + x0, rect.y + y0, x1 - x0, y1 - y0);
            var pivot = new Vector2((centroid.x - x0) / sub.width, (centroid.y - y0) / sub.height);
            var sprite = Sprite.Create(source.texture, sub, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = source.name + "_shard";

            var vertices = new Vector2[poly.Count];
            for (int k = 0; k < poly.Count; k++)
                vertices[k] = new Vector2(Mathf.Clamp(poly[k].x - x0, 0f, sub.width), Mathf.Clamp(poly[k].y - y0, 0f, sub.height));
            var triangles = new ushort[(poly.Count - 2) * 3];
            for (int k = 0; k < poly.Count - 2; k++)
            {
                triangles[k * 3] = 0;
                triangles[k * 3 + 1] = (ushort)(k + 1);
                triangles[k * 3 + 2] = (ushort)(k + 2);
            }
            sprite.OverrideGeometry(vertices, triangles);
            return new Piece { Sprite = sprite, Offset = (centroid - source.pivot) / source.pixelsPerUnit };
        }
    }
}
