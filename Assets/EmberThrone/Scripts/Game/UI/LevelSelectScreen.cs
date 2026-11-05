using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EmberThrone.Game
{
    /// <summary>Prototype level list, read from Resources/Levels. The map screen (SCR-002) replaces it in Phase 3.</summary>
    public sealed class LevelSelectScreen : MonoBehaviour
    {
        public int ButtonCount { get; private set; }

        public static LevelSelectScreen Create(Transform parent, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var root = Ui.Stretch(Ui.Rect("LevelSelect", parent));
            var screen = root.gameObject.AddComponent<LevelSelectScreen>();
            screen.Build(root, levels, starsOf, onPick);
            return screen;
        }

        void Build(RectTransform root, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var title = Ui.Label("Title", root, "EmberThrone", 118, Palette.Accent);
            title.fontStyle = FontStyle.Bold;
            Ui.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1000f, 150f));

            var subtitle = Ui.Label("Subtitle", root, "Seviye seç", 54, Palette.TextDim);
            Ui.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(1000f, 70f));

            var viewport = Ui.Rect("Viewport", root);
            Ui.Stretch(viewport);
            viewport.offsetMax = new Vector2(0f, -320f);
            viewport.offsetMin = new Vector2(0f, 40f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var content = Ui.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300f, 330f);
            grid.spacing = new Vector2(30f, 30f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(0, 0, 10, 40);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                BuildButton(content, levels[i], starsOf(levels[i]), () => onPick(index));
                ButtonCount++;
            }
        }

        static void BuildButton(Transform parent, LevelDefinition level, int stars, Action onClick)
        {
            var color = stars > 0 ? Palette.PanelLight : Palette.Panel;
            var button = Ui.Button("Level" + level.Id, parent, null, color, onClick);
            var t = button.transform;

            var number = Ui.Label("Number", t, level.Id.ToString(), 104, stars > 0 ? Palette.Gold : Palette.Text);
            number.fontStyle = FontStyle.Bold;
            Ui.Place(number.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(280f, 120f));

            var name = Ui.Label("Name", t, level.Name, 34, Palette.TextDim);
            Ui.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -156f), new Vector2(280f, 84f));

            for (int i = 0; i < 3; i++)
            {
                var star = Ui.Icon("Star", t, Art.StarIcon(), i < stars ? Palette.Gold : Palette.StarEmpty);
                Ui.Place(star.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - 1) * 74f, 24f), new Vector2(68f, 68f));
            }
        }
    }
}
