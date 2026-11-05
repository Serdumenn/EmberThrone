using System.Collections.Generic;
using EmberThrone.Core.Simulation;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    /// <summary>Plays thousands of random moves on feature-heavy boards and checks engine invariants.</summary>
    public class FuzzAndDeterminismTests
    {
        static readonly string[] Levels =
        {
            // open board, all colors
            @"{""moves"": 40, ""colors"": 6, ""goals"": [{""type"":""color"",""color"":""R"",""count"":60}],
               ""board"": [""........"", ""........"", ""........"", ""........"", ""........"", ""........"", ""........"", ""........"", ""........""]}",
            // boxes, floor, cover, starting specials
            @"{""moves"": 40, ""colors"": 5, ""goals"": [{""type"":""box""}, {""type"":""floor""}, {""type"":""cover""}],
               ""board"": [""........"", "".-....|."", ""..2332.."", ""..3..3.."", ""..2^*2.."", ""........"", "".@....@."", ""1......1"", ""........""],
               ""floor"": [""........"", ""........"", ""........"", ""...11..."", ""........"", ""11111111"", ""11111111"", "".111111."", ""22222222""],
               ""cover"": [""........"", ""........"", ""........"", ""........"", ""........"", ""..1..1.."", ""...22..."", ""........"", ""1......1""]}",
            // shaped board with holes and collectibles
            @"{""moves"": 40, ""colors"": 4, ""goals"": [{""type"":""collectible""}, {""type"":""color"",""color"":""B"",""count"":30}],
               ""board"": [""##....##"", ""#......#"", ""...$$..."", ""...##..."", ""..1##1.."", ""........"", ""#......#"", ""##....##""],
               ""collectibles"": {""total"": 4, ""maxOnBoard"": 2, ""spawnChance"": 0.3}}",
            // narrow funnel forcing diagonal refills
            @"{""moves"": 40, ""colors"": 5, ""goals"": [{""type"":""box""}],
               ""board"": [""#..#..#"", ""......."", "".1.1.1."", ""......."", ""1.1.1.1"", ""......."", ""......."" ]}",
        };

        [Test]
        public void RandomPlay_KeepsInvariants([Values(0, 1, 2, 3)] int levelIndex)
        {
            var level = LevelParser.FromJson(Levels[levelIndex]);
            for (int seed = 1; seed <= 40; seed++)
            {
                var r = LevelSimulator.PlayOnce(level, seed, new RandomPolicy(), null, validate: true);
                if (r.Error != null) Assert.Fail("level " + levelIndex + " seed " + seed + ": " + r.Error);
            }
        }

        [Test]
        public void BoostersAndCombos_KeepInvariants()
        {
            var level = LevelParser.FromJson(Levels[1]);
            for (int seed = 1; seed <= 30; seed++)
            {
                var s = new GameSession(level, seed);
                s.PlaceStartingSpecials(new[] { PieceKind.ColorBomb, PieceKind.ColorBomb, PieceKind.Bomb, PieceKind.Homing });
                var rng = new Pcg32((ulong)seed);
                for (int i = 0; i < 25 && s.State == GameState.Playing; i++)
                {
                    if (i % 7 == 3)
                    {
                        var type = (BoosterType)rng.Next(4);
                        s.UseBooster(type, new GridPos(rng.Next(level.Width), rng.Next(level.Height)));
                    }
                    else
                    {
                        var moves = s.FindValidMoves();
                        if (moves.Count == 0) break;
                        Assert.IsTrue(s.Apply(moves[rng.Next(moves.Count)]).Accepted);
                    }
                    var errors = BoardDebug.Validate(s.Board, true);
                    if (errors.Count > 0) Assert.Fail("seed " + seed + " step " + i + ": " + string.Join("; ", errors) + "\n" + BoardDebug.ToText(s.Board));
                }
            }
        }

        [Test]
        public void SameSeedAndMoves_GiveIdenticalGames()
        {
            var level = LevelParser.FromJson(Levels[2]);
            var a = new GameSession(level, 77);
            var b = new GameSession(level, 77);
            Assert.AreEqual(a.Board.ComputeHash(), b.Board.ComputeHash());
            var rngA = new Pcg32(5);
            var rngB = new Pcg32(5);
            for (int i = 0; i < 30 && a.State == GameState.Playing; i++)
            {
                var ma = new RandomPolicy().Choose(a, rngA);
                var mb = new RandomPolicy().Choose(b, rngB);
                Assert.AreEqual(ma.HasValue, mb.HasValue);
                if (!ma.HasValue) break;
                a.Apply(ma.Value);
                b.Apply(mb.Value);
                Assert.AreEqual(a.Board.ComputeHash(), b.Board.ComputeHash(), "diverged at move " + i);
            }
            Assert.AreNotEqual(new GameSession(level, 1).Board.ComputeHash(), new GameSession(level, 2).Board.ComputeHash());
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var level = LevelParser.FromJson(Levels[0]);
            var s = new GameSession(level, 3);
            ulong before = s.Board.ComputeHash();
            var copy = s.Clone(999);
            var moves = copy.FindValidMoves();
            copy.Apply(moves[0]);
            Assert.AreEqual(before, s.Board.ComputeHash());
            Assert.AreEqual(level.Moves, s.MovesLeft);
        }

        [Test]
        public void GreedyBot_BeatsRandomBot()
        {
            var level = LevelParser.FromJson(Levels[1]);
            var greedy = LevelSimulator.Run(level, 12, 100, () => new GreedyPolicy(0));
            var random = LevelSimulator.Run(level, 12, 100, () => new RandomPolicy());
            Assert.AreEqual(0, greedy.Errors.Count, string.Join("\n", greedy.Errors));
            Assert.GreaterOrEqual(greedy.WinRate + greedy.AvgGoalProgressOnLoss, random.WinRate + random.AvgGoalProgressOnLoss);
        }

        [Test]
        public void Spawning_RespectsCollectibleLimits()
        {
            var level = LevelParser.FromJson(Levels[2]);
            var counts = new List<int>();
            for (int seed = 1; seed <= 10; seed++)
            {
                var s = new GameSession(level, seed);
                var rng = new Pcg32((ulong)seed);
                var policy = new RandomPolicy();
                while (s.State == GameState.Playing)
                {
                    var m = policy.Choose(s, rng);
                    if (!m.HasValue) break;
                    s.Apply(m.Value);
                    int onBoard = s.Board.CountPieces(PieceKind.Collectible);
                    // 2 placed in the layout + spawned ones, never more than maxOnBoard from spawners on top of the layout ones.
                    Assert.LessOrEqual(onBoard, 4);
                    counts.Add(onBoard);
                }
            }
            Assert.IsTrue(counts.Count > 0);
        }
    }
}
