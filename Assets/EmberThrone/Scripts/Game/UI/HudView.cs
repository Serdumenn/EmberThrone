using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class HudView
    {
        public const float Height = 280f;

        const string Warn = "et-hud__moves-count--warn";
        const string Danger = "et-hud__moves-count--danger";

        sealed class GoalSlot
        {
            public VisualElement Root;
            public Label Count;
            public VisualElement Check;
        }

        readonly List<GoalSlot> _slots = new List<GoalSlot>();
        Label _moves;
        VisualElement _movesPanel;

        public VisualElement Root { get; private set; }
        public Button HomeButton { get; private set; }
        public VisualElement MovesPanel => _movesPanel;
        public string MovesText => _moves.text;
        public string GoalText(int index) => _slots[index].Count.text;
        public bool GoalChecked(int index) => !_slots[index].Check.ClassListContains(Ui.Hidden);

        public static HudView Create(VisualElement parent, GameSession session, string levelLabel, Action onBack)
        {
            var hud = new HudView();
            hud.Build(parent, session, levelLabel, onBack);
            return hud;
        }

        void Build(VisualElement parent, GameSession session, string levelLabel, Action onBack)
        {
            Root = Ui.El("et-hud", parent, "Hud");
            Root.style.height = Height;

            var left = Ui.El("et-hud__left", Root);
            HomeButton = Ui.Button(null, "et-ring et-pressable", onBack, left, "Home");
            Ui.El("et-icon et-hud__home-icon", HomeButton);
            Ui.Text(levelLabel, "et-chip et-caption et-hud__level", left, "Level");

            var goals = Ui.El("et-panel et-hud__goals", Root, "Goals");
            Ui.Text("Hedefler", "et-tag et-outlined", goals);
            foreach (var goal in session.Goals) _slots.Add(BuildSlot(goals, goal));

            _movesPanel = Ui.El("et-panel et-hud__moves", Root, "Moves");
            Ui.Text("Hamle", "et-tag et-outlined", _movesPanel);
            _moves = Ui.Text("0", "et-ink et-hud__moves-count", _movesPanel, "Count");

            SetMoves(session.MovesLeft, false);
            for (int i = 0; i < session.Goals.Count; i++) SetGoal(i, session.Goals[i].Remaining, false);
        }

        static GoalSlot BuildSlot(VisualElement parent, Goal goal)
        {
            var s = new GoalSlot { Root = Ui.El("et-goal", parent, "Goal") };
            Ui.Image(GoalSprite(goal), "et-fill", s.Root);
            s.Count = Ui.Text("", "et-outlined et-goal__count", s.Root, "Count");
            s.Check = Ui.El("et-check et-goal__check", s.Root, "Check");
            return s;
        }

        public static Sprite GoalSprite(Goal goal)
        {
            switch (goal.Type)
            {
                case GoalType.Color: return Art.Gem(goal.Color);
                case GoalType.Box: return Art.Box(1);
                case GoalType.Floor: return Art.Floor(1);
                case GoalType.Cover: return Art.Cover(2);
                default: return Art.Collectible();
            }
        }

        public void SetMoves(int moves, bool animate = true)
        {
            string text = moves.ToString();
            bool changed = _moves.text != text;
            _moves.text = text;
            _moves.EnableInClassList(Warn, moves < 10 && moves >= 5);
            _moves.EnableInClassList(Danger, moves < 5);
            if (!animate || !changed) return;

            Bump(_moves, 0.22f, Timing.CounterBounce);
            if (moves > 0 && moves < 5)
            {
                var panel = _movesPanel;
                Tweens.Run(Timing.CounterBounce, t =>
                {
                    if (Ui.Alive(panel)) panel.style.translate = new Translate(Mathf.Sin(t * Mathf.PI * 4f) * 4f * (1f - t), 0f);
                }, 0f, () =>
                {
                    if (Ui.Alive(panel)) panel.style.translate = StyleKeyword.Null;
                });
            }
        }

        public void SetGoal(int index, int remaining, bool animate = true)
        {
            var s = _slots[index];
            bool done = remaining <= 0;
            s.Count.text = Mathf.Max(0, remaining).ToString();
            s.Count.EnableInClassList(Ui.Hidden, done);
            s.Check.EnableInClassList(Ui.Hidden, !done);
            if (animate) Bump(s.Root, 0.25f, 0.14f);
        }

        static void Bump(VisualElement e, float amount, float duration)
        {
            Tweens.Run(duration, t =>
            {
                if (Ui.Alive(e)) e.style.scale = new Scale(Vector2.one * (1f + amount * Mathf.Sin(t * Mathf.PI)));
            }, 0f, () =>
            {
                if (Ui.Alive(e)) e.style.scale = StyleKeyword.Null;
            });
        }

        public void Remove() => Root.RemoveFromHierarchy();
    }
}
