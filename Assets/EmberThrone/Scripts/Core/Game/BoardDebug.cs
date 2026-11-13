using System.Collections.Generic;
using System.Text;

namespace EmberThrone.Core
{
    public static class BoardDebug
    {
        public static string ToText(Board b)
        {
            var sb = new StringBuilder();
            for (int y = b.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var c = b.CellAt(x, y);
                    if (!c.Exists) sb.Append('#');
                    else if (c.Piece == null) sb.Append('_');
                    else sb.Append(LevelParser.PieceToken(c.Piece.Kind, c.Piece.Color, c.Piece.Hp));
                }
                if (y > 0) sb.Append('\n');
            }
            return sb.ToString();
        }

        public static List<string> Validate(Board b, bool settled)
        {
            var errors = new List<string>();
            var ids = new HashSet<int>();
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var c = b.CellAt(x, y);
                    var p = new GridPos(x, y);
                    if (!c.Exists)
                    {
                        if (c.Piece != null || c.Floor != 0 || c.Cover != 0 || c.Spawner) errors.Add("Hole " + p + " has content");
                        continue;
                    }
                    if (c.Floor < 0 || c.Cover < 0) errors.Add("Negative layer HP at " + p);
                    if (c.Cover > 0 && c.Piece == null) errors.Add("Cover without piece at " + p);
                    if (c.Frost && c.Cover != 1) errors.Add("Frost must be a single layer at " + p);
                    if (c.Piece == null) continue;
                    if (!ids.Add(c.Piece.Id) && c.Piece.Kind != PieceKind.Statue) errors.Add("Duplicate piece id " + c.Piece.Id + " at " + p);
                    if (c.Piece.Kind == PieceKind.Gem && (c.Piece.Color < 0 || c.Piece.Color >= GemColor.Count)) errors.Add("Bad gem color at " + p);
                    if (c.Piece.Kind != PieceKind.Gem && c.Piece.Color != GemColor.None) errors.Add("Colored non-gem at " + p);
                    if (c.Piece.Kind.IsBlocker() && c.Piece.Hp <= 0) errors.Add("Dead blocker at " + p);
                    if (c.Piece.Kind == PieceKind.Statue && b.CellsOf(c.Piece).Count != 4) errors.Add("Broken statue at " + p);
                    if (c.Piece.Triggered) errors.Add("Piece left triggered at " + p);
                    if (settled && c.Piece.Kind == PieceKind.Collectible && c.Cover == 0 && b.IsExit(x, y)) errors.Add("Uncollected collectible at " + p);
                }
            }
            if (settled)
            {
                if (!Gravity.IsStable(b)) errors.Add("Board not stable after resolution");
                if (MatchFinder.HasAnyMatch(b)) errors.Add("Unresolved match after resolution");
            }
            return errors;
        }
    }
}
