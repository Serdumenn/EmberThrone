using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class EventButton
    {
        Label _left;
        Label _badge;
        VisualElement _icon;
        string _shown;

        public Button Root { get; private set; }
        public bool Shown => !Root.ClassListContains(Ui.Hidden);
        public string LeftText => _left.text;
        public string BadgeText => _badge.ClassListContains(Ui.Hidden) ? "" : _badge.text;

        public static EventButton Create(VisualElement parent, Action onTap)
        {
            var button = new EventButton();
            button.Root = Ui.Button(null, "et-ring et-pressable et-side__button et-side__button--event", onTap, parent, "Event");
            button._icon = Ui.El("et-icon et-ring__icon", button.Root, "Icon");
            button._left = Ui.Text("", "et-chip et-caption et-side__chip", button.Root, "Left");
            button._badge = Ui.Text("", "et-badge " + Ui.Hidden, button.Root, "Badge");
            button.Refresh();
            return button;
        }

        public void Refresh()
        {
            long now = Meta.Now;
            var live = Meta.Event;
            var def = live.Current(now);
            Root.EnableInClassList(Ui.Hidden, def == null);
            if (def == null) return;
            if (_shown != def.Emblem)
            {
                _shown = def.Emblem;
                _icon.style.backgroundImage = new StyleBackground(Avatar.Art(def.Emblem));
            }
            _left.text = Meta.Span(live.SecondsLeft(now));
            int claimable = live.Claimable(now);
            _badge.text = claimable.ToString();
            _badge.EnableInClassList(Ui.Hidden, claimable <= 0);
        }
    }

    public sealed class EventPopup : Popup
    {
        const int TickMs = 1000;

        Label _left;
        VisualElement _content;
        Action<string, Reward[], bool> _showRewards;

        public Button ClaimButton { get; private set; }
        public EventKind Kind { get; private set; }
        public int TierRows { get; private set; }
        public string LeftText => _left.text;
        public string PointsText { get; private set; }

        public static EventPopup Show(VisualElement parent, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new EventPopup { _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            long now = Meta.Now;
            var def = Meta.Event.Current(now);
            Kind = def.Kind;
            Open(parent, "Event", def.Name, closed);

            var top = Ui.El("et-event__top", Panel, "Top");
            Avatar.Build(top, def.Emblem, "et-avatar--big");
            var texts = Ui.El("et-event__texts", top, "Texts");
            _left = Ui.Text("", "et-chip et-caption et-social__chip", texts, "Left");
            Ui.Text(HintOf(def), "et-caption et-ink et-ink--muted et-event__hint", texts, "Hint");

            _content = Ui.El("et-event__content", Panel, "Content");
            Rebuild();
            AddClose();
            Meta.Changed += Rebuild;
            Root.schedule.Execute(UpdateLeft).Every(TickMs);
        }

        protected override void OnClosed() => Meta.Changed -= Rebuild;

        static string HintOf(EventDef def)
        {
            if (def.Kind == EventKind.Ladder) return "Kazan, engel kır, booster kullan:\nkor topla, ödülleri aç.";
            return "Süre bitmeden hedefe ulaş,\nödülü kap.";
        }

        static string GoalOf(EventDef def)
        {
            switch (def.Goal)
            {
                case EventMetric.Wins: return def.Target + " seviye kazan";
                case EventMetric.Stars: return def.Target + " yıldız topla";
                case EventMetric.Obstacles: return def.Target + " engel kır";
                default: return def.Target + " booster kullan";
            }
        }

        void UpdateLeft()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            if (Meta.Event.Current(now) == null)
            {
                Close();
                return;
            }
            _left.text = "Kalan: " + Meta.Span(Meta.Event.SecondsLeft(now));
        }

        void Rebuild()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            var live = Meta.Event;
            var def = live.Current(now);
            if (def == null)
            {
                Close();
                return;
            }
            UpdateLeft();
            _content.Clear();
            ClaimButton = null;
            TierRows = 0;
            if (def.Kind == EventKind.Ladder) BuildLadder(def, live, now);
            else BuildSprint(def, live, now);
        }

        VisualElement Token(VisualElement parent, string classes)
        {
            var token = Ui.El("et-icon " + classes, parent, "Token");
            token.style.backgroundImage = new StyleBackground(Avatar.Art(Meta.EventRules.Token));
            return token;
        }

        void BuildLadder(EventDef def, LiveEvent live, long now)
        {
            var points = Ui.El("et-event__points", _content, "Points");
            Token(points, "et-event__token");
            PointsText = live.Points.ToString();
            Ui.Text(PointsText, "et-ink et-event__points-text", points, "Text");

            for (int i = 0; i < def.Tiers.Length; i++)
            {
                var tier = def.Tiers[i];
                var row = Ui.El("et-row et-event__tier", _content, "Tier" + (i + 1));
                var need = Ui.El("et-event__need", row, "Need");
                Token(need, "et-event__need-icon");
                Ui.Text(tier.Points.ToString(), "et-ink et-event__need-text", need, "Text");
                BuildRewards(row, tier.Rewards);

                var slot = Ui.El("et-event__slot", row, "Slot");
                bool reached = live.Points >= tier.Points;
                if (i < live.Claimed) Ui.El("et-check", slot, "Done");
                else if (reached && i == live.Claimed) ClaimButton = ClaimIn(slot, "Al", def);
                else if (!reached) Ui.El("et-icon et-event__lock et-ic--lock", slot, "Lock");
                TierRows++;
            }
        }

        void BuildSprint(EventDef def, LiveEvent live, long now)
        {
            Ui.Text(GoalOf(def), "et-ink et-result__message et-event__goal", _content, "Goal");
            var bar = Ui.El("et-progress et-event__bar", _content, "Bar");
            var fill = Ui.El("et-progress__fill", bar, "Fill");
            fill.style.width = Length.Percent(100f * live.Points / def.Target);
            fill.EnableInClassList(Ui.Hidden, live.Points <= 0);
            PointsText = live.Points + "/" + def.Target;
            Ui.Text(PointsText, "et-progress__text", bar, "Text");

            var rewards = Ui.El("et-rewards et-rewards--small", _content, "Rewards");
            foreach (var reward in live.GoalRewards(now)) RewardTile(reward, rewards).AddToClassList("et-reward--small");

            string bonus = "+%" + live.Rules.EarlyBonusPercent + " altın";
            if (live.Claimed > 0) Ui.Text("Ödül alındı", "et-caption et-ink et-event__note", _content, "Note");
            else if (live.Early(now)) Ui.Text("Erken bitirdin: " + bonus, "et-caption et-ink et-event__note", _content, "Note");
            else if (live.BonusOpen(now)) Ui.Text("İlk yarıda bitir: " + bonus, "et-caption et-ink et-event__note", _content, "Note");

            if (live.Claimable(now) > 0)
            {
                ClaimButton = Ui.Button("Ödülü al", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", () => Claim(def), _content, "Claim");
            }
        }

        static void BuildRewards(VisualElement row, Reward[] rewards)
        {
            var list = Ui.El("et-event__rewards", row, "Rewards");
            foreach (var reward in rewards)
            {
                var e = Ui.El("et-event__reward", list, "Reward");
                Ui.El("et-icon et-event__reward-icon " + IconOf(reward), e);
                Ui.Text(reward.Amount.ToString(), "et-ink et-event__reward-text", e, "Amount");
            }
        }

        Button ClaimIn(VisualElement slot, string label, EventDef def)
        {
            var button = Rows.Price(slot, label, null, ButtonKind.Cta, () => Claim(def), "Claim");
            button.AddToClassList("et-event__claim");
            return button;
        }

        void Claim(EventDef def)
        {
            var rewards = Meta.Event.Claim(Meta.Now);
            if (rewards == null) return;
            Meta.Save();
            _showRewards(def.Name, rewards, false);
        }
    }
}
