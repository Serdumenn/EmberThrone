using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class LevelSelectScreen
    {
        const string Locked = "et-map-node--locked";
        const string Current = "et-map-node--current";
        const string Done = "et-map-node--done";

        public VisualElement Root { get; private set; }
        public int ButtonCount => _buttons.Count;
        public IReadOnlyList<Button> Buttons => _buttons;
        public Button SettingsButton { get; private set; }
        public SettingsPopup Settings { get; private set; }
        public int CurrentIndex { get; private set; }
        public ScrollView Scroll { get; private set; }

        readonly List<Button> _buttons = new List<Button>();
        readonly List<bool> _locked = new List<bool>();

        public bool IsLocked(int index) => _locked[index];

        public static LevelSelectScreen Create(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var screen = new LevelSelectScreen();
            screen.Build(ui, levels, starsOf, onPick);
            return screen;
        }

        void Build(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            ui.Backdrop.AddToClassList("et-backdrop--map");
            Root = Ui.El("et-map", ui.Content, "LevelSelect");

            Scroll = new ScrollView(ScrollViewMode.Vertical) { name = "Scroll" };
            Scroll.AddToClassList("et-map__scroll");
            Scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(Scroll);

            CurrentIndex = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                bool locked = i > 0 && starsOf(levels[i - 1]) <= 0;
                if (!locked && starsOf(levels[i]) <= 0 && CurrentIndex < 0) CurrentIndex = i;
                _locked.Add(locked);
                _buttons.Add(null);
            }
            if (CurrentIndex < 0) CurrentIndex = levels.Count - 1;

            for (int c = LevelMap.Chapters.Length - 1; c >= 0; c--)
                BuildChapter(c, levels, starsOf, onPick);

            SettingsButton = Ui.Button(null, "et-ring et-pressable et-map__settings", () => OpenSettings(ui), Root, "SettingsButton");
            Ui.El("et-icon et-map__settings-icon", SettingsButton);

            if (CurrentIndex >= 0) CenterWhenLaidOut(_buttons[CurrentIndex]);
        }

        void BuildChapter(int chapterIndex, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var chapter = LevelMap.Chapters[chapterIndex];
            var area = Ui.El("et-map__chapter " + chapter.Style, Scroll, "Chapter" + (chapterIndex + 1));

            int first = chapterIndex * LevelMap.LevelsPerChapter;
            int last = chapterIndex == LevelMap.Chapters.Length - 1 ? levels.Count : Mathf.Min(levels.Count, first + LevelMap.LevelsPerChapter);
            if (last > first)
            {
                var spots = chapter.Spots(last - first);
                for (int i = first; i < last; i++)
                    _buttons[i] = BuildNode(area, levels[i], i, spots[i - first], starsOf(levels[i]), onPick);
            }

            if (chapterIndex == 0)
                Ui.Text(chapter.Title, "et-chip et-map__banner et-map__banner--first", area, "Banner");
            if (chapterIndex + 1 < LevelMap.Chapters.Length)
            {
                var next = LevelMap.Chapters[chapterIndex + 1];
                Ui.Text(next.Title, next.BannerRight ? "et-chip et-map__banner et-map__banner--right" : "et-chip et-map__banner", area, "NextBanner");
            }
            if (CurrentIndex >= first && CurrentIndex < last) _buttons[CurrentIndex].BringToFront();
        }

        Button BuildNode(VisualElement area, LevelDefinition level, int index, Vector2 spot, int stars, Action<int> onPick)
        {
            bool locked = _locked[index];
            Button node = null;
            node = Ui.Button(null, "et-pressable et-map-node", () =>
            {
                if (_locked[index]) Shake(node);
                else onPick(index);
            }, area, "Level" + level.Id);
            node.style.left = Length.Percent(spot.x);
            node.style.top = Length.Percent(spot.y);
            node.EnableInClassList(Locked, locked);
            node.EnableInClassList(Current, index == CurrentIndex);
            node.EnableInClassList(Done, stars > 0 && !locked);

            Ui.El("et-map-node__ring", node, "Ring").pickingMode = PickingMode.Ignore;
            Ui.Text(level.Id.ToString(), "et-outlined et-map-node__number", node, "Number");
            var row = Ui.El("et-map-node__stars", node, "Stars");
            row.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < 3; i++)
                Ui.El(i < stars && !locked ? "et-star et-star--on et-map-node__star" : "et-star et-map-node__star", row, "Star").pickingMode = PickingMode.Ignore;
            if (index == CurrentIndex) Ui.El("et-icon et-map-node__mascot", node, "Mascot").pickingMode = PickingMode.Ignore;
            return node;
        }

        void CenterWhenLaidOut(VisualElement node)
        {
            EventCallback<GeometryChangedEvent> onLayout = null;
            onLayout = e =>
            {
                Center(node);
                Scroll.schedule.Execute(() => Center(node));
            };
            Scroll.contentContainer.RegisterCallback(onLayout);
            Scroll.RegisterCallback(onLayout);
        }

        void Center(VisualElement node)
        {
            if (!Ui.Alive(node)) return;
            float view = Scroll.contentViewport.layout.height;
            float content = Scroll.contentContainer.layout.height;
            if (float.IsNaN(view) || float.IsNaN(content) || view <= 0f || content <= view) return;
            float y = node.parent.layout.y + node.layout.y;
            Scroll.scrollOffset = new Vector2(0f, Mathf.Clamp(y - view * 0.5f, 0f, content - view));
        }

        static void Shake(VisualElement e)
        {
            Tweens.Run(Timing.CounterBounce * 2f, t =>
            {
                if (Ui.Alive(e)) e.style.rotate = new Rotate(Mathf.Sin(t * Mathf.PI * 4f) * 8f * (1f - t));
            }, 0f, () =>
            {
                if (Ui.Alive(e)) e.style.rotate = StyleKeyword.Null;
            });
        }

        void OpenSettings(UiRoot ui)
        {
            if (Settings != null) return;
            Settings = SettingsPopup.Show(ui.Popups, () => Settings = null);
        }
    }
}
