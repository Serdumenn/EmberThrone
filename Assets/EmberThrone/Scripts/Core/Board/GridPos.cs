using System;

namespace EmberThrone.Core
{
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly GridPos Invalid = new GridPos(int.MinValue, int.MinValue);

        public bool IsValid => X != int.MinValue;

        public GridPos Up => new GridPos(X, Y + 1);
        public GridPos Down => new GridPos(X, Y - 1);
        public GridPos Left => new GridPos(X - 1, Y);
        public GridPos Right => new GridPos(X + 1, Y);

        public GridPos Offset(int dx, int dy) => new GridPos(X + dx, Y + dy);

        public bool IsAdjacent(GridPos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;

        public int Manhattan(GridPos other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public int Chebyshev(GridPos other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode() => unchecked((X * 397) ^ Y);

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);

        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
