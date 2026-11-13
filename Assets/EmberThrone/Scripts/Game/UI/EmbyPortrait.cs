using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public enum EmbyMood
    {
        Idle,
        Worry,
        Panic,
        Joy,
        Wow,
        Sad,
    }

    public sealed class EmbyPortrait
    {
        public const int WorryMoves = 5;
        public const int PanicMoves = 2;
        public const float ReactTime = 1.6f;

        const string Folder = "UI/Art/";
        const float BlinkEvery = 3.6f;
        const float BlinkLength = 0.13f;
        const float HopTime = 0.45f;
        const float HopHeight = 26f;
        const float CheerEvery = 1.3f;
        const long TickMs = 16;

        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        readonly VisualElement _face;
        EmbyMood _base = EmbyMood.Idle;
        EmbyMood _reaction;
        float _reactionEnds = -1f;
        float _hopStart = -10f;
        float _nextBlink;
        bool _won;
        string _shown;

        public VisualElement Root { get; }
        public EmbyMood Mood { get; private set; }

        public EmbyPortrait(VisualElement parent)
        {
            Root = Ui.El("et-ring et-emby", parent, "Emby");
            _face = Ui.El("et-emby__face", Root, "Face");
            _nextBlink = Time.time + BlinkEvery;
            Mood = EmbyMood.Idle;
            Apply(0f);
            Root.schedule.Execute(() => Apply(Time.time)).Every(TickMs);
        }

        public void SetMoves(int left)
        {
            if (_won) return;
            _base = left <= PanicMoves ? EmbyMood.Panic : left <= WorryMoves ? EmbyMood.Worry : EmbyMood.Idle;
            Refresh();
        }

        public void React(EmbyMood mood)
        {
            if (_won) return;
            _reaction = mood;
            _reactionEnds = Time.time + ReactTime;
            if (mood == EmbyMood.Joy || mood == EmbyMood.Wow) _hopStart = Time.time;
            Refresh();
        }

        public void Win()
        {
            _won = true;
            _base = EmbyMood.Joy;
            _reactionEnds = -1f;
            _hopStart = Time.time;
            Refresh();
        }

        public void Lose()
        {
            if (_won) return;
            _base = EmbyMood.Sad;
            _reactionEnds = -1f;
            Refresh();
        }

        void Refresh() => Mood = Time.time < _reactionEnds ? _reaction : _base;

        void Apply(float now)
        {
            if (!Ui.Alive(_face)) return;
            Refresh();
            bool blink = Mood == EmbyMood.Idle && now >= _nextBlink;
            if (now >= _nextBlink + BlinkLength) _nextBlink = now + BlinkEvery * Random.Range(0.7f, 1.3f);
            Show(blink ? "mascot_calm" : FileOf(Mood));

            float breath = Mathf.Sin(now * 2.4f);
            float sx = 1f - 0.012f * breath, sy = 1f + 0.025f * breath;
            float x = 0f, y = 0f, angle = 0f;
            switch (Mood)
            {
                case EmbyMood.Panic:
                    x = Mathf.Sin(now * 42f) * 4f;
                    break;
                case EmbyMood.Worry:
                    angle = Mathf.Sin(now * 3f) * 4f;
                    break;
                case EmbyMood.Sad:
                    sy *= 0.97f;
                    y = 4f;
                    break;
                case EmbyMood.Joy:
                case EmbyMood.Wow:
                    if (_won && now - _hopStart > CheerEvery) _hopStart = now;
                    break;
            }
            float hop = (now - _hopStart) / HopTime;
            if (hop >= 0f && hop < 1f)
            {
                y -= Mathf.Sin(hop * Mathf.PI) * HopHeight;
                float squash = Mathf.Sin(hop * Mathf.PI * 2f) * 0.06f * (1f - hop);
                sx += squash;
                sy -= squash;
            }
            _face.style.translate = new Translate(x, y);
            _face.style.scale = new Scale(new Vector2(sx, sy));
            _face.style.rotate = new Rotate(angle);
        }

        void Show(string file)
        {
            if (file == _shown) return;
            _shown = file;
            _face.style.backgroundImage = new StyleBackground(Load(file));
        }

        static string FileOf(EmbyMood mood)
        {
            switch (mood)
            {
                case EmbyMood.Worry: return "mascot_worry";
                case EmbyMood.Panic: return "mascot_panic";
                case EmbyMood.Joy: return "mascot_joy";
                case EmbyMood.Wow: return "mascot_wow";
                case EmbyMood.Sad: return "mascot_sad";
                default: return "mascot";
            }
        }

        static Texture2D Load(string file)
        {
            if (Textures.TryGetValue(file, out var tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>(Folder + file);
            Textures[file] = tex;
            return tex;
        }
    }
}
