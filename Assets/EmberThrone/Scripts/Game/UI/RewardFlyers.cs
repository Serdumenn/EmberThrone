using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class RewardFlyers
    {
        public const float Burst = 0.2f;
        public const float Flight = 0.55f;
        public const float Stagger = 0.06f;

        const float Size = 112f;
        const float Spread = 130f;
        const float LandScale = 0.55f;
        const float GoldenAngle = 2.39996f;

        readonly VisualElement _layer;

        public int Launched { get; private set; }
        public int Flying { get; private set; }

        RewardFlyers(VisualElement layer) => _layer = layer;

        public static RewardFlyers Create(VisualElement parent)
        {
            var layer = Ui.El("et-fill", parent, "RewardFlyers");
            layer.pickingMode = PickingMode.Ignore;
            return new RewardFlyers(layer);
        }

        public static int CountOf(Reward reward)
        {
            switch (reward.Kind)
            {
                case RewardKind.Coins: return Mathf.Clamp(reward.Amount / 40, 3, 8);
                case RewardKind.Gems: return Mathf.Clamp(reward.Amount / 2, 2, 6);
                default: return Mathf.Clamp(reward.Amount, 1, 5);
            }
        }

        public static float Duration(int count) => Burst + Flight + Stagger * Mathf.Max(0, count - 1);

        public void Fly(Reward reward, Vector2 fromPanel, VisualElement target, Action<int> landed)
        {
            int count = CountOf(reward);
            int share = reward.Amount / count;
            int rest = reward.Amount - share * count;
            if (!Ui.Alive(target) || !Ui.Alive(_layer))
            {
                landed?.Invoke(reward.Amount);
                return;
            }
            _layer.BringToFront();
            var from = _layer.WorldToLocal(fromPanel);
            var to = _layer.WorldToLocal(target.worldBound.center);
            string icon = Popup.IconOf(reward);
            for (int i = 0; i < count; i++)
            {
                int amount = share + (i == count - 1 ? rest : 0);
                float angle = i * GoldenAngle;
                float reach = Spread * (0.55f + 0.45f * Mathf.Repeat(i * 0.618034f, 1f));
                var scatter = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
                var e = Ui.El("et-icon et-flyer " + icon, _layer, "Flyer");
                e.style.width = Size;
                e.style.height = Size;
                e.style.translate = new Translate(from.x - Size * 0.5f, from.y - Size * 0.5f);
                e.style.scale = new Scale(Vector2.zero);
                Flying++;
                Launched++;
                float total = Burst + Flight;
                float split = Burst / total;
                Tweens.Run(total, t =>
                {
                    if (!Ui.Alive(e)) return;
                    Vector2 p;
                    float s;
                    if (t < split)
                    {
                        float u = Ease.OutCubic(t / split);
                        p = Vector2.LerpUnclamped(from, scatter, u);
                        s = Mathf.Lerp(0.2f, 1.1f, u);
                    }
                    else
                    {
                        float u = (t - split) / (1f - split);
                        float k = u * u * (3f - 2f * u);
                        var bend = Vector2.Lerp(scatter, to, 0.5f) + new Vector2(0f, -Spread);
                        var a = Vector2.LerpUnclamped(scatter, bend, k);
                        var b = Vector2.LerpUnclamped(bend, to, k);
                        p = Vector2.LerpUnclamped(a, b, k);
                        s = Mathf.Lerp(1.1f, LandScale, u);
                    }
                    e.style.translate = new Translate(p.x - Size * 0.5f, p.y - Size * 0.5f);
                    e.style.scale = new Scale(Vector2.one * s);
                }, i * Stagger, () =>
                {
                    Flying--;
                    if (Ui.Alive(e)) e.RemoveFromHierarchy();
                    landed?.Invoke(amount);
                });
            }
        }
    }

    public static class UiBurst
    {
        static readonly string[] Colors =
        {
            "et-confetti--red", "et-confetti--gold", "et-confetti--green", "et-confetti--blue", "et-confetti--purple",
            "et-confetti--orange", "et-confetti--white",
        };

        static readonly string[] Shapes = { "", "et-confetti--square", "et-confetti--round" };

        public static void Confetti(VisualElement layer, Vector2 centerPanel, int count, float seconds)
        {
            if (!Ui.Alive(layer)) return;
            var center = layer.WorldToLocal(centerPanel);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Repeat(i * 0.618034f, 1f) * Mathf.PI * 2f;
                float speed = 700f + 600f * Mathf.Repeat(i * 0.754878f, 1f);
                var velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a) - 0.6f) * speed;
                float spin = (i % 2 == 0 ? 1f : -1f) * (360f + 300f * Mathf.Repeat(i * 0.5698f, 1f));
                var piece = Ui.El("et-confetti " + Colors[i % Colors.Length] + " " + Shapes[i % Shapes.Length], layer, "Confetti");
                piece.style.translate = new Translate(center.x, center.y);
                Tweens.Run(seconds, t =>
                {
                    if (!Ui.Alive(piece)) return;
                    float time = t * seconds;
                    var p = center + velocity * time + new Vector2(0f, 1600f) * (0.5f * time * time);
                    piece.style.translate = new Translate(p.x, p.y);
                    piece.style.rotate = new Rotate(spin * time);
                    piece.style.opacity = 1f - t * t;
                }, 0f, () =>
                {
                    if (Ui.Alive(piece)) piece.RemoveFromHierarchy();
                });
            }
        }
    }
}
