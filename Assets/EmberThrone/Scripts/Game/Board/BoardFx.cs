using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    /// <summary>Pooled sprite effects: pops, rings, beams and projectiles. Everything here is cosmetic.</summary>
    public sealed class BoardFx
    {
        public const int SortOrder = 30;

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

        /// <summary>A ring grows and fades. Radius is in cells.</summary>
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

        /// <summary>The short burst when a piece is cleared: ring plus a few shards.</summary>
        public float Burst(Vector3 pos, Color color, float delay, int shards = 5)
        {
            Ring(pos, color, 0.2f, 0.62f, delay, 0.22f);
            for (int i = 0; i < shards; i++)
            {
                float angle = (i + 0.5f) / shards * Mathf.PI * 2f + pos.x * 1.7f + pos.y * 2.3f;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float dist = 0.55f + 0.2f * Mathf.Sin(angle * 3f);
                Tweens.After(delay, () =>
                {
                    var r = Take(Art.Disc(), color, pos, Vector3.one * 0.26f);
                    Tweens.Run(0.26f, t =>
                    {
                        r.transform.localPosition = pos + dir * (dist * Ease.OutCubic(t));
                        r.transform.localScale = Vector3.one * (0.26f * (1f - t));
                    }, 0f, () => Release(r));
                });
            }
            return delay + 0.26f;
        }

        /// <summary>A light beam between two points that thins out.</summary>
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

        /// <summary>A sprite that flies from one point to another, then disappears.</summary>
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

        /// <summary>A short full-cell flash.</summary>
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
