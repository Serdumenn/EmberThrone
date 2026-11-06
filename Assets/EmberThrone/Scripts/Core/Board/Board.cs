using System;

namespace EmberThrone.Core
{
    public sealed class Cell
    {
        public bool Exists;
        public bool Spawner;
        public Piece Piece;
        public int Floor;
        public int Cover;

        public bool IsEmpty => Exists && Piece == null;

        public Cell Clone()
        {
            return new Cell
            {
                Exists = Exists,
                Spawner = Spawner,
                Piece = Piece?.Clone(),
                Floor = Floor,
                Cover = Cover,
            };
        }
    }

    public sealed class Board
    {
        public int Width { get; }
        public int Height { get; }

        readonly Cell[] _cells;
        bool[] _exit;
        int _nextPieceId = 1;

        public Board(int width, int height)
        {
            if (width < 1 || height < 1) throw new ArgumentException("Board size must be positive");
            Width = width;
            Height = height;
            _cells = new Cell[width * height];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = new Cell();
        }

        public int CellCount => _cells.Length;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool InBounds(GridPos p) => InBounds(p.X, p.Y);

        public Cell CellAt(int x, int y) => _cells[y * Width + x];

        public Cell CellAt(GridPos p) => _cells[p.Y * Width + p.X];

        public bool Exists(int x, int y) => InBounds(x, y) && _cells[y * Width + x].Exists;

        public bool Exists(GridPos p) => Exists(p.X, p.Y);

        public Piece PieceAt(GridPos p) => InBounds(p) ? CellAt(p).Piece : null;

        public Piece CreatePiece(PieceKind kind, int color = GemColor.None, int hp = 0)
        {
            return new Piece(_nextPieceId++, kind, color, hp);
        }

        public void FinalizeLayout()
        {
            _exit = new bool[_cells.Length];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!CellAt(x, y).Exists) continue;
                    _exit[y * Width + x] = true;
                    break;
                }
            }
        }

        public bool IsExit(int x, int y)
        {
            if (_exit == null) FinalizeLayout();
            return _exit[y * Width + x];
        }

        public Board Clone()
        {
            var b = new Board(Width, Height);
            for (int i = 0; i < _cells.Length; i++) b._cells[i] = _cells[i].Clone();
            b._exit = _exit == null ? null : (bool[])_exit.Clone();
            b._nextPieceId = _nextPieceId;
            return b;
        }

        public int CountPieces(PieceKind kind)
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                var p = _cells[i].Piece;
                if (p != null && p.Kind == kind) n++;
            }
            return n;
        }

        public ulong ComputeHash()
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                void Mix(long v)
                {
                    h ^= (ulong)v;
                    h *= 1099511628211UL;
                }

                Mix(Width);
                Mix(Height);
                for (int i = 0; i < _cells.Length; i++)
                {
                    var c = _cells[i];
                    Mix(c.Exists ? 1 : 0);
                    Mix(c.Spawner ? 1 : 0);
                    Mix(c.Floor);
                    Mix(c.Cover);
                    if (c.Piece == null)
                    {
                        Mix(-1);
                    }
                    else
                    {
                        Mix(c.Piece.Id);
                        Mix((int)c.Piece.Kind);
                        Mix(c.Piece.Color);
                        Mix(c.Piece.Hp);
                    }
                }
                return h;
            }
        }
    }
}
