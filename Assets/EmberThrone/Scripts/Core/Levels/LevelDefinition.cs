using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum GoalType
    {
        Color,
        Box,
        Floor,
        Cover,
        Collectible,
        Lamp,
        Flower,
        Statue,
    }

    public enum CellContent
    {
        Random,
        Empty,
        Fixed,
    }

    public struct CellSpec
    {
        public bool Exists;
        public bool Spawner;
        public CellContent Content;
        public PieceKind Kind;
        public int Color;
        public int Hp;
        public int Floor;
        public int Cover;
        public bool Frost;
        public int Group;
    }

    public sealed class GoalDefinition
    {
        public GoalType Type;
        public int Color = GemColor.None;
        public int Count = -1;

        public GoalDefinition() { }

        public GoalDefinition(GoalType type, int count = -1, int color = GemColor.None)
        {
            Type = type;
            Count = count;
            Color = color;
        }
    }

    public sealed class CollectibleSettings
    {
        public int Total;
        public int MaxOnBoard = 2;
        public double SpawnChance = 0.12;
    }

    public sealed class LevelDefinition
    {
        public int Id;
        public string Name = "";
        public int Width;
        public int Height;
        public int Moves;
        public int ColorCount = 5;
        public string Difficulty = "normal";

        public bool IsSuperHard => Difficulty == "superhard";
        public bool IsHard => Difficulty == "hard" || IsSuperHard;
        public CellSpec[] Cells;
        public readonly List<GoalDefinition> Goals = new List<GoalDefinition>();
        public CollectibleSettings Collectibles;

        public CellSpec GetCell(int x, int y) => Cells[y * Width + x];

        public LevelDefinition Clone()
        {
            var c = new LevelDefinition
            {
                Id = Id,
                Name = Name,
                Width = Width,
                Height = Height,
                Moves = Moves,
                ColorCount = ColorCount,
                Difficulty = Difficulty,
                Cells = (CellSpec[])Cells.Clone(),
            };
            foreach (var g in Goals) c.Goals.Add(new GoalDefinition(g.Type, g.Count, g.Color));
            if (Collectibles != null)
            {
                c.Collectibles = new CollectibleSettings
                {
                    Total = Collectibles.Total,
                    MaxOnBoard = Collectibles.MaxOnBoard,
                    SpawnChance = Collectibles.SpawnChance,
                };
            }
            return c;
        }
    }
}
