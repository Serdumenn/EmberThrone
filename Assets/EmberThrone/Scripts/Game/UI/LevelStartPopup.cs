using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class LevelStartPopup
    {
        public VisualElement Root { get; private set; }
        public Button PlayButton { get; private set; }
        public Button BackButton { get; private set; }
        public int GoalCount { get; private set; }
        public bool Hard { get; private set; }

        Action _closed;

        public static LevelStartPopup Show(VisualElement parent, LevelDefinition level, Action play, Action closed)
        {
            var popup = new LevelStartPopup { _closed = closed };
            popup.Build(parent, level, play);
            return popup;
        }

        void Build(VisualElement parent, LevelDefinition level, Action play)
        {
            Root = Ui.El("et-fill et-scrim et-result", parent, "LevelStart");
            Root.pickingMode = PickingMode.Position;
            var body = Ui.El("et-result__body", Root, "Body");
            var panel = Ui.El("et-panel et-result__panel", body, "Panel");
            var ribbon = Ui.El("et-ribbon", panel);
            Ui.Text("Seviye " + level.Id, "et-title", ribbon, "Title");

            Hard = level.Difficulty == "hard";
            if (Hard) Ui.Text("Zor seviye", "et-tag et-outlined et-level-start__hard", panel, "Hard");
            Ui.Text(level.Name, "et-ink et-level-start__name", panel, "Name");

            var goals = Ui.El("et-level-start__goals", panel, "Goals");
            var session = new GameSession(level, 0);
            foreach (var goal in session.Goals)
            {
                var slot = Ui.El("et-goal et-level-start__goal", goals, "Goal");
                Ui.Image(HudView.GoalSprite(goal), "et-fill", slot);
                Ui.Text(goal.Remaining.ToString(), "et-outlined et-goal__count", slot, "Count");
                GoalCount++;
            }
            Ui.Text(level.Moves + " hamle", "et-ink et-result__message", panel, "Moves");

            Ui.El("et-icon et-level-start__mascot", body, "Mascot");

            PlayButton = Ui.Button("Oyna", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", () =>
            {
                Close();
                play();
            }, panel, "Play");
            BackButton = Ui.Button("Geri", Ui.ClassOf(ButtonKind.Secondary) + " et-result__button", Close, panel, "Back");

            const float from = 1600f;
            body.style.translate = new Translate(0f, from);
            Tweens.Run(Timing.ModalIn, t =>
            {
                if (Ui.Alive(body)) body.style.translate = new Translate(0f, Mathf.LerpUnclamped(from, 0f, Ease.OutCubic(t)));
            });
        }

        public void Close()
        {
            if (Root.parent == null) return;
            Root.RemoveFromHierarchy();
            _closed?.Invoke();
        }
    }
}
