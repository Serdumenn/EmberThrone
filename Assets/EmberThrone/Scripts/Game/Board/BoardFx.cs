using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class BoardFx
    {
        public const int SortOrder = 30;

        static readonly Color Wood = new Color(0.66f, 0.38f, 0.16f, 1f);
        static readonly Color Dust = new Color(0.86f, 0.74f, 0.58f, 0.55f);
        static readonly Color Ice = new Color(0.8f, 0.95f, 1f, 1f);
        static readonly Color Ash = new Color(0.5f, 0.46f, 0.52f, 0.6f);
        static readonly Color Ember = new Color(1f, 0.58f, 0.16f, 1f);
        static readonly Color Twinkle = new Color(1f, 0.98f, 0.88f, 1f);

        readonly Transform _root;
        readonly Material _material;
        readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();

        public BoardFx(Transform root, Material material)
        {
            _root = root;
            _material = material;
        }

        SpriteRenderer Take(Sprite sprite, Color color, Vector3 pos, Vector3 scale)
        {
            SpriteRenderer r;
            if (_pool.Count > 0)
            {
                r = _pool.Pop();
                r.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("fx");
                go.transform.SetParent(_root, false);
                r = go.AddComponent<SpriteRenderer>();
                r.sharedMaterial = _material;
                r.sortingOrder = SortOrder;
            }
            r.sprite = sprite;
            r.color = color;
            r.transform.localPosition = pos;
            r.transform.localScale = scale;
            r.transform.localRotation = Quaternion.identity;
            return r;
        }

        void Release(SpriteRenderer r)
        {
            r.gameObject.SetActive(false);
            _pool.Push(r);
        }

        public float Ring(Vector3 pos, Color color, float fromRadius, float toRadius, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(Art.Ring(), color, pos, Vector3.one * (fromRadius * 2f));
                Tweens.Run(duration, t =>
                {
                    float e = Ease.OutCubic(t);
                    r.transform.localScale = Vector3.one * (Mathf.Lerp(fromRadius, toRadius, e) * 2f);
                    var c = color;
                    c.a = color.a * (1f - t);
                    r.color = c;
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }

        public float Burst(Vector3 pos, Color color, float delay, int shards = 6)
        {
            var ring = color;
            ring.a *= 0.8f;
            Ring(pos, ring, 0.2f, 0.68f, delay, 0.22f);
            Throw(Art.Shard(), pos, color, delay, shards, 0.2f, 3.2f, 9f, 0.42f, 0.35f, 1);
            Sparkles(pos, 2, delay, 0.4f, 0.5f);
            return delay + 0.26f;
        }

        public float Splinters(Vector3 pos, float delay, int count = 7)
        {
            Throw(Art.Splinter(), pos, Wood, delay, count, 0.34f, 3.4f, 10f, 0.5f, 0.5f, 2);
            Puff(pos, Dust, delay, 2, 0.4f);
            return delay + 0.26f;
        }

        public float IceShards(Vector3 pos, float delay)
        {
            Throw(Art.Shard(), pos, Ice, delay, 8, 0.22f, 3.6f, 8f, 0.45f, 0.4f, 3);
            Sparkles(pos, 3, delay, 0.45f, 0.55f);
            return delay + 0.26f;
        }

        public float AshPuff(Vector3 pos, float delay)
        {
            Puff(pos, Ash, delay, 4, 0.5f);
            Throw(Art.Disc(), pos, Ember, delay, 5, 0.12f, 1.2f, -1.6f, 0.7f, 1.2f, 4);
            return delay + 0.26f;
        }

        public float Firework(Vector3 pos, Color color, float delay)
        {
            var glow = color;
            glow.a = 0.6f;
            Flash(pos, glow, 1.6f, delay, 0.3f);
            Ring(pos, color, 0.3f, 1.7f, delay, 0.4f);
            Throw(Art.Shard(), pos, color, delay, 12, 0.22f, 5.5f, 6f, 0.6f, 0.2f, 5);
            Throw(Art.Sparkle(), pos, Twinkle, delay, 6, 0.32f, 3f, 4f, 0.55f, 0.3f, 6);
            return delay + 0.4f;
        }

        public float Spark(Vector3 from, Vector3 to, Color color, float delay, float duration)
        {
            const int trail = 5;
            for (int k = 0; k <= trail; k++)
            {
                bool head = k == 0;
                float lag = k * 0.025f;
                float size = head ? 0.7f : 0.34f * (1f - k / (float)(trail + 1));
                var tint = head ? Twinkle : color;
                Tweens.After(delay + lag, () =>
                {
                    var r = Take(head ? Art.Sparkle() : Art.Disc(), tint, from, Vector3.one * size);
                    r.sortingOrder = SortOrder + 2;
                    Tweens.Run(duration, t =>
                    {
                        r.transform.localPosition = Vector3.LerpUnclamped(from, to, Ease.OutQuad(t));
                        if (head) r.transform.localRotation = Quaternion.Euler(0f, 0f, t * 360f);
                    }, 0f, () =>
                    {
                        r.sortingOrder = SortOrder;
                        Release(r);
                    });
                });
            }
            return delay + duration;
        }

        public void Sparkles(Vector3 pos, int count, float delay, float spread, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Hash(pos.x, pos.y, i + 40) * Mathf.PI * 2f;
                float d = spread * (0.4f + 0.6f * Hash(pos.y, pos.x, i + 41));
                var at = pos + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * d;
                Tweens.After(delay + 0.04f * i, () =>
                {
                    var r = Take(Art.Sparkle(), Twinkle, at, Vector3.zero);
                    r.sortingOrder = SortOrder + 1;
                    Tweens.Run(0.36f, t =>
                    {
                        r.transform.localScale = Vector3.one * (size * Mathf.Sin(t * Mathf.PI));
                        r.transform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                        r.transform.localPosition = at + Vector3.up * (0.15f * t);
                    }, 0f, () =>
                    {
                        r.sortingOrder = SortOrder;
                        Release(r);
                    });
                });
            }
        }

        void Puff(Vector3 pos, Color color, float delay, int count, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Hash(pos.x, pos.y, i + 60) * Mathf.PI * 2f;
                var drift = new Vector3(Mathf.Cos(a) * 0.35f, 0.25f + 0.2f * Mathf.Sin(a), 0f);
                Tweens.After(delay, () =>
                {
                    var r = Take(Art.Disc(), color, pos, Vector3.one * size);
                    Tweens.Run(0.5f, t =>
                    {
                        float e = Ease.OutCubic(t);
                        r.transform.localPosition = pos + drift * e;
                        r.transform.localScale = Vector3.one * (size * (1f + 1.2f * e));
                        var c = color;
                        c.a = color.a * (1f - t);
                        r.color = c;
                    }, 0f, () => Release(r));
                });
            }
        }

        void Throw(Sprite sprite, Vector3 pos, Color color, float delay, int count, float size, float speed, float gravity,
            float life, float upBias, int salt)
        {
            for (int i = 0; i < count; i++)
            {
                float r1 = Hash(pos.x, pos.y, i * 3 + salt * 101);
                float r2 = Hash(pos.y, pos.x, i * 7 + salt * 211);
                float r3 = Hash(pos.x + 3f, pos.y - 5f, i * 13 + salt * 307);
                float angle = (i + r1 * 0.8f) / count * Mathf.PI * 2f;
                var velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) + upBias, 0f) * (speed * (0.65f + 0.6f * r2));
                float spin = (r3 - 0.5f) * 900f;
                float start = size * (0.7f + 0.6f * r3);
                float span = life * (0.8f + 0.4f * r1);
                Tweens.After(delay, () =>
                {
                    var r = Take(sprite, color, pos, Vector3.one * start);
                    Tweens.Run(span, t =>
                    {
                        float time = t * span;
                        r.transform.localPosition = pos + velocity * time + Vector3.down * (0.5f * gravity * time * time);
                        r.transform.localRotation = Quaternion.Euler(0f, 0f, spin * time);
                        float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                        r.transform.localScale = Vector3.one * (start * (0.4f + 0.6f * fade));
                        var c = color;
                        c.a = color.a * fade;
                        r.color = c;
                    }, 0f, () => Release(r));
                });
            }
        }

        static float Hash(float x, float y, int i)
        {
            float h = Mathf.Sin(x * 12.9898f + y * 78.233f + i * 37.719f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        public float Beam(Vector3 from, Vector3 to, Color color, float width, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var d = to - from;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                var r = Take(Art.Disc(), color, (from + to) * 0.5f, new Vector3(d.magnitude + width, width, 1f));
                r.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                r.sortingOrder = SortOrder - 1;
                Tweens.Run(duration, t =>
                {
                    r.transform.localScale = new Vector3(d.magnitude + width, width * (1f - t), 1f);
                }, 0f, () =>
                {
                    r.sortingOrder = SortOrder;
                    Release(r);
                });
            });
            return delay + duration;
        }

        public float Fly(Sprite sprite, Vector3 from, Vector3 to, float delay, float duration, float scale = 1f)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(sprite, Color.white, from, Vector3.one * scale);
                r.sortingOrder = SortOrder + 1;
                Tweens.Run(duration, t =>
                {
                    r.transform.localPosition = Vector3.LerpUnclamped(from, to, Ease.InOutSine(t));
                }, 0f, () =>
                {
                    r.sortingOrder = SortOrder;
                    Release(r);
                });
            });
            return delay + duration;
        }

        public float Flash(Vector3 pos, Color color, float scale, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(Art.Disc(), color, pos, Vector3.one * scale);
                Tweens.Run(duration, t =>
                {
                    var c = color;
                    c.a = color.a * (1f - t);
                    r.color = c;
                    r.transform.localScale = Vector3.one * (scale * (1f + 0.3f * t));
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }
    }
}
