using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class FxParticles
    {
        public const int OrderSmoke = 28;
        public const int OrderSolid = 31;
        public const int OrderLight = 33;
        public const int OrderAmbient = -50;

        static readonly Color[] ConfettiColors =
        {
            Palette.Hex("FF5A4E"), Palette.Hex("FFD24A"), Palette.Hex("5BD66B"), Palette.Hex("4DA3FF"),
            Palette.Hex("B978F2"), Palette.Hex("FF9A3D"), Palette.Hex("FFFFFF"),
        };

        readonly Transform _root;
        readonly ParticleSystem _sparks;
        readonly ParticleSystem _glints;
        readonly ParticleSystem _chunks;
        readonly ParticleSystem _splinters;
        readonly ParticleSystem _smoke;
        readonly ParticleSystem _embers;
        readonly ParticleSystem _confetti;

        public int Emitted { get; private set; }

        public int Alive =>
            _sparks.particleCount + _glints.particleCount + _chunks.particleCount + _splinters.particleCount +
            _smoke.particleCount + _embers.particleCount + _confetti.particleCount;

        public FxParticles(Transform root)
        {
            _root = root;

            _sparks = Make("Sparks", FxArt.ParticleAdditive(FxArt.Core()), OrderLight, 900, 0.55f, 2.4f);
            Fade(_sparks, 0f, 1f, 0.45f);
            Size(_sparks, new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
            var sparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.velocityScale = 0.05f;
            sparkRenderer.lengthScale = 1.5f;
            sparkRenderer.cameraVelocityScale = 0f;

            _glints = Make("Glints", FxArt.ParticleAdditive(FxArt.Flare()), OrderLight + 1, 300, 0f, 0f);
            Size(_glints, new Keyframe(0f, 0f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));

            _chunks = Make("Chunks", FxArt.ParticleBlend(FxArt.Chunks()), OrderSolid, 900, 1.5f, 0.7f);
            Fade(_chunks, 0f, 1f, 0.7f);
            Size(_chunks, new Keyframe(0f, 1f), new Keyframe(1f, 0.7f));
            var sheet = _chunks.textureSheetAnimation;
            sheet.enabled = true;
            sheet.numTilesX = FxArt.ChunkTiles;
            sheet.numTilesY = FxArt.ChunkTiles;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, FxArt.ChunkTiles * FxArt.ChunkTiles - 0.01f);
            sheet.cycleCount = 1;

            _splinters = Make("Splinters", FxArt.ParticleBlend(Art.Splinter()), OrderSolid, 300, 1.5f, 0.7f);
            Fade(_splinters, 0f, 1f, 0.7f);

            _smoke = Make("Smoke", FxArt.ParticleBlend(FxArt.Smoke()), OrderSmoke, 300, -0.03f, 1.6f);
            Fade(_smoke, 0.12f, 0.9f, 0.3f);
            Size(_smoke, new Keyframe(0f, 0.55f), new Keyframe(1f, 1.6f));

            _embers = Make("Embers", FxArt.ParticleAdditive(FxArt.Glow()), OrderLight, 500, -0.1f, 1.2f);
            Fade(_embers, 0.08f, 1f, 0.5f);
            Size(_embers, new Keyframe(0f, 1f), new Keyframe(1f, 0.2f));
            Wander(_embers, 0.5f, 0.7f);

            _confetti = Make("Confetti", FxArt.ParticleBlend(FxArt.Confetti()), OrderLight + 2, 500, 0.5f, 1.5f);
            Fade(_confetti, 0f, 1f, 0.85f);
            var confettiMain = _confetti.main;
            confettiMain.startRotation3D = true;
            confettiMain.startSize3D = true;
            var spin = _confetti.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-7f, 7f);
            spin.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
            spin.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
        }

        ParticleSystem Make(string name, Material material, int order, int max, float gravity, float drag)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            return Configure(go.AddComponent<ParticleSystem>(), material, order, max, gravity, drag);
        }

        static ParticleSystem Configure(ParticleSystem ps, Material material, int order, int max, float gravity, float drag)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.startSize = 1f;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            if (drag > 0f)
            {
                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.limit = 1000f;
                limit.dampen = 0f;
                limit.drag = drag;
                limit.multiplyDragByParticleSize = false;
                limit.multiplyDragByParticleVelocity = false;
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.maxParticleSize = 4f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }

        static void Fade(ParticleSystem ps, float fadeInUntil, float peak, float holdUntil)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            var alpha = fadeInUntil > 0f
                ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, fadeInUntil), new GradientAlphaKey(peak, holdUntil), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(peak, 0f), new GradientAlphaKey(peak, holdUntil), new GradientAlphaKey(0f, 1f) };
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, alpha);
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        static void Size(ParticleSystem ps, params Keyframe[] keys)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        static void Wander(ParticleSystem ps, float strength, float frequency)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = strength;
            noise.frequency = frequency;
            noise.scrollSpeed = 0.4f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        public static ParticleSystem Ambient(Transform parent)
        {
            var go = new GameObject("AmbientEmbers");
            go.transform.SetParent(parent, false);
            var ps = Configure(go.AddComponent<ParticleSystem>(), FxArt.ParticleAdditive(FxArt.Glow()), OrderAmbient, 80, -0.012f, 0f);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.prewarm = true;
            main.duration = 8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.2f, 0.8f), new Color(1f, 0.9f, 0.55f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1f, 1f, 0.01f);

            var drift = ps.velocityOverLifetime;
            drift.enabled = true;
            drift.space = ParticleSystemSimulationSpace.Local;
            drift.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            drift.y = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            drift.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            Fade(ps, 0.2f, 1f, 0.7f);
            Wander(ps, 0.25f, 0.35f);
            ps.Play();
            return ps;
        }

        void Emit(ParticleSystem ps, Vector3 pos, Vector3 velocity, float size, float life, Color color, float rotation = 0f, float spin = 0f)
        {
            var e = new ParticleSystem.EmitParams
            {
                position = _root.TransformPoint(pos),
                velocity = velocity,
                startSize = size,
                startLifetime = life,
                startColor = color,
                rotation = rotation,
                angularVelocity = spin,
            };
            ps.Emit(e, 1);
            Emitted++;
        }

        static Vector3 Around(int i, int count, float up)
        {
            float a = (i + Random.value) / count * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(a), Mathf.Sin(a) + up, 0f);
        }

        static Color Hot(Color color, float amount)
        {
            var c = Color.Lerp(color, Color.white, amount);
            c.a = color.a;
            return c;
        }

        public void Sparks(Vector3 pos, Color color, int count, float speed, float life = 0.4f, float size = 0.16f, float up = 0.15f)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Around(i, count, up);
                Emit(_sparks, pos + dir * 0.12f, dir * (speed * Random.Range(0.4f, 1f)), size * Random.Range(0.7f, 1.3f),
                    life * Random.Range(0.7f, 1.25f), Hot(color, 0.4f));
            }
        }

        public void Glints(Vector3 pos, Color color, int count, float spread, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var at = pos + (Vector3)(Random.insideUnitCircle * spread);
                Emit(_glints, at, Vector3.up * 0.25f, size * Random.Range(0.6f, 1.2f), Random.Range(0.3f, 0.55f), Hot(color, 0.7f),
                    Random.Range(0f, 90f), Random.Range(-140f, 140f));
            }
        }

        public void Chunks(Vector3 pos, Color color, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Around(i, count, 0.55f);
                var tint = Color.Lerp(color, Random.value < 0.5f ? Color.white : Color.black, Random.Range(0f, 0.22f));
                tint.a = 1f;
                Emit(_chunks, pos + (Vector3)(Random.insideUnitCircle * 0.2f), dir * (speed * Random.Range(0.45f, 1.1f)),
                    size * Random.Range(0.7f, 1.35f), Random.Range(0.45f, 0.75f), tint, Random.Range(0f, 360f), Random.Range(-620f, 620f));
            }
        }

        public void Splinters(Vector3 pos, Color color, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Around(i, count, 0.5f);
                var tint = Color.Lerp(color, Color.black, Random.Range(0f, 0.3f));
                tint.a = 1f;
                Emit(_splinters, pos + (Vector3)(Random.insideUnitCircle * 0.2f), dir * (speed * Random.Range(0.5f, 1.1f)),
                    size * Random.Range(0.7f, 1.3f), Random.Range(0.45f, 0.8f), tint, Random.Range(0f, 360f), Random.Range(-720f, 720f));
            }
        }

        public void Smoke(Vector3 pos, Color color, int count, float size, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Around(i, count, 0.3f);
                Emit(_smoke, pos + dir * 0.1f, dir * (speed * Random.Range(0.4f, 1f)), size * Random.Range(0.7f, 1.3f),
                    Random.Range(0.45f, 0.85f), color, Random.Range(0f, 360f), Random.Range(-60f, 60f));
            }
        }

        public void Embers(Vector3 pos, Color color, int count, float spread, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var dir = Around(i, count, 0.6f);
                Emit(_embers, pos + (Vector3)(Random.insideUnitCircle * spread), dir * (speed * Random.Range(0.3f, 1f)),
                    Random.Range(0.07f, 0.17f), Random.Range(0.6f, 1.3f), Hot(color, 0.25f));
            }
        }

        public void Trail(Vector3 pos, Color color, Vector3 back)
        {
            Emit(_embers, pos + (Vector3)(Random.insideUnitCircle * 0.12f), back * Random.Range(0.4f, 1.6f) + (Vector3)(Random.insideUnitCircle * 0.5f),
                Random.Range(0.1f, 0.24f), Random.Range(0.25f, 0.5f), Hot(color, 0.35f));
            if (Random.value < 0.5f)
                Emit(_sparks, pos, back * Random.Range(1f, 3f) + (Vector3)(Random.insideUnitCircle * 1.6f), Random.Range(0.08f, 0.14f),
                    Random.Range(0.2f, 0.4f), Hot(color, 0.5f));
        }

        public void Confetti(Vector3 pos, Vector3 direction, int count, float speed, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(-spread, spread) * Mathf.Deg2Rad;
                var dir = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg) * direction;
                var e = new ParticleSystem.EmitParams
                {
                    position = _root.TransformPoint(pos + (Vector3)(Random.insideUnitCircle * 0.3f)),
                    velocity = dir * (speed * Random.Range(0.45f, 1f)),
                    startSize3D = new Vector3(Random.Range(0.16f, 0.26f), Random.Range(0.1f, 0.16f), 1f),
                    startLifetime = Random.Range(1.6f, 2.8f),
                    startColor = ConfettiColors[Random.Range(0, ConfettiColors.Length)],
                    rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                };
                _confetti.Emit(e, 1);
                Emitted++;
            }
        }
    }
}
