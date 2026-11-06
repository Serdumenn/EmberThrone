using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class ObstacleAndGoalTests
    {
        [Test]
        public void AdjacentMatch_DamagesBoxOnce()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRY", "O3RPO", "YPRGY");
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            var dmg = o.EventsOf<PieceDamagedEvent>().Single();
            Assert.AreEqual(T.P(1, 1), dmg.Pos);
            Assert.AreEqual(2, dmg.HpLeft);
            Assert.AreEqual(PieceKind.Box, T.KindAt(s, 1, 1));
            Assert.AreEqual(2, s.Board.CellAt(1, 1).Piece.Hp);
        }

        [Test]
        public void BoxIsStatic_AndDiagonalFillWorksBelowIt()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRY", "O3RPO", "YPRGY");
            s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.AreEqual(PieceKind.Box, T.KindAt(s, 1, 1), "box did not fall");
            Assert.AreEqual(0, BoardDebug.Validate(s.Board, true).Count, string.Join("; ", BoardDebug.Validate(s.Board, true)));
        }

        [Test]
        public void Blast_DestroysBox_AndCompletesGoal()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "-PO1Y", T.Odd, T.Even }, goals: new GoalDefinition(GoalType.Box));
            var s = T.Session(level);
            Assert.AreEqual(1, s.Goals[0].Target);
            var o = s.TryTap(T.P(0, 2));
            Assert.AreEqual(GameState.Won, s.State);
            Assert.AreEqual(GameState.Won, o.State);
            Assert.IsTrue(o.EventsOf<GameEndedEvent>().Single().Won);
            Assert.AreEqual(0, o.EventsOf<GoalProgressEvent>().Last().Remaining);
        }

        [Test]
        public void Cover_LocksPiece_AndAbsorbsTheHit()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "-POGY", T.Odd, T.Even },
                cover: new[] { ".....", ".....", "..1..", ".....", "....." },
                goals: new GoalDefinition(GoalType.Cover));
            var s = T.Session(level);
            int coveredId = s.Board.CellAt(2, 2).Piece.Id;
            Assert.IsFalse(s.TrySwap(T.P(2, 2), T.P(3, 2)).Accepted, "covered pieces cannot be swapped");

            var o = s.TryTap(T.P(0, 2));
            var cover = o.EventsOf<CoverDamagedEvent>().Single();
            Assert.AreEqual(T.P(2, 2), cover.Pos);
            Assert.AreEqual(0, cover.HpLeft);
            Assert.AreEqual(coveredId, s.Board.CellAt(2, 2).Piece.Id, "gem under the cover survives");
            Assert.AreEqual(0, s.Board.CellAt(2, 2).Cover);
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void CoveredPiece_DoesNotFall()
        {
            var level = T.Level(new[] { T.Even, "OGRPO", T.Even, "-GYPO", T.Even },
                cover: new[] { ".....", "..1..", ".....", ".....", "....." });
            var s = T.Session(level);
            int id = s.Board.CellAt(2, 3).Piece.Id;
            s.TryTap(T.P(0, 1));
            Assert.AreEqual(id, s.Board.CellAt(2, 3).Piece.Id);
        }

        [Test]
        public void Match_ClearsFloor_AndCompletesFloorGoal()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even },
                floor: new[] { ".....", ".....", "111..", ".....", "....." },
                goals: new GoalDefinition(GoalType.Floor));
            var s = T.Session(level);
            Assert.AreEqual(3, s.Goals[0].Target);
            var o = s.TrySwap(T.P(2, 2), T.P(3, 2));
            Assert.AreEqual(3, o.Count<FloorDamagedEvent>());
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void FloorUnderBox_IsProtected()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "-PO1Y", T.Odd, T.Even },
                floor: new[] { ".....", ".....", "...1.", ".....", "....." },
                goals: new GoalDefinition(GoalType.Floor));
            var s = T.Session(level);
            var o = s.TryTap(T.P(0, 2));
            Assert.AreEqual(1, o.EventsOf<PieceClearedEvent>().Count(e => e.Kind == PieceKind.Box));
            Assert.AreEqual(1, s.Board.CellAt(3, 2).Floor);
            Assert.AreEqual(GameState.Playing, s.State);
        }

        [Test]
        public void Collectible_IsImmuneToBlasts_AndCollectedAtBottom()
        {
            var level = T.Level(new[] { "Y$P", "O|G", "PGY" }, goals: new GoalDefinition(GoalType.Collectible));
            var s = T.Session(level);
            Assert.AreEqual(1, s.Goals[0].Target);
            var o = s.TryTap(T.P(1, 1));
            Assert.AreEqual(1, o.Count<PieceCollectedEvent>());
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void ColorGoal_CountsGemsAndWins()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even },
                goals: new GoalDefinition(GoalType.Color, 3, GemColor.Red));
            var s = T.Session(level);
            var o = s.TrySwap(T.P(2, 2), T.P(3, 2));
            Assert.AreEqual(3, o.Count<GoalProgressEvent>());
            Assert.AreEqual(GameState.Won, s.State);
            Assert.IsFalse(s.TrySwap(T.P(0, 0), T.P(0, 1)).Accepted, "no input after the game ended");
        }

        [Test]
        public void RunningOutOfMoves_Loses_AndContinueResumes()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", T.Odd, T.Even }, moves: 1);
            var s = T.Session(level);
            var o = s.TrySwap(T.P(2, 2), T.P(3, 2));
            Assert.AreEqual(GameState.Lost, s.State);
            Assert.IsFalse(o.EventsOf<GameEndedEvent>().Single().Won);
            s.AddMoves(5);
            Assert.AreEqual(GameState.Playing, s.State);
            Assert.AreEqual(5, s.MovesLeft);
            Assert.AreEqual(1, s.Stats.ContinuesUsed);
        }
    }
}
