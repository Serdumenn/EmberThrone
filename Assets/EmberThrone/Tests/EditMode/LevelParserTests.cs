using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class LevelParserTests
    {
        const string Sample = @"{
  ""id"": 7, ""name"": ""Ash Gate"", ""moves"": 22, ""colors"": 5, ""difficulty"": ""hard"",
  ""goals"": [ {""type"": ""box""}, {""type"": ""color"", ""color"": ""R"", ""count"": 12}, {""type"": ""floor""} ],
  ""board"": [
    ""# . . . #"",
    "". 2 . 1 ."",
    "". . * . ."",
    ""R _ $ | @""
  ],
  ""floor"": [ "".....""
             , "".1.1.""
             , ""..2..""
             , "".....""],
  ""collectibles"": {""total"": 2, ""maxOnBoard"": 1, ""spawnChance"": 0.2}
}";

        [Test]
        public void ParsesAllLayersAndMetadata()
        {
            var level = LevelParser.FromJson(Sample);
            Assert.AreEqual(7, level.Id);
            Assert.AreEqual("Ash Gate", level.Name);
            Assert.AreEqual(22, level.Moves);
            Assert.AreEqual(5, level.ColorCount);
            Assert.AreEqual("hard", level.Difficulty);
            Assert.AreEqual(5, level.Width);
            Assert.AreEqual(4, level.Height);
            Assert.AreEqual(3, level.Goals.Count);
            Assert.AreEqual(GemColor.Red, level.Goals[1].Color);
            Assert.AreEqual(2, level.Collectibles.Total);

            // top row is y = 3
            Assert.IsFalse(level.GetCell(0, 3).Exists);
            Assert.IsTrue(level.GetCell(1, 3).Spawner, "top existing cell of a column is a spawner by default");
            Assert.IsTrue(level.GetCell(0, 2).Spawner, "column 0 starts below the hole");
            Assert.AreEqual(PieceKind.Box, level.GetCell(1, 2).Kind);
            Assert.AreEqual(2, level.GetCell(1, 2).Hp);
            Assert.AreEqual(1, level.GetCell(1, 2).Floor);
            Assert.AreEqual(PieceKind.Bomb, level.GetCell(2, 1).Kind);
            Assert.AreEqual(2, level.GetCell(2, 1).Floor);
            Assert.AreEqual(CellContent.Random, level.GetCell(0, 1).Content);
            Assert.AreEqual(GemColor.Red, level.GetCell(0, 0).Color);
            Assert.AreEqual(CellContent.Empty, level.GetCell(1, 0).Content);
            Assert.AreEqual(PieceKind.Collectible, level.GetCell(2, 0).Kind);
            Assert.AreEqual(PieceKind.RocketV, level.GetCell(3, 0).Kind);
            Assert.AreEqual(PieceKind.ColorBomb, level.GetCell(4, 0).Kind);
        }

        [Test]
        public void ToJson_RoundTrips()
        {
            var level = LevelParser.FromJson(Sample);
            var again = LevelParser.FromJson(LevelParser.ToJson(level));
            Assert.AreEqual(level.Width, again.Width);
            Assert.AreEqual(level.Height, again.Height);
            Assert.AreEqual(level.Moves, again.Moves);
            Assert.AreEqual(level.Goals.Count, again.Goals.Count);
            for (int i = 0; i < level.Cells.Length; i++)
            {
                var a = level.Cells[i];
                var b = again.Cells[i];
                Assert.AreEqual(a.Exists, b.Exists, "exists " + i);
                Assert.AreEqual(a.Spawner, b.Spawner, "spawner " + i);
                Assert.AreEqual(a.Content, b.Content, "content " + i);
                if (a.Content == CellContent.Fixed)
                {
                    Assert.AreEqual(a.Kind, b.Kind, "kind " + i);
                    Assert.AreEqual(a.Color, b.Color, "color " + i);
                    Assert.AreEqual(a.Hp, b.Hp, "hp " + i);
                }
                Assert.AreEqual(a.Floor, b.Floor, "floor " + i);
                Assert.AreEqual(a.Cover, b.Cover, "cover " + i);
            }
        }

        [Test]
        public void RejectsBrokenLevels()
        {
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson("{\"moves\": 10, \"goals\": [{\"type\":\"box\"}]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [{\"type\":\"box\"}], \"board\": [\"...\", \"..\", \"...\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [{\"type\":\"box\"}], \"board\": [\"...\", \".x.\", \"...\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [], \"board\": [\"...\", \"...\", \"...\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [{\"type\":\"color\",\"color\":\"R\"}], \"board\": [\"...\", \"...\", \"...\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [{\"type\":\"cover\"}], \"board\": [\"...\", \".1.\", \"...\"], \"cover\": [\"000\", \"010\", \"000\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 99999999999, \"goals\": [{\"type\":\"box\"}], \"board\": [\"...\", \".1.\", \"...\"]}"));
            Assert.Throws<LevelFormatException>(() => LevelParser.FromJson(
                "{\"moves\": 10, \"goals\": [{\"type\":\"color\",\"color\":\"R\",\"count\":1},{\"type\":\"color\",\"color\":\"G\",\"count\":1}," +
                "{\"type\":\"color\",\"color\":\"B\",\"count\":1},{\"type\":\"color\",\"color\":\"Y\",\"count\":1}], \"board\": [\"...\", \"...\", \"...\"]}"));
        }
    }
}
