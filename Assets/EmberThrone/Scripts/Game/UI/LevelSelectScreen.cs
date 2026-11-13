using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public enum LobbyPage
    {
        Home,
        World,
        Cards,
        Rank,
        Team,
    }

    public sealed class LevelSelectScreen
    {
        const string Locked = "et-map-node--locked";
        const string Current = "et-map-node--current";
        const string Done = "et-map-node--done";

        public VisualElement Root { get; private set; }
        public int ButtonCount => _buttons.Count;
        public IReadOnlyList<Button> Buttons => _buttons;
        public Button SettingsButton { get; private set; }
        public TopBar Top { get; private set; }
        public LobbyBar Lobby { get; private set; }
        public TabBar Tabs { get; private set; }
        public CollectionView Cards { get; private set; }
        public RaceView Race { get; private set; }
        public IReadOnlyList<Button> StoryButtons => _stories;
        public HomeDiorama Diorama { get; private set; }
        public int RestoreFocus => Meta.Restore.Focus(LevelMap.ChapterOf(Mathf.Max(0, CurrentIndex)));
        public TeamView Team { get; private set; }
        public LobbyPage Page { get; private set; }
        public bool MapShown => Page == LobbyPage.World;

        UiRoot _ui;
        GameApp _app;
        public SettingsPopup Settings { get; private set; }
        public int CurrentIndex { get; private set; }
        public ScrollView Scroll { get; private set; }

        readonly List<Button> _buttons = new List<Button>();
        readonly List<bool> _locked = new List<bool>();
        readonly List<Button> _stories = new List<Button>();

        public bool IsLocked(int index) => _locked[index];

        public static LevelSelectScreen Create(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick, GameApp app)
        {
            var screen = new LevelSelectScreen();
            screen.Build(ui, levels, starsOf, onPick, app);
            return screen;
        }

        void Build(UiRoot ui, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick, GameApp app)
        {
            _ui = ui;
            _app = app;
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

            Ui.El("et-map__pad et-map__pad--top", Scroll, "TopPad");
            for (int c = 0; c < LevelMap.Chapters.Length; c++)
                BuildChapter(c, levels, starsOf, onPick);
            Ui.El("et-map__pad et-map__pad--bottom", Scroll, "BottomPad");

            Diorama = HomeDiorama.Create(ui.Backdrop);
            var diorama = Diorama;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => diorama.Root.RemoveFromHierarchy());
            Diorama.Show(RestoreFocus);

            int current = CurrentIndex;
            Top = TopBar.Create(Root, app.OpenLives, app.OpenShop, () => OpenSettings(ui));
            SettingsButton = Top.SettingsButton;
            Lobby = LobbyBar.Create(Root, "Seviye " + levels[current].Id, () => onPick(current), () => ShowMap(true), app.OpenDaily, app.OpenQuests, app.OpenEvent, app.OpenRestore, app.OpenPass, app.OpenContest, app.OpenLadder, () => RestoreFocus, () => Mathf.Max(0, CurrentIndex));
            Tabs = TabBar.Create(Root, app.OpenShop,
                () => Show(LobbyPage.Rank),
                () => ShowMap(false),
                () => Show(LobbyPage.Team),
                () => Show(LobbyPage.Cards));
            Cards = CollectionView.Create(Root, app);
            Race = RaceView.Create(Root);
            Team = TeamView.Create(Root, app);
            Tabs.Root.BringToFront();
            ShowMap(false);

            if (CurrentIndex >= 0) CenterWhenLaidOut(_buttons[CurrentIndex]);
        }

        void BuildChapter(int chapterIndex, IReadOnlyList<LevelDefinition> levels, Func<LevelDefinition, int> starsOf, Action<int> onPick)
        {
            var chapter = LevelMap.Chapters[chapterIndex];
            int first = chapterIndex * LevelMap.LevelsPerChapter;
            int last = chapterIndex == LevelMap.Chapters.Length - 1 ? levels.Count : Mathf.Min(levels.Count, first + LevelMap.LevelsPerChapter);
            if (last <= first) return;

            int done = 0, stars = 0;
            for (int i = first; i < last; i++)
            {
                int s = starsOf(levels[i]);
                if (s > 0) done++;
                stars += s;
            }

            var card = Ui.El(_locked[first] ? "et-panel et-district et-district--locked" : "et-panel et-district", Scroll, "Chapter" + (chapterIndex + 1));
            VisualElement art;
            if (_locked[first] || !StoryScene.Available(chapterIndex))
            {
                art = Ui.El("et-district__art " + chapter.Style, card, "Art");
                if (_locked[first]) Ui.El("et-icon et-district__lock et-ic--lock", art, "Lock");
            }
            else
            {
                int index = chapterIndex;
                var story = Ui.Button(null, "et-pressable et-district__art " + chapter.Style, () => _app.PlayStory(index), card, "Art");
                Ui.El("et-icon et-district__play et-ic--video", story, "Play");
                _stories.Add(story);
                art = story;
            }
            Ui.Text(chapter.Title, "et-chip et-district__title", art, "Title");

            var progress = Ui.El("et-district__progress", card, "Progress");
            var bar = Ui.El("et-progress et-district__bar", progress, "Bar");
            var fill = Ui.El("et-progress__fill", bar, "Fill");
            fill.style.width = Length.Percent(100f * done / (last - first));
            fill.EnableInClassList(Ui.Hidden, done == 0);
            Ui.Text(done + "/" + (last - first), "et-progress__text", bar, "Text");
            Ui.El("et-icon et-district__star et-ic--star", progress);
            Ui.Text(stars.ToString(), "et-ink et-district__stars", progress, "Stars");

            var grid = Ui.El("et-district__grid", card, "Grid");
            for (int i = first; i < last; i++)
                _buttons[i] = BuildNode(grid, levels[i], i, starsOf(levels[i]), onPick);
        }

        Button BuildNode(VisualElement area, LevelDefinition level, int index, int stars, Action<int> onPick)
        {
            bool locked = _locked[index];
            Button node = null;
            node = Ui.Button(null, "et-pressable et-map-node", () =>
            {
                if (_locked[index]) Shake(node);
                else onPick(index);
            }, area, "Level" + level.Id);
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

        public void ShowMap(bool show) => Show(show ? LobbyPage.World : LobbyPage.Home);

        public void Show(LobbyPage page)
        {
            Page = page;
            bool home = page == LobbyPage.Home;
            Scroll.EnableInClassList(Ui.Hidden, page != LobbyPage.World);
            Cards.Root.EnableInClassList(Ui.Hidden, page != LobbyPage.Cards);
            Race.Root.EnableInClassList(Ui.Hidden, page != LobbyPage.Rank);
            Team.Root.EnableInClassList(Ui.Hidden, page != LobbyPage.Team);
            Lobby.SetShown(home);
            Tabs.SetActive(TabOf(page));
            _ui.Backdrop.EnableInClassList("et-backdrop--map", !home);
            _ui.Backdrop.EnableInClassList("et-backdrop--home", home);
            Diorama.Root.EnableInClassList(Ui.Hidden, !home);
            if (page == LobbyPage.Cards) Cards.Refresh();
            if (page == LobbyPage.Team) Team.Refresh();
            if (page == LobbyPage.Rank)
            {
                _app.ShowRaceResult();
                Race.Refresh();
                Race.ShowMine();
            }
            if (page == LobbyPage.World && CurrentIndex >= 0) Scroll.schedule.Execute(() => Center(_buttons[CurrentIndex]));
        }

        Button TabOf(LobbyPage page)
        {
            switch (page)
            {
                case LobbyPage.Cards: return Tabs.CardsTab;
                case LobbyPage.Rank: return Tabs.RankTab;
                case LobbyPage.Team: return Tabs.TeamTab;
                default: return Tabs.HomeTab;
            }
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
            float y = node.worldBound.center.y - Scroll.contentContainer.worldBound.yMin;
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
