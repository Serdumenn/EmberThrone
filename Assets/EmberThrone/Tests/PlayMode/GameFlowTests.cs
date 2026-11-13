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
        const long Noon = 12 * 3600;
        const int Seed = 12345;

        GameApp _app;
        Mouse _mouse;
        System.Collections.Generic.Dictionary<int, int> _savedStars;
        string _savedProfile;
        long _now;
        System.Collections.Generic.Dictionary<string, int> _savedSettings;
        InputSettings.BackgroundBehavior _background;
        InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        GameController Game => _app.Game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _savedStars = StarsBackup.Snapshot();
            _savedProfile = StarsBackup.SnapshotProfile();
            StarsBackup.FreshProfile();
            _savedSettings = StarsBackup.SnapshotSettings();
            CameraShake.Enabled = true;
            AdService.SimulateRewardedInEditor = true;
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _mouse = InputSystem.AddDevice<Mouse>();
            _mouse.MakeCurrent();
            UiRoot.SafeAreaOverride = null;
            Time.timeScale = 1f;
            GameApp.ShowLoading = false;
            GameApp.ShowStories = false;
            GameApp.StreakBoosts = false;
            GameApp.WinFinale = false;
            GameApp.OfferStarter = false;
            _app = GameApp.Create();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameApp.ShowLoading = true;
            GameApp.ShowStories = true;
            GameApp.StreakBoosts = true;
            GameApp.WinFinale = true;
            GameApp.OfferStarter = true;
            UiRoot.SafeAreaOverride = null;
            InputSystem.RemoveDevice(_mouse);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            Object.Destroy(_app.gameObject);
            StarsBackup.Restore(_savedStars);
            StarsBackup.RestoreProfile(_savedProfile);
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
            yield return OpenWorld();
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
            yield return OpenWorld();
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
            yield return TapUi(Game.Boosters.HomeButton);
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
                Assert.IsTrue(_app.ScreenStaysOn, "the screen stays on during a level");
            }
            _app.ShowLevelSelect();
            yield return null;
            Assert.IsFalse(_app.ScreenStaysOn, "the screen may sleep on the map");
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

            Assert.AreEqual(4, Game.Popup.Buttons.Count, "ad, coins, retry, levels");
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons.First(b => b.text == "Tekrar dene"));
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
            Assert.AreEqual("İzle: +5 hamle", Game.Popup.Buttons[0].text);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons[0]);
            Assert.AreEqual(1, _app.Ads.RewardedShown, "the extra moves come from a rewarded ad");
            Assert.AreSame(game, _app.Game, "the same game goes on");
            Assert.IsNull(Game.Popup);
            Assert.AreEqual(GameState.Playing, Game.Session.State);
            Assert.AreEqual(5, Game.Session.MovesLeft);
            Assert.AreEqual("5", Game.Hud.MovesText);
        }

        [UnityTest]
        public IEnumerator Lose_WithoutAReadyAd_StillOffersMovesForCoins_AndTheShopWhenShort()
        {
            AdService.SimulateRewardedInEditor = false;
            yield return Begin(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int price = Meta.Rules.ExtraMovesCost(0);
            Assert.AreEqual(3, Game.Popup.Buttons.Count);
            Assert.AreEqual(GameController.CoinContinueLabel(5, price), Game.Popup.Buttons[0].text, "the moves offer is always there");
            Assert.AreEqual("Tekrar dene", Game.Popup.Buttons[1].text);
            Assert.AreEqual(0, _app.Ads.RewardedShown);
            Assert.Less(Meta.Economy.Coins, price);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons[0]);
            Assert.IsNotNull(_app.Modal, "short of coins, the offer opens the shop");
            Assert.AreEqual(GameState.Lost, Game.Session.State);
        }

        [UnityTest]
        public IEnumerator Streak_PutsSpecialsOnTheNextBoard_AndGivingUpEndsIt()
        {
            GameApp.StreakBoosts = true;
            Meta.Economy.Profile.WinStreak = 2;
            _app.StartLevel(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even), 0, Seed);
            Game.HintDelay = float.MaxValue;
            yield return null;
            Assert.IsTrue(Game.IsBusy, "the specials land after the intro");
            yield return WaitIdle();
            Assert.AreEqual(2, Game.StreakSpecialsPlaced);
            int specials = 0;
            var board = Game.Session.Board;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (board.CellAt(x, y).Piece != null && board.CellAt(x, y).Piece.IsSpecial) specials++;
            Assert.AreEqual(2, specials, "a rocket and a bomb wait on the board");
            Assert.IsNull(Game.LastMismatch);
            Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));

            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            StringAssert.Contains("serin biter", Game.Popup.Message);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons.First(b => b.text == "Tekrar dene"));
            Assert.AreEqual(0, Meta.Economy.WinStreak, "giving up ends the streak");
        }

        [UnityTest]
        public IEnumerator Win_TurnsLeftoverMovesIntoRockets_AndPaysForThem()
        {
            GameApp.WinFinale = true;
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4243;
            PlayerPrefs.DeleteKey(LevelProgress.StarsKey + level.Id);
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.IsTrue(Game.Session.FinalePlayed);
            int rockets = Game.Session.Stats.FinaleSpecials;
            Assert.Greater(rockets, 0);
            Assert.AreEqual(rockets * Meta.Rules.FinaleCoinsPerSpecial, Game.FinaleCoins);
            Assert.AreEqual(Meta.Rules.WinReward(Game.Session.Rules.StarsFor(Game.Session.MovesLeft)) + Game.FinaleCoins, Game.WinCoins);
            Assert.IsNull(Game.LastMismatch);
            Assert.AreEqual(1, Meta.Economy.WinStreak, "a win grows the streak");
        }

        [UnityTest]
        public IEnumerator Win_AsksForAnInterstitial_OnlyAfterTheFirstLevels()
        {
            var early = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            early.Id = AdRules.FirstInterstitialLevel - 1;
            yield return Begin(early);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            yield return new WaitForSeconds(Timing.ModalIn + Timing.StarFill * 3f);
            yield return TapUi(Game.Popup.Buttons[1]);
            Assert.IsNull(_app.Game, "back on the map");
            Assert.AreEqual(0, _app.Ads.InterstitialRequests, "no ads on the first levels");

            var later = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            later.Id = 4242;
            yield return Begin(later);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            yield return new WaitForSeconds(Timing.ModalIn + Timing.StarFill * 3f);
            yield return TapUi(Game.Popup.Buttons[1]);
            Assert.IsNull(_app.Game, "the ad does not block the way back");
            Assert.AreEqual(1, _app.Ads.InterstitialRequests);
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
            Assert.AreEqual(1, BoosterInventory.Count(BoosterType.Hammer));
            Assert.AreEqual("1", Game.Boosters.CountText(BoosterType.Hammer));
            var piece = Game.Session.Board.CellAt(target).Piece;
            Assert.IsTrue(piece == null || piece.Id != id, "the piece under the hammer is gone");
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator BoosterBar_TappingAgain_Disarms_AndAnEmptyBoosterOpensTheShop()
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
            Assert.IsTrue(_app.Modal is ShopPopup, "an empty booster leads to the shop");
            Assert.AreEqual(1, BoosterInventory.Count(BoosterType.RowBlast));
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

            string opened = null;
            var open = SettingsPopup.OpenUrl;
            SettingsPopup.OpenUrl = url => opened = url;
            yield return TapUi(settings.PolicyButton);
            SettingsPopup.OpenUrl = open;
            Assert.AreEqual(SettingsPopup.PrivacyPolicyUrl, opened, "the privacy policy opens from Settings");
            Assert.IsNull(settings.PrivacyButton, "privacy options show only where consent is required");

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
        [Timeout(900000)]
        public IEnumerator EveryLevel_PlayedToTheEnd_StaysInSync()
        {
            Time.timeScale = 24f;
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

        [UnityTest]
        public IEnumerator Effects_UseTheirOwnShaders_AndThrowParticles()
        {
            Assert.AreEqual("EmberThrone/Additive", FxArt.Additive.shader.name);
            Assert.AreEqual("EmberThrone/Piece", FxArt.Piece.shader.name);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.IsTrue(FxArt.Additive.shader.isSupported);
                Assert.IsTrue(FxArt.Piece.shader.isSupported);
            }

            yield return Begin(Level(10, null, Even, Odd, "YP*GY", Odd, Even));
            yield return Click(new GridPos(2, 2));
            float limit = Time.time + 2f;
            while (Game.View.Fx.Particles.Alive < 20)
            {
                Assert.Less(Time.time, limit, "the bomb throws sparks, embers and smoke");
                yield return null;
            }
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
        }

        [UnityTest]
        public IEnumerator Bomb_FreezesTheFrame_ThenTimeRunsAgain()
        {
            yield return Begin(Level(10, null, Even, Odd, "YP*GY", Odd, Even));
            int before = HitStop.Count;
            yield return Click(new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(before + 1, HitStop.Count);
            Assert.AreEqual(1f, Time.timeScale, "time runs again after the hit");
        }

        [UnityTest]
        public IEnumerator Frost_SpreadsOnTheBoard_AndTheGoalGrows()
        {
            var board = new[] { Even, Odd, "RRBRY", Odd, Even };
            var level = LevelParser.FromLayers(board, null, null, board.Select(r => new string('.', r.Length)).ToArray(),
                new[] { ".....", ".....", ".....", ".....", "1...." });
            level.Moves = 10;
            level.ColorCount = 6;
            level.Goals.Add(new GoalDefinition(GoalType.Cover));
            yield return Begin(level);
            Assert.IsTrue(Game.Hud.FrostIcons, "the goal shows frost, not ice");
            Assert.AreEqual("1", Game.Hud.GoalText(0));
            int spreads = 0;
            Game.View.FrostSpread += _ => spreads++;
            Game.RequestSwap(new GridPos(3, 2), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(1, spreads, "the frost creeps to a neighbour");
            Assert.AreEqual(1, Game.Session.Stats.FrostSpreads);
            Assert.IsNull(Game.LastMismatch);
            Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));
            Assert.AreEqual("2", Game.Hud.GoalText(0), "the goal grows with the frost");
        }

        [UnityTest]
        public IEnumerator NewObstacles_BreakInSync_AndCountTowardTheirGoals()
        {
            BoosterInventory.Set(BoosterType.Hammer, 20);
            var board = new[] { Even, "LFOGY", "SSYPO", "SSOGY", Even };
            var spawn = board.Select(r => new string('.', r.Length)).ToArray();
            var level = LevelParser.FromLayers(board, null, null, spawn);
            level.Moves = 10;
            level.ColorCount = 6;
            level.Goals.Add(new GoalDefinition(GoalType.Lamp));
            level.Goals.Add(new GoalDefinition(GoalType.Flower));
            level.Goals.Add(new GoalDefinition(GoalType.Statue));
            yield return Begin(level);
            Assert.AreEqual("1", Game.Hud.GoalText(0));
            Assert.AreEqual("3", Game.Hud.GoalText(1));
            Assert.AreEqual("1", Game.Hud.GoalText(2));

            foreach (var p in new[] { new GridPos(0, 3), new GridPos(0, 3), new GridPos(1, 3), new GridPos(0, 1), new GridPos(1, 2), new GridPos(0, 2), new GridPos(1, 1) })
            {
                if (Game.Session.State != GameState.Playing) break;
                Game.PickBooster(BoosterType.Hammer);
                Game.OnTap(p);
                yield return WaitIdle();
                Assert.IsNull(Game.LastMismatch, "after a hit at " + p);
                Assert.IsNull(Game.View.FindMismatch(Game.Session.Board));
            }
            Assert.AreEqual(0, Game.Session.Goals[0].Remaining, "the lamp lit and was collected");
            Assert.AreEqual(2, Game.Session.Goals[1].Remaining, "one flower bloomed");
            Assert.AreEqual(0, Game.Session.Goals[2].Remaining, "the villager is free");
            yield return new WaitForSeconds(GoalFlyers.Flight + 0.3f);
            Assert.AreEqual("2", Game.Hud.GoalText(1));
        }

        [UnityTest]
        public IEnumerator FrostLevel_SaysHowFrostWorks_BeforeItStarts()
        {
            yield return null;
            int index = _app.Levels.ToList().FindIndex(l => l.Cells.Any(c => c.Frost));
            Assert.GreaterOrEqual(index, 0, "some level uses frost");
            _app.ShowLevelStart(index);
            Assert.IsTrue(_app.LevelStart.FrostHint);
            _app.LevelStart.Close();
            _app.ShowLevelStart(0);
            Assert.IsFalse(_app.LevelStart.FrostHint);
            _app.LevelStart.Close();
        }

        [UnityTest]
        public IEnumerator Emby_WorriesWhenMovesRunLow_AndCheersOnAWin()
        {
            yield return Begin(Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even));
            var emby = Game.Hud.Emby;
            Assert.AreEqual(EmbyMood.Idle, emby.Mood);
            emby.SetMoves(EmbyPortrait.WorryMoves);
            Assert.AreEqual(EmbyMood.Worry, emby.Mood);
            emby.SetMoves(EmbyPortrait.PanicMoves);
            Assert.AreEqual(EmbyMood.Panic, emby.Mood);
            emby.SetMoves(10);
            Assert.AreEqual(EmbyMood.Idle, emby.Mood);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.AreEqual(EmbyMood.Joy, emby.Mood, "Emby cheers the win");
        }

        [UnityTest]
        public IEnumerator ColorBrush_ClearsAColour_WithoutAMove()
        {
            BoosterInventory.Set(BoosterType.ColorBrush, 2);
            yield return Begin(Level(10, null, Even, Odd, "YP2GY", Odd, Even));
            Assert.AreEqual("2", Game.Boosters.CountText(BoosterType.ColorBrush));
            Game.PickBooster(BoosterType.ColorBrush);
            Game.OnTap(new GridPos(0, 2));
            yield return WaitIdle();
            Assert.AreEqual(1, BoosterInventory.Count(BoosterType.ColorBrush));
            Assert.AreEqual(10, Game.Session.MovesLeft, "boosters cost no move");
            Assert.AreEqual(1, Game.Session.Stats.BoostersUsed);
            Assert.IsNull(Game.LastMismatch);
            Assert.AreEqual("1", Game.Boosters.CountText(BoosterType.ColorBrush));
        }

        [UnityTest]
        public IEnumerator Goal_PiecesFlyToTheHud_ThenCount()
        {
            yield return Begin(Level(10, new GoalDefinition(GoalType.Color, 30, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.Greater(Game.Flyers.Launched, 0, "cleared goal pieces fly to the goal panel");
            yield return new WaitForSeconds(GoalFlyers.Flight + 0.3f);
            Assert.Less(Game.Session.Goals[0].Remaining, 30);
            Assert.AreEqual(Game.Session.Goals[0].Remaining.ToString(), Game.Hud.GoalText(0));
        }

        IEnumerator OpenWorld()
        {
            yield return null;
            yield return TapUi(_app.LevelSelect.Lobby.WorldButton);
            Assert.IsTrue(_app.LevelSelect.MapShown, "the world button opens the map");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Home_HasFiveTabs_AndTheWorldMapOpensAndCloses()
        {
            yield return null;
            var select = _app.LevelSelect;
            Assert.IsFalse(select.MapShown, "the game opens on the home screen");
            Assert.AreEqual(5, select.Tabs.Root.childCount);

            yield return OpenWorld();
            Assert.IsNotNull(select.Buttons[0].panel);
            yield return TapUi(select.Tabs.HomeTab);
            Assert.IsFalse(select.MapShown, "the home tab leads back");

            yield return TapUi(select.Tabs.TeamTab);
            Assert.AreEqual(LobbyPage.Team, select.Page);
            Assert.IsNull(_app.Modal);
            yield return TapUi(select.Tabs.RankTab);
            Assert.AreEqual(LobbyPage.Rank, select.Page);
            yield return TapUi(select.Tabs.HomeTab);
            Assert.AreEqual(LobbyPage.Home, select.Page);
        }

        [UnityTest]
        public IEnumerator Race_ListsTheVillage_CountsWins_AndPaysLastWeeksPrize()
        {
            FreezeClock();
            yield return null;
            var select = _app.LevelSelect;
            yield return TapUi(select.Tabs.RankTab);
            Assert.AreEqual(Meta.RaceRules.Rivals + 1, select.Race.RowCount);
            Assert.AreEqual("Sıran: " + select.Race.MyRank, select.Race.MineText);
            Assert.IsTrue(select.Race.LeftText.StartsWith("Kalan: "));
            Assert.IsFalse(select.Tabs.RankBadgeShown);

            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int stars = LevelProgress.GetStars(4242);
            Assert.AreEqual(stars * Meta.RaceRules.FirstWinFactor, Game.WinTrophies, "a first win counts double");
            Assert.AreEqual(Game.WinTrophies, Meta.Race.Points);
            Assert.AreEqual(Game.WinTrophies, Game.Popup.Trophies, "the window shows the trophies");

            _now += RaceRules.SecondsPerWeek;
            _app.ShowLevelSelect();
            yield return null;
            select = _app.LevelSelect;
            Assert.IsTrue(select.Tabs.RankBadgeShown, "last week's prize waits");
            int coins = Meta.Economy.Coins;
            yield return TapUi(select.Tabs.RankTab);
            Assert.IsNotNull(_app.Rewards, "the prize is shown");
            Assert.Greater(Meta.Economy.Coins, coins);
            Assert.IsFalse(Meta.Race.ResultReady);
            Assert.AreEqual(0, Meta.Race.Points, "a new week starts from zero");
            Assert.IsFalse(select.Tabs.RankBadgeShown);
        }

        static long At(int year, int month, int day) =>
            (long)(new System.DateTime(year, month, day, 12, 0, 0) - new System.DateTime(1970, 1, 1)).TotalSeconds;

        [UnityTest]
        public IEnumerator Event_CollectsEmbersFromWins_AndPaysTheFirstTier()
        {
            _now = At(2026, 11, 12);
            Meta.Clock = () => _now;
            _app.ShowLevelSelect();
            yield return null;
            var button = _app.LevelSelect.Lobby.Event;
            Assert.IsTrue(button.Shown, "November has an event");
            Assert.AreEqual("", button.BadgeText);
            Assert.AreEqual(Meta.Span(Meta.Event.SecondsLeft(_now)), button.LeftText);

            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int stars = LevelProgress.GetStars(4242);
            Assert.AreEqual(Meta.EventRules.WinPoints(stars, false), Meta.Event.Points, "a win brings embers");

            Meta.Event.Report(EventMetric.Obstacles, 50, _now);
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            button = _app.LevelSelect.Lobby.Event;
            Assert.AreEqual("1", button.BadgeText, "the first tier waits");
            yield return TapUi(button.Root);
            var popup = _app.Modal as EventPopup;
            Assert.IsNotNull(popup);
            Assert.AreEqual(EventKind.Ladder, popup.Kind);
            Assert.AreEqual(5, popup.TierRows);
            Assert.AreEqual(Meta.Event.Points.ToString(), popup.PointsText);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            int coins = Meta.Economy.Coins;
            yield return TapUi(popup.ClaimButton);
            Assert.AreEqual(coins + 100, Meta.Economy.Coins);
            Assert.IsNotNull(_app.Rewards, "the prize is shown");
            Assert.IsNull(popup.ClaimButton, "the next tier is still shut");
        }

        [UnityTest]
        public IEnumerator Event_HidesBetweenEvents_AndASprintPaysItsGoal()
        {
            _now = At(2027, 2, 5);
            Meta.Clock = () => _now;
            _app.ShowLevelSelect();
            yield return null;
            var lobby = _app.LevelSelect.Lobby;
            Assert.IsFalse(lobby.Event.Shown, "nothing runs before the tenth");
            _app.OpenEvent();
            Assert.IsNull(_app.Modal);

            _now = At(2027, 2, 10);
            lobby.Refresh();
            Assert.IsTrue(lobby.Event.Shown);
            yield return null;
            yield return TapUi(lobby.Event.Root);
            var popup = _app.Modal as EventPopup;
            Assert.IsNotNull(popup);
            Assert.AreEqual(EventKind.Sprint, popup.Kind);
            Assert.AreEqual("0/40", popup.PointsText);
            Assert.IsNull(popup.ClaimButton);

            for (int i = 0; i < 14; i++) Meta.Event.ReportWin(3, false, _now);
            Meta.Save();
            Assert.AreEqual("40/40", popup.PointsText);
            Assert.IsNotNull(popup.ClaimButton);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            int coins = Meta.Economy.Coins;
            yield return TapUi(popup.ClaimButton);
            Assert.AreEqual(coins + 960, Meta.Economy.Coins, "finished in the first half");
            Assert.IsNull(popup.ClaimButton);
        }

        [UnityTest]
        public IEnumerator Team_IsJoined_SendsLives_AndHelpingPaysCoins()
        {
            FreezeClock();
            yield return null;
            var select = _app.LevelSelect;
            yield return TapUi(select.Tabs.TeamTab);
            Assert.AreEqual(Meta.TeamRules.Teams.Length, select.Team.JoinButtons.Count);
            yield return TapUi(select.Team.JoinButtons[1]);
            Assert.IsTrue(Meta.Team.Joined);
            Assert.AreEqual(1, Meta.Team.Index);
            Assert.AreEqual(Meta.TeamRules.Teams[1].Size + 1, select.Team.MemberRows);
            Assert.IsFalse(select.Team.RequestButton.enabledSelf, "lives are full");
            Assert.AreEqual("Canın dolu", select.Team.StatusText);

            Meta.Economy.LoseLife(_now);
            Meta.Economy.LoseLife(_now);
            Meta.Save();
            yield return null;
            Assert.IsTrue(select.Team.RequestButton.enabledSelf);
            yield return TapUi(select.Team.RequestButton);
            Assert.IsTrue(Meta.Team.RequestOpen(_now));
            Assert.AreEqual(3, Meta.Economy.Lives);
            Assert.IsFalse(select.Team.RequestButton.enabledSelf, "one request at a time");

            _now += 1100;
            Assert.AreEqual(5, Meta.Economy.Lives, "teammates fill the lives");
            select.Team.Refresh();
            Assert.IsTrue(select.Team.StatusText.StartsWith("Yeni istek: "), select.Team.StatusText);

            int guard = 0;
            while (Meta.Team.OpenAsks(_now) == 0 && guard++ < 600) _now += 300;
            Assert.AreEqual(1, Meta.Team.OpenAsks(_now), "a teammate asks for lives");
            select.Team.Refresh();
            select.Tabs.Refresh();
            Assert.AreEqual("1", select.Tabs.TeamBadgeText);
            yield return null;
            Assert.AreEqual(1, select.Team.HelpButtons.Count);
            int coins = Meta.Economy.Coins;
            yield return TapUi(select.Team.HelpButtons[0]);
            Assert.AreEqual(coins + Meta.TeamRules.HelpCoins, Meta.Economy.Coins);
            Assert.AreEqual(0, select.Team.HelpButtons.Count, "one help per ask");
            Assert.AreEqual("", select.Tabs.TeamBadgeText);

            yield return TapUi(select.Team.LeaveButton);
            var confirm = _app.Modal as ConfirmPopup;
            Assert.IsNotNull(confirm);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(confirm.YesButton);
            Assert.IsFalse(Meta.Team.Joined);
            Assert.AreEqual(Meta.TeamRules.Teams.Length, select.Team.JoinButtons.Count);
        }

        IEnumerator WaitForThaw()
        {
            float limit = Time.realtimeSinceStartup + 8f;
            while (_app.LevelSelect.Diorama.Busy && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsFalse(_app.LevelSelect.Diorama.Busy, "the thaw ends");
        }

        [UnityTest]
        public IEnumerator Restore_ThawsAnAreaPerTask_AndTheLastWarmsTheDistrict()
        {
            yield return null;
            var select = _app.LevelSelect;
            int district = select.RestoreFocus;
            var profile = Meta.Economy.Profile;
            profile.RestoreLevels[district] = 10;
            profile.RestoreStars[district] = 30;
            profile.RestoreBoosters[district] = 5;
            profile.RestoreClaimed[district] = 0;
            Meta.Save();
            select.Diorama.Show(district);
            select.Lobby.Refresh();
            Assert.AreEqual(district, select.Diorama.District);
            Assert.IsFalse(select.Diorama.Warm, "the district starts frozen");
            Assert.AreEqual(0, select.Diorama.ThawedCount);
            Assert.AreEqual("0/5", select.Lobby.RestoreText);
            Assert.AreEqual("5", select.Lobby.RestoreBadgeText, "five repairs wait");

            yield return TapUi(select.Lobby.RestoreButton);
            var popup = _app.Modal as RestorePopup;
            Assert.IsNotNull(popup);
            Assert.AreEqual(5, popup.TaskRows);
            Assert.AreEqual(5, popup.RepairButtons.Count);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            int coins = Meta.Economy.Coins;
            yield return TapUi(popup.RepairButtons[1]);
            Assert.IsNull(_app.Modal, "the window closes to show the repair");
            Assert.IsTrue(select.Diorama.Busy);
            yield return WaitForThaw();
            Assert.IsNotNull(_app.Rewards, "then the prize");
            Assert.Greater(Meta.Economy.Coins, coins);
            Assert.AreEqual(1, select.Diorama.ThawedCount);
            Assert.IsTrue(Meta.Restore.IsClaimed(district, 1));
            Assert.AreEqual("1/5", select.Lobby.RestoreText);
            Assert.AreEqual("4", select.Lobby.RestoreBadgeText);

            foreach (int task in new[] { 0, 2, 3 })
            {
                _app.Repair(district, task);
                yield return WaitForThaw();
            }
            Assert.IsFalse(select.Diorama.Warm);
            int packs = Meta.Cards.Packs;
            _app.Repair(district, 4);
            yield return WaitForThaw();
            Assert.IsTrue(Meta.Restore.Restored(district));
            Assert.IsTrue(select.Diorama.Warm, "the whole district warms up");
            Assert.Greater(Meta.Cards.Packs, packs, "the district bonus is paid");
            Assert.AreEqual("", select.Lobby.RestoreBadgeText);
        }

        [UnityTest]
        public IEnumerator Restore_CountsWinsAndBoosters_InTheirDistrict()
        {
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            int before = Meta.Economy.Profile.RestoreLevels[0];
            int starsBefore = Meta.Economy.Profile.RestoreStars[0];
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int stars = LevelProgress.GetStars(4242);
            Assert.AreEqual(before + 1, Meta.Economy.Profile.RestoreLevels[0], "a first win counts");
            Assert.AreEqual(starsBefore + stars, Meta.Economy.Profile.RestoreStars[0]);
        }

        [UnityTest]
        public IEnumerator Story_PlaysBeforeADistrictsFirstLevel_OnlyOnce()
        {
            GameApp.ShowStories = true;
            yield return null;
            yield return TapUi(_app.LevelSelect.Lobby.PlayButton);
            var story = _app.Story;
            Assert.IsNotNull(story, "the village story comes before level 1");
            Assert.IsNull(_app.LevelStart);
            Assert.AreEqual(0, story.Chapter);
            Assert.IsTrue(Meta.Economy.SceneSeen(0));
            Assert.IsTrue(story.Video.HasClip, "the clip ships with the game");
            if (VideoView.Supported)
            {
                float limit = Time.realtimeSinceStartup + 8f;
                while (!story.Video.Started && !story.Video.Failed && Time.realtimeSinceStartup < limit) yield return null;
                Assert.IsTrue(story.Video.Started, "the video plays");
            }

            yield return TapUi(story.SkipButton);
            Assert.IsNull(_app.Story);
            Assert.IsFalse(story.IsOpen);
            Assert.IsNotNull(_app.LevelStart, "then the level window opens");
            _app.LevelStart.Close();
            yield return null;

            yield return TapUi(_app.LevelSelect.Lobby.PlayButton);
            Assert.IsNull(_app.Story, "a story plays once");
            Assert.IsNotNull(_app.LevelStart);
            _app.LevelStart.Close();
            yield return null;

            yield return OpenWorld();
            Assert.AreEqual(1, _app.LevelSelect.StoryButtons.Count, "only open districts can be replayed");
            yield return TapUi(_app.LevelSelect.StoryButtons[0]);
            Assert.IsNotNull(_app.Story, "the district picture replays its story");
            _app.Story.Close();
            Assert.IsNull(_app.Story);
            Assert.IsNull(_app.LevelStart, "a replay opens no level");

            _app.PlayStory(1);
            _app.Story.Close(false);
            Assert.IsNull(_app.Story, "a scene closed without going on is gone too");
            _app.PlayStory(2);
            Assert.IsNotNull(_app.Story, "so the next one can play");
            _app.Story.Close(false);
        }

        [UnityTest]
        public IEnumerator Story_ClosesByItself_WhenItEnds()
        {
            yield return null;
            _app.PlayStory(2);
            var story = _app.Story;
            Assert.IsNotNull(story);
            float limit = Time.realtimeSinceStartup + 40f;
            while (story.IsOpen && Time.realtimeSinceStartup < limit) yield return null;
            Assert.IsFalse(story.IsOpen, "the story closes when it ends");
            Assert.IsNull(_app.Story);
        }

        [UnityTest]
        public IEnumerator LoadingScreen_ShowsFirst_ThenLeaves()
        {
            _app.ShowLoadingScreen();
            Assert.IsNotNull(_app.Loading);
            Assert.IsTrue(_app.Loading.IsOpen);
            float limit = Time.realtimeSinceStartup + 6f;
            while (_app.Loading != null)
            {
                Assert.Less(Time.realtimeSinceStartup, limit, "the loading screen never left");
                yield return null;
            }
            Assert.IsNull(_app.Loading);
        }

        [UnityTest]
        public IEnumerator Collection_OpensPacks_AndPaysForACompleteSet()
        {
            yield return null;
            var select = _app.LevelSelect;
            Assert.AreEqual("2", select.Tabs.CardsBadgeText, "two packs wait on the tab");
            yield return TapUi(select.Tabs.CardsTab);
            Assert.AreEqual(LobbyPage.Cards, select.Page);
            Assert.AreEqual("0/" + Meta.CardRules.CardCount, select.Cards.TotalText);
            Assert.AreEqual(Meta.CardRules.Sets.Length, select.Cards.SetTiles.Count);
            for (int card = 0; card < Meta.CardRules.CardCount; card++)
                Assert.IsTrue(CardArt.Has(Meta.CardRules, card), Meta.CardRules.Card(card).Id + " has its own picture");

            yield return TapUi(select.Cards.PackButton);
            var pack = _app.Modal as PackPopup;
            Assert.IsNotNull(pack);
            Assert.AreEqual(3, pack.Count);
            Assert.GreaterOrEqual(pack.NewCount, 1);
            Assert.AreEqual(1, Meta.Cards.Packs);
            Assert.GreaterOrEqual(Meta.Cards.TotalOwned, 1);
            Assert.IsNotNull(pack.AgainButton, "one more pack is waiting");
            pack.Close();
            Assert.AreEqual(Meta.Cards.TotalOwned + "/" + Meta.CardRules.CardCount, select.Cards.TotalText);

            for (int c = 0; c < Meta.CardRules.Sets[1].Cards.Length; c++) Meta.Economy.Profile.Cards[Meta.CardRules.IndexOf(1, c)] = 1;
            Meta.Save();
            int coins = Meta.Economy.Coins;
            yield return TapUi(select.Cards.SetTiles[1]);
            var set = _app.Modal as SetPopup;
            Assert.IsNotNull(set);
            Assert.AreEqual(6, set.CardCount);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(set.ClaimButton);
            Assert.AreEqual(coins + 400, Meta.Economy.Coins);
            Assert.IsTrue(Meta.Cards.SetClaimed(1));
            Assert.IsNotNull(_app.Rewards, "the set prize is shown");
        }

        [UnityTest]
        public IEnumerator FirstWin_BringsACardPack()
        {
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            int packs = Meta.Cards.Packs;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.AreEqual(packs + 1, Meta.Cards.Packs);
            Assert.AreEqual(1, Game.Popup.Packs, "the window shows the pack");
        }

        void FreezeClock()
        {
            long wall = Meta.WallClock();
            _now = wall - wall % EconomyRules.SecondsPerDay + Noon;
            Meta.Clock = () => _now;
        }

        IEnumerator WaitForFlyers()
        {
            float limit = Time.realtimeSinceStartup + 8f;
            while (_app.Flyers.Flying > 0 && Time.realtimeSinceStartup < limit) yield return null;
            Assert.AreEqual(0, _app.Flyers.Flying, "every flyer lands");
        }

        [UnityTest]
        public IEnumerator Rewards_FlyToTheTopBar_AndTheCounterCatchesUp()
        {
            yield return null;
            var top = _app.LevelSelect.Top;
            int coins = Meta.Economy.Coins, gems = Meta.Economy.Gems;
            Meta.Economy.Grant(Reward.Coins(300));
            Meta.Economy.Grant(Reward.Gems(10));
            Meta.Save();
            _app.ShowRewards("Ödül", new[] { Reward.Coins(300), Reward.Gems(10) }, false);
            Assert.AreEqual(coins.ToString(), top.CoinsText, "the counter waits for the coins");
            Assert.AreEqual(gems.ToString(), top.GemsText);
            Assert.IsTrue(top.Holding);

            yield return new WaitForSeconds(Timing.ModalIn + Timing.StarFill * 3f);
            int launched = _app.Flyers.Launched;
            yield return TapUi(_app.Rewards.CloseButton);
            Assert.IsNull(_app.Rewards);
            Assert.Greater(_app.Flyers.Launched, launched, "the coins fly");
            Assert.AreNotEqual((coins + 300).ToString(), top.CoinsText, "not there yet");
            yield return WaitForFlyers();
            Assert.AreEqual((coins + 300).ToString(), top.CoinsText);
            Assert.AreEqual((gems + 10).ToString(), top.GemsText);
            Assert.IsFalse(top.Holding);
        }

        [UnityTest]
        public IEnumerator Chest_ShakesThenBursts_AndPopsTheRewards()
        {
            yield return null;
            _app.ShowRewards("Sandık", new[] { Reward.Coins(100) }, true);
            var popup = _app.Rewards;
            Assert.IsFalse(popup.Opened, "the chest is shut at first");
            yield return new WaitForSeconds(RewardPopup.ChestShake * 0.5f);
            Assert.IsFalse(popup.Opened, "still shaking");
            yield return new WaitForSeconds(RewardPopup.ChestTime + 0.15f);
            Assert.IsTrue(popup.Opened, "the chest bursts open");
            yield return new WaitForSeconds(0.6f);
            yield return TapUi(popup.CloseButton);
            yield return WaitForFlyers();
        }

        [UnityTest]
        public IEnumerator CoinCap_ShowsTheSpendHint()
        {
            yield return null;
            var top = _app.LevelSelect.Top;
            Assert.IsFalse(top.CapWarningShown);
            Meta.Economy.AddCoins(Meta.Rules.CoinCap);
            Meta.Save();
            Assert.IsTrue(top.CapWarningShown, "a full purse says spend");
            Meta.Economy.SpendCoins(Meta.Economy.Coins - 1000);
            Meta.Save();
            Assert.IsFalse(top.CapWarningShown);
        }

        [UnityTest]
        public IEnumerator WinCoins_FlyHome_FromTheLevelsButton()
        {
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = AdRules.FirstInterstitialLevel - 1;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int won = Game.WinCoins;
            Assert.Greater(won, 0);
            yield return new WaitForSeconds(Timing.ModalIn + Timing.StarFill * 3f);
            yield return TapUi(Game.Popup.Buttons[1]);
            Assert.IsNull(_app.Game);
            var top = _app.LevelSelect.Top;
            Assert.AreEqual((Meta.Economy.Coins - won).ToString(), top.CoinsText, "the lobby opens with the old purse");
            yield return new WaitForSeconds(0.5f);
            yield return WaitForFlyers();
            Assert.AreEqual(Meta.Economy.Coins.ToString(), top.CoinsText, "then the prize flies in");
        }

        [UnityTest]
        public IEnumerator Lobby_ShowsLivesAndCoins_AndPlayOpensTheNextLevel()
        {
            yield return null;
            var top = _app.LevelSelect.Top;
            Assert.AreEqual("5", top.LivesText);
            Assert.AreEqual("Dolu", top.LifeTimerText);
            Assert.AreEqual("500", top.CoinsText);
            Assert.AreEqual("50", top.GemsText);
            Assert.AreEqual("Seviye " + _app.Levels[_app.LevelSelect.CurrentIndex].Id, _app.LevelSelect.Lobby.PlayButton.text);

            yield return TapUi(_app.LevelSelect.Lobby.PlayButton);
            Assert.IsNotNull(_app.LevelStart, "the big button opens the level the player is on");
        }

        [UnityTest]
        public IEnumerator Losing_CostsALife_AndWithoutLivesTheLivesWindowOffersMore()
        {
            FreezeClock();
            Meta.Economy.Profile.Lives = 1;
            Meta.Economy.Profile.LifeAnchor = _now;
            yield return Begin(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.AreEqual(1, Meta.Economy.Lives, "the life goes when the player gives up, not before");
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons.First(b => b.text == "Tekrar dene"));

            Assert.AreEqual(0, Meta.Economy.Lives);
            Assert.IsNull(_app.Game, "no lives, no retry");
            var lives = _app.Modal as LivesPopup;
            Assert.IsNotNull(lives);
            Assert.AreEqual("0", _app.LevelSelect.Top.LivesText);
            Assert.AreNotEqual("Dolu", _app.LevelSelect.Top.LifeTimerText);

            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(lives.BuyButton);
            Assert.AreEqual(1, Meta.Economy.Lives);
            Assert.AreEqual(500 - Meta.Rules.LifeCoinPrice, Meta.Economy.Coins);

            _now += Meta.Rules.LifeSeconds;
            Assert.AreEqual(2, Meta.Economy.Lives, "half an hour brings one back");
        }

        [UnityTest]
        public IEnumerator Win_PaysCoinsOnce_AndAReplayPaysLittle()
        {
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            int stars = LevelProgress.GetStars(4242);
            Assert.AreEqual(Meta.Rules.WinReward(stars), Game.WinCoins);
            Assert.AreEqual(500 + Game.WinCoins, Meta.Economy.Coins);
            Assert.AreEqual(Game.WinCoins, Game.Popup.Coins, "the window shows the prize");
            Assert.AreEqual(5, Meta.Economy.Lives, "winning costs no life");

            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            Assert.AreEqual(Meta.Rules.ReplayCoins, Game.WinCoins);
        }

        [UnityTest]
        public IEnumerator Shop_SellsBoosters_AndOpensChests()
        {
            Meta.Economy.AddCoins(800);
            Meta.Save();
            yield return TapUi(_app.LevelSelect.Tabs.ShopTab);
            var shop = _app.Modal as ShopPopup;
            Assert.IsNotNull(shop);
            Assert.AreEqual("1300", shop.BalanceText);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);

            shop.Reveal(shop.BuyButton(BoosterType.Hammer));
            yield return null;
            yield return TapUi(shop.BuyButton(BoosterType.Hammer));
            Assert.AreEqual(4, BoosterInventory.Count(BoosterType.Hammer));
            Assert.AreEqual(400, Meta.Economy.Coins);
            Assert.AreEqual("400", _app.LevelSelect.Top.CoinsText);
            Assert.IsFalse(shop.BuyButton(BoosterType.Hammer).enabledSelf, "too dear now");
            Assert.IsTrue(shop.ChestButton.enabledSelf);

            int gems = Meta.Economy.Gems, packs = Meta.Cards.Packs, boosters = BoosterInventory.All.Sum(BoosterInventory.Count);
            shop.Reveal(shop.ChestButton);
            yield return null;
            yield return TapUi(shop.ChestButton);
            Assert.IsNotNull(_app.Rewards, "the chest shows what was inside");
            Assert.AreEqual(1, _app.Rewards.Count);
            Assert.AreEqual(1, Meta.Economy.Profile.ChestsOpened);
            Assert.Greater(Meta.Economy.Coins + (Meta.Economy.Gems - gems) + (Meta.Cards.Packs - packs) + (BoosterInventory.All.Sum(BoosterInventory.Count) - boosters), 0);
        }

        [UnityTest]
        public IEnumerator Shop_SellsGemsForMoney_InTheTestStore()
        {
            yield return TapUi(_app.LevelSelect.Tabs.ShopTab);
            var shop = _app.Modal as ShopPopup;
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            int gems = Meta.Economy.Gems;
            var button = shop.OfferButton("gems_200");
            Assert.IsNotNull(button);
            Assert.AreEqual("$1.99", Rows.LabelOf(button), "the editor shows the fallback price");
            shop.Reveal(button);
            yield return null;
            yield return TapUi(button);
            Assert.AreEqual(gems + 200, Meta.Economy.Gems);
            Assert.AreEqual(1, _app.Purchases.Completed);
            Assert.IsNotNull(_app.Rewards, "a thank-you shows what came");
            Assert.IsFalse(shop.OfferButton(StoreRules.PiggyId).enabledSelf, "the piggy is not full yet");
        }

        [UnityTest]
        public IEnumerator Pass_OpensFromHome_ClaimsSteps_AndGivesEightLives()
        {
            FreezeClock();
            Meta.Economy.ReportStoreWin(Meta.Now);
            Meta.Economy.ReportStoreWin(Meta.Now);
            Meta.Save();
            yield return null;
            Assert.AreEqual("2/30", _app.LevelSelect.Lobby.PassText);
            Assert.AreEqual("2", _app.LevelSelect.Lobby.PassBadgeText);
            yield return TapUi(_app.LevelSelect.Lobby.PassButton);
            var pass = _app.Modal as PassPopup;
            Assert.IsNotNull(pass);
            Assert.AreEqual(30, pass.StepCount);
            Assert.IsFalse(pass.PremiumButton(1).enabledSelf, "the paid row waits for the pass");
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(pass.FreeButton(1));
            Assert.IsTrue(Meta.Economy.PassClaimed(1, false));
            _app.Rewards?.Close();
            yield return null;
            yield return TapUi(pass.BuyButton);
            Assert.IsTrue(Meta.Economy.PassPremium(Meta.Now));
            Assert.AreEqual(8, Meta.Economy.MaxLives);
            Assert.IsTrue(pass.PremiumButton(1).enabledSelf);
        }

        void WinLevels(int count)
        {
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            for (int i = 0; i < count; i++) LevelProgress.SaveStars(_app.Levels[i].Id, 1);
        }

        [UnityTest]
        public IEnumerator Rush_StartsFromHome_CountsAWin_AndPaysTheWinner()
        {
            FreezeClock();
            yield return null;
            WinLevels(Meta.ContestRules.FromWins[0]);
            _app.ShowLevelSelect();
            yield return null;
            var lobby = _app.LevelSelect.Lobby;
            Assert.IsTrue(lobby.ContestShown(ContestKind.Rush));
            Assert.IsFalse(lobby.ContestShown(ContestKind.Lantern), "the lantern race opens later");
            Assert.AreEqual("Başla", lobby.ContestText(ContestKind.Rush));
            yield return TapUi(lobby.RushButton);
            var popup = _app.Modal as ContestPopup;
            Assert.IsNotNull(popup);
            Assert.AreEqual(ContestKind.Rush, popup.Kind);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(popup.StartButton);
            Assert.IsTrue(Meta.Contests.Running(ContestKind.Rush, Meta.Now));
            Assert.AreEqual(Meta.ContestRules.RivalCounts[0] + 1, popup.RowCount);
            Assert.AreEqual("0/5", popup.ProgressText);
            popup.Close();

            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4243;
            PlayerPrefs.DeleteKey(LevelProgress.StarsKey + level.Id);
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            Assert.AreEqual(1, Meta.Contests.MyWins(ContestKind.Rush), "a win moves the racer");
            Assert.AreEqual(1, Meta.Contests.LadderWins(Meta.Now), "and climbs the ladder");

            for (int i = 0; i < 4; i++) Meta.Contests.ReportWin(Meta.Now);
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            lobby = _app.LevelSelect.Lobby;
            Assert.AreEqual("Bitti", lobby.ContestText(ContestKind.Rush));
            Assert.AreEqual("!", lobby.ContestBadgeText(ContestKind.Rush));
            yield return TapUi(lobby.RushButton);
            popup = _app.Modal as ContestPopup;
            Assert.AreEqual("1. oldun!", popup.ResultText);
            int coins = Meta.Economy.Coins;
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(popup.ClaimButton);
            Assert.AreEqual(coins + 150, Meta.Economy.Coins);
            Assert.IsNotNull(_app.Rewards, "the prize is shown");
            Assert.IsNull(popup.ClaimButton, "nothing left to claim");
        }

        [UnityTest]
        public IEnumerator Ladder_ClimbsWithWins_AndALossSendsItBack()
        {
            FreezeClock();
            yield return null;
            WinLevels(Meta.ContestRules.LadderFromWins);
            for (int i = 0; i < 2; i++) Meta.Contests.ReportWin(Meta.Now);
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            var lobby = _app.LevelSelect.Lobby;
            Assert.IsTrue(lobby.LadderShown);
            Assert.AreEqual("2/7", lobby.LadderText);
            yield return TapUi(lobby.LadderButton);
            var ladder = _app.Modal as LadderPopup;
            Assert.IsNotNull(ladder);
            Assert.AreEqual(7, ladder.StepCount);
            Assert.AreEqual(2, ladder.Lit);
            StringAssert.Contains("/100", ladder.PlayersText);
            Assert.IsNull(ladder.ClaimButton);
            ladder.Close();

            _app.StartLevel(Level(1, null, Even, Odd, "RRBRY", "OGRPO", Even), Meta.ContestRules.LadderFromWins, Seed);
            Game.HintDelay = float.MaxValue;
            yield return null;
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return WaitForPopup();
            StringAssert.Contains("Kor Merdiveni", Game.Popup.Message);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(Game.Popup.Buttons.First(b => b.text == "Tekrar dene"));
            Assert.AreEqual(0, Meta.Contests.LadderWins(Meta.Now), "a loss sends the climb back");

            for (int i = 0; i < 7; i++) Meta.Contests.ReportWin(Meta.Now);
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            Assert.AreEqual("!", _app.LevelSelect.Lobby.LadderBadgeText);
            _app.OpenLadder();
            ladder = _app.Modal as LadderPopup;
            Assert.AreEqual(7, ladder.Lit);
            int coins = Meta.Economy.Coins;
            int prize = Meta.Contests.LadderPrize(Meta.Now);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(ladder.ClaimButton);
            Assert.AreEqual(coins + prize, Meta.Economy.Coins);
            Assert.IsNotNull(_app.Rewards);
        }

        [UnityTest]
        public IEnumerator TeamChest_OpensInTheTeamTab()
        {
            FreezeClock();
            yield return null;
            Assert.IsTrue(Meta.Team.Join(0));
            Meta.Contests.TeamWins(Meta.Now);
            Meta.Economy.Profile.ChestWins = Meta.ContestRules.ChestSteps[2];
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            var select = _app.LevelSelect;
            yield return TapUi(select.Tabs.TeamTab);
            var chest = select.Team.Chest;
            Assert.IsNotNull(chest, "the chest sits on top of the team feed");
            Assert.AreEqual(3, chest.ClaimButtons.Count);
            int coins = Meta.Economy.Coins;
            yield return TapUi(chest.ClaimButtons[0]);
            Assert.IsTrue(Meta.Contests.ChestClaimed(0));
            Assert.AreEqual(coins + 100, Meta.Economy.Coins);
            Assert.IsNotNull(_app.Rewards);
            Assert.AreEqual(2, select.Team.Chest.ClaimButtons.Count);
        }

        [UnityTest]
        public IEnumerator Starter_IsOfferedOnce_AfterTheFirstLevels()
        {
            yield return null;
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            for (int i = 0; i < Meta.Rules.Store.StarterFromLevel; i++) LevelProgress.SaveStars(_app.Levels[i].Id, 1);
            GameApp.OfferStarter = true;
            _app.ShowLevelSelect();
            var starter = _app.Modal as StarterPopup;
            Assert.IsNotNull(starter, "the starter pack shows after the first levels");
            starter.Close();
            _app.ShowLevelSelect();
            Assert.IsNull(_app.Modal, "only once");
        }

        [UnityTest]
        public IEnumerator DailyGift_ComesOnceADay()
        {
            FreezeClock();
            _app.ShowLevelSelect();
            yield return null;
            Assert.IsTrue(_app.LevelSelect.Lobby.GiftBadgeShown);
            yield return TapUi(_app.LevelSelect.Lobby.GiftButton);
            var daily = _app.Modal as DailyPopup;
            Assert.IsNotNull(daily);
            Assert.AreEqual(0, daily.Today);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(daily.ClaimButton);

            Assert.AreEqual(650, Meta.Economy.Coins);
            Assert.IsNull(_app.Modal);
            Assert.IsNotNull(_app.Rewards);
            _app.Rewards.Close();
            Assert.IsFalse(_app.LevelSelect.Lobby.GiftBadgeShown);

            _app.OpenDaily();
            Assert.IsNull(((DailyPopup)_app.Modal).ClaimButton, "nothing more today");
            _app.Modal.Close();

            _now += EconomyRules.SecondsPerDay;
            _app.LevelSelect.Lobby.Refresh();
            Assert.IsTrue(_app.LevelSelect.Lobby.GiftBadgeShown, "a new day, a new gift");
            _app.OpenDaily();
            Assert.AreEqual(1, ((DailyPopup)_app.Modal).Today);
        }

        [UnityTest]
        public IEnumerator Quests_FillWhilePlaying_AndPayWhenClaimed()
        {
            FreezeClock();
            var before = Meta.Economy.Quests(_now);
            var level = Level(10, new GoalDefinition(GoalType.Color, 3, GemColor.Red), Even, Odd, "RRBRY", "OGRPO", Even);
            level.Id = 4242;
            yield return Begin(level);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            var after = Meta.Economy.Quests(_now);
            Assert.Greater(after.Sum(q => q.Progress), before.Sum(q => q.Progress), "playing moves at least one quest forward");

            Meta.Economy.Report(after[0].Kind, after[0].Target, _now);
            Meta.Save();
            _app.ShowLevelSelect();
            yield return null;
            Assert.AreEqual("1", _app.LevelSelect.Lobby.QuestBadgeText);

            yield return TapUi(_app.LevelSelect.Lobby.QuestsButton);
            var quests = _app.Modal as QuestsPopup;
            Assert.IsNotNull(quests);
            Assert.AreEqual(after[0].Target + "/" + after[0].Target, quests.ProgressText(0));
            int coins = Meta.Economy.Coins;
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(quests.ClaimButtons[0]);
            Assert.AreEqual(coins + after[0].Coins, Meta.Economy.Coins);
            Assert.AreEqual("", _app.LevelSelect.Lobby.QuestBadgeText);
        }

        [UnityTest]
        public IEnumerator LeavingMidLevel_AsksFirst_AndCostsALife()
        {
            _app.StartLevel(_app.Levels[0], 0, Seed);
            yield return null;
            var move = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            Game.RequestSwap(move.A, move.B);
            yield return WaitIdle();

            yield return TapUi(Game.Boosters.HomeButton);
            var confirm = _app.Modal as ConfirmPopup;
            Assert.IsNotNull(confirm, "leaving after a move asks first");
            Assert.IsNotNull(_app.Game);
            yield return new WaitForSeconds(Timing.ModalIn + 0.05f);
            yield return TapUi(confirm.YesButton);
            Assert.IsNull(_app.Game);
            Assert.AreEqual(4, Meta.Economy.Lives);
        }
    }
}
