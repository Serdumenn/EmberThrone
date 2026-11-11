using System;
using System.Collections;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class GameController : MonoBehaviour
    {
        const float BottomMargin = BoosterBar.Height + 24f;
        const float BoardMargin = 0.32f;
        const int BackdropOrder = -100;
        const float BackdropOverscan = 1.04f;

        GameApp _app;
        BoardInput _input;
        SpriteRenderer _backdrop;
        HudView _hud;
        PraiseView _praise;
        BoosterBar _boosters;
        int _praiseLevel;
        int _wave;
        GridPos _selected = GridPos.Invalid;
        LevelDefinition _level;
        int _levelIndex;
        BoosterType? _armed;
        float _idle;
        bool _hintShown;
        bool _continued;

        public GameSession Session { get; private set; }
        public BoardView View { get; private set; }
        public HudView Hud => _hud;
        public PraiseView Praise => _praise;
        public BoosterBar Boosters => _boosters;
        public BoosterType? Armed => _armed;
        public float HintDelay { get; set; } = Timing.HintDelay;
        public ResultPopup Popup { get; private set; }
        public bool IsBusy { get; private set; }
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
            HintDelay = levelIndex < Timing.EarlyHintLevels ? Timing.EarlyHintDelay : Timing.HintDelay;
            Session = new GameSession(level, seed);
            BuildBackdrop();
            BuildView();

            _hud = HudView.Create(app.Ui.Content, Session, "Seviye " + level.Id, app.ShowLevelSelect);
            _praise = PraiseView.Create(app.Ui.Content);
            _boosters = BoosterBar.Create(app.Ui.Content, PickBooster);
            RefreshBoosters();

            _input = gameObject.AddComponent<BoardInput>();
            _input.Init(app.Camera, View);
            _input.Pressed += p =>
            {
                ResetIdle();
                if (!_armed.HasValue && View.IsGrabbableAt(p)) View.SetLifted(p, true);
            };
            _input.Released += p => View.SetLifted(p, false);
            _input.Swiped += RequestSwap;
            _input.Tapped += OnTap;

            app.Ui.LayoutChanged += FitCamera;
            FitCamera();

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
            View.GoalProgressed += e =>
            {
                Sound.Play(Sfx.Collect);
                _hud.SetGoal(e.GoalIndex, e.Remaining);
            };
            View.Impacted += OnImpact;
            View.WaveStarted += OnWave;
            View.ComboFired += OnCombo;
        }

        void BuildBackdrop()
        {
            _backdrop = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            _backdrop.transform.SetParent(transform, false);
            _backdrop.sprite = Art.Backdrop();
            _backdrop.sharedMaterial = Art.SpriteMaterial;
            _backdrop.sortingOrder = BackdropOrder;
        }

        void RebuildView()
        {
            Tweens.KillAll();
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
            _boosters?.Remove();
            Popup?.Close();
        }

        void Update()
        {
            if (View == null || IsBusy || Popup != null || _armed.HasValue || Session.State != GameState.Playing) return;
            _idle += Time.deltaTime;
            if (_hintShown || _idle < HintDelay) return;
            var hint = Session.FindHint();
            if (!hint.HasValue) return;
            View.ShowHint(hint.Value);
            _hintShown = true;
        }

        void ResetIdle()
        {
            _idle = 0f;
            if (!_hintShown) return;
            _hintShown = false;
            View.ClearHint();
        }

        void RefreshBoosters()
        {
            foreach (var type in BoosterInventory.All) _boosters.SetCount(type, BoosterInventory.Count(type, Session.Rules));
        }

        public void PickBooster(BoosterType type)
        {
            ResetIdle();
            if (IsBusy || Session.State != GameState.Playing) return;
            if (BoosterInventory.Count(type, Session.Rules) <= 0)
            {
                _boosters.Shake(type);
                return;
            }
            Select(GridPos.Invalid);
            if (type == BoosterType.Shuffle)
            {
                Arm(null);
                FireBooster(type, GridPos.Invalid);
                return;
            }
            Arm(_armed == type ? (BoosterType?)null : type);
        }

        void Arm(BoosterType? type)
        {
            _armed = type;
            _boosters.SetArmed(type);
        }

        void FireBooster(BoosterType type, GridPos target)
        {
            var outcome = Session.UseBooster(type, target);
            if (!outcome.Accepted)
            {
                if (target.IsValid) View.Nudge(target);
                else _boosters.Shake(type);
                return;
            }
            BoosterInventory.Spend(type, Session.Rules);
            Arm(null);
            RefreshBoosters();
            Run(outcome);
        }

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

            float centerX = safe.xMin + width * 0.5f;
            float centerY = bottom + height * 0.5f;
            var rest = new Vector3(
                (UiRoot.ScreenWidth * 0.5f - centerX) / ppu,
                (UiRoot.ScreenHeight * 0.5f - centerY) / ppu,
                -10f);
            _app.Shake.SetRest(rest);

            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * UiRoot.ScreenWidth / UiRoot.ScreenHeight;
            var size = _backdrop.sprite.bounds.size;
            _backdrop.transform.position = new Vector3(rest.x, rest.y, 0f);
            _backdrop.transform.localScale = Vector3.one * (Mathf.Max(viewWidth / size.x, viewHeight / size.y) * BackdropOverscan);
        }

        public void OnTap(GridPos p)
        {
            ResetIdle();
            if (IsBusy) return;
            if (_armed.HasValue)
            {
                FireBooster(_armed.Value, p);
                return;
            }
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
            ResetIdle();
            if (IsBusy || _armed.HasValue) return;
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
            ResetIdle();
            IsBusy = true;
            _praiseLevel = 0;
            _wave = 0;
            _input.SetEnabled(false);
            _hud.SetMoves(outcome.MovesLeft);
            SaveIfWon();

            yield return PlayGuarded(outcome);

            CheckSync();
            IsBusy = false;

            if (outcome.State == GameState.Playing) _input.SetEnabled(true);
            else yield return EndLevel();
        }

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

        void OnImpact(Impact impact)
        {
            _app.Shake.Add(Feel.TraumaOf(impact));
            Haptics.Play(Feel.HapticOf(impact));
            Sound.Play(Feel.SoundOf(impact), impact == Impact.Match ? Sound.CascadeStep(_wave) : 0f);
        }

        void OnWave(int wave)
        {
            _wave = wave;
            ShowPraise(wave >= 6 ? 3 : wave >= 4 ? 2 : wave >= 3 ? 1 : 0);
        }

        void OnCombo(ComboKind combo) => ShowPraise(combo == ComboKind.ColorColor ? 3 : 2);

        void ShowPraise(int level)
        {
            if (level <= _praiseLevel) return;
            _praiseLevel = level;
            _praise.Show(level);
        }

        void SaveIfWon()
        {
            if (Session.State == GameState.Won) LevelProgress.SaveStars(_level.Id, Session.Rules.StarsFor(Session.MovesLeft));
        }

        IEnumerator EndLevel() => Session.State == GameState.Won ? ShowWin() : ShowLose();

        IEnumerator ShowWin()
        {
            yield return new WaitForSeconds(Timing.WinDelay);

            int left = Session.MovesLeft;
            int sparks = Mathf.Clamp(left, 1, Timing.WinSparksMax);
            float show = View.Celebrate(MovesPanelWorld(), sparks,
                i => _hud.SetMoves(left - Mathf.RoundToInt((i + 1) * (float)left / sparks)));
            yield return new WaitForSeconds(show + Timing.WinWindowPause);

            OnImpact(Impact.Win);
            int stars = Session.Rules.StarsFor(Session.MovesLeft);
            var next = _app.HasLevel(_levelIndex + 1)
                ? new ResultPopup.Choice("Sonraki seviye", ButtonKind.Cta, AfterAd(() => _app.StartLevel(_levelIndex + 1)))
                : new ResultPopup.Choice("Tekrar oyna", ButtonKind.Cta, AfterAd(() => _app.StartLevel(_levelIndex)));
            Popup = ResultPopup.Show(_app.Ui.Popups, "Kazandın!", Session.MovesLeft + " hamle arttı", stars,
                next,
                new ResultPopup.Choice("Seviyeler", ButtonKind.Secondary, AfterAd(_app.ShowLevelSelect)));
        }

        Action AfterAd(Action go)
        {
            if (_level.Id < AdRules.FirstInterstitialLevel) return go;
            return () => _app.Ads.ShowInterstitialThen(go);
        }

        IEnumerator ShowLose()
        {
            yield return new WaitForSeconds(Timing.LoseDelay);
            OnImpact(Impact.Lose);
            var choices = new List<ResultPopup.Choice>();
            bool offer = !_continued && _app.Ads.IsRewardedReady;
            if (offer)
                choices.Add(new ResultPopup.Choice(ContinueLabel(Session.Rules.ContinueMoves), ButtonKind.Cta, WatchForExtraMoves));
            choices.Add(new ResultPopup.Choice("Tekrar dene", offer ? ButtonKind.Secondary : ButtonKind.Cta, () => _app.StartLevel(_levelIndex)));
            choices.Add(new ResultPopup.Choice("Seviyeler", ButtonKind.Secondary, _app.ShowLevelSelect));
            Popup = ResultPopup.Show(_app.Ui.Popups, "Kaybettin", RemainingText(), -1, choices.ToArray());
        }

        public static string ContinueLabel(int moves) => "İzle: +" + moves + " hamle";

        void WatchForExtraMoves()
        {
            if (_continued || Session.State != GameState.Lost) return;
            _app.Ads.ShowRewarded(ContinueWithExtraMoves, null);
        }

        void ContinueWithExtraMoves()
        {
            if (_continued || Session.State != GameState.Lost) return;
            _continued = true;
            Popup?.Close();
            Popup = null;
            Session.AddMoves(Session.Rules.ContinueMoves);
            Sound.Play(Sfx.MovesAdded);
            _hud.SetMoves(Session.MovesLeft);
            ResetIdle();
            _input.SetEnabled(true);
        }

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
