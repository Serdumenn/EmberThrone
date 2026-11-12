using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class BoardFx
    {
        public const int SortOrder = 30;

        const int BoltPoints = 8;
        const float BoltJitter = 0.035f;
        const float ShockwaveRim = 0.41f;

        static readonly Color Wood = new Color(0.72f, 0.45f, 0.2f, 1f);
        static readonly Color Dust = new Color(0.86f, 0.74f, 0.58f, 0.55f);
        static readonly Color Ice = new Color(0.8f, 0.95f, 1f, 1f);
        static readonly Color Mist = new Color(0.85f, 0.95f, 1f, 0.45f);
        static readonly Color Ash = new Color(0.5f, 0.46f, 0.52f, 0.6f);
        static readonly Color Ember = new Color(1f, 0.58f, 0.16f, 1f);
        static readonly Color Fire = new Color(1f, 0.74f, 0.32f, 1f);
        static readonly Color Soot = new Color(0.3f, 0.25f, 0.3f, 0.55f);
        static readonly Color Twinkle = new Color(1f, 0.98f, 0.88f, 1f);

        sealed class Bolt
        {
            public GameObject Root;
            public LineRenderer Outer;
            public LineRenderer Inner;
            public readonly Vector3[] Points = new Vector3[BoltPoints];
        }

        readonly Transform _root;
        readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();
        readonly Stack<Bolt> _bolts = new Stack<Bolt>();
        Material _boltMaterial;

        public FxParticles Particles { get; }

        public BoardFx(Transform root)
        {
            _root = root;
            Particles = new FxParticles(root);
        }

        SpriteRenderer Take(Sprite sprite, Color color, Vector3 pos, Vector3 scale, bool additive, int order = SortOrder)
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
            }
            r.sharedMaterial = additive ? FxArt.Additive : Art.SpriteMaterial;
            r.sortingOrder = order;
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

        static Color Alpha(Color c, float a)
        {
            c.a *= a;
            return c;
        }

        public float Glow(Vector3 pos, Color color, float scale, float delay, float duration, float grow = 0.4f)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(FxArt.Glow(), color, pos, Vector3.one * scale, true);
                Tweens.Run(duration, t =>
                {
                    float fade = (1f - t) * (1f - t);
                    r.color = Alpha(color, fade);
                    r.transform.localScale = Vector3.one * (scale * (1f + grow * Ease.OutCubic(t)));
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }

        public float Shockwave(Vector3 pos, Color color, float fromRadius, float toRadius, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(FxArt.Shockwave(), color, pos, Vector3.one * (fromRadius / ShockwaveRim), true, SortOrder - 1);
                Tweens.Run(duration, t =>
                {
                    float radius = Mathf.Lerp(fromRadius, toRadius, Ease.OutCubic(t));
                    r.transform.localScale = Vector3.one * (radius / ShockwaveRim);
                    r.color = Alpha(color, Mathf.Pow(1f - t, 1.5f));
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }

        public float Rays(Vector3 pos, Color color, float scale, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(FxArt.Rays(), color, pos, Vector3.zero, true, SortOrder - 2);
                float turn = UnityEngine.Random.Range(0f, 360f);
                Tweens.Run(duration, t =>
                {
                    r.transform.localScale = Vector3.one * (scale * Mathf.Lerp(0.5f, 1f, Ease.OutCubic(Mathf.Min(1f, t * 3f))));
                    r.transform.localRotation = Quaternion.Euler(0f, 0f, turn + t * 80f);
                    r.color = Alpha(color, Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.7f));
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }

        public float Beam(Vector3 from, Vector3 to, Color color, float width, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var d = to - from;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                var r = Take(FxArt.Glow(), color, (from + to) * 0.5f, new Vector3(d.magnitude + width, width, 1f), true, SortOrder - 1);
                r.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                Tweens.Run(duration, t =>
                {
                    r.transform.localScale = new Vector3(d.magnitude + width, width * (1f - t * t), 1f);
                    r.color = Alpha(color, 1f - t);
                }, 0f, () => Release(r));
            });
            return delay + duration;
        }

        Bolt TakeBolt()
        {
            if (_bolts.Count > 0)
            {
                var pooled = _bolts.Pop();
                pooled.Root.SetActive(true);
                return pooled;
            }
            if (_boltMaterial == null) _boltMaterial = FxArt.ParticleAdditive(FxArt.Bolt());
            var bolt = new Bolt { Root = new GameObject("bolt") };
            bolt.Root.transform.SetParent(_root, false);
            bolt.Outer = Line(bolt.Root.transform, SortOrder + 1);
            bolt.Inner = Line(bolt.Root.transform, SortOrder + 2);
            return bolt;
        }

        LineRenderer Line(Transform parent, int order)
        {
            var go = new GameObject("line");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _boltMaterial;
            line.useWorldSpace = false;
            line.positionCount = BoltPoints;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.sortingOrder = order;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        public float Lightning(Vector3 from, Vector3 to, Color color, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var bolt = TakeBolt();
                var d = to - from;
                var normal = new Vector3(-d.y, d.x, 0f).normalized;
                float sway = Mathf.Min(0.32f, 0.08f + d.magnitude * 0.05f);
                float next = 0f;
                Tweens.Run(duration, t =>
                {
                    if (t >= next)
                    {
                        next = t + BoltJitter / duration;
                        for (int i = 0; i < BoltPoints; i++)
                        {
                            float s = i / (float)(BoltPoints - 1);
                            bolt.Points[i] = Vector3.Lerp(from, to, s) + normal * (UnityEngine.Random.Range(-1f, 1f) * sway * Mathf.Sin(s * Mathf.PI));
                        }
                        bolt.Outer.SetPositions(bolt.Points);
                        bolt.Inner.SetPositions(bolt.Points);
                    }
                    float life = 1f - t * t;
                    bolt.Outer.widthMultiplier = 0.42f * life;
                    bolt.Inner.widthMultiplier = 0.14f * life;
                    var outer = Alpha(color, life);
                    bolt.Outer.startColor = outer;
                    bolt.Outer.endColor = outer;
                    var inner = Alpha(Color.white, life);
                    bolt.Inner.startColor = inner;
                    bolt.Inner.endColor = inner;
                }, 0f, () =>
                {
                    bolt.Root.SetActive(false);
                    _bolts.Push(bolt);
                });
            });
            return delay + duration;
        }

        public float Fly(Sprite sprite, Vector3 from, Vector3 to, float delay, float duration, float scale = 1f, float arc = 0f,
            Color? trail = null, Action arrived = null)
        {
            Tweens.After(delay, () =>
            {
                var r = Take(sprite, Color.white, from, Vector3.one * scale, false, SortOrder + 1);
                var d = to - from;
                var bend = (from + to) * 0.5f + new Vector3(-d.y, d.x, 0f).normalized * arc;
                var last = from;
                Tweens.Run(duration, t =>
                {
                    float e = Ease.InOutSine(t);
                    var a = Vector3.LerpUnclamped(from, bend, e);
                    var b = Vector3.LerpUnclamped(bend, to, e);
                    var pos = Vector3.LerpUnclamped(a, b, e);
                    r.transform.localPosition = pos;
                    if (trail.HasValue) Particles.Trail(pos, trail.Value, (last - pos).normalized);
                    last = pos;
                }, 0f, () =>
                {
                    Release(r);
                    arrived?.Invoke();
                });
            });
            return delay + duration;
        }

        public float Rocket(Sprite sprite, Vector3 from, Vector3 to, Color color, float duration)
        {
            var body = Take(sprite, Color.white, from, Vector3.one * 0.9f, false, SortOrder + 2);
            var head = Take(FxArt.Core(), color, from, Vector3.one * 1.5f, true, SortOrder + 1);
            var back = (from - to).normalized;
            Tweens.Run(duration, t =>
            {
                var pos = Vector3.LerpUnclamped(from, to, Ease.InQuad(t) * 0.6f + t * 0.4f);
                body.transform.localPosition = pos;
                head.transform.localPosition = pos + back * 0.35f;
                head.transform.localScale = Vector3.one * (1.5f + 0.25f * Mathf.Sin(t * 40f));
                Particles.Trail(pos + back * 0.3f, color, back);
                Particles.Trail(pos + back * 0.3f, color, back);
            }, 0f, () =>
            {
                Release(body);
                Release(head);
            });
            return duration;
        }

        public float Strike(Sprite sprite, Vector3 from, Vector3 to, Color color, float duration)
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Fly(sprite, from, to, 0f, duration, 0.85f, side * Mathf.Min(1.4f, (to - from).magnitude * 0.3f), color, () =>
            {
                Particles.Sparks(to, color, 10, 5f);
                Particles.Glints(to, Twinkle, 3, 0.4f, 0.5f);
            });
            Glow(to, Alpha(color, 0.8f), 1.6f, duration, 0.22f);
            Shockwave(to, Alpha(color, 0.8f), 0.15f, 0.8f, duration, 0.25f);
            return duration;
        }

        public float GemBurst(Vector3 pos, Color color, float delay, int chunks = 5)
        {
            Tweens.After(delay, () =>
            {
                Particles.Chunks(pos, color, chunks, 3.4f, 0.2f);
                Particles.Sparks(pos, color, 5, 4.5f);
                Particles.Glints(pos, Twinkle, 2, 0.35f, 0.45f);
            });
            Glow(pos, Alpha(color, 0.85f), 1.5f, delay, 0.24f);
            Shockwave(pos, Alpha(color, 0.6f), 0.15f, 0.72f, delay, 0.26f);
            return delay + 0.26f;
        }

        public float WoodBurst(Vector3 pos, float delay, bool broken)
        {
            Tweens.After(delay, () =>
            {
                Particles.Chunks(pos, Wood, broken ? 7 : 3, 3.6f, 0.24f);
                Particles.Splinters(pos, Wood, broken ? 6 : 3, 4f, 0.3f);
                Particles.Smoke(pos, Dust, broken ? 4 : 2, 0.7f, 0.9f);
            });
            if (broken) Shockwave(pos, Alpha(Dust, 0.9f), 0.2f, 0.85f, delay, 0.26f);
            return delay + 0.26f;
        }

        public float IceBurst(Vector3 pos, float delay)
        {
            Tweens.After(delay, () =>
            {
                Particles.Chunks(pos, Ice, 7, 3.8f, 0.17f);
                Particles.Glints(pos, Ice, 4, 0.45f, 0.5f);
                Particles.Smoke(pos, Mist, 3, 0.7f, 0.7f);
            });
            Glow(pos, Alpha(Ice, 0.7f), 1.5f, delay, 0.24f);
            return delay + 0.26f;
        }

        public float AshBurst(Vector3 pos, float delay)
        {
            Tweens.After(delay, () =>
            {
                Particles.Smoke(pos, Ash, 4, 0.7f, 0.8f);
                Particles.Embers(pos, Ember, 6, 0.25f, 1.2f);
            });
            return delay + 0.26f;
        }

        public float Explosion(Vector3 pos, float radius, float delay)
        {
            Tweens.After(delay, () =>
            {
                Particles.Sparks(pos, Fire, 28, 9f, 0.55f, 0.2f);
                Particles.Embers(pos, Ember, 18, radius * 0.5f, 2.6f);
                Particles.Smoke(pos, Soot, 8, 1.1f, 1.6f);
                Particles.Glints(pos, Twinkle, 6, radius * 0.6f, 0.6f);
            });
            Glow(pos, new Color(1f, 0.95f, 0.8f, 1f), radius * 2.2f, delay, 0.3f, 0.25f);
            Glow(pos, Alpha(Fire, 0.9f), radius * 3f, delay, 0.42f);
            Shockwave(pos, new Color(1f, 0.9f, 0.65f, 1f), 0.3f, radius + 1f, delay, 0.36f);
            Shockwave(pos, Alpha(Fire, 0.7f), 0.2f, radius + 0.4f, delay + 0.07f, 0.34f);
            return delay + 0.42f;
        }

        public float Firework(Vector3 pos, Color color, float delay)
        {
            Tweens.After(delay, () =>
            {
                Particles.Sparks(pos, color, 22, 7f, 0.7f, 0.18f);
                Particles.Glints(pos, Twinkle, 6, 1f, 0.55f);
                Particles.Embers(pos, color, 8, 0.4f, 1.8f);
            });
            Glow(pos, Alpha(color, 0.9f), 2.4f, delay, 0.32f);
            Shockwave(pos, color, 0.3f, 1.7f, delay, 0.4f);
            return delay + 0.4f;
        }

        public float Spark(Vector3 from, Vector3 to, Color color, float delay, float duration)
        {
            Tweens.After(delay, () =>
            {
                var head = Take(FxArt.Core(), Color.Lerp(color, Color.white, 0.5f), from, Vector3.one * 0.9f, true, SortOrder + 2);
                var back = (from - to).normalized;
                Tweens.Run(duration, t =>
                {
                    var pos = Vector3.LerpUnclamped(from, to, Ease.OutQuad(t));
                    head.transform.localPosition = pos;
                    Particles.Trail(pos, color, back);
                }, 0f, () => Release(head));
            });
            return delay + duration;
        }

        public void Sparkles(Vector3 pos, int count, float delay, float spread, float size)
        {
            Tweens.After(delay, () => Particles.Glints(pos, Twinkle, count, spread, size));
        }

        public void Confetti(Vector3 pos, Vector3 direction, int count, float delay)
        {
            Tweens.After(delay, () => Particles.Confetti(pos, direction, count, 13f, 24f));
        }
    }
}
