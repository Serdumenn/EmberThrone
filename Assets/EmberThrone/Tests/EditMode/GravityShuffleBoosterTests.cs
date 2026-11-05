using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class GravityShuffleBoosterTests
    {
        static Board Build(LevelDefinition level) => BoardFactory.Create(level, new Pcg32(1), T.Rules());

        [Test]
        public void Gravity_SlidesDiagonallyUnderABox()
        {
            var b = Build(T.Level(new[] { "YPO", "G1B", "O_P", "PYG" }));
            int gemId = b.CellAt(0, 2).Piece.Id;
            var events = new List<BoardEvent>();
            bool moved = Gravity.Run(b, new GameRules(), p => b.CreatePiece(PieceKind.Gem, 0), events);
            Assert.IsTrue(moved);
            Assert.AreEqual(gemId, b.CellAt(1, 1).Piece.Id, "left neighbour slid under the box");
            var move = events.OfType<PieceMovedEvent>().First(e => e.PieceId == gemId);
            Assert.AreEqual(T.P(0, 2), move.From);
            Assert.AreEqual(T.P(1, 1), move.To);
            Assert.IsTrue(Gravity.IsStable(b));
        }

        [Test]
        public void Gravity_SpawnersRefillColumns_WithTimedPaths()
        {
            var level = T.Level(new[] { "___", "___", "___" }, spawners: true);
            var b = Build(level);
            var events = new List<BoardEvent>();
            Gravity.Run(b, new GameRules(), p => b.CreatePiece(PieceKind.Gem, p.X), events);
            Assert.AreEqual(9, events.OfType<PieceSpawnedEvent>().Count());
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    Assert.IsNotNull(b.CellAt(x, y).Piece);
            var firstSpawn = events.OfType<PieceSpawnedEvent>().First(e => e.Pos.X == 0);
            var path = events.OfType<PieceMovedEvent>().First(e => e.PieceId == firstSpawn.PieceId).Path;
            Assert.AreEqual(2, path.Count, "first piece falls two cells");
            Assert.Less(path[0].Tick, path[1].Tick);
        }

        [Test]
        public void Gravity_HolesBlockColumns()
        {
            var b = Build(T.Level(new[] { "YPO", "G#B", "O_P" }));
            Gravity.Run(b, new GameRules(), p => b.CreatePiece(PieceKind.Gem, 0), new List<BoardEvent>());
            Assert.IsNotNull(b.CellAt(1, 0).Piece, "filled diagonally around the hole");
            Assert.IsFalse(b.CellAt(1, 1).Exists);
        }

        [Test]
        public void StartingBoard_FillsEmptyCellsWithGravity()
        {
            var level = T.Level(new[] { "....", "....", "____", "____" }, colors: 4, spawners: true);
            var s = T.Session(level, 3, T.Rules(true));
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.IsNotNull(s.Board.CellAt(x, y).Piece);
            Assert.AreEqual(0, BoardDebug.Validate(s.Board, true).Count);
        }

        [Test]
        public void NoMoves_TriggersShuffle_KeepingSamePieces()
        {
            var level = T.Level(new[] { T.Even, T.Odd, T.Even, T.Odd, T.Even });
            Assert.IsFalse(MoveFinder.HasAnyMove(Build(level)), "premise: filler has no moves");
            var s = T.Session(level, 5, T.Rules(true));
            var shuffle = s.InitialOutcome.EventsOf<ShuffleEvent>().Single();
            Assert.AreEqual(25, shuffle.Entries.Count);
            Assert.IsTrue(MoveFinder.HasAnyMove(s.Board));
            Assert.IsFalse(MatchFinder.HasAnyMatch(s.Board));
            Assert.AreEqual(1, s.Stats.Shuffles);
        }

        [Test]
        public void Hint_PrefersSpecialCreation()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRR", "OGRPO", T.Even);
            var hint = s.FindHint();
            Assert.IsTrue(hint.HasValue);
            Assert.AreEqual(MoveType.Swap, hint.Value.Type);
            var set = new[] { hint.Value.A, hint.Value.B };
            CollectionAssert.AreEquivalent(new[] { T.P(2, 2), T.P(2, 1) }, set, "the 5-in-a-row move");
        }

        [Test]
        public void Hammer_DamagesBox_WithoutUsingAMove()
        {
            var s = T.Session(T.Even, T.Odd, "YP2GY", T.Odd, T.Even);
            var o = s.UseBooster(BoosterType.Hammer, T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(20, s.MovesLeft);
            Assert.AreEqual(1, o.EventsOf<PieceDamagedEvent>().Single().HpLeft);
            Assert.AreEqual(1, s.Stats.BoostersUsed);
        }

        [Test]
        public void Hammer_OnCollectible_IsRejected_AndNotCounted()
        {
            var s = T.Session(T.Even, T.Odd, "YP$GY", T.Odd, T.Even);
            var o = s.UseBooster(BoosterType.Hammer, T.P(2, 2));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual(0, o.Steps.Count);
            Assert.AreEqual(0, s.Stats.BoostersUsed);
        }

        [Test]
        public void Hammer_OnEmptyCell_NeedsAFloorToHit()
        {
            string[] board = { "YP_GY", T.Odd, T.Even, T.Odd, T.Even };
            var bare = T.Session(T.Level(board));
            Assert.IsFalse(bare.UseBooster(BoosterType.Hammer, T.P(2, 4)).Accepted);
            Assert.AreEqual(0, bare.Stats.BoostersUsed);

            var floored = T.Session(T.Level(board, floor: new[] { "00100", "00000", "00000", "00000", "00000" }));
            var o = floored.UseBooster(BoosterType.Hammer, T.P(2, 4));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(0, o.EventsOf<FloorDamagedEvent>().Single().HpLeft);
            Assert.AreEqual(1, floored.Stats.BoostersUsed);
        }

        [Test]
        public void LineBlast_WithNothingToHit_IsRejected()
        {
            var s = T.Session(T.Level(new[] { "OGY", "$$$", "YPO" }));
            var o = s.UseBooster(BoosterType.RowBlast, T.P(0, 1));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual(0, s.Stats.BoostersUsed);
            Assert.IsTrue(s.UseBooster(BoosterType.ColumnBlast, T.P(0, 1)).Accepted, "the column still has gems");
        }

        [Test]
        public void RowAndColumnBlast_ClearLines()
        {
            var s = T.Session(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            var row = s.UseBooster(BoosterType.RowBlast, T.P(0, 0));
            Assert.AreEqual(5, row.Steps.First(st => st.Kind == StepKind.Clear).Events.OfType<PieceClearedEvent>().Count());
            var s2 = T.Session(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            var col = s2.UseBooster(BoosterType.ColumnBlast, T.P(4, 4));
            Assert.AreEqual(5, col.Steps.First(st => st.Kind == StepKind.Clear).Events.OfType<PieceClearedEvent>().Count());
        }

        [Test]
        public void ShuffleBooster_Rearranges()
        {
            var level = T.Level(new[] { "....", "....", "....", "...." }, colors: 4, spawners: true);
            var s = T.Session(level, 9, T.Rules(true));
            var o = s.UseBooster(BoosterType.Shuffle, GridPos.Invalid);
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(16, o.EventsOf<ShuffleEvent>().Single().Entries.Count);
            Assert.IsTrue(MoveFinder.HasAnyMove(s.Board));
        }

        [Test]
        public void Stars_FollowMovesLeft()
        {
            var rules = new GameRules();
            Assert.AreEqual(1, rules.StarsFor(0));
            Assert.AreEqual(1, rules.StarsFor(2));
            Assert.AreEqual(2, rules.StarsFor(3));
            Assert.AreEqual(2, rules.StarsFor(5));
            Assert.AreEqual(3, rules.StarsFor(6));
        }

        [Test]
        public void StartingSpecials_ArePlaced()
        {
            var s = T.Session(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            var o = s.PlaceStartingSpecials(new[] { PieceKind.Bomb, PieceKind.RocketH, PieceKind.ColorBomb });
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(3, o.Count<PieceConvertedEvent>());
            Assert.AreEqual(1, s.Board.CountPieces(PieceKind.Bomb));
            Assert.AreEqual(1, s.Board.CountPieces(PieceKind.RocketH));
            Assert.AreEqual(1, s.Board.CountPieces(PieceKind.ColorBomb));
        }
    }
}
