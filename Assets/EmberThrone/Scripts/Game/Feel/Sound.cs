using System;
using System.Text;
using UnityEngine;

namespace EmberThrone.Game
{
    public enum Sfx
    {
        None,
        Swap,
        SwapBack,
        Match,
        SpecialMade,
        Rocket,
        Bomb,
        ColorBomb,
        Combo,
        Firework,
        Collect,
        Hammer,
        LineBlast,
        Shuffle,
        UiTap,
        Win,
        Lose,
        Star,
        MovesAdded,
        Freeze,
        BreakWood,
        BreakIce,
        BreakAsh,
    }

    public static class Sound
    {
        public const string EnabledKey = "et.sound";
        const string Folder = "Audio/";
        const int Voices = 12;
        const float MinGap = 0.04f;

        static readonly int[] Ladder = { 0, 2, 4, 5, 7, 9, 11, 12 };

        static AudioSource[] _sources;
        static AudioClip[] _clips;
        static float[] _last;
        static int _next;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        public static int Count { get; private set; }
        public static Sfx Last { get; private set; }
        public static int Loaded { get; private set; }

        public static void Init(GameObject host)
        {
            var all = (Sfx[])Enum.GetValues(typeof(Sfx));
            _clips = new AudioClip[all.Length];
            _last = new float[all.Length];
            Loaded = 0;
            foreach (var sfx in all)
            {
                if (sfx == Sfx.None) continue;
                _clips[(int)sfx] = Resources.Load<AudioClip>(Folder + FileOf(sfx));
                if (_clips[(int)sfx] != null) Loaded++;
                else Debug.LogWarning("[EmberThrone] Missing sound " + FileOf(sfx));
            }

            _sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
            _next = 0;
        }

        public static float CascadeStep(int wave) => Ladder[Mathf.Clamp(wave - 1, 0, Ladder.Length - 1)];

        public static void Play(Sfx sfx, float semitones = 0f)
        {
            if (sfx == Sfx.None || !Enabled || _clips == null) return;
            int index = (int)sfx;
            float now = Time.unscaledTime;
            if (_last[index] > 0f && now - _last[index] < MinGap) return;
            _last[index] = now;
            Count++;
            Last = sfx;

            var clip = _clips[index];
            if (clip == null || _sources == null) return;
            var source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            if (source == null) return;
            source.pitch = Mathf.Pow(2f, semitones / 12f);
            source.PlayOneShot(clip, VolumeOf(sfx));
        }

        static float VolumeOf(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Swap: return 0.55f;
                case Sfx.UiTap: return 0.6f;
                case Sfx.Match: return 0.7f;
                case Sfx.Collect: return 0.6f;
                case Sfx.Firework: return 0.7f;
                default: return 1f;
            }
        }

        public static string FileOf(Sfx sfx)
        {
            var name = sfx.ToString();
            var sb = new StringBuilder("sfx_");
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
