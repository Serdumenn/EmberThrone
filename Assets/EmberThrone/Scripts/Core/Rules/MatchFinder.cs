using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class MatchGroup
    {
        public int Color;
        public readonly List<GridPos> Cells = new List<GridPos>();
        readonly HashSet<GridPos> _set = new HashSet<GridPos>();

        public int MaxHorizontalRun;
        public int MaxVerticalRun;
        public bool HasSquare;
        public GridPos Intersection = GridPos.Invalid;
        public GridPos LongestRunStart = GridPos.Invalid;
        public int LongestRunLength;
        public bool LongestRunHorizontal;
        public GridPos SquareOrigin = GridPos.Invalid;

        public bool HasHorizontalRun => MaxHorizontalRun >= 3;
        public bool HasVerticalRun => MaxVerticalRun >= 3;

        internal void Add(GridPos p)
        {
            if (_set.Add(p)) Cells.Add(p);
        }

        public bool Contains(GridPos p) => _set.Contains(p);

        public GridPos LongestRunCenter
        {
            get
            {
                if (!LongestRunStart.IsValid) return GridPos.Invalid;
                int offset = (LongestRunLength - 1) / 2;
                return LongestRunHorizontal ? LongestRunStart.Offset(offset, 0) : LongestRunStart.Offset(0, offset);
            }
        }
    }

    public static class MatchFinder
    {
        public static bool IsMatchable(Cell c)
        {
            return c.Exists && c.Piece != null && c.Piece.Kind == PieceKind.Gem && !c.Piece.Triggered;
        }

        static bool SameColor(Board b, int x, int y, int color)
        {
            if (!b.InBounds(x, y)) return false;
            var c = b.CellAt(x, y);
            return IsMatchable(c) && c.Piece.Color == color;
        }

        public static PieceKind? DetermineSpecial(MatchGroup g, GameRules rules)
        {
            if (g.MaxHorizontalRun >= 5 || g.MaxVerticalRun >= 5) return PieceKind.ColorBomb;
            if (g.HasHorizontalRun && g.HasVerticalRun) return PieceKind.Bomb;
            if (g.MaxHorizontalRun == 4) return rules.RocketPerpendicularToMatch ? PieceKind.RocketV : PieceKind.RocketH;
            if (g.MaxVerticalRun == 4) return rules.RocketPerpendicularToMatch ? PieceKind.RocketH : PieceKind.RocketV;
            if (g.HasSquare) return PieceKind.Homing;
            return null;
        }

        public static bool HasMatchAt(Board b, GridPos p)
        {
            if (!b.InBounds(p)) return false;
            var cell = b.CellAt(p);
            if (!IsMatchable(cell)) return false;
            int color = cell.Piece.Color;
            int x = p.X, y = p.Y;

            int run = 1;
            for (int i = x - 1; SameColor(b, i, y, color); i--) run++;
            for (int i = x + 1; SameColor(b, i, y, color); i++) run++;
            if (run >= 3) return true;

            run = 1;
            for (int j = y - 1; SameColor(b, x, j, color); j--) run++;
            for (int j = y + 1; SameColor(b, x, j, color); j++) run++;
            if (run >= 3) return true;

            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    int ox = x + dx, oy = y + dy;
                    if (SameColor(b, ox, oy, color) && SameColor(b, ox + 1, oy, color) &&
                        SameColor(b, ox, oy + 1, color) && SameColor(b, ox + 1, oy + 1, color))
                        return true;
                }
            }
            return false;
        }

        public static bool HasAnyMatch(Board b)
        {
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                    if (HasMatchAt(b, new GridPos(x, y))) return true;
            return false;
        }

        struct Run
        {
            public int StartIndex;
            public int Length;
            public bool Horizontal;
        }

        public static List<MatchGroup> FindGroups(Board b)
        {
            int w = b.Width, h = b.Height, n = w * h;
            int[] parent = null;
            byte[] flags = null;
            var runs = new List<Run>();
            var squares = new List<int>();

            int Find(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }
                return i;
            }

            void Union(int a, int c)
            {
                int ra = Find(a), rc = Find(c);
                if (ra == rc) return;
                if (ra < rc) parent[rc] = ra; else parent[ra] = rc;
            }

            void Ensure()
            {
                if (parent != null) return;
                parent = new int[n];
                for (int i = 0; i < n; i++) parent[i] = i;
                flags = new byte[n];
            }

            for (int y = 0; y < h; y++)
            {
                int x = 0;
                while (x < w)
                {
                    var c = b.CellAt(x, y);
                    if (!IsMatchable(c)) { x++; continue; }
                    int color = c.Piece.Color;
                    int end = x + 1;
                    while (end < w && SameColor(b, end, y, color)) end++;
                    int len = end - x;
                    if (len >= 3)
                    {
                        Ensure();
                        int start = y * w + x;
                        runs.Add(new Run { StartIndex = start, Length = len, Horizontal = true });
                        for (int k = 0; k < len; k++)
                        {
                            flags[start + k] |= 1;
                            if (k > 0) Union(start, start + k);
                        }
                    }
                    x = end;
                }
            }

            for (int x = 0; x < w; x++)
            {
                int y = 0;
                while (y < h)
                {
                    var c = b.CellAt(x, y);
                    if (!IsMatchable(c)) { y++; continue; }
                    int color = c.Piece.Color;
                    int end = y + 1;
                    while (end < h && SameColor(b, x, end, color)) end++;
                    int len = end - y;
                    if (len >= 3)
                    {
                        Ensure();
                        int start = y * w + x;
                        runs.Add(new Run { StartIndex = start, Length = len, Horizontal = false });
                        for (int k = 0; k < len; k++)
                        {
                            flags[start + k * w] |= 2;
                            if (k > 0) Union(start, start + k * w);
                        }
                    }
                    y = end;
                }
            }

            for (int y = 0; y < h - 1; y++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    var c = b.CellAt(x, y);
                    if (!IsMatchable(c)) continue;
                    int color = c.Piece.Color;
                    if (!SameColor(b, x + 1, y, color) || !SameColor(b, x, y + 1, color) || !SameColor(b, x + 1, y + 1, color)) continue;
                    Ensure();
                    int i0 = y * w + x;
                    squares.Add(i0);
                    int[] idx = { i0, i0 + 1, i0 + w, i0 + w + 1 };
                    foreach (int i in idx)
                    {
                        flags[i] |= 4;
                        Union(i0, i);
                    }
                }
            }

            var result = new List<MatchGroup>();
            if (parent == null) return result;

            var byRoot = new Dictionary<int, MatchGroup>();
            for (int i = 0; i < n; i++)
            {
                if (flags[i] == 0) continue;
                int root = Find(i);
                if (!byRoot.TryGetValue(root, out var g))
                {
                    g = new MatchGroup { Color = b.CellAt(i % w, i / w).Piece.Color };
                    byRoot[root] = g;
                    result.Add(g);
                }
                var pos = new GridPos(i % w, i / w);
                g.Add(pos);
                if ((flags[i] & 3) == 3 && !g.Intersection.IsValid) g.Intersection = pos;
            }

            foreach (var run in runs)
            {
                var g = byRoot[Find(run.StartIndex)];
                if (run.Horizontal) { if (run.Length > g.MaxHorizontalRun) g.MaxHorizontalRun = run.Length; }
                else if (run.Length > g.MaxVerticalRun) g.MaxVerticalRun = run.Length;
                if (run.Length > g.LongestRunLength)
                {
                    g.LongestRunLength = run.Length;
                    g.LongestRunHorizontal = run.Horizontal;
                    g.LongestRunStart = new GridPos(run.StartIndex % w, run.StartIndex / w);
                }
            }

            foreach (int sq in squares)
            {
                var g = byRoot[Find(sq)];
                if (!g.HasSquare)
                {
                    g.HasSquare = true;
                    g.SquareOrigin = new GridPos(sq % w, sq / w);
                }
            }

            foreach (var g in result)
            {
                if (g.HasHorizontalRun && g.HasVerticalRun && !g.Intersection.IsValid)
                {
                    if (g.MaxHorizontalRun >= g.MaxVerticalRun) g.MaxVerticalRun = 0; else g.MaxHorizontalRun = 0;
                }
            }

            return result;
        }
    }
}
