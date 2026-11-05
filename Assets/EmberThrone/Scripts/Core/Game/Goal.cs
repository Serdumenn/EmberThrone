using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class Goal
    {
        public readonly GoalType Type;
        public readonly int Color;
        public readonly int Target;
        public int Remaining;

        public Goal(GoalType type, int color, int target)
        {
            Type = type;
            Color = color;
            Target = target;
            Remaining = target;
        }

        public bool IsComplete => Remaining <= 0;

        /// <summary>0..1 progress.</summary>
        public double Progress => Target <= 0 ? 1.0 : (Target - System.Math.Max(0, Remaining)) / (double)Target;

        public Goal Clone()
        {
            return new Goal(Type, Color, Target) { Remaining = Remaining };
        }

        public override string ToString()
        {
            string name = Type == GoalType.Color ? "Color " + GemColor.ToChar(Color) : Type.ToString();
            return name + " " + (Target - Remaining) + "/" + Target;
        }

        /// <summary>Resolves "-1 = all" counts against the freshly built board.</summary>
        public static List<Goal> CreateAll(LevelDefinition level, Board board)
        {
            var goals = new List<Goal>();
            foreach (var def in level.Goals)
            {
                int count = def.Count;
                if (count < 0)
                {
                    switch (def.Type)
                    {
                        case GoalType.Box:
                            count = board.CountPieces(PieceKind.Box);
                            break;
                        case GoalType.Floor:
                            count = CountCells(board, c => c.Floor > 0);
                            break;
                        case GoalType.Cover:
                            count = CountCells(board, c => c.Cover > 0);
                            break;
                        case GoalType.Collectible:
                            count = board.CountPieces(PieceKind.Collectible) + (level.Collectibles?.Total ?? 0);
                            break;
                        default:
                            count = 0;
                            break;
                    }
                }
                goals.Add(new Goal(def.Type, def.Color, count));
            }
            return goals;
        }

        static int CountCells(Board board, System.Func<Cell, bool> predicate)
        {
            int n = 0;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var c = board.CellAt(x, y);
                    if (c.Exists && predicate(c)) n++;
                }
            return n;
        }
    }

    public sealed class SessionStats
    {
        public int MovesMade;
        public int SpecialsCreated;
        public int SpecialsActivated;
        public int Combos;
        public int Shuffles;
        public int BoostersUsed;
        public int MaxClearRounds;
        public int ContinuesUsed;

        public SessionStats Clone() => (SessionStats)MemberwiseClone();
    }
}
