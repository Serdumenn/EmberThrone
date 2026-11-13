using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class FinaleStreakTests
    {
        static GameSession Won()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", "OGRPO", T.Even }, 20, 6, null, null, true,
                new GoalDefinition(GoalType.Color, 3, GemColor.Red));
            var s = T.Session(level);
            s.TrySwap(T.P(2, 1), T.P(2, 2));
            Assert.AreEqual(GameState.Won, s.State);
            return s;
        }

        [Test]
        public void Finale_TurnsLeftoverMovesIntoRockets_AndFiresThem()
        {
            var s = Won();
            int left = s.MovesLeft;
            var o = s.PlayFinale();
            Assert.IsTrue(o.Accepted);
            int made = o.EventsOf<PieceConvertedEvent>().Count();
            Assert.AreEqual(System.Math.Min(left, s.Rules.FinaleMaxSpecials), made);
            Assert.AreEqual(made, s.Stats.FinaleSpecials);
            Assert.GreaterOrEqual(o.EventsOf<SpecialActivatedEvent>().Count(), made, "every rocket fires");
            Assert.AreEqual(GameState.Won, s.State);
            Assert.AreEqual(left, s.MovesLeft, "the stars were earned before the finale");
            Assert.AreEqual(0, BoardDebug.Validate(s.Board, true).Count, string.Join("; ", BoardDebug.Validate(s.Board, true)));
            Assert.IsFalse(s.PlayFinale().Accepted, "only once");
        }

        [Test]
        public void Finale_NeedsAWin()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRY", T.Odd, T.Even);
            Assert.IsFalse(s.PlayFinale().Accepted);
            Assert.IsFalse(s.FinalePlayed);
        }

        [Test]
        public void Finale_IsTheSameForTheSameSeed()
        {
            var a = Won();
            var b = Won();
            a.PlayFinale();
            b.PlayFinale();
            Assert.AreEqual(a.Board.ComputeHash(), b.Board.ComputeHash());
        }

        static Economy Fresh()
        {
            var rules = new EconomyRules();
            return new Economy(rules, Profile.New(rules, 7));
        }

        [Test]
        public void Streak_GrowsWithWins_AndGivesUpToThreeSpecials()
        {
            var e = Fresh();
            Assert.AreEqual(0, e.StreakSpecials().Length);
            e.ExtendStreak();
            CollectionAssert.AreEqual(new[] { PieceKind.RocketH }, e.StreakSpecials());
            e.ExtendStreak();
            e.ExtendStreak();
            e.ExtendStreak();
            Assert.AreEqual(4, e.WinStreak);
            CollectionAssert.AreEqual(new[] { PieceKind.RocketH, PieceKind.Bomb, PieceKind.ColorBomb }, e.StreakSpecials());
            Assert.IsTrue(e.BreakStreak());
            Assert.AreEqual(0, e.WinStreak);
            Assert.IsFalse(e.BreakStreak());
        }

        [Test]
        public void Streak_IsSavedWithTheProfile()
        {
            var e = Fresh();
            e.ExtendStreak();
            e.ExtendStreak();
            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(2, loaded.WinStreak);
        }

        [Test]
        public void FinaleCoins_ArePaidOnTheFirstWinOnly()
        {
            var e = Fresh();
            int coins = e.Coins;
            Assert.AreEqual(5 * e.Rules.FinaleCoinsPerSpecial, e.RewardFinale(5, true));
            Assert.AreEqual(0, e.RewardFinale(5, false));
            Assert.AreEqual(coins + 5 * e.Rules.FinaleCoinsPerSpecial, e.Coins);
        }

        [Test]
        public void Simulator_MeasuresNearLosses()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", "OGRPO", T.Even }, 1, 6, null, null, true,
                new GoalDefinition(GoalType.Color, 40, GemColor.Red));
            var report = Simulation.LevelSimulator.Run(level, 10, 1, () => new Simulation.GreedyPolicy(0));
            Assert.AreEqual(0, report.Wins);
            Assert.GreaterOrEqual(report.NearLossRate, 0);
            Assert.LessOrEqual(report.NearLossRate, 1);
        }
    }
}
