using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class GoalFlyers
    {
        public const int MaxAtOnce = 14;
        public const float Flight = 0.5f;

        const float LandSize = 80f;
        const float Bend = 150f;

        readonly VisualElement _layer;
        int _flying;
        int _run;

        public int Launched { get; private set; }

        GoalFlyers(VisualElement layer) => _layer = layer;

        public static GoalFlyers Create(VisualElement parent) => new GoalFlyers(Ui.El("et-fill", parent, "GoalFlyers"));

        public bool Fly(Sprite sprite, Vector2 fromPanel, float size, VisualElement target, Action arrived)
        {
            if (_flying >= MaxAtOnce || !Ui.Alive(target) || !Ui.Alive(_layer)) return false;
            var from = _layer.WorldToLocal(fromPanel);
            var to = _layer.WorldToLocal(target.worldBound.center);
            var d = to - from;
            float side = Launched % 2 == 0 ? 1f : -1f;
            var bend = (from + to) * 0.5f + new Vector2(-d.y, d.x).normalized * (Bend * side);

            var e = Ui.Image(sprite, "et-flyer", _layer);
            e.style.width = size;
            e.style.height = size;
            e.style.translate = new Translate(from.x - size * 0.5f, from.y - size * 0.5f);
            float land = LandSize / size;
            _flying++;
            Launched++;
            int run = _run;
            Tweens.Run(Flight, t =>
            {
                if (run != _run || !Ui.Alive(e)) return;
                float k = Ease.InOutSine(t);
                var a = Vector2.LerpUnclamped(from, bend, k);
                var b = Vector2.LerpUnclamped(bend, to, k);
                var p = Vector2.LerpUnclamped(a, b, k);
                e.style.translate = new Translate(p.x - size * 0.5f, p.y - size * 0.5f);
                e.style.scale = new Scale(Vector2.one * (Mathf.Lerp(1f, land, t) + 0.25f * Mathf.Sin(t * Mathf.PI)));
            }, 0f, () =>
            {
                if (run != _run) return;
                _flying--;
                e.RemoveFromHierarchy();
                arrived?.Invoke();
            });
            return true;
        }

        public void Clear()
        {
            _run++;
            _flying = 0;
            _layer.Clear();
        }
    }
}
