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
    }

    /// <summary>
    /// Small tween runner (no third-party dependency). Everything runs on scaled time, so tests can
    /// fast-forward with Time.timeScale. One instance lives on the GameApp object.
    /// </summary>
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

        /// <summary>Calls <paramref name="update"/> with 0..1 over <paramref name="duration"/> seconds, after the delay.</summary>
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
            int count = _items.Count; // tweens added by callbacks start next frame
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
                    if (_items.Count == 0) return; // a callback killed everything
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

    /// <summary>Animation lengths. Values come from UX-001 §6.2 (ANI-xxx) unless noted.</summary>
    public static class Timing
    {
        public const float Swap = 0.14f;
        public const float Pop = 0.11f;              // ANI-001
        public const float GravityTick = 0.055f;     // ANI-002: a typical 3-cell fall takes ~165 ms
        public const float LandSquash = 0.09f;
        public const float ObstacleBreak = 0.2f;     // ANI-003
        public const float SpecialForm = 0.3f;       // ANI-004
        public const float MergeFlight = 0.16f;
        public const float RocketPerCell = 0.045f;   // ANI-005: rocket ~400 ms across the board
        public const float BombPerCell = 0.09f;      // ANI-005: bomb ~500 ms
        public const float HomingPerCell = 0.05f;
        public const float ColorPerCell = 0.04f;     // ANI-005: color bomb ~600 ms
        public const float StrikeFlight = 0.26f;
        public const float Shuffle = 0.35f;
        public const float Collect = 0.28f;
        public const float CounterBounce = 0.1f;     // ANI-019
        public const float ModalIn = 0.2f;           // ANI-020
        public const float StarFill = 0.2f;          // ANI-009
        public const float WinDelay = 0.5f;
        public const float LoseDelay = 1.5f;         // SYS-CORE-001 §4.3: fail screen waits at least 1.5 s
    }
}
