using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class Ease
    {
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float OutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }
        public static float InOutSine(float t) => 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

        public static float OutBack(float t)
        {
            const float c = 1.70158f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        public static float OutBackSoft(float t)
        {
            const float c = 0.9f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }
    }

    public sealed class Tweens : MonoBehaviour
    {
        sealed class Item
        {
            public float Delay;
            public float Duration;
            public float Time;
            public Action<float> Update;
            public Action Start;
            public Action Done;
            public bool Started;
            public bool Finished;
        }

        static Tweens _instance;
        static readonly Predicate<Item> IsFinished = i => i.Finished;

        readonly List<Item> _items = new List<Item>();

        void Awake() => _instance = this;

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Run(float duration, Action<float> update, float delay = 0f, Action done = null, Action start = null)
        {
            if (_instance == null) return;
            _instance._items.Add(new Item { Delay = delay, Duration = duration, Update = update, Done = done, Start = start });
        }

        public static void After(float delay, Action action) => Run(0f, null, delay, action);

        public static void KillAll()
        {
            if (_instance != null) _instance._items.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            int count = _items.Count;
            for (int i = 0; i < count && i < _items.Count; i++)
            {
                var it = _items[i];
                float step = dt;
                if (it.Delay > 0f)
                {
                    it.Delay -= dt;
                    if (it.Delay > 0f) continue;
                    step = -it.Delay;
                    it.Delay = 0f;
                }
                if (!it.Started)
                {
                    it.Started = true;
                    it.Start?.Invoke();
                    if (_items.Count == 0) return;
                }
                it.Time += step;
                float t = it.Duration <= 0f ? 1f : Mathf.Clamp01(it.Time / it.Duration);
                it.Update?.Invoke(t);
                if (t >= 1f)
                {
                    it.Finished = true;
                    it.Done?.Invoke();
                    if (_items.Count == 0) return;
                }
            }
            _items.RemoveAll(IsFinished);
        }
    }

    public static class Timing
    {
        public const float Swap = 0.14f;
        public const float Pop = 0.11f;
        public const float GravityTick = 0.055f;
        public const float LandSquash = 0.09f;
        public const float ObstacleBreak = 0.2f;
        public const float SpecialForm = 0.3f;
        public const float MergeFlight = 0.16f;
        public const float RocketPerCell = 0.045f;
        public const float BombPerCell = 0.09f;
        public const float HomingPerCell = 0.05f;
        public const float ColorPerCell = 0.04f;
        public const float StrikeFlight = 0.26f;
        public const float Shuffle = 0.35f;
        public const float Collect = 0.28f;
        public const float CounterBounce = 0.1f;
        public const float ModalIn = 0.2f;
        public const float StarFill = 0.2f;
        public const float WinDelay = 0.5f;
        public const float LoseDelay = 1.5f;
        public const float WinSparkInterval = 0.09f;
        public const float WinSparkFlight = 0.32f;
        public const int WinSparksMax = 10;
        public const float WinWindowPause = 0.25f;
        public const float PraisePop = 0.32f;
        public const float PraiseHold = 0.55f;
        public const float PraiseOut = 0.25f;
        public const float HintDelay = 10f;
        public const float EarlyHintDelay = 3f;
        public const int EarlyHintLevels = 3;
        public const float HintPulse = 0.9f;
        public const float HintScale = 0.1f;
    }
}
