using System;
using UnityEngine;

namespace EmberThrone.Game
{
    public enum HapticLevel
    {
        Light,
        Medium,
        Strong,
    }

    /// <summary>
    /// Short vibrations on Android through the system Vibrator (amplitude control on Android 8+, plain duration
    /// before). Elsewhere it only counts, so tests can check which events buzz. A failure never reaches the game.
    /// </summary>
    public static class Haptics
    {
        const string EnabledKey = "et.haptics";
        /// <summary>Matches in one cascade come in bursts; one buzz per this many seconds is enough.</summary>
        const float MinGap = 0.06f;

        static AndroidJavaObject _vibrator;
        static bool _amplitude;
        static bool _initialized;
        static float _last = -1f;

        /// <summary>Player setting (Settings screen, Phase 3). On by default.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        /// <summary>How many pulses were asked for (tests).</summary>
        public static int Count { get; private set; }
        public static HapticLevel LastLevel { get; private set; }

        public static void Play(HapticLevel level)
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            if (level == HapticLevel.Light && now - _last < MinGap) return;
            _last = now;
            Count++;
            LastLevel = level;
            if (Application.platform != RuntimePlatform.Android) return;

            switch (level)
            {
                case HapticLevel.Light: Vibrate(12, 70); break;
                case HapticLevel.Medium: Vibrate(24, 150); break;
                default: Vibrate(45, 255); break;
            }
        }

        static void Vibrate(long milliseconds, int amplitude)
        {
            try
            {
                if (!_initialized) Init();
                if (_vibrator == null) return;
                if (_amplitude)
                {
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EmberThrone] Vibration failed, turning it off for this session: " + e.Message);
                _vibrator = null;
            }
        }

        static void Init()
        {
            _initialized = true;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _amplitude = version.GetStatic<int>("SDK_INT") >= 26 && _vibrator != null && _vibrator.Call<bool>("hasAmplitudeControl");
        }

        /// <summary>
        /// Never called. Unity adds the Android VIBRATE permission only when a script references Handheld.Vibrate,
        /// so this reference keeps the permission in the manifest.
        /// </summary>
        static void KeepVibratePermission() => Handheld.Vibrate();
    }
}
