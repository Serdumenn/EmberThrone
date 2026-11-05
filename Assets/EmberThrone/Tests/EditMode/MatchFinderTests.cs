using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class MatchFinderTests
    {
        static Board Build(params string[] rows)
        {
            return BoardFactory.Create(T.Level(rows), new Pcg32(1), T.Rules());
        }

        static PieceKind? SingleGroupSpecial(Board b, int expectedCells)
        {
            var groups = MatchFinder.FindGroups(b);
            Assert.AreEqual(1, groups.Count, "group count");
            Assert.AreEqual(expectedCells, groups[0].Cells.Count, "cells in group");
            return MatchFinder.DetermineSpecial(groups[0], new GameRules());
        }

        [Test]
        public void FillerHasNoMatches()
        {
            var b = Build(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            Assert.AreEqual(0, MatchFinder.FindGroups(b).Count);
            Assert.IsFalse(MatchFinder.HasAnyMatch(b));
        }

        [Test]
        public void ThreeInARow_IsPlainMatch()
        {
            var b = Build(T.Even, T.Odd, "RRROY", T.Odd, T.Even);
            Assert.IsNull(SingleGroupSpecial(b, 3));
        }

        [Test]
        public void FourHorizontal_MakesVerticalRocket()
        {
            var b = Build(T.Even, T.Odd, "RRRRY", T.Odd, T.Even);
            Assert.AreEqual(PieceKind.RocketV, SingleGroupSpecial(b, 4));
        }

        [Test]
        public void FourVertical_MakesHorizontalRocket()
        {
            var b = Build("YPRGY", "OGRPO", "YPRGY", "OGRPO", T.Even);
            Assert.AreEqual(PieceKind.RocketH, SingleGroupSpecial(b, 4));
        }

        [Test]
        public void RocketDirectionIsConfigurable()
        {
            var b = Build(T.Even, T.Odd, "RRRRY", T.Odd, T.Even);
            var g = MatchFinder.FindGroups(b)[0];
            Assert.AreEqual(PieceKind.RocketH, MatchFinder.DetermineSpecial(g, new GameRules { RocketPerpendicularToMatch = false }));
        }

        [Test]
        public void FiveInARow_MakesColorBomb()
        {
            var b = Build(T.Even, T.Odd, "RRRRR", T.Odd, T.Even);
            Assert.AreEqual(PieceKind.ColorBomb, SingleGroupSpecial(b, 5));
        }

        [Test]
        public void LShape_MakesBomb_AtCorner()
        {
            var b = Build(T.Even, T.Odd, "RRRGY", "OGRPO", "YPRGY");
            var groups = MatchFinder.FindGroups(b);
            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual(5, groups[0].Cells.Count);
            Assert.AreEqual(PieceKind.Bomb, MatchFinder.DetermineSpecial(groups[0], new GameRules()));
            Assert.AreEqual(T.P(2, 2), groups[0].Intersection);
        }

        [Test]
        public void TShape_MakesBomb()
        {
            var b = Build(T.Even, "OGRPO", "RRROY", "OGRPO", T.Even);
            Assert.AreEqual(PieceKind.Bomb, SingleGroupSpecial(b, 5));
        }

        [Test]
        public void Square_MakesHoming()
        {
            var b = Build(T.Even, T.Odd, "RROGY", "RRYPO", T.Even);
            Assert.AreEqual(PieceKind.Homing, SingleGroupSpecial(b, 4));
        }

        [Test]
        public void SeparateGroups_AreReportedSeparately()
        {
            var b = Build("RRRGY", T.Odd, T.Even, T.Odd, "BBBGY");
            var groups = MatchFinder.FindGroups(b);
            Assert.AreEqual(2, groups.Count);
            Assert.AreEqual(GemColor.Blue, groups[0].Color, "bottom-left first");
            Assert.AreEqual(GemColor.Red, groups[1].Color);
        }

        [Test]
        public void SpecialsAndBoxesNeverMatch()
        {
            var b = Build(T.Even, T.Odd, "**1-Y", T.Odd, T.Even);
            Assert.AreEqual(0, MatchFinder.FindGroups(b).Count);
        }

        [Test]
        public void CoveredGemsStillMatch()
        {
            var level = T.Level(new[] { T.Even, T.Odd, "RRROY", T.Odd, T.Even },
                cover: new[] { ".....", ".....", ".1...", ".....", "....." });
            var b = BoardFactory.Create(level, new Pcg32(1), T.Rules());
            Assert.AreEqual(1, MatchFinder.FindGroups(b).Count);
        }
    }
}
