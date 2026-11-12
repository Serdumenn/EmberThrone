using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class HitStop : MonoBehaviour
    {
        const float Slow = 0.05f;

        static HitStop _instance;

        float _until;
        bool _active;

        public static int Count { get; private set; }

        void Awake() => _instance = this;

        void OnDestroy()
        {
            Release();
            if (_instance == this) _instance = null;
        }

        public static void Play(float seconds)
        {
            if (seconds <= 0f || _instance == null || !CameraShake.Enabled) return;
            _instance.Begin(seconds);
        }

        void Begin(float seconds)
        {
            if (!_active)
            {
                if (!Mathf.Approximately(Time.timeScale, 1f)) return;
                Time.timeScale = Slow;
                _active = true;
                Count++;
            }
            _until = Mathf.Max(_until, Time.unscaledTime + seconds);
        }

        void Update()
        {
            if (_active && Time.unscaledTime >= _until) Release();
        }

        void Release()
        {
            if (!_active) return;
            _active = false;
            if (Mathf.Approximately(Time.timeScale, Slow)) Time.timeScale = 1f;
        }
    }
}
