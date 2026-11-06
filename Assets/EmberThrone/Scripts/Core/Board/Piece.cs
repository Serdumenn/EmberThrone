namespace EmberThrone.Core
{
    public enum PieceKind
    {
        Gem = 0,
        RocketH = 1,
        RocketV = 2,
        Bomb = 3,
        Homing = 4,
        ColorBomb = 5,
        Box = 6,
        Collectible = 7,
    }

    public static class PieceKindExtensions
    {
        public static bool IsSpecial(this PieceKind kind)
        {
            return kind == PieceKind.RocketH || kind == PieceKind.RocketV || kind == PieceKind.Bomb ||
                   kind == PieceKind.Homing || kind == PieceKind.ColorBomb;
        }

        public static bool IsRocket(this PieceKind kind) => kind == PieceKind.RocketH || kind == PieceKind.RocketV;

        public static bool IsBlocker(this PieceKind kind) => kind == PieceKind.Box;

        public static bool CanFall(this PieceKind kind) => kind != PieceKind.Box;

        public static bool IsSwappable(this PieceKind kind) => kind != PieceKind.Box;
    }

    public static class GemColor
    {
        public const int None = -1;
        public const int Red = 0;
        public const int Green = 1;
        public const int Blue = 2;
        public const int Yellow = 3;
        public const int Purple = 4;
        public const int Orange = 5;
        public const int Count = 6;

        const string Letters = "RGBYPO";

        public static char ToChar(int color) => color >= 0 && color < Count ? Letters[color] : '?';

        public static int FromChar(char c)
        {
            int i = Letters.IndexOf(char.ToUpperInvariant(c));
            return i;
        }
    }

    public sealed class Piece
    {
        public readonly int Id;
        public PieceKind Kind;
        public int Color;
        public int Hp;
        internal bool Triggered;

        public Piece(int id, PieceKind kind, int color, int hp)
        {
            Id = id;
            Kind = kind;
            Color = kind == PieceKind.Gem ? color : GemColor.None;
            Hp = hp;
        }

        public bool IsGem => Kind == PieceKind.Gem;

        public bool IsSpecial => Kind.IsSpecial();

        public Piece Clone()
        {
            var p = new Piece(Id, Kind, Color, Hp);
            p.Triggered = Triggered;
            return p;
        }

        public override string ToString()
        {
            return Kind == PieceKind.Gem ? "Gem(" + GemColor.ToChar(Color) + ")#" + Id : Kind + "#" + Id;
        }
    }
}
