using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    /// <summary>
    /// Win and lose windows (SCR-005a/b). The window slides up from below (SCR-RULE-02); on a win the stars
    /// fill one after another (ANI-009) over light rays, and the mascot cheers.
    /// </summary>
    public sealed class ResultPopup
    {
        /// <summary>Degrees per second of the light rays behind the stars.</summary>
        const float RaySpin = 6f;

        public string Title { get; private set; }
        public VisualElement Root { get; private set; }
        public IReadOnlyList<Button> Buttons => _buttons;

        readonly List<Button> _buttons = new List<Button>();

        public readonly struct Choice
        {
            public readonly string Label;
            public readonly ButtonKind Kind;
            public readonly Action Run;
            public Choice(string label, ButtonKind kind, Action run) { Label = label; Kind = kind; Run = run; }
        }

        /// <summary>Pass stars below 0 for a window without stars (the lose window).</summary>
        public static ResultPopup Show(VisualElement parent, string title, string message, int stars, params Choice[] buttons)
        {
            var popup = new ResultPopup { Title = title };
            popup.Build(parent, message, stars, buttons);
            return popup;
        }

        void Build(VisualElement parent, string message, int stars, Choice[] choices)
        {
            // The scrim takes every touch, so nothing behind the window reacts.
            Root = Ui.El("et-fill et-scrim et-result", parent, "ResultPopup");
            Root.pickingMode = PickingMode.Position;
            var body = Ui.El("et-result__body", Root, "Body");

            bool win = stars >= 0;
            if (win)
            {
                BuildConfetti();
                BuildStars(body, stars);
            }

            var panel = Ui.El("et-panel et-result__panel", body, "Panel");
            var ribbon = Ui.El("et-ribbon", panel);
            Ui.Text(Title, "et-title", ribbon, "Title");
            Ui.Text(message, "et-ink et-result__message", panel, "Message");
            foreach (var c in choices)
                _buttons.Add(Ui.Button(c.Label, Ui.ClassOf(c.Kind) + " et-result__button", c.Run, panel));

            if (win) Ui.El("et-icon et-result__mascot", body, "Mascot");

            const float from = 1600f;
            body.style.translate = new Translate(0f, from);
            Tweens.Run(Timing.ModalIn, t =>
            {
                if (Ui.Alive(body)) body.style.translate = new Translate(0f, Mathf.LerpUnclamped(from, 0f, Ease.OutCubic(t)));
            });
        }

        void BuildStars(VisualElement body, int stars)
        {
            var rays = Ui.El("et-fx-rays et-result__rays", body, "Rays");
            Ui.El("et-fx-glow et-result__glow", body, "Glow");
            float angle = 0f;
            rays.schedule.Execute(t =>
            {
                angle += RaySpin * t.deltaTime / 1000f;
                rays.style.rotate = new Rotate(angle);
            }).Every(16);

            var row = Ui.El("et-result__stars", body, "Stars");
            string[] sides = { "et-result__star--left", "et-result__star--middle", "et-result__star--right" };
            for (int i = 0; i < 3; i++)
            {
                var slot = Ui.El("et-star et-result__star " + sides[i], row, "Star");
                if (i >= stars) continue;
                var fill = Ui.El("et-fill et-star et-star--on", slot, "Fill");
                fill.style.scale = new Scale(Vector2.zero);
                Tweens.Run(Timing.StarFill, t =>
                {
                    if (Ui.Alive(fill)) fill.style.scale = new Scale(Vector2.one * Ease.OutBack(t));
                }, Timing.ModalIn + i * Timing.StarFill);
            }
        }

        /// <summary>
        /// Paper confetti raining behind the window (design-system "Kutlama katmanları", level 3). Each piece has its own
        /// speed, sway and spin from a fixed sequence, so every win looks alike and a screenshot is repeatable.
        /// </summary>
        void BuildConfetti()
        {
            const int count = 44;
            const float seconds = 4f;
            string[] colors =
            {
                "et-confetti--red", "et-confetti--gold", "et-confetti--green", "et-confetti--blue", "et-confetti--purple",
                "et-confetti--orange", "et-confetti--white",
            };
            string[] shapes = { "", "", "et-confetti--square", "et-confetti--round" };

            var layer = Ui.El("et-fill", null, "Confetti");
            Root.Insert(0, layer);
            var pieces = new VisualElement[count];
            var spots = new Vector4[count]; // x 0..1, delay s, fall speed px/s, sway phase
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Repeat(i * 0.618034f, 1f), b = Mathf.Repeat(i * 0.754878f + 0.3f, 1f), c = Mathf.Repeat(i * 0.569840f + 0.6f, 1f);
                pieces[i] = Ui.El("et-confetti " + colors[i % colors.Length] + " " + shapes[i % shapes.Length], layer);
                pieces[i].style.opacity = 0f;
                spots[i] = new Vector4(a, b * 0.7f, 520f + 420f * c, b * Mathf.PI * 2f);
            }

            float time = 0f;
            layer.schedule.Execute(t =>
            {
                time += t.deltaTime / 1000f;
                float width = layer.resolvedStyle.width, height = layer.resolvedStyle.height;
                if (float.IsNaN(width) || float.IsNaN(height)) return;
                for (int i = 0; i < count; i++)
                {
                    var s = spots[i];
                    float age = time - s.y;
                    if (age < 0f) continue;
                    float y = -60f + age * s.z;
                    float x = s.x * width + Mathf.Sin(age * 3.2f + s.w) * 36f;
                    var e = pieces[i];
                    e.style.translate = new Translate(x, y);
                    e.style.rotate = new Rotate(age * (180f + 260f * s.x) * (i % 2 == 0 ? 1f : -1f));
                    e.style.opacity = y > height * 0.8f ? Mathf.Clamp01((height - y) / (height * 0.2f)) : 1f;
                }
            }).Every(16).Until(() => time > seconds);
        }

        public void Close() => Root.RemoveFromHierarchy();
    }
}
