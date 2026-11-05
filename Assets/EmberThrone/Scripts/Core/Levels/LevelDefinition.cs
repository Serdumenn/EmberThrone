using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum GoalType
    {
        /// <summary>Collect N gems of one color.</summary>
        Color,
        /// <summary>Destroy boxes.</summary>
        Box,
        /// <summary>Clear floor tiles (grass-like layer).</summary>
        Floor,
        /// <summary>Break covers (ice-like layer).</summary>
        Cover,
        /// <summary>Bring collectibles to the bottom.</summary>
        Collectible,
    }

    public enum CellContent
    {
        /// <summary>Random gem chosen at level start.</summary>
        Random,
        /// <summary>Existing cell that starts empty (filled by gravity).</summary>
        Empty,
        /// <summary>A fixed piece (gem of a given color, special, box, collectible).</summary>
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
    }

    public sealed class GoalDefinition
    {
        public GoalType Type;
        public int Color = GemColor.None;
        /// <summary>-1 = "all of them on the board" (Box, Floor, Cover, Collectible).</summary>
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
        /// <summary>How many collectibles spawn from spawners during the level.</summary>
        public int Total;
        public int MaxOnBoard = 2;
        public double SpawnChance = 0.12;
    }

    /// <summary>Immutable-by-convention level description, loaded from JSON (see LevelParser).</summary>
    public sealed class LevelDefinition
    {
        public int Id;
        public string Name = "";
        public int Width;
        public int Height;
        public int Moves;
        public int ColorCount = 5;
        /// <summary>Free tag for designers/tools: "normal", "hard", "superhard".</summary>
        public string Difficulty = "normal";
        /// <summary>Row-major, y = 0 is the bottom row.</summary>
        public CellSpec[] Cells;
        public readonly List<GoalDefinition> Goals = new List<GoalDefinition>();
        public CollectibleSettings Collectibles;

        public CellSpec GetCell(int x, int y) => Cells[y * Width + x];

        /// <summary>Deep copy (tools tweak moves/goals on copies).</summary>
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
