using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class NewObstacleTests
    {
        static void AssertValid(GameSession s) =>
            Assert.AreEqual(0, BoardDebug.Validate(s.Board, true).Count, string.Join("; ", BoardDebug.Validate(s.Board, true)));

        [Test]
        public void Lamp_LightsUpOnTheFirstHit_AndIsCollectedOnTheSecond()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRBRY", "LGYPO", T.Even }, 20, 6, null, null, false, new GoalDefinition(GoalType.Lamp));
            var s = T.Session(level);
            Assert.AreEqual(1, s.Goals[0].Target);
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.AreEqual(1, o.EventsOf<PieceDamagedEvent>().Single().HpLeft, "the match next to it lights it");
            Assert.AreEqual(PieceKind.Lamp, s.Board.CellAt(0, 1).Piece.Kind, "a lamp never falls");
            AssertValid(s);

            o = s.UseBooster(BoosterType.Hammer, T.P(0, 1));
            var cleared = o.EventsOf<PieceClearedEvent>().Single(e => e.Kind == PieceKind.Lamp);
            Assert.AreEqual(T.P(0, 1), cleared.Pos);
            Assert.AreEqual(0, s.Goals[0].Remaining);
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void Planter_BloomsAFlowerOnEveryHit()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "YPFGY", T.Odd, T.Even }, 20, 6, null, null, false, new GoalDefinition(GoalType.Flower));
            var s = T.Session(level);
            Assert.AreEqual(3, s.Goals[0].Target, "three buds");
            s.UseBooster(BoosterType.Hammer, T.P(2, 2));
            Assert.AreEqual(2, s.Goals[0].Remaining);
            Assert.AreEqual(2, s.Board.CellAt(2, 2).Piece.Hp);
            s.UseBooster(BoosterType.Hammer, T.P(2, 2));
            var o = s.UseBooster(BoosterType.Hammer, T.P(2, 2));
            Assert.AreEqual(1, o.EventsOf<PieceClearedEvent>().Count(e => e.Kind == PieceKind.Planter));
            Assert.AreEqual(0, s.Board.CountPieces(PieceKind.Planter), "the pot is gone and a gem falls in");
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void Statue_FillsTwoByTwo_TakesOneHitPerWave_AndBreaksFree()
        {
            var level = T.Level(new[] { T.Even, "SSYPO", "SSOG-", "OGYPO", T.Even }, 20, 6, null, null, false, new GoalDefinition(GoalType.Statue));
            var s = T.Session(level);
            Assert.AreEqual(1, s.Goals[0].Target, "one statue, four cells");
            var statue = s.Board.CellAt(0, 3).Piece;
            Assert.AreSame(statue, s.Board.CellAt(1, 2).Piece);
            Assert.AreEqual(4, statue.Hp);
            AssertValid(s);

            var clone = s.Clone(1);
            Assert.AreSame(clone.Board.CellAt(0, 3).Piece, clone.Board.CellAt(1, 2).Piece, "a copy keeps the block whole");

            var o = s.TryTap(T.P(4, 2));
            Assert.AreEqual(1, o.EventsOf<PieceDamagedEvent>().Count(e => e.PieceId == statue.Id), "a rocket across two cells hits once");
            Assert.AreEqual(3, statue.Hp);
            AssertValid(s);

            s.UseBooster(BoosterType.Hammer, T.P(0, 2));
            s.UseBooster(BoosterType.Hammer, T.P(1, 3));
            o = s.UseBooster(BoosterType.Hammer, T.P(0, 3));
            var cleared = o.EventsOf<PieceClearedEvent>().Single(e => e.Kind == PieceKind.Statue);
            Assert.AreEqual(T.P(0, 2), cleared.Pos, "the block is reported at its lower left cell");
            foreach (var p in new[] { T.P(0, 2), T.P(1, 2), T.P(0, 3), T.P(1, 3) })
                Assert.IsFalse(s.Board.CellAt(p).Piece != null && s.Board.CellAt(p).Piece.Kind == PieceKind.Statue);
            Assert.AreEqual(GameState.Won, s.State);
            AssertValid(s);
        }

        [Test]
        public void Parser_ReadsTheNewTokens_AndRejectsAHalfStatue()
        {
            var level = T.Level(new[] { T.Even, "SSYPO", "SSOGL", "OGYPF", T.Even }, 20, 6, null, null, true);
            var back = LevelParser.FromJson(LevelParser.ToJson(level));
            Assert.AreEqual(PieceKind.Lamp, back.GetCell(4, 2).Kind);
            Assert.AreEqual(PieceKind.Planter, back.GetCell(4, 1).Kind);
            Assert.AreEqual(PieceKind.Statue, back.GetCell(1, 3).Kind);
            Assert.AreEqual(back.GetCell(0, 3).Group, back.GetCell(1, 2).Group);
            Assert.Greater(back.GetCell(0, 3).Group, 0);

            Assert.Throws<LevelFormatException>(() => LevelParser.FromLayers(new[] { T.Even, "SYPOG", "SSOGY", T.Odd, T.Even }));
        }
    }
}
