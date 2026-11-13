using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class TopBar
    {
        Label _lifeCount;
        Label _lifeText;
        Label _coins;
        Label _gems;
        Label _warn;
        int _heldCoins;
        int _heldGems;
        int _heldLives;

        public VisualElement Root { get; private set; }
        public Button LivesPill { get; private set; }
        public Button CoinsPill { get; private set; }
        public Button GemsPill { get; private set; }
        public Button SettingsButton { get; private set; }
        public string LivesText => _lifeCount.text;
        public string LifeTimerText => _lifeText.text;
        public string CoinsText => _coins.text;
        public string GemsText => _gems.text;
        public bool CapWarningShown => !_warn.ClassListContains(Ui.Hidden);
        public bool Holding => _heldCoins > 0 || _heldGems > 0 || _heldLives > 0;

        public static TopBar Create(VisualElement parent, Action onLives, Action onCoins, Action onSettings)
        {
            var bar = new TopBar();
            bar.Build(parent, onLives, onCoins, onSettings);
            return bar;
        }

        void Build(VisualElement parent, Action onLives, Action onCoins, Action onSettings)
        {
            Root = Ui.El("et-topbar", parent, "TopBar");
            LivesPill = Pill("Lives", "et-ic--heart", onLives, true, out _lifeText);
            _lifeCount = Ui.Text("", "et-outlined et-pill__count", LivesPill, "Count");
            CoinsPill = Pill("Coins", "et-ic--coin", onCoins, true, out _coins);
            _warn = Ui.Text("Harca!", "et-tag et-outlined et-pill__warn " + Ui.Hidden, CoinsPill, "Warn");
            GemsPill = Pill("Gems", "et-ic--gem", onCoins, false, out _gems);
            SettingsButton = Ui.Button(null, "et-ring et-pressable et-topbar__gear", onSettings, Root, "SettingsButton");
            Ui.El("et-icon et-topbar__gear-icon et-ic--gear", SettingsButton);

            Refresh();
            Meta.Changed += Refresh;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= Refresh);
            Root.schedule.Execute(Refresh).Every(500);
        }

        Button Pill(string name, string icon, Action onTap, bool plus, out Label text)
        {
            var pill = Ui.Button(null, "et-pressable et-pill", onTap, Root, name);
            Ui.El("et-icon et-pill__icon " + icon, pill);
            text = Ui.Text("", "et-ink et-pill__text", pill, "Text");
            if (plus) Ui.El("et-icon et-pill__plus et-ic--plus", pill);
            return pill;
        }

        public VisualElement TargetOf(RewardKind kind)
        {
            switch (kind)
            {
                case RewardKind.Coins: return CoinsPill;
                case RewardKind.Gems: return GemsPill;
                case RewardKind.Life: return LivesPill;
                default: return null;
            }
        }

        public void Hold(RewardKind kind, int amount)
        {
            if (amount <= 0) return;
            if (kind == RewardKind.Coins) _heldCoins += amount;
            else if (kind == RewardKind.Gems) _heldGems += amount;
            else if (kind == RewardKind.Life) _heldLives += amount;
            Refresh();
        }

        public void Release(RewardKind kind, int amount)
        {
            if (kind == RewardKind.Coins) _heldCoins = Mathf.Max(0, _heldCoins - amount);
            else if (kind == RewardKind.Gems) _heldGems = Mathf.Max(0, _heldGems - amount);
            else if (kind == RewardKind.Life) _heldLives = Mathf.Max(0, _heldLives - amount);
            Refresh();
            var pill = TargetOf(kind);
            if (pill != null) Popup.Bump(pill, 0.12f, 0.14f);
        }

        public void Refresh()
        {
            var economy = Meta.Economy;
            _lifeCount.text = Mathf.Max(0, economy.Lives - _heldLives).ToString();
            _lifeText.text = economy.LivesFull && _heldLives == 0 ? "Dolu" : Meta.Countdown(economy.SecondsToNextLife(Meta.Now));
            _coins.text = Mathf.Max(0, economy.Coins - _heldCoins).ToString();
            _gems.text = Mathf.Max(0, economy.Gems - _heldGems).ToString();
            var rules = economy.Rules;
            _warn.EnableInClassList(Ui.Hidden, economy.Coins * 100L < (long)rules.CoinCap * rules.CoinWarnPercent);
        }
    }

    public sealed class LobbyBar
    {
        Label _giftBadge;
        Label _questBadge;

        public VisualElement Root { get; private set; }
        public VisualElement Side { get; private set; }
        public Button PlayButton { get; private set; }
        public Button WorldButton { get; private set; }
        public Button GiftButton { get; private set; }
        public Button QuestsButton { get; private set; }
        public EventButton Event { get; private set; }
        public VisualElement RightSide { get; private set; }
        public Button RestoreButton { get; private set; }
        public Button PassButton { get; private set; }
        public string PassText => _passText.text;
        public string PassBadgeText => _passBadge.ClassListContains(Ui.Hidden) ? "" : _passBadge.text;
        Label _passText;
        Label _passBadge;
        public string RestoreText => _restoreText.text;
        public string RestoreBadgeText => _restoreBadge.ClassListContains(Ui.Hidden) ? "" : _restoreBadge.text;

        Label _restoreText;
        Label _restoreBadge;
        Label _rushText;
        Label _rushBadge;
        Label _lanternText;
        Label _lanternBadge;
        Label _ladderText;
        Label _ladderBadge;
        Func<int> _focus;
        Func<int> _levelsWon;

        public Button RushButton { get; private set; }
        public Button LanternButton { get; private set; }
        public Button LadderButton { get; private set; }
        public Button ContestButton(ContestKind kind) => kind == ContestKind.Rush ? RushButton : LanternButton;
        public bool ContestShown(ContestKind kind) => !ContestButton(kind).ClassListContains(Ui.Hidden);
        public string ContestText(ContestKind kind) => (kind == ContestKind.Rush ? _rushText : _lanternText).text;
        public string ContestBadgeText(ContestKind kind)
        {
            var badge = kind == ContestKind.Rush ? _rushBadge : _lanternBadge;
            return badge.ClassListContains(Ui.Hidden) ? "" : badge.text;
        }
        public bool LadderShown => !LadderButton.ClassListContains(Ui.Hidden);
        public string LadderText => _ladderText.text;
        public string LadderBadgeText => _ladderBadge.ClassListContains(Ui.Hidden) ? "" : _ladderBadge.text;
        public bool GiftBadgeShown => !_giftBadge.ClassListContains(Ui.Hidden);
        public string QuestBadgeText => _questBadge.ClassListContains(Ui.Hidden) ? "" : _questBadge.text;

        public static LobbyBar Create(VisualElement parent, string playLabel, Action onPlay, Action onWorld, Action onGift, Action onQuests, Action onEvent, Action onRestore, Action onPass, Action<ContestKind> onContest, Action onLadder, Func<int> focus, Func<int> levelsWon)
        {
            var bar = new LobbyBar { _focus = focus, _levelsWon = levelsWon };
            bar.Build(parent, playLabel, onPlay, onWorld, onGift, onQuests, onEvent, onRestore, onPass, onContest, onLadder);
            return bar;
        }

        void Build(VisualElement parent, string playLabel, Action onPlay, Action onWorld, Action onGift, Action onQuests, Action onEvent, Action onRestore, Action onPass, Action<ContestKind> onContest, Action onLadder)
        {
            Side = Ui.El("et-side", parent, "Side");
            GiftButton = Ring(Side, "Gift", "et-ic--gift", "et-side__button", null, onGift, out _giftBadge);
            Event = EventButton.Create(Side, onEvent);
            RushButton = Ring(Side, "Rush", "et-ic--rush", "et-side__button et-side__button--event", null, () => onContest(ContestKind.Rush), out _rushBadge);
            _rushText = Ui.Text("", "et-chip et-caption et-side__chip", RushButton, "Progress");
            LadderButton = Ring(Side, "Ladder", "et-ic--ladder", "et-side__button et-side__button--event", null, onLadder, out _ladderBadge);
            _ladderText = Ui.Text("", "et-chip et-caption et-side__chip", LadderButton, "Progress");
            RightSide = Ui.El("et-side et-side--right", parent, "RightSide");
            RestoreButton = Ring(RightSide, "Restore", "et-ic--hammer", "et-side__button et-side__button--event", null, onRestore, out _restoreBadge);
            _restoreText = Ui.Text("", "et-chip et-caption et-side__chip", RestoreButton, "Progress");
            PassButton = Ring(RightSide, "Pass", "et-ic--pass", "et-side__button et-side__button--event", null, onPass, out _passBadge);
            _passText = Ui.Text("", "et-chip et-caption et-side__chip", PassButton, "Progress");
            LanternButton = Ring(RightSide, "Lantern", "et-ic--lantern-race", "et-side__button et-side__button--event", null, () => onContest(ContestKind.Lantern), out _lanternBadge);
            _lanternText = Ui.Text("", "et-chip et-caption et-side__chip", LanternButton, "Progress");

            Root = Ui.El("et-lobby", parent, "Lobby");
            WorldButton = Ring(Root, "World", "et-ic--world", "et-lobby__ring", "Dünya", onWorld, out _);
            PlayButton = Ui.Button(playLabel, Ui.ClassOf(ButtonKind.Cta) + " et-lobby__play", onPlay, Root, "Play");
            QuestsButton = Ring(Root, "Quests", "et-ic--quests", "et-lobby__ring", "Görevler", onQuests, out _questBadge);

            Refresh();
            Meta.Changed += Refresh;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= Refresh);
            Root.schedule.Execute(Refresh).Every(2000);
        }

        static Button Ring(VisualElement parent, string name, string icon, string classes, string caption, Action onTap, out Label badge)
        {
            var button = Ui.Button(null, "et-ring et-pressable " + classes, onTap, parent, name);
            Ui.El("et-icon et-ring__icon " + icon, button);
            if (caption != null) Ui.Text(caption, "et-outlined et-ring__caption", button, "Caption");
            badge = Ui.Text("!", "et-badge " + Ui.Hidden, button, "Badge");
            return button;
        }

        public void SetShown(bool shown)
        {
            Root.EnableInClassList(Ui.Hidden, !shown);
            Side.EnableInClassList(Ui.Hidden, !shown);
            RightSide.EnableInClassList(Ui.Hidden, !shown);
        }

        public void Refresh()
        {
            var economy = Meta.Economy;
            long now = Meta.Now;
            _giftBadge.EnableInClassList(Ui.Hidden, !economy.DailyReady(now));
            int claimable = economy.ClaimableQuests(now);
            _questBadge.text = claimable.ToString();
            _questBadge.EnableInClassList(Ui.Hidden, claimable <= 0);
            Event.Refresh();
            int district = _focus();
            var restore = Meta.Restore;
            _restoreText.text = restore.ClaimedCount(district) + "/" + restore.Tasks(district).Length;
            int repairs = restore.Claimable(district);
            _restoreBadge.text = repairs.ToString();
            _restoreBadge.EnableInClassList(Ui.Hidden, repairs <= 0);
            _passText.text = economy.PassReached(now) + "/" + Meta.Rules.Store.PassTrack.Length;
            int passes = economy.PassClaimable(now);
            _passBadge.text = passes.ToString();
            _passBadge.EnableInClassList(Ui.Hidden, passes <= 0);

            var contests = Meta.Contests;
            int won = _levelsWon();
            RefreshContest(contests, ContestKind.Rush, RushButton, _rushText, _rushBadge, won, now);
            RefreshContest(contests, ContestKind.Lantern, LanternButton, _lanternText, _lanternBadge, won, now);
            LadderButton.EnableInClassList(Ui.Hidden, !contests.LadderUnlocked(won));
            _ladderText.text = contests.LadderWins(now) + "/" + contests.Rules.LadderTarget;
            _ladderBadge.text = "!";
            _ladderBadge.EnableInClassList(Ui.Hidden, !contests.LadderClaimable(now));
        }

        static void RefreshContest(Contests contests, ContestKind kind, Button button, Label text, Label badge, int won, long now)
        {
            button.EnableInClassList(Ui.Hidden, !contests.Unlocked(kind, won));
            bool over = contests.Over(kind, now);
            if (over) text.text = "Bitti";
            else if (contests.Running(kind, now)) text.text = Meta.Span(contests.SecondsLeft(kind, now));
            else if (contests.CanStart(kind, now)) text.text = "Başla";
            else text.text = Meta.Span(contests.SecondsLeft(kind, now));
            badge.text = "!";
            badge.EnableInClassList(Ui.Hidden, !over);
        }
    }

    public sealed class TabBar
    {
        const string Active = "et-tab--active";
        const int RefreshMs = 5000;

        Label _cardsBadge;
        Label _rankBadge;
        Label _teamBadge;

        public VisualElement Root { get; private set; }
        public Button ShopTab { get; private set; }
        public Button RankTab { get; private set; }
        public Button HomeTab { get; private set; }
        public Button TeamTab { get; private set; }
        public Button CardsTab { get; private set; }

        public static TabBar Create(VisualElement parent, Action onShop, Action onRank, Action onHome, Action onTeam, Action onCards)
        {
            var bar = new TabBar { Root = Ui.El("et-tabbar", parent, "Tabs") };
            bar.ShopTab = Tab(bar.Root, "Shop", "et-ic--shop", "Mağaza", onShop);
            bar.RankTab = Tab(bar.Root, "Rank", "et-ic--trophy", "Sıralama", onRank);
            bar.HomeTab = Tab(bar.Root, "Home", "et-ic--home", "Ana Ekran", onHome);
            bar.TeamTab = Tab(bar.Root, "Team", "et-ic--team", "Takım", onTeam);
            bar.CardsTab = Tab(bar.Root, "Cards", "et-ic--cards", "Koleksiyon", onCards);
            bar._cardsBadge = Ui.Text("", "et-badge et-tab__badge " + Ui.Hidden, bar.CardsTab, "Badge");
            bar._rankBadge = Ui.Text("!", "et-badge et-tab__badge " + Ui.Hidden, bar.RankTab, "Badge");
            bar._teamBadge = Ui.Text("", "et-badge et-tab__badge " + Ui.Hidden, bar.TeamTab, "Badge");
            bar.SetActive(bar.HomeTab);
            bar.Refresh();
            Meta.Changed += bar.Refresh;
            bar.Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= bar.Refresh);
            bar.Root.schedule.Execute(bar.Refresh).Every(RefreshMs);
            return bar;
        }

        static Button Tab(VisualElement parent, string name, string icon, string label, Action onTap)
        {
            var tab = Ui.Button(null, "et-pressable et-tab", onTap, parent, name);
            Ui.El("et-icon et-tab__icon " + icon, tab);
            Ui.Text(label, "et-outlined et-tab__label", tab, "Label");
            return tab;
        }

        public string CardsBadgeText => _cardsBadge.ClassListContains(Ui.Hidden) ? "" : _cardsBadge.text;
        public string TeamBadgeText => _teamBadge.ClassListContains(Ui.Hidden) ? "" : _teamBadge.text;
        public bool RankBadgeShown => !_rankBadge.ClassListContains(Ui.Hidden);

        public void SetActive(Button tab)
        {
            foreach (var child in Root.Children()) child.EnableInClassList(Active, child == tab);
        }

        public void Refresh()
        {
            int waiting = Meta.Cards.Packs + Meta.Cards.Claimable;
            _cardsBadge.text = waiting.ToString();
            _cardsBadge.EnableInClassList(Ui.Hidden, waiting <= 0);
            _rankBadge.EnableInClassList(Ui.Hidden, !Meta.Race.ResultReady);
            int asks = Meta.Team.OpenAsks(Meta.Now);
            _teamBadge.text = asks.ToString();
            _teamBadge.EnableInClassList(Ui.Hidden, asks <= 0);
        }
    }

    public sealed class LoadingScreen
    {
        public const float Duration = 1.4f;
        const float FadeOut = 0.3f;

        public VisualElement Root { get; private set; }
        public bool IsOpen => Root != null && Root.parent != null;

        public VideoView Video { get; private set; }

        public static LoadingScreen Show(VisualElement parent, Transform owner, Action done)
        {
            var screen = new LoadingScreen { Root = Ui.El("et-fill et-loading", parent, "Loading") };
            screen.Root.pickingMode = PickingMode.Position;
            screen.Video = VideoView.Create(screen.Root, owner, "scene_loading", true, false);
            Ui.Text("EmberThrone", "et-title et-title--display et-loading__logo", screen.Root, "Logo");
            var bar = Ui.El("et-progress et-loading__bar", screen.Root, "Bar");
            var fill = Ui.El("et-progress__fill", bar, "Fill");
            Ui.Text("Yükleniyor", "et-outlined et-loading__label", screen.Root, "Label");

            var root = screen.Root;
            float time = 0f;
            root.schedule.Execute(t =>
            {
                time += t.deltaTime / 1000f;
                fill.style.width = Length.Percent(100f * Ease.OutCubic(Mathf.Clamp01(time / Duration)));
                if (time > Duration) root.style.opacity = 1f - Mathf.Clamp01((time - Duration) / FadeOut);
                if (time < Duration + FadeOut) return;
                root.RemoveFromHierarchy();
                done?.Invoke();
            }).Every(16).Until(() => !screen.IsOpen);
            return screen;
        }

        public void Close()
        {
            if (IsOpen) Root.RemoveFromHierarchy();
        }
    }
}
