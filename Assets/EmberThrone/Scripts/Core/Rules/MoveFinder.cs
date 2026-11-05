using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum MoveType
    {
        Swap,
        Tap,
    }

    public readonly struct Move
    {
        public readonly MoveType Type;
        public readonly GridPos A;
        public readonly GridPos B;

        Move(MoveType type, GridPos a, GridPos b)
        {
            Type = type;
            A = a;
            B = b;
        }

        public static Move Swap(GridPos a, GridPos b) => new Move(MoveType.Swap, a, b);
        public static Move Tap(GridPos a) => new Move(MoveType.Tap, a, GridPos.Invalid);

        public override string ToString() => Type == MoveType.Tap ? "Tap" + A : "Swap" + A + "->" + B;
    }

    public static class MoveFinder
    {
        public static bool IsSwappable(Cell c)
        {
            return c.Exists && c.Piece != null && c.Cover == 0 && c.Piece.Kind.IsSwappable() && !c.Piece.Triggered;
        }

        public static bool IsTappable(Cell c)
        {
            return c.Exists && c.Piece != null && c.Cover == 0 && c.Piece.Kind.IsSpecial() && !c.Piece.Triggered;
        }

        /// <summary>Whether swapping a and b is a legal, move-consuming action.</summary>
        public static bool IsValidSwap(Board b, GridPos a, GridPos c)
        {
            if (!b.InBounds(a) || !b.InBounds(c) || !a.IsAdjacent(c)) return false;
            var ca = b.CellAt(a);
            var cc = b.CellAt(c);
            if (!IsSwappable(ca) || !IsSwappable(cc)) return false;
            if (ca.Piece.Kind.IsSpecial() || cc.Piece.Kind.IsSpecial()) return true;
            if (ca.Piece.Kind == PieceKind.Gem && cc.Piece.Kind == PieceKind.Gem && ca.Piece.Color == cc.Piece.Color) return false;

            var pa = ca.Piece;
            ca.Piece = cc.Piece;
            cc.Piece = pa;
            bool ok = MatchFinder.HasMatchAt(b, a) || MatchFinder.HasMatchAt(b, c);
            cc.Piece = ca.Piece;
            ca.Piece = pa;
            return ok;
        }

        public static bool HasAnyMove(Board b)
        {
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (IsTappable(b.CellAt(p))) return true;
                    if (x + 1 < b.Width && IsValidSwap(b, p, p.Right)) return true;
                    if (y + 1 < b.Height && IsValidSwap(b, p, p.Up)) return true;
                }
            }
            return false;
        }

        /// <summary>All legal moves: taps on specials and valid swaps (each pair listed once).</summary>
        public static List<Move> FindAllMoves(Board b)
        {
            var moves = new List<Move>();
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (IsTappable(b.CellAt(p))) moves.Add(Move.Tap(p));
                    if (x + 1 < b.Width && IsValidSwap(b, p, p.Right)) moves.Add(Move.Swap(p, p.Right));
                    if (y + 1 < b.Height && IsValidSwap(b, p, p.Up)) moves.Add(Move.Swap(p, p.Up));
                }
            }
            return moves;
        }

        /// <summary>
        /// Best move for the hint system: special combos first, then swaps that create the strongest
        /// special, then the biggest plain match.
        /// </summary>
        public static Move? FindHint(Board b, GameRules rules)
        {
            Move? best = null;
            int bestScore = int.MinValue;
            foreach (var m in FindAllMoves(b))
            {
                int score = ScoreMove(b, m, rules);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = m;
                }
            }
            return best;
        }

        static int SpecialValue(PieceKind? kind)
        {
            switch (kind)
            {
                case PieceKind.ColorBomb: return 50;
                case PieceKind.Bomb: return 40;
                case PieceKind.RocketH:
                case PieceKind.RocketV: return 30;
                case PieceKind.Homing: return 25;
                default: return 0;
            }
        }

        static int ScoreMove(Board b, Move m, GameRules rules)
        {
            if (m.Type == MoveType.Tap) return 20;
            var ca = b.CellAt(m.A);
            var cb = b.CellAt(m.B);
            bool sa = ca.Piece.Kind.IsSpecial(), sb = cb.Piece.Kind.IsSpecial();
            if (sa && sb) return 100 + SpecialValue(ca.Piece.Kind) + SpecialValue(cb.Piece.Kind);
            if (sa || sb) return 60;

            var pa = ca.Piece;
            ca.Piece = cb.Piece;
            cb.Piece = pa;
            int score = 0;
            foreach (var g in MatchFinder.FindGroups(b))
            {
                if (!g.Contains(m.A) && !g.Contains(m.B)) continue;
                score += g.Cells.Count + SpecialValue(MatchFinder.DetermineSpecial(g, rules));
            }
            cb.Piece = ca.Piece;
            ca.Piece = pa;
            return score;
        }
    }
}
