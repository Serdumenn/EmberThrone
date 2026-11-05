using System;
using UnityEngine;
using UnityEngine.UI;

namespace EmberThrone.Game
{
    /// <summary>Win and lose windows (SCR-005a/b). The panel slides up from below (SCR-RULE-02).</summary>
    public sealed class ResultPopup : MonoBehaviour
    {
        public string Title { get; private set; }

        public readonly struct Choice
        {
            public readonly string Label;
            public readonly Color Color;
            public readonly Action Run;
            public Choice(string label, Color color, Action run) { Label = label; Color = color; Run = run; }
        }

        /// <summary>Pass stars below 0 to hide the star row.</summary>
        public static ResultPopup Show(Transform parent, string title, string message, int stars, params Choice[] buttons)
        {
            var dim = Ui.Panel("ResultPopup", parent, Palette.Dim, false);
            dim.raycastTarget = true; // blocks everything behind the window
            Ui.Stretch(dim.rectTransform);
            var popup = dim.gameObject.AddComponent<ResultPopup>();
            popup.Title = title;

            const float buttonHeight = 150f;
            float height = 380f + (stars >= 0 ? 190f : 0f) + buttons.Length * (buttonHeight + 26f);
            var panel = Ui.Panel("Panel", dim.transform, Palette.Panel);
            var rt = panel.rectTransform;
            Ui.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, height));

            float y = -60f;
            var titleText = Ui.Label("Title", rt, title, 92, Palette.Text);
            titleText.fontStyle = FontStyle.Bold;
            Ui.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(820f, 120f));
            y -= 130f;

            var body = Ui.Label("Message", rt, message, 46, Palette.TextDim);
            Ui.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(800f, 120f));
            y -= 130f;

            if (stars >= 0)
            {
                popup.BuildStars(rt, stars, y);
                y -= 190f;
            }

            foreach (var b in buttons)
            {
                var run = b.Run;
                var button = Ui.Button(b.Label, rt, b.Label, b.Color, run);
                Ui.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(740f, buttonHeight));
                y -= buttonHeight + 26f;
            }

            var from = new Vector2(0f, -1600f);
            rt.anchoredPosition = from;
            Tweens.Run(Timing.ModalIn, t =>
            {
                if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, Vector2.zero, Ease.OutCubic(t));
            });
            return popup;
        }

        void BuildStars(RectTransform parent, int stars, float y)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 180f;
                var empty = Ui.Icon("StarEmpty", parent, Art.StarIcon(), Palette.StarEmpty);
                Ui.Place(empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(160f, 160f));
                if (i >= stars) continue;
                var fill = Ui.Icon("Star", parent, Art.StarIcon(), Palette.Gold);
                var rt = fill.rectTransform;
                Ui.Place(rt, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(160f, 160f));
                rt.localScale = Vector3.zero;
                // ANI-009: one star after another
                Tweens.Run(Timing.StarFill, t =>
                {
                    if (rt != null) rt.localScale = Vector3.one * Ease.OutBack(t);
                }, Timing.ModalIn + i * Timing.StarFill);
            }
        }

        public void Close() => Destroy(gameObject);
    }
}
