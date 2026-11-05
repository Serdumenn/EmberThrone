using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EmberThrone.Game
{
    /// <summary>In-level HUD (UX-001 §5): back button, level name, move counter, goal panel. Display only.</summary>
    public sealed class HudView : MonoBehaviour
    {
        /// <summary>Canvas units reserved at the top of the safe area (UX-001 HUD-RULE-01: at most 18% of the screen).</summary>
        public const float Height = 280f;

        sealed class GoalSlot
        {
            public Image Icon;
            public Text Count;
            public Image Check;
        }

        readonly List<GoalSlot> _slots = new List<GoalSlot>();
        Text _moves;
        RectTransform _movesPanel;

        public string MovesText => _moves.text;
        public string GoalText(int index) => _slots[index].Count.text;
        public bool GoalChecked(int index) => _slots[index].Check.enabled;

        public static HudView Create(Transform parent, GameSession session, string levelLabel, Action onBack)
        {
            var rt = Ui.Rect("Hud", parent);
            Ui.Place(rt, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, Height);
            var hud = rt.gameObject.AddComponent<HudView>();
            hud.Build(session, levelLabel, onBack);
            return hud;
        }

        void Build(GameSession session, string levelLabel, Action onBack)
        {
            // Left: back button and level name.
            var back = Ui.Button("Back", transform, "‹", Palette.PanelLight, onBack, 96);
            Ui.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(132f, 132f));

            var level = Ui.Label("Level", transform, levelLabel, 40, Palette.TextDim, TextAnchor.UpperLeft);
            Ui.Place(level.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -172f), new Vector2(300f, 90f));

            // Center: moves, always the biggest text on screen (HUD-RULE-03).
            var panel = Ui.Panel("Moves", transform, Palette.Panel);
            _movesPanel = panel.rectTransform;
            Ui.Place(_movesPanel, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(240f, 232f));
            var caption = Ui.Label("Caption", panel.transform, "HAMLE", 36, Palette.TextDim);
            Ui.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(240f, 50f));
            _moves = Ui.Label("Count", panel.transform, "0", 124, Palette.Text);
            _moves.fontStyle = FontStyle.Bold;
            Ui.Place(_moves.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(240f, 150f));

            // Right: one slot per goal.
            var goals = session.Goals;
            const float slot = 112f;
            var box = Ui.Panel("Goals", transform, Palette.Panel);
            float boxWidth = Mathf.Max(2, goals.Count) * slot + 28f;
            Ui.Place(box.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(boxWidth, 170f));
            for (int i = 0; i < goals.Count; i++)
            {
                float x = (i - (goals.Count - 1) * 0.5f) * slot;
                _slots.Add(BuildSlot(box.transform, goals[i], x));
            }

            SetMoves(session.MovesLeft, false);
            for (int i = 0; i < goals.Count; i++) SetGoal(i, goals[i].Remaining, false);
        }

        GoalSlot BuildSlot(Transform parent, Goal goal, float x)
        {
            var s = new GoalSlot();
            s.Icon = Ui.Icon("Icon", parent, GoalSprite(goal));
            Ui.Place(s.Icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(x, -12f), new Vector2(96f, 96f));
            s.Count = Ui.Label("Count", parent, "", 46, Palette.Text);
            s.Count.fontStyle = FontStyle.Bold;
            Ui.Place(s.Count.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, 6f), new Vector2(112f, 58f));
            s.Check = Ui.Icon("Check", parent, Art.Check(), Palette.Good);
            Ui.Place(s.Check.rectTransform, new Vector2(0.5f, 0f), new Vector2(x, 6f), new Vector2(60f, 60f));
            s.Check.enabled = false;
            return s;
        }

        static Sprite GoalSprite(Goal goal)
        {
            switch (goal.Type)
            {
                case GoalType.Color: return Art.Gem(goal.Color);
                case GoalType.Box: return Art.Box(1);
                case GoalType.Floor: return Art.Floor(1);
                case GoalType.Cover: return Art.Cover(2);
                default: return Art.Crown();
            }
        }

        public void SetMoves(int moves, bool animate = true)
        {
            bool changed = _moves.text != moves.ToString();
            _moves.text = moves.ToString();
            // UX-001 §5.2
            _moves.color = moves >= 10 ? Palette.Text : moves >= 5 ? Palette.Warning : Palette.Danger;
            if (!animate || !changed) return;

            var rt = _moves.rectTransform;
            Tweens.Run(Timing.CounterBounce, t =>
            {
                if (rt != null) rt.localScale = Vector3.one * (1f + 0.22f * Mathf.Sin(t * Mathf.PI));
            });
            if (moves > 0 && moves < 5)
            {
                var panel = _movesPanel;
                var home = panel.anchoredPosition;
                Tweens.Run(Timing.CounterBounce, t =>
                {
                    if (panel == null) return;
                    panel.anchoredPosition = home + Vector2.right * (Mathf.Sin(t * Mathf.PI * 4f) * 4f * (1f - t));
                }, 0f, () =>
                {
                    if (panel != null) panel.anchoredPosition = home;
                });
            }
        }

        public void SetGoal(int index, int remaining, bool animate = true)
        {
            var s = _slots[index];
            bool done = remaining <= 0;
            s.Count.text = Mathf.Max(0, remaining).ToString();
            s.Count.enabled = !done;
            s.Check.enabled = done;
            if (!animate) return;
            var rt = s.Icon.rectTransform;
            Tweens.Run(0.14f, t =>
            {
                if (rt != null) rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            });
        }
    }
}
