using System.Linq;

namespace EmberThrone.Core.Tests
{
    static class T
    {
        public const string Even = "YPOGY";
        public const string Odd = "OGYPO";

        public static GameRules Rules(bool shuffle = false) => new GameRules { ShuffleWhenNoMoves = shuffle };

        public static GridPos P(int x, int y) => new GridPos(x, y);

        public static LevelDefinition Level(string[] board, int moves = 20, int colors = 6, string[] floor = null,
            string[] cover = null, bool spawners = false, params GoalDefinition[] goals)
            => Level(board, moves, colors, floor, cover, null, spawners, goals);

        public static LevelDefinition Level(string[] board, int moves, int colors, string[] floor,
            string[] cover, string[] frost, bool spawners, params GoalDefinition[] goals)
        {
            string[] spawn = null;
            if (!spawners)
                spawn = board.Select(r => new string('.', r.Replace(" ", "").Length)).ToArray();
            var level = LevelParser.FromLayers(board, floor, cover, spawn, frost);
            level.Moves = moves;
            level.ColorCount = colors;
            if (goals == null || goals.Length == 0) level.Goals.Add(new GoalDefinition(GoalType.Color, 999, GemColor.Red));
            else level.Goals.AddRange(goals);
            return level;
        }

        public static GameSession Session(LevelDefinition level, int seed = 1, GameRules rules = null)
        {
            return new GameSession(level, seed, rules ?? Rules());
        }

        public static GameSession Session(params string[] board) => Session(Level(board));

        public static string Text(GameSession s) => BoardDebug.ToText(s.Board);

        public static string Rows(params string[] rows) => string.Join("\n", rows);

        public static PieceKind? KindAt(GameSession s, int x, int y) => s.Board.CellAt(x, y).Piece?.Kind;
    }
}
