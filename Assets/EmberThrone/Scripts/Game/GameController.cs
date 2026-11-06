using System;
using System.Collections;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    /// <summary>
    /// Runs one level: forwards player gestures to the GameSession and hands every resulting
    /// MoveOutcome to the view. Only GameSession changes game state.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        /// <summary>Panel units kept free below the board.</summary>
        const float BottomMargin = 60f;
        /// <summary>Empty border around the board, in cells.</summary>
        const float BoardMargin = 0.32f;
        /// <summary>Drawn behind every board sprite.</summary>
        const int BackdropOrder = -100;
        /// <summary>The backdrop is a little larger than the view, so a camera shake never shows its edge.</summary>
        const float BackdropOverscan = 1.04f;

        GameApp _app;
        BoardInput _input;
        SpriteRenderer _backdrop;
        HudView _hud;
        PraiseView _praise;
        /// <summary>The biggest praise shown during the current move; a move never repeats or lowers it.</summary>
        int _praiseLevel;
        GridPos _selected = GridPos.Invalid;
        LevelDefinition _level;
        int _levelIndex;

        public GameSession Session { get; private set; }
        public BoardView View { get; private set; }
        public HudView Hud => _hud;
        public PraiseView Praise => _praise;
        public ResultPopup Popup { get; private set; }
        public bool IsBusy { get; private set; }
        /// <summary>Set if the view ever disagreed with the engine. Should stay null forever.</summary>
        public string LastMismatch { get; private set; }

        public static GameController Create(GameApp app, Transform parent, LevelDefinition level, int levelIndex, int seed)
        {
            var go = new GameObject("Game");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<GameController>();
            c.Begin(app, level, levelIndex, seed);
            return c;
        }

        void Begin(GameApp app, LevelDefinition level, int levelIndex, int seed)
        {
            _app = app;
            _level = level;
            _levelIndex = levelIndex;
            Session = new GameSession(level, seed);
            BuildBackdrop();
            BuildView();

            _hud = HudView.Create(app.Ui.Content, Session, "Seviye " + level.Id, app.ShowLevelSelect);
            _praise = PraiseView.Create(app.Ui.Content);

            _input = gameObject.AddComponent<BoardInput>();
            _input.Init(app.Camera, View);
            _input.Pressed += p =>
            {
                if (View.IsGrabbableAt(p)) View.SetLifted(p, true);
            };
            _input.Released += p => View.SetLifted(p, false);
            _input.Swiped += RequestSwap;
            _input.Tapped += OnTap;

            app.Ui.LayoutChanged += FitCamera;
            FitCamera();

            // A level can already be over once its board has settled.
            SaveIfWon();
            if (Session.State == GameState.Playing) _input.SetEnabled(true);
            else StartCoroutine(EndLevel());
        }

        void BuildView()
        {
            var boardObject = new GameObject("Board");
            boardObject.transform.SetParent(transform, false);
            View = boardObject.AddComponent<BoardView>();
            View.Build(Session);
            View.GoalProgressed += e => _hud.SetGoal(e.GoalIndex, e.Remaining);
            View.Impacted += OnImpact;
            View.WaveStarted += OnWave;
            View.ComboFired += OnCombo;
        }

        /// <summary>The castle and village behind the board; FitCamera stretches it over the whole view.</summary>
        void BuildBackdrop()
        {
            _backdrop = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            _backdrop.transform.SetParent(transform, false);
            _backdrop.sprite = Art.Backdrop();
            _backdrop.sharedMaterial = Art.SpriteMaterial;
            _backdrop.sortingOrder = BackdropOrder;
        }

        /// <summary>Redraws everything from the engine's state, which a view failure cannot have damaged.</summary>
        void RebuildView()
        {
            Tweens.KillAll(); // they still point at the old pieces
            Destroy(View.gameObject);
            BuildView();
            _input.Init(_app.Camera, View);
            _hud.SetMoves(Session.MovesLeft, false);
            for (int i = 0; i < Session.Goals.Count; i++) _hud.SetGoal(i, Session.Goals[i].Remaining, false);
        }

        void OnDestroy()
        {
            if (_app != null && _app.Ui != null) _app.Ui.LayoutChanged -= FitCamera;
            _hud?.Remove();
            Popup?.Close();
        }

        /// <summary>Fits the board in the part of the safe area the HUD leaves free, on any screen shape.</summary>
        void FitCamera()
        {
            var cam = _app.Camera;
            var safe = UiRoot.SafeArea;
            float scale = UiRoot.Scale;
            float bottom = safe.yMin + BottomMargin * scale;
            float top = safe.yMax - HudView.Height * scale;
            float width = safe.width;
            float height = Mathf.Max(1f, top - bottom);

            float ppu = Mathf.Min(width / (View.Width + BoardMargin * 2f), height / (View.Height + BoardMargin * 2f));
            cam.orthographicSize = UiRoot.ScreenHeight / (2f * ppu);

            // Move the camera so the world origin (board center) lands on the center of the free area.
            float centerX = safe.xMin + width * 0.5f;
            float centerY = bottom + height * 0.5f;
            var rest = new Vector3(
                (UiRoot.ScreenWidth * 0.5f - centerX) / ppu,
                (UiRoot.ScreenHeight * 0.5f - centerY) / ppu,
                -10f);
            _app.Shake.SetRest(rest);

            // Cover the view like a phone wallpaper: no letterbox on any screen shape.
            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * UiRoot.ScreenWidth / UiRoot.ScreenHeight;
            var size = _backdrop.sprite.bounds.size;
            _backdrop.transform.position = new Vector3(rest.x, rest.y, 0f);
            _backdrop.transform.localScale = Vector3.one * (Mathf.Max(viewWidth / size.x, viewHeight / size.y) * BackdropOverscan);
        }

        // ---------------------------------------------------------------- gestures

        /// <summary>Tap flow: a tap fires a special, or selects a piece; a second tap on a neighbor swaps.</summary>
        public void OnTap(GridPos p)
        {
            if (IsBusy) return;
            if (_selected.IsValid)
            {
                var first = _selected;
                Select(GridPos.Invalid);
                if (p == first) return;
                if (p.IsAdjacent(first))
                {
                    RequestSwap(first, p);
                    return;
                }
            }
            if (View.IsSpecialAt(p)) Run(Session.TryTap(p));
            else if (View.IsGrabbableAt(p)) Select(p);
            else View.Nudge(p);
        }

        public void RequestSwap(GridPos a, GridPos b)
        {
            if (IsBusy) return;
            Select(GridPos.Invalid);
            var outcome = Session.TrySwap(a, b);
            if (outcome.Steps.Count == 0)
            {
                View.Nudge(a);
                return;
            }
            Run(outcome);
        }

        void Select(GridPos p)
        {
            _selected = p;
            View.SetSelection(p);
        }

        void Run(MoveOutcome outcome)
        {
            if (!outcome.Accepted && outcome.Steps.Count == 0) return;
            StartCoroutine(Resolve(outcome));
        }

        IEnumerator Resolve(MoveOutcome outcome)
        {
            IsBusy = true;
            _praiseLevel = 0;
            _input.SetEnabled(false);
            _hud.SetMoves(outcome.MovesLeft);
            SaveIfWon(); // the win counts even if the player leaves during the animation

            yield return PlayGuarded(outcome);

            CheckSync();
            IsBusy = false;

            if (outcome.State == GameState.Playing) _input.SetEnabled(true);
            else yield return EndLevel();
        }

        /// <summary>
        /// Steps through the view's nested routines here, because a coroutine cannot catch what
        /// happens inside a routine it yields to. A view bug must not freeze the board.
        /// </summary>
        IEnumerator PlayGuarded(MoveOutcome outcome)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(View.Play(outcome));
            while (stack.Count > 0)
            {
                object current;
                try
                {
                    if (!stack.Peek().MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }
                    current = stack.Peek().Current;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    RebuildView();
                    yield break;
                }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
        }

        void CheckSync()
        {
            var mismatch = View.FindMismatch(Session.Board);
            if (mismatch == null) return;
            LastMismatch = mismatch;
            Debug.LogError("[EmberThrone] View out of sync with the engine: " + mismatch);
        }

        // ---------------------------------------------------------------- feel

        void OnImpact(Impact impact)
        {
            _app.Shake.Add(Feel.TraumaOf(impact));
            Haptics.Play(Feel.HapticOf(impact));
        }

        /// <summary>Long cascades earn a word: 3 waves "Harika!", 4 "Muhteşem!", 6 "İnanılmaz!".</summary>
        void OnWave(int wave) => ShowPraise(wave >= 6 ? 3 : wave >= 4 ? 2 : wave >= 3 ? 1 : 0);

        void OnCombo(ComboKind combo) => ShowPraise(combo == ComboKind.ColorColor ? 3 : 2);

        void ShowPraise(int level)
        {
            if (level <= _praiseLevel) return;
            _praiseLevel = level;
            _praise.Show(level);
        }

        // ---------------------------------------------------------------- end of level

        void SaveIfWon()
        {
            if (Session.State == GameState.Won) LevelProgress.SaveStars(_level.Id, Session.Rules.StarsFor(Session.MovesLeft));
        }

        IEnumerator EndLevel() => Session.State == GameState.Won ? ShowWin() : ShowLose();

        IEnumerator ShowWin()
        {
            yield return new WaitForSeconds(Timing.WinDelay);

            // Every move left becomes a firework, and the counter counts down with the sparks. Only a show:
            // the session keeps its moves, so the stars are already decided.
            int left = Session.MovesLeft;
            int sparks = Mathf.Clamp(left, 1, Timing.WinSparksMax);
            float show = View.Celebrate(MovesPanelWorld(), sparks,
                i => _hud.SetMoves(left - Mathf.RoundToInt((i + 1) * (float)left / sparks)));
            yield return new WaitForSeconds(show + Timing.WinWindowPause);

            OnImpact(Impact.Win);
            int stars = Session.Rules.StarsFor(Session.MovesLeft);
            var next = _app.HasLevel(_levelIndex + 1)
                ? new ResultPopup.Choice("Sonraki seviye", ButtonKind.Cta, () => _app.StartLevel(_levelIndex + 1))
                : new ResultPopup.Choice("Tekrar oyna", ButtonKind.Cta, () => _app.StartLevel(_levelIndex));
            Popup = ResultPopup.Show(_app.Ui.Popups, "Kazandın!", Session.MovesLeft + " hamle arttı", stars,
                next,
                new ResultPopup.Choice("Seviyeler", ButtonKind.Secondary, _app.ShowLevelSelect));
        }

        IEnumerator ShowLose()
        {
            yield return new WaitForSeconds(Timing.LoseDelay);
            OnImpact(Impact.Lose);
            Popup = ResultPopup.Show(_app.Ui.Popups, "Kaybettin", RemainingText(), -1,
                new ResultPopup.Choice("Tekrar dene", ButtonKind.Cta, () => _app.StartLevel(_levelIndex)),
                new ResultPopup.Choice("Seviyeler", ButtonKind.Secondary, _app.ShowLevelSelect));
        }

        /// <summary>The move counter's position on the board, where the end-of-level sparks start.</summary>
        Vector3 MovesPanelWorld()
        {
            var screen = UiRoot.PanelToScreen(_hud.MovesPanel.worldBound.center);
            var world = _app.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector3(world.x, world.y, 0f);
        }

        string RemainingText()
        {
            int left = 0;
            foreach (var g in Session.Goals) left += Math.Max(0, g.Remaining);
            return "Hedefe " + left + " kaldı";
        }
    }
}
