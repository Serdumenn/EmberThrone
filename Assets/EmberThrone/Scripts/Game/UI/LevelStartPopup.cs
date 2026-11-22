using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class LevelStartPopup
    {
        const float FlagHidden = 190f;
        const float FlagOvershoot = 56f;

        public VisualElement Root { get; private set; }
        public Button PlayButton { get; private set; }
        public Button CloseButton { get; private set; }
        public VisualElement Flag { get; private set; }
        public int GoalCount { get; private set; }
        public bool Hard { get; private set; }
        public bool SuperHard { get; private set; }
        public int StreakShown { get; private set; }
        public bool FrostHint { get; private set; }
        public string Hint { get; private set; }
        public bool Failed { get; private set; }

        Action _closed;
        Action _leave;

        public static LevelStartPopup Show(VisualElement parent, LevelDefinition level, Action play, Action closed)
        {
            var popup = new LevelStartPopup { _closed = closed };
            popup.Build(parent, level, play);
            return popup;
        }

        public static LevelStartPopup ShowFailed(VisualElement parent, LevelDefinition level, Action retry, Action leave, Action closed)
        {
            var popup = new LevelStartPopup { _closed = closed, _leave = leave, Failed = true };
            popup.Build(parent, level, retry);
            return popup;
        }

        void Build(VisualElement parent, LevelDefinition level, Action play)
        {
            Root = Ui.El(Failed ? "et-fill et-scrim et-level--failed" : "et-fill et-scrim", parent, Failed ? "Failed" : "LevelStart");
            Root.pickingMode = PickingMode.Position;
            var stage = Ui.El("et-stage", Root, "Stage");

            Hard = level.IsHard;
            SuperHard = level.IsSuperHard;
            if (SuperHard)
            {
                var left = Ui.El("et-level__flag-side et-level__flag-side--left", stage, "FlagLeft");
                var right = Ui.El("et-level__flag-side et-level__flag-side--right", stage, "FlagRight");
                Flag = Ui.El("et-level__flag", stage, "Flag");
                Ui.Text("Süper Zor!", "et-level__flag-text", Flag, "Text");
                Rise(Flag, 0f, Timing.FlagRise, Timing.FlagSettle);
                Rise(left, Timing.FlagSideDelay, Timing.FlagSideRise, Timing.FlagSideSettle);
                Rise(right, Timing.FlagSideDelay, Timing.FlagSideRise, Timing.FlagSideSettle);
            }

            Ui.El("et-level__frame", stage, "Frame");
            Ui.El("et-level__pattern", stage, "Pattern");
            var band = Ui.El("et-level__band", stage, "Band");
            Ui.Text("Bölüm " + level.Id, "et-level__title", band, "Title");
            if (Failed)
            {
                var failed = Ui.El("et-level__fail", stage, "FailLine");
                Ui.El("et-icon et-level__fail-icon", failed);
                Ui.Text("Bölüm Başarısız!", "et-level__fail-text", failed, "Text");
            }

            var panel = Ui.El("et-level__card", stage, "Panel");
            if (Hard && !SuperHard) Ui.Text("Zor seviye", "et-level-start__hard", panel, "Hard");
            Ui.Text(level.Name, "et-ink et-level-start__name", panel, "Name");

            var goals = Ui.El("et-level-start__goals", panel, "Goals");
            var session = new GameSession(level, 0);
            bool frost = HudView.CoversAreFrost(session.Board);
            foreach (var goal in session.Goals)
            {
                var slot = Ui.El("et-goal et-level-start__goal", goals, "Goal");
                Ui.Image(HudView.GoalSprite(goal, frost), "et-fill", slot);
                Ui.Text(goal.Remaining.ToString(), "et-outlined et-goal__count", slot, "Count");
                GoalCount++;
            }
            Ui.Text(level.Moves + " hamle", "et-ink et-result__message", panel, "Moves");
            var streak = GameApp.StreakBoosts ? Meta.Economy.StreakSpecials() : new PieceKind[0];
            StreakShown = streak.Length;
            if (streak.Length > 0)
            {
                var row = Ui.El("et-level-start__streak", panel, "Streak");
                Ui.Text("Galibiyet serisi " + Meta.Economy.WinStreak, "et-ink et-level-start__streak-label", row);
                foreach (var kind in streak) Ui.Image(SpriteOf(kind), "et-level-start__streak-icon", row);
            }
            FrostHint = HudView.HasFrost(session.Board);
            Hint = HintFor(session.Board, FrostHint);
            if (Hint != null) Ui.Text(Hint, "et-ink et-level-start__hint", panel, "Hint");

            PlayButton = Ui.Button(Failed ? "Tekrar Dene" : "Oyna", Ui.ClassOf(ButtonKind.Cta) + " et-btn--framed et-level__play", () =>
            {
                Close();
                play();
            }, stage, "Play");
            CloseButton = Ui.Button(null, "et-pressable et-level__close", () =>
            {
                Close();
                _leave?.Invoke();
            }, stage, "Close");
        }

        static void Rise(VisualElement e, float delay, float peak, float settle)
        {
            e.style.translate = new Translate(0f, FlagHidden);
            Tweens.Run(settle, t =>
            {
                if (!Ui.Alive(e)) return;
                float time = t * settle;
                float y = time < peak
                    ? Mathf.LerpUnclamped(FlagHidden, -FlagOvershoot, Ease.OutQuad(time / peak))
                    : Mathf.LerpUnclamped(-FlagOvershoot, 0f, Ease.InOutSine((time - peak) / (settle - peak)));
                e.style.translate = new Translate(0f, y);
            }, delay, () =>
            {
                if (Ui.Alive(e)) e.style.translate = new Translate(0f, 0f);
            });
        }

        static string HintFor(Board board, bool frost)
        {
            if (frost) return "Kırağı her hamlede yayılır. Kırarak durdur!";
            if (board.CountPieces(PieceKind.Statue) > 0) return "Buzdaki köylüye dört kez vur, kurtar!";
            if (board.CountPieces(PieceKind.Planter) > 0) return "Saksının yanında eşleştir: her vuruşta bir çiçek açar.";
            if (board.CountPieces(PieceKind.Lamp) > 0) return "Kandil ilk vuruşta yanar, ikincide toplanır.";
            return null;
        }

        static Sprite SpriteOf(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Bomb: return Art.Bomb();
                case PieceKind.ColorBomb: return Art.ColorBomb();
                case PieceKind.Homing: return Art.Homing();
                default: return Art.Rocket(kind != PieceKind.RocketV);
            }
        }

        public void Close()
        {
            if (Root.parent == null) return;
            Root.RemoveFromHierarchy();
            _closed?.Invoke();
        }
    }
}
