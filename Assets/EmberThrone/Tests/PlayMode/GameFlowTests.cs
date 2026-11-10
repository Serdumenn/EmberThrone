using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using EmberThrone.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using VisualElement = UnityEngine.UIElements.VisualElement;

namespace EmberThrone.Game.Tests
{
    public class GameFlowTests
    {
        const string Even = "YPOGY";
        const string Odd = "OGYPO";
        const int Seed = 12345;

        GameApp _app;
        Mouse _mouse;
        System.Collections.Generic.Dictionary<int, int> _savedStars;
        System.Collections.Generic.Dictionary<BoosterType, int> _savedBoosters;
        System.Collections.Generic.Dictionary<string, int> _savedSettings;
        InputSettings.BackgroundBehavior _background;
        InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        GameController Game => _app.Game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savedStars = StarsBackup.Snapshot();
            _savedBoosters = StarsBackup.SnapshotBoosters();
            _savedSettings = StarsBackup.SnapshotSettings();
            CameraShake.Enabled = true;
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
            StarsBackup.RestoreBoosters(_savedBoosters);
            StarsBackup.RestoreSettings(_savedSettings);
            yield return null;
        }

        static LevelDefinition Level(int moves, GoalDefinition goal, params string[] board)
        {
            var spawn = board.Select(r => new string('.', r.Length)).ToArray();
            var level = LevelParser.FromLayers(board, null, null, spawn);
            level.Moves = moves;
            level.ColorCount = 6;
            level.Goals.Add(goal ?? new GoalDefinition(GoalType.Color, 999, GemColor.Red));
            return level;
        }

