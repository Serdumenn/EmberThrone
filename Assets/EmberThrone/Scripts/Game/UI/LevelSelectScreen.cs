using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class LevelSelectScreen
    {
        public VisualElement Root { get; private set; }
        public int ButtonCount => _buttons.Count;
        public IReadOnlyList<Button> Buttons => _buttons;

        readonly List<Button> _buttons = new List<Button>();

        public static LevelSelectScreen Create(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var screen = new LevelSelectScreen();
            screen.Build(ui, levels, starsOf, onPick);
            return screen;
        }

        void Build(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            ui.Backdrop.AddToClassList("et-backdrop--map");
            Root = Ui.El("et-levels", ui.Content, "LevelSelect");

            var header = Ui.El("et-levels__header", Root);
            Ui.Text("EmberThrone", "et-title et-title--display", header, "Title");
            Ui.Text("Seviye seç", "et-chip et-levels__subtitle", header, "Subtitle");

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "Scroll" };
            scroll.AddToClassList("et-levels__scroll");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(scroll);
            var grid = Ui.El("et-levels__grid", scroll, "Grid");

            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                _buttons.Add(BuildCard(grid, levels[i], starsOf(levels[i]), () => onPick(index)));
            }
        }

        static Button BuildCard(VisualElement parent, LevelDefinition level, int stars, Action onClick)
        {
            var card = Ui.Button(null, "et-panel et-pressable et-level-card", onClick, parent, "Level" + level.Id);
            Ui.Text(level.Id.ToString(), "et-ink et-level-card__number", card, "Number");
            Ui.Text(level.Name, "et-ink et-ink--muted et-caption et-level-card__name", card, "Name");
            var row = Ui.El("et-level-card__stars", card);
            for (int i = 0; i < 3; i++)
                Ui.El(i < stars ? "et-star et-star--on et-level-card__star" : "et-star et-level-card__star", row, "Star");
            return card;
        }
    }
}
