using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class FrostTests
    {
        static int FrostCount(GameSession s)
        {
            int n = 0;
            for (int y = 0; y < s.Board.Height; y++)
                for (int x = 0; x < s.Board.Width; x++)
                    if (s.Board.CellAt(x, y).Frost) n++;
            return n;
        }

        static LevelDefinition Frosted(string[] board, string[] frost, params GoalDefinition[] goals) =>
            T.Level(board, 20, 6, null, null, frost, false, goals);

        static readonly string[] BottomLeft = { ".....", ".....", ".....", ".....", "1...." };

        [Test]
        public void Frost_SpreadsToANeighbour_WhenTheMoveBreaksNone()
        {
            var level = Frosted(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even }, BottomLeft,
                new GoalDefinition(GoalType.Cover));
            var s = T.Session(level);
            Assert.AreEqual(1, s.Goals[0].Target);
            Assert.AreEqual(0, s.InitialOutcome.EventsOf<FrostSpreadEvent>().Count(), "no spread before the first move");

            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            var spread = o.EventsOf<FrostSpreadEvent>().Single();
            Assert.AreEqual(T.P(0, 0), spread.From);
            Assert.IsTrue(spread.From.IsAdjacent(spread.To));
            Assert.IsTrue(s.Board.CellAt(spread.To).Frost);
            Assert.AreEqual(1, s.Board.CellAt(spread.To).Cover);
            Assert.AreEqual(2, FrostCount(s));
            Assert.AreEqual(0, spread.GoalIndex);
            Assert.AreEqual(2, spread.Remaining);
            Assert.AreEqual(2, s.Goals[0].Remaining, "the goal grows with the frost");
            Assert.AreEqual(1, s.Stats.FrostSpreads);
            Assert.AreEqual(0, BoardDebug.Validate(s.Board, true).Count, string.Join("; ", BoardDebug.Validate(s.Board, true)));
        }

        [Test]
        public void Frost_StaysPut_WhenTheMoveBreaksSome()
        {
            var level = Frosted(new[] { T.Even, T.Odd, "-POGY", T.Odd, T.Even },
                new[] { "....1", ".....", "..1..", ".....", "....." },
                new GoalDefinition(GoalType.Cover));
            var s = T.Session(level);
            Assert.AreEqual(2, s.Goals[0].Target);

            var o = s.TryTap(T.P(0, 2));
            Assert.AreEqual(0, o.EventsOf<FrostSpreadEvent>().Count());
            Assert.IsFalse(s.Board.CellAt(2, 2).Frost);
            Assert.AreEqual(0, s.Board.CellAt(2, 2).Cover);
            Assert.AreEqual(1, FrostCount(s));
            Assert.AreEqual(1, s.Goals[0].Remaining);
        }

        [Test]
        public void Frost_NeedsAFreeGemNextToIt()
        {
            var level = Frosted(new[] { T.Even, T.Odd, "RRBRY", "2GYPO", "Y2OGY" }, BottomLeft);
            var s = T.Session(level);
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(0, o.EventsOf<FrostSpreadEvent>().Count(), "boxes all around");
            Assert.AreEqual(1, FrostCount(s));
        }

        [Test]
        public void Frost_DoesNotGrowAGoalWithAFixedCount()
        {
            var level = Frosted(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even }, BottomLeft,
                new GoalDefinition(GoalType.Cover, 1));
            var s = T.Session(level);
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            var spread = o.EventsOf<FrostSpreadEvent>().Single();
            Assert.AreEqual(-1, spread.GoalIndex);
            Assert.AreEqual(1, s.Goals[0].Remaining);
        }

        [Test]
        public void Frost_SpreadsTheSameWay_ForTheSameSeed()
        {
            var level = Frosted(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even },
                new[] { ".....", "..1..", ".....", ".....", "....." });
            var a = T.Session(level, 5);
            var b = T.Session(level, 5);
            var ea = a.TrySwap(T.P(3, 2), T.P(2, 2)).EventsOf<FrostSpreadEvent>().Single();
            var eb = b.TrySwap(T.P(3, 2), T.P(2, 2)).EventsOf<FrostSpreadEvent>().Single();
            Assert.AreEqual(ea.To, eb.To);
            Assert.AreEqual(a.Board.ComputeHash(), b.Board.ComputeHash());
        }

        [Test]
        public void Frost_RoundTripsThroughJson_AndCannotShareACellWithCover()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even }, 20, 6, null, null,
                new[] { "1....", ".....", ".....", ".....", "....1" }, true);
            var json = LevelParser.ToJson(level);
            StringAssert.Contains("\"frost\"", json);
            var back = LevelParser.FromJson(json);
            Assert.IsTrue(back.GetCell(0, 4).Frost);
            Assert.IsTrue(back.GetCell(4, 0).Frost);
            Assert.AreEqual(1, back.GetCell(0, 4).Cover);
            Assert.IsFalse(back.GetCell(2, 2).Frost);

            Assert.Throws<LevelFormatException>(() => LevelParser.FromLayers(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even }, null,
                new[] { "1....", ".....", ".....", ".....", "....." }, null, new[] { "1....", ".....", ".....", ".....", "....." }));
        }
    }
}
