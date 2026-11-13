using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class ContestPopup : Popup
    {
        const int TickMs = 1000;
        const int RebuildTicks = 5;
        const int Podium = 3;

        ContestKind _kind;
        Label _left;
        VisualElement _content;
        Action<string, Reward[], bool> _showRewards;
        string _state;
        string _shape;
        int _ticks;

        public ContestKind Kind => _kind;
        public Button StartButton { get; private set; }
        public Button ClaimButton { get; private set; }
        public int RowCount { get; private set; }
        public int MyRank { get; private set; }
        public string LeftText => _left.text;
        public string ProgressText { get; private set; }
        public string ResultText { get; private set; }

        public static string NameOf(ContestKind kind) => kind == ContestKind.Rush ? "Kıvılcım Koşusu" : "Fener Yarışı";

        public static string IconOf(ContestKind kind) => kind == ContestKind.Rush ? "et-ic--rush" : "et-ic--lantern-race";

        static string TokenOf(ContestKind kind) => kind == ContestKind.Rush ? "et-ic--rush" : "et-ic--lanterns";

        public static ContestPopup Show(VisualElement parent, ContestKind kind, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new ContestPopup { _kind = kind, _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            var rules = Meta.ContestRules;
            Open(parent, "Contest", NameOf(_kind), closed);
            var top = Ui.El("et-event__top", Panel, "Top");
            Ui.El("et-icon et-contest__emblem " + IconOf(_kind), top, "Emblem");
            var texts = Ui.El("et-event__texts", top, "Texts");
            _left = Ui.Text("", "et-chip et-caption et-social__chip", texts, "Left");
            Ui.Text(HintOf(_kind), "et-caption et-ink et-ink--muted et-event__hint", texts, "Hint");

            var prizes = Ui.El("et-social__prizes et-contest__prizes", Panel, "Prizes");
            for (int rank = 1; rank <= Podium; rank++)
            {
                var prize = Ui.El("et-prize", prizes, "Prize" + rank);
                Ui.El("et-icon et-prize__medal " + MedalOf(rank), prize);
                Ui.El("et-icon et-prize__coin et-ic--coin", prize);
                Ui.Text(CoinsOf(rules.PrizeFor(_kind, rank)).ToString(), "et-ink et-prize__amount", prize, "Amount");
            }

            _content = Ui.El("et-contest__content", Panel, "Content");
            Rebuild();
            AddClose();
            Meta.Changed += Rebuild;
            Root.schedule.Execute(Tick).Every(TickMs);
        }

        protected override void OnClosed() => Meta.Changed -= Rebuild;

        static string HintOf(ContestKind kind)
        {
            var rules = Meta.ContestRules;
            int seconds = rules.Seconds[(int)kind];
            string span = seconds % EconomyRules.SecondsPerDay == 0
                ? seconds / EconomyRules.SecondsPerDay + " günde"
                : seconds / 3600 + " saatte";
            return span + " " + rules.Targets[(int)kind] + " seviye kazan.\nİlk " + Podium + "'e gir, ödülü kap!";
        }

        static string MedalOf(int rank) => rank == 1 ? "et-ic--medal" : "et-ic--medal-" + rank;

        static int CoinsOf(Reward[] prize)
        {
            foreach (var reward in prize)
                if (reward.Kind == RewardKind.Coins) return reward.Amount;
            return 0;
        }

        string StateNow(long now)
        {
            var contests = Meta.Contests;
            if (contests.Over(_kind, now)) return "over";
            if (contests.Running(_kind, now)) return "run";
            return contests.CanStart(_kind, now) ? "start" : "rest";
        }

        string ShapeNow(long now)
        {
            var shape = new System.Text.StringBuilder(StateNow(now));
            foreach (var row in Meta.Contests.Standings(_kind, now)) shape.Append('|').Append(row.Name).Append(row.Wins);
            return shape.ToString();
        }

        void Tick()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            _ticks++;
            if (StateNow(now) != _state || (_state == "run" && _ticks % RebuildTicks == 0 && ShapeNow(now) != _shape)) Rebuild();
            else UpdateLeft(now);
        }

        void UpdateLeft(long now)
        {
            var contests = Meta.Contests;
            int seconds = contests.SecondsLeft(_kind, now);
            switch (_state)
            {
                case "run": _left.text = "Kalan: " + Meta.Span(seconds); break;
                case "rest": _left.text = "Yeni yarış: " + Meta.Span(seconds); break;
                case "over": _left.text = "Yarış bitti"; break;
                default: _left.text = Meta.Span(Meta.ContestRules.Seconds[(int)_kind]) + " sürer"; break;
            }
        }

        void Rebuild()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            var contests = Meta.Contests;
            _state = StateNow(now);
            _shape = ShapeNow(now);
            UpdateLeft(now);
            _content.Clear();
            StartButton = null;
            ClaimButton = null;
            RowCount = 0;
            MyRank = 0;
            ProgressText = "";
            ResultText = "";

            if (_state == "start")
            {
                Ui.Text("Rakiplerin hazır. Sen de katıl!", "et-ink et-result__message", _content, "Message");
                StartButton = Ui.Button("Başla", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Begin, _content, "Start");
                return;
            }
            if (_state == "rest")
            {
                Ui.Text("Yeni yarış birazdan başlar.", "et-ink et-result__message", _content, "Message");
                return;
            }

            int target = contests.Target(_kind);
            int wins = contests.MyWins(_kind);
            var bar = Ui.El("et-progress et-contest__bar", _content, "Bar");
            var fill = Ui.El("et-progress__fill", bar, "Fill");
            fill.style.width = Length.Percent(100f * wins / target);
            fill.EnableInClassList(Ui.Hidden, wins <= 0);
            ProgressText = wins + "/" + target;
            Ui.Text(ProgressText, "et-progress__text", bar, "Text");

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "Rows" };
            scroll.AddToClassList("et-contest__list");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _content.Add(scroll);
            foreach (var row in contests.Standings(_kind, now))
            {
                var e = Ui.El(row.IsMe ? "et-rank et-rank--me" : "et-rank", scroll, "Row" + row.Rank);
                if (row.Rank <= Podium) Ui.El("et-icon et-rank__medal " + MedalOf(row.Rank), e, "Medal");
                else Ui.Text(row.Rank.ToString(), "et-ink et-rank__place", e, "Place");
                Avatar.Build(e, row.Avatar);
                Ui.Text(row.IsMe ? Avatar.MyName : row.Name, "et-rank__name", e, "Name");
                Ui.El("et-icon et-rank__trophy " + TokenOf(_kind), e);
                Ui.Text(row.Wins + "/" + target, "et-ink et-rank__points", e, "Wins");
                RowCount++;
                if (row.IsMe) MyRank = row.Rank;
            }

            if (_state != "over") return;
            var prize = contests.PrizeNow(_kind, now);
            ResultText = prize.Length > 0 ? MyRank + ". oldun!" : "Bu sefer yetişemedin";
            Ui.Text(ResultText, "et-ink et-result__message et-contest__result", _content, "Result");
            ClaimButton = Ui.Button(prize.Length > 0 ? "Ödülü al" : "Tamam", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Claim, _content, "Claim");
        }

        void Begin()
        {
            if (!Meta.Contests.Begin(_kind, Meta.Now)) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
        }

        void Claim()
        {
            long now = Meta.Now;
            int rank = Meta.Contests.Rank(_kind, now);
            var prize = Meta.Contests.Claim(_kind, now);
            if (prize == null) return;
            Meta.Save();
            if (prize.Length > 0) _showRewards?.Invoke(rank + ". oldun!", prize, false);
        }
    }

    public sealed class LadderPopup : Popup
    {
        const int TickMs = 1000;
        const int NarrowestPercent = 58;
        const int StepPercent = 6;

        Label _left;
        Label _players;
        VisualElement _content;
        Action<string, Reward[], bool> _showRewards;

        public Button ClaimButton { get; private set; }
        public int StepCount { get; private set; }
        public int Lit { get; private set; }
        public string PlayersText => _players.text;
        public string LeftText => _left.text;

        public static LadderPopup Show(VisualElement parent, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new LadderPopup { _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            var rules = Meta.ContestRules;
            Open(parent, "Ladder", "Kor Merdiveni", closed);
            var top = Ui.El("et-event__top", Panel, "Top");
            Ui.El("et-icon et-contest__emblem et-ic--ladder", top, "Emblem");
            var texts = Ui.El("et-event__texts", top, "Texts");
            var chips = Ui.El("et-social__chips", texts, "Chips");
            _left = Ui.Text("", "et-chip et-caption et-social__chip", chips, "Left");
            _players = Ui.Text("", "et-chip et-caption et-social__chip", chips, "Players");
            Ui.Text("Bugün " + rules.LadderTarget + " seviyeyi art arda kazan.\nKaybedersen başa dönersin!", "et-caption et-ink et-ink--muted et-event__hint", texts, "Hint");

            _content = Ui.El("et-contest__content", Panel, "Content");
            Rebuild();
            AddClose();
            Meta.Changed += Rebuild;
            Root.schedule.Execute(UpdateLeft).Every(TickMs);
        }

        protected override void OnClosed() => Meta.Changed -= Rebuild;

        void UpdateLeft()
        {
            if (!IsOpen) return;
            _left.text = "Kalan: " + Meta.Span(Meta.Contests.LadderSecondsLeft(Meta.Now));
        }

        void Rebuild()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            var contests = Meta.Contests;
            var rules = contests.Rules;
            int wins = contests.LadderWins(now);
            UpdateLeft();
            _players.text = "Kalan oyuncu: " + contests.LadderPlayersLeft(now) + "/" + rules.LadderPlayers;
            _content.Clear();
            ClaimButton = null;
            StepCount = 0;
            Lit = wins;

            var pool = Ui.El("et-ladder__pool", _content, "Pool");
            Ui.El("et-icon et-ladder__trophy et-ic--trophy-flame", pool, "Trophy");
            var poolTexts = Ui.El("et-ladder__pool-texts", pool, "Texts");
            Ui.Text(rules.LadderPool + " altın", "et-outlined et-ladder__pool-amount", poolTexts, "Amount");
            Ui.Text("Zirvedekiler paylaşır: kişi başı " + contests.LadderPrize(now), "et-caption et-ink et-ladder__share", poolTexts, "Share");

            var stairs = Ui.El("et-ladder", _content, "Stairs");
            for (int step = rules.LadderTarget; step >= 1; step--)
            {
                var e = Ui.El("et-ladder__step", stairs, "Step" + step);
                e.style.width = Length.Percent(NarrowestPercent + StepPercent * (rules.LadderTarget - step));
                e.EnableInClassList("et-ladder__step--done", step <= wins);
                e.EnableInClassList("et-ladder__step--next", step == wins + 1);
                Ui.Text(step.ToString(), "et-outlined et-ladder__number", e, "Number");
                if (step <= wins) Ui.El("et-check et-ladder__check", e, "Done");
                else if (step == wins + 1) Ui.El("et-icon et-ladder__mascot et-ic--mascot", e, "Emby");
                StepCount++;
            }

            if (contests.LadderClaimable(now))
                ClaimButton = Ui.Button("Ödülü al", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Claim, _content, "Claim");
            else if (contests.LadderDone(now))
                Ui.Text("Bugünün ödülü alındı. Yarın yeniden!", "et-caption et-ink et-event__note", _content, "Note");
            else
                Ui.Text("Sıradaki adım: " + (wins + 1) + ". galibiyet", "et-caption et-ink et-event__note", _content, "Note");
        }

        void Claim()
        {
            var prize = Meta.Contests.ClaimLadder(Meta.Now);
            if (prize == null) return;
            Meta.Save();
            _showRewards?.Invoke("Kor Merdiveni", prize, false);
        }
    }

    public sealed class TeamChest
    {
        readonly List<Button> _claims = new List<Button>();
        Action<string, Reward[], bool> _showRewards;

        public VisualElement Root { get; private set; }
        public IReadOnlyList<Button> ClaimButtons => _claims;
        public string ProgressText { get; private set; }

        public static TeamChest Build(VisualElement parent, Action<string, Reward[], bool> showRewards)
        {
            var chest = new TeamChest { _showRewards = showRewards };
            chest.Create(parent);
            return chest;
        }

        void Create(VisualElement parent)
        {
            long now = Meta.Now;
            var contests = Meta.Contests;
            var rules = contests.Rules;
            int wins = contests.TeamWins(now);
            int last = rules.ChestSteps[rules.ChestSteps.Length - 1];

            Root = Ui.El("et-row et-chest", parent, "Chest");
            Ui.El("et-icon et-chest__icon et-ic--team-chest", Root, "Icon");
            var texts = Ui.El("et-row__texts", Root, "Texts");
            Ui.Text("Takım sandığı · " + Meta.Span(contests.ChestSecondsLeft(now)), "et-row__title", texts, "Title");
            var bar = Ui.El("et-progress et-chest__bar", texts, "Bar");
            var fill = Ui.El("et-progress__fill", bar, "Fill");
            fill.style.width = Length.Percent(100f * Mathf.Min(wins, last) / last);
            fill.EnableInClassList(Ui.Hidden, wins <= 0);
            ProgressText = wins + "/" + last;
            Ui.Text(ProgressText + " galibiyet", "et-progress__text", bar, "Text");

            var steps = Ui.El("et-chest__steps", texts, "Steps");
            for (int i = 0; i < rules.ChestSteps.Length; i++)
            {
                int step = i;
                var slot = Ui.El("et-chest__step", steps, "Step" + (i + 1));
                if (contests.ChestClaimed(i)) Ui.El("et-check et-chest__check", slot, "Done");
                else if (contests.CanClaimChest(i, now))
                {
                    var button = Rows.Price(slot, "Aç", null, ButtonKind.Cta, () => Claim(step), "Claim");
                    button.AddToClassList("et-chest__claim");
                    _claims.Add(button);
                }
                else Ui.Text(rules.ChestSteps[i].ToString(), "et-chip et-caption et-chest__need", slot, "Need");
            }
        }

        void Claim(int step)
        {
            var rewards = Meta.Contests.ClaimChest(step, Meta.Now);
            if (rewards == null) return;
            Meta.Save();
            _showRewards?.Invoke("Takım sandığı", rewards, true);
        }
    }
}
