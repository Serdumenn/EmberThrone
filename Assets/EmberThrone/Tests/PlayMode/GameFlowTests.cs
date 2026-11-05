using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using EmberThrone.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EmberThrone.Game.Tests
{
    /// <summary>
    /// Runs the real game: real view, real input path, real animations (sped up by Time.timeScale).
    /// Any Debug.LogError fails a test, and that includes the view-versus-engine check after every outcome.
    /// Rows are written top first, as in level files.
    /// </summary>
    public class GameFlowTests
    {
        const string Even = "YPOGY";
        const string Odd = "OGYPO";
        const int Seed = 12345;

        GameApp _app;
        Mouse _mouse;
        System.Collections.Generic.Dictionary<int, int> _savedStars;
        InputSettings.BackgroundBehavior _background;
        InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        GameController Game => _app.Game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savedStars = StarsBackup.Snapshot();
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _mouse = InputSystem.AddDevice<Mouse>();
            _mouse.MakeCurrent();
            UiRoot.SafeAreaOverride = null;
            Time.timeScale = 1f;
            _app = GameApp.Create();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            UiRoot.SafeAreaOverride = null;
            InputSystem.RemoveDevice(_mouse);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            Object.Destroy(_app.gameObject);
            StarsBackup.Restore(_savedStars);
            yield return null;
        }

        // ---------------------------------------------------------------- helpers

        static LevelDefinition Level(int moves, GoalDefinition goal, params string[] board)
        {
            var spawn = board.Select(r => new string('.', r.Length)).ToArray(); // no spawners: exact results
            var level = LevelParser.FromLayers(board, null, null, spawn);
            level.Moves = moves;
            level.ColorCount = 6;
            level.Goals.Add(goal ?? new GoalDefinition(GoalType.Color, 999, GemColor.Red));
            return level;
        }

        IEnumerator Begin(LevelDefinition level)
        {
            _app.StartLevel(level, 0, Seed);
            yield return null;
            Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));
        }

        Vector2 ScreenOf(GridPos p)
        {
            return _app.Camera.WorldToScreenPoint(Game.View.World(p));
        }

        void SetMouse(Vector2 position, bool pressed)
        {
            var state = new MouseState { position = position };
            state = state.WithButton(MouseButton.Left, pressed);
            InputSystem.QueueStateEvent(_mouse, state);
        }

        IEnumerator Drag(GridPos a, GridPos b)
        {
            SetMouse(ScreenOf(a), false);
            yield return null;
            SetMouse(ScreenOf(a), true);
            yield return null;
            yield return null;
            SetMouse(ScreenOf(b), true);
            yield return null;
            yield return null;
            SetMouse(ScreenOf(b), false);
            yield return null;
        }

        IEnumerator Click(GridPos p)
        {
            SetMouse(ScreenOf(p), false);
            yield return null;
            SetMouse(ScreenOf(p), true);
            yield return null;
            yield return null;
            SetMouse(ScreenOf(p), false);
            yield return null;
            yield return null;
        }

        IEnumerator WaitIdle()
        {
            yield return null;
            float limit = Time.realtimeSinceStartup + 60f;
            while (Game.IsBusy)
            {
                Assert.Less(Time.realtimeSinceStartup, limit, "outcome animation never finished");
                yield return null;
            }
        }

        // ---------------------------------------------------------------- level list and start

        [Test]
        public void Boot_ListsEveryLevelFile()
        {
            Assert.GreaterOrEqual(_app.Levels.Count, 10);
            Assert.AreEqual(_app.Levels.Count, _app.LevelSelect.ButtonCount);
            CollectionAssert.IsOrdered(_app.Levels.Select(l => l.Id));
        }

        [UnityTest]
        public IEnumerator EveryLevel_StartsInSync_WithItsHud()
        {
            for (int i = 0; i < _app.Levels.Count; i++)
            {
                _app.StartLevel(i);
                yield return null;
                Assert.IsNull(Game.View.FindMismatch(Game.Session.Board), "level index " + i);
                Assert.AreEqual(_app.Levels[i].Moves.ToString(), Game.Hud.MovesText);
                Assert.AreEqual(SleepTimeout.NeverSleep, Screen.sleepTimeout);
            }
            _app.ShowLevelSelect();
            yield return null;
            Assert.AreEqual(SleepTimeout.SystemSetting, Screen.sleepTimeout);
            Assert.IsNull(_app.Game);
            Assert.AreEqual(_app.Levels.Count, _app.LevelSelect.ButtonCount);
        }

        // ---------------------------------------------------------------- input

        [UnityTest]
        public IEnumerator Swipe_OnValidMove_SpendsAMove()
        {
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return null;
            var move = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            int before = Game.Session.MovesLeft;
            yield return Drag(move.A, move.B);
            yield return WaitIdle();
            Assert.AreEqual(before - 1, Game.Session.MovesLeft);
            Assert.AreEqual((before - 1).ToString(), Game.Hud.MovesText);
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator Swipe_WithoutMatch_SwapsBack_AndCostsNothing()
        {
            yield return Begin(Level(10, null, Even, Odd, Even, Odd, Even));
            int before = Game.Session.MovesLeft;
            yield return Drag(new GridPos(0, 0), new GridPos(1, 0));
            yield return WaitIdle();
            Assert.AreEqual(before, Game.Session.MovesLeft);
            Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));
        }

        [UnityTest]
        public IEnumerator TwoTaps_SwapNeighbors()
        {
            // Y at (2,1) moving up to (2,2) completes three Y in the row above.
            yield return Begin(Level(10, null, "OGPYO", "YPOGP", "GYYOG", "PGYGO", "YOPGY"));
            var move = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            int before = Game.Session.MovesLeft;
            yield return Click(move.A);
            Assert.AreEqual(before, Game.Session.MovesLeft, "the first tap only selects");
            yield return Click(move.B);
            yield return WaitIdle();
            Assert.AreEqual(before - 1, Game.Session.MovesLeft);
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator Tap_OnSpecial_FiresIt()
        {
            yield return Begin(Level(10, null, Even, Odd, "YP-GY", Odd, Even));
            int before = Game.Session.MovesLeft;
            yield return Click(new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(before - 1, Game.Session.MovesLeft);
            Assert.AreEqual(0, Game.Session.Board.CountPieces(PieceKind.RocketH));
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator Input_IsIgnoredWhileAnimating()
        {
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return null;
            var move = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            Game.RequestSwap(move.A, move.B);
            int after = Game.Session.MovesLeft;
            Assert.IsTrue(Game.IsBusy);
            var another = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            Game.RequestSwap(another.A, another.B);
            Assert.AreEqual(after, Game.Session.MovesLeft);
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
        }

        // ---------------------------------------------------------------- specials

        [UnityTest]
        public IEnumerator FourInARow_CreatesARocket()
        {
            yield return Begin(Level(10, null, Even, Odd, "RRBRY", "OGRPO", Even));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(1, Game.Session.Board.CountPieces(PieceKind.RocketV));
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator FiveInARow_CreatesAColorBomb()
        {
            yield return Begin(Level(10, null, Even, Odd, "RRBRR", "OGRPO", Even));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(1, Game.Session.Board.CountPieces(PieceKind.ColorBomb));
            Assert.IsNull(Game.LastMismatch);
        }

        static readonly char[] Specials = { '-', '|', '*', '^', '@' };

        [UnityTest]
        public IEnumerator EveryCombo_PlaysInSync()
        {
            Time.timeScale = 8f;
            foreach (var a in Specials)
            {
                foreach (var b in Specials)
                {
                    yield return Begin(Level(10, null, Even, Odd, "YP" + a + b + "Y", Odd, Even));
                    Game.RequestSwap(new GridPos(2, 2), new GridPos(3, 2));
                    yield return WaitIdle();
                    Assert.IsNull(Game.LastMismatch, "combo " + a + b);
                }
            }
        }

        [UnityTest]
        public IEnumerator SpecialSwappedWithGem_Fires()
        {
            Time.timeScale = 8f;
            foreach (var s in Specials)
            {
                yield return Begin(Level(10, null, Even, Odd, "YP" + s + "GY", Odd, Even));
                Game.RequestSwap(new GridPos(2, 2), new GridPos(3, 2));
                yield return WaitIdle();
                Assert.IsNull(Game.LastMismatch, "special " + s);
            }
        }

        // ---------------------------------------------------------------- end of level

        [UnityTest]
        public IEnumerator Win_ShowsStars_AndSavesThem()
        {
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            Assert.Greater(LevelProgress.GetStars(4242), 0, "saved before the animations, in case the player leaves");
            yield return WaitIdle();
            Assert.AreEqual(GameState.Won, Game.Session.State);
            yield return new WaitForSeconds(Timing.WinDelay + 0.1f);
            Assert.AreEqual("Kazandın!", Game.Popup.Title);
            Assert.AreEqual(Game.Session.Rules.StarsFor(Game.Session.MovesLeft), LevelProgress.GetStars(4242));
            Assert.IsTrue(Game.Hud.GoalChecked(0));
        }

        [UnityTest]
        public IEnumerator Level_FinishedOnStart_ShowsItsWindow()
        {
            // A box goal on a board without boxes has nothing left to do.
            yield return Begin(Level(10, new GoalDefinition(GoalType.Box), Even, Odd, Even, Odd, Even));
            Assert.AreEqual(GameState.Won, Game.Session.State);
            yield return new WaitForSeconds(Timing.WinDelay + 0.1f);
            Assert.AreEqual("Kazandın!", Game.Popup.Title);
        }

        [UnityTest]
        public IEnumerator ViewFailure_RedrawsTheBoard_AndPlayGoesOn()
        {
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return null;
            var move = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            int before = Game.Session.MovesLeft;
            Object.DestroyImmediate(Game.View.transform.Find("Pieces").gameObject);

            LogAssert.Expect(LogType.Exception, new Regex("MissingReferenceException"));
            Game.RequestSwap(move.A, move.B);
            yield return WaitIdle();

            Assert.AreEqual(before - 1, Game.Session.MovesLeft);
            Assert.AreEqual((before - 1).ToString(), Game.Hud.MovesText);
            Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));

            var next = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            yield return Drag(next.A, next.B);
            yield return WaitIdle();
            Assert.AreEqual(before - 2, Game.Session.MovesLeft, "input works again");
        }

        [UnityTest]
        public IEnumerator Lose_ShowsLoseWindow_AndRetryRestarts()
        {
            yield return Begin(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even));
            var game = Game;
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(GameState.Lost, Game.Session.State);
            Assert.IsNull(Game.Popup, "the window waits so the player can see the board first");
            yield return new WaitForSeconds(Timing.LoseDelay + 0.1f);
            Assert.AreEqual("Kaybettin", Game.Popup.Title);

            var buttons = Game.Popup.GetComponentsInChildren<Button>();
            Assert.AreEqual(2, buttons.Length);
            buttons[0].onClick.Invoke();
            yield return null;
            Assert.AreNotSame(game, _app.Game, "retry starts a new game");
            Assert.AreEqual(GameState.Playing, _app.Game.Session.State);
        }

        [UnityTest]
        public IEnumerator EveryLevel_PlayedToTheEnd_StaysInSync()
        {
            Time.timeScale = 12f;
            for (int i = 0; i < _app.Levels.Count; i++)
            {
                _app.StartLevel(_app.Levels[i], i, Seed + i);
                yield return null;
                var session = Game.Session;
                int guard = 0;
                while (session.State == GameState.Playing)
                {
                    Assert.Less(guard++, 200, "level index " + i + " did not end");
                    var hint = session.FindHint();
                    Assert.IsTrue(hint.HasValue, "no move on level index " + i);
                    var move = hint.Value;
                    if (move.Type == MoveType.Tap) Game.OnTap(move.A);
                    else Game.RequestSwap(move.A, move.B);
                    yield return WaitIdle();
                }
                Assert.IsNull(Game.LastMismatch, "level index " + i);
                yield return new WaitForSeconds(Timing.LoseDelay + 0.2f);
                Assert.IsNotNull(Game.Popup, "level index " + i);
            }
        }

        // ---------------------------------------------------------------- layout

        [UnityTest]
        public IEnumerator Board_StaysInTheFreeArea_WithANotch()
        {
            int w = UiRoot.ScreenWidth, h = UiRoot.ScreenHeight;
            UiRoot.SafeAreaOverride = new Rect(w * 0.02f, h * 0.05f, w * 0.96f, h * 0.88f);
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return null;
            yield return null;

            var safe = UiRoot.SafeArea;
            float hudBottom = safe.yMax - HudView.Height * UiRoot.Scale;
            var view = Game.View;
            var low = ScreenOf(new GridPos(0, 0));
            var high = ScreenOf(new GridPos(view.Width - 1, view.Height - 1));
            float half = (high.x - low.x) / (view.Width - 1) * 0.5f;
            Assert.GreaterOrEqual(low.x - half, safe.xMin - 0.5f);
            Assert.LessOrEqual(high.x + half, safe.xMax + 0.5f);
            Assert.GreaterOrEqual(low.y - half, safe.yMin - 0.5f);
            Assert.LessOrEqual(high.y + half, hudBottom + 0.5f, "the board must not run under the HUD");
        }
    }
}