        IEnumerator Begin(LevelDefinition level)
        {
            _app.StartLevel(level, 0, Seed);
            Game.HintDelay = float.MaxValue;
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

        IEnumerator TapUi(VisualElement e)
        {
            yield return null;
            Assert.IsNotNull(e.panel, "element is on screen");
            var screen = UiRoot.PanelToScreen(e.worldBound.center);
            SetMouse(screen, false);
            yield return null;
            SetMouse(screen, true);
            yield return null;
            yield return null;
            SetMouse(screen, false);
            yield return null;
            yield return null;
        }

        IEnumerator WaitForPopup(string why = null)
        {
            float limit = Time.time + 10f;
            while (Game.Popup == null)
            {
                Assert.Less(Time.time, limit, "no window " + why);
                yield return null;
            }
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

        [Test]
        public void Boot_ListsEveryLevelFile()
        {
            Assert.GreaterOrEqual(_app.Levels.Count, 10);
            Assert.AreEqual(_app.Levels.Count, _app.LevelSelect.ButtonCount);
            CollectionAssert.IsOrdered(_app.Levels.Select(l => l.Id));
        }

        [UnityTest]
        public IEnumerator LevelSelect_TappingACard_ShowsItsGoals_ThenPlayStartsIt()
        {
            LevelProgress.SaveStars(_app.Levels[0].Id, 1);
            _app.ShowLevelSelect();
            yield return null;
            Assert.IsFalse(_app.LevelSelect.IsLocked(1));
            yield return TapUi(_app.LevelSelect.Buttons[1]);
            Assert.IsNull(_app.Game, "a window comes first");
            var start = _app.LevelStart;
            Assert.IsNotNull(start, "the tap reached the card");
            Assert.AreEqual(_app.Levels[1].Goals.Count, start.GoalCount);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(start.PlayButton);
            Assert.IsNull(_app.LevelStart);
            Assert.IsNotNull(_app.Game);
            Assert.AreEqual(_app.Levels[1].Moves.ToString(), Game.Hud.MovesText);
        }

        [UnityTest]
        public IEnumerator LevelSelect_LockedCard_StaysShut_AndBackClosesTheWindow()
        {
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            _app.ShowLevelSelect();
            yield return null;
            Assert.AreEqual(0, _app.LevelSelect.CurrentIndex);
            Assert.IsFalse(_app.LevelSelect.IsLocked(0));
            Assert.IsTrue(_app.LevelSelect.IsLocked(1));
            yield return TapUi(_app.LevelSelect.Buttons[1]);
            Assert.IsNull(_app.LevelStart, "a locked level does not open");

            yield return TapUi(_app.LevelSelect.Buttons[0]);
            Assert.IsNotNull(_app.LevelStart);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(_app.LevelStart.BackButton);
            Assert.IsNull(_app.LevelStart);
            Assert.IsNull(_app.Game);
        }

        [UnityTest]
        public IEnumerator HomeButton_GoesBackToTheList()
        {
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return TapUi(Game.Hud.HomeButton);
            Assert.IsNull(_app.Game);
            Assert.AreEqual(_app.Levels.Count, _app.LevelSelect.ButtonCount);
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
            yield return WaitForPopup();
            Assert.AreEqual("Kazandın!", Game.Popup.Title);
            Assert.AreEqual(Game.Session.Rules.StarsFor(Game.Session.MovesLeft), LevelProgress.GetStars(4242));
            Assert.IsTrue(Game.Hud.GoalChecked(0));
        }

        [UnityTest]
        public IEnumerator Level_FinishedOnStart_ShowsItsWindow()
        {
            yield return Begin(Level(10, new GoalDefinition(GoalType.Box), Even, Odd, Even, Odd, Even));
            Assert.AreEqual(GameState.Won, Game.Session.State);
            yield return WaitForPopup();
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

            Assert.AreEqual(3, Game.Popup.Buttons.Count);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons[1]);
            Assert.AreNotSame(game, _app.Game, "retry starts a new game");
            Assert.AreEqual(GameState.Playing, _app.Game.Session.State);
        }

        [UnityTest]
        public IEnumerator Lose_OffersExtraMovesOnce_AndPlayContinues()
        {
            yield return Begin(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even));
            var game = Game;
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.AreEqual("+5 hamle", Game.Popup.Buttons[0].text);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons[0]);
            Assert.AreSame(game, _app.Game, "the same game goes on");
            Assert.IsNull(Game.Popup);
            Assert.AreEqual(GameState.Playing, Game.Session.State);
            Assert.AreEqual(5, Game.Session.MovesLeft);
            Assert.AreEqual("5", Game.Hud.MovesText);
        }

        [UnityTest]
        public IEnumerator BoosterBar_Hammer_ClearsTheTappedPiece_WithoutSpendingAMove()
        {
            BoosterInventory.Set(BoosterType.Hammer, 2);
            yield return Begin(Level(10, null, Even, Odd, Even, Odd, Even));
            Assert.AreEqual("2", Game.Boosters.CountText(BoosterType.Hammer));
            int moves = Game.Session.MovesLeft;
            yield return TapUi(Game.Boosters.ButtonOf(BoosterType.Hammer));
            Assert.AreEqual(BoosterType.Hammer, Game.Armed);

            var target = new GridPos(2, 2);
            int id = Game.Session.Board.CellAt(target).Piece.Id;
            yield return Click(target);
            yield return WaitIdle();

            Assert.IsNull(Game.Armed);
            Assert.AreEqual(moves, Game.Session.MovesLeft, "a booster is not a move");
            Assert.AreEqual(1, BoosterInventory.Count(BoosterType.Hammer, Game.Session.Rules));
            Assert.AreEqual("1", Game.Boosters.CountText(BoosterType.Hammer));
            var piece = Game.Session.Board.CellAt(target).Piece;
            Assert.IsTrue(piece == null || piece.Id != id, "the piece under the hammer is gone");
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator BoosterBar_TappingAgain_Disarms_AndAnEmptyBoosterDoesNothing()
        {
            BoosterInventory.Set(BoosterType.RowBlast, 1);
            BoosterInventory.Set(BoosterType.ColumnBlast, 0);
            yield return Begin(Level(10, null, Even, Odd, Even, Odd, Even));
            yield return TapUi(Game.Boosters.ButtonOf(BoosterType.RowBlast));
            Assert.AreEqual(BoosterType.RowBlast, Game.Armed);
            yield return TapUi(Game.Boosters.ButtonOf(BoosterType.RowBlast));
            Assert.IsNull(Game.Armed);
            yield return TapUi(Game.Boosters.ButtonOf(BoosterType.ColumnBlast));
            Assert.IsNull(Game.Armed, "no charges left");
            Assert.AreEqual(1, BoosterInventory.Count(BoosterType.RowBlast, Game.Session.Rules));
        }

        [UnityTest]
        public IEnumerator Sound_AllClipsLoad_AndASwapPlaysSounds()
        {
            Assert.AreEqual(System.Enum.GetValues(typeof(Sfx)).Length - 1, Sound.Loaded, "every sound has a clip");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, "someone has to hear it");
            Sound.Enabled = true;
            yield return Begin(Level(10, null, Even, Odd, "RRBRY", "OGRPO", Even));
            int before = Sound.Count;
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.Greater(Sound.Count, before + 1, "a swap and its match are heard");
        }

        [UnityTest]
        public IEnumerator Settings_TogglesSoundHapticsAndShake_AndCloses()
        {
            Sound.Enabled = true;
            Haptics.Enabled = true;
            CameraShake.Enabled = true;
            yield return TapUi(_app.LevelSelect.SettingsButton);
            var settings = _app.LevelSelect.Settings;
            Assert.IsNotNull(settings);
            Assert.AreEqual("Ses: Açık", settings.SoundButton.text);
            Assert.AreEqual("Titreşim: Açık", settings.HapticsButton.text);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);

            yield return TapUi(settings.SoundButton);
            Assert.IsFalse(Sound.Enabled);
            Assert.AreEqual("Ses: Kapalı", settings.SoundButton.text);
            int count = Sound.Count;
            yield return TapUi(settings.HapticsButton);
            Assert.IsFalse(Haptics.Enabled);
            Assert.AreEqual("Titreşim: Kapalı", settings.HapticsButton.text);
            Assert.AreEqual(count, Sound.Count, "a muted game stays silent");
            yield return TapUi(settings.ShakeButton);
            Assert.IsFalse(CameraShake.Enabled);
            Assert.AreEqual("Sarsıntı: Kapalı", settings.ShakeButton.text);
            _app.Shake.Add(0.8f);
            Assert.AreEqual(0f, _app.Shake.Trauma, "no shake when it is turned off");

            yield return TapUi(settings.CloseButton);
            Assert.IsNull(_app.LevelSelect.Settings);
            Assert.IsNull(settings.Root.panel);
        }

        [UnityTest]
        public IEnumerator Hint_ComesSooner_OnTheFirstLevels()
        {
            _app.StartLevel(0);
            yield return null;
            Assert.AreEqual(Timing.EarlyHintDelay, Game.HintDelay);
            _app.StartLevel(Timing.EarlyHintLevels);
            yield return null;
            Assert.AreEqual(Timing.HintDelay, Game.HintDelay);
        }

        [UnityTest]
        public IEnumerator Hint_PulsesAMove_AfterIdle_AndAMoveClearsIt()
        {
            yield return Begin(Level(10, null, Even, Odd, "RRBRY", "OGRPO", Even));
            Game.HintDelay = 0.3f;
            float limit = Time.time + 5f;
            while (!Game.View.HintShown)
            {
                Assert.Less(Time.time, limit, "no hint after idling");
                yield return null;
            }
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            Assert.IsFalse(Game.View.HintShown, "a move clears the hint");
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
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
                yield return WaitForPopup("level index " + i);
            }
        }

        [UnityTest]
        public IEnumerator Win_TurnsMovesLeftIntoFireworks_ThenShowsTheWindow()
        {
            yield return Begin(Level(6, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even));
            int buzzes = Haptics.Count;
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(GameState.Won, Game.Session.State);
            Assert.AreEqual("5", Game.Hud.MovesText);
            Assert.IsNull(Game.Popup, "the fireworks come first");

            yield return WaitForPopup();
            Assert.AreEqual("0", Game.Hud.MovesText, "every move left became a firework");
            Assert.AreEqual(5, Game.Session.MovesLeft, "the celebration is only a show");
            Assert.AreEqual(HapticLevel.Strong, Haptics.LastLevel, "the win window comes with a strong buzz");
            Assert.Greater(Haptics.Count, buzzes);
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator Combo_ShowsPraise_AndTheShakeSettles()
        {
            yield return Begin(Level(20, null, "YPOGYO", "OGYPOG", "YP-*YO", "OGYPOG", "YPOGYO"));
            Assert.IsNull(Game.Praise.Text);
            Game.RequestSwap(new GridPos(2, 2), new GridPos(3, 2));

            float limit = Time.time + 3f;
            while (Game.Praise.Text == null)
            {
                Assert.Less(Time.time, limit, "no praise for a rocket + bomb combo");
                yield return null;
            }
            Assert.AreEqual("Muhteşem!", Game.Praise.Text);
            Assert.Greater(_app.Shake.Trauma, 0f, "a combo shakes the camera");

            yield return WaitIdle();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(0f, _app.Shake.Trauma);
            Assert.AreEqual(_app.Shake.Rest, _app.Camera.transform.position, "the camera is back at rest");
            Assert.AreEqual(Quaternion.identity, _app.Camera.transform.rotation);
            Assert.IsNull(Game.LastMismatch);
        }

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

        [UnityTest]
        public IEnumerator Hud_StaysInItsBand_AboveTheBoard()
        {
            int w = UiRoot.ScreenWidth, h = UiRoot.ScreenHeight;
            UiRoot.SafeAreaOverride = new Rect(0f, h * 0.03f, w, h * 0.92f);
            _app.StartLevel(_app.Levels[8], 8, Seed);
            yield return null;
            yield return null;
            yield return null;

            var hud = Game.Hud.Root;
            float bandBottom = hud.worldBound.yMin + HudView.Height;
            float safeTop = (h - UiRoot.SafeArea.yMax) / UiRoot.Scale;
            Assert.AreEqual(safeTop, hud.worldBound.yMin, 0.5f, "the HUD starts at the safe area");
            float panelWidth = w / UiRoot.Scale;
            foreach (var e in UnityEngine.UIElements.UQueryExtensions.Query<VisualElement>(hud).ToList())
            {
                if (e.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None) continue;
                Assert.LessOrEqual(e.worldBound.yMax, bandBottom + 0.5f, e.name + " runs below the HUD band");
                Assert.GreaterOrEqual(e.worldBound.xMin, -0.5f, e.name + " leaves the screen");
                Assert.LessOrEqual(e.worldBound.xMax, panelWidth + 0.5f, e.name + " leaves the screen");
            }
        }
    }
}
