namespace EmberThrone.Core
{
    /// <summary>
    /// Everything that can occupy a cell's piece layer.
    /// Specials are colorless: they never take part in color matches and are fired by tap,
    /// by swapping, or by being hit.
    /// </summary>
    public enum PieceKind
    {
        Gem = 0,
        /// <summary>Clears its whole row.</summary>
        RocketH = 1,
        /// <summary>Clears its whole column.</summary>
        RocketV = 2,
        /// <summary>Area blast (radius from GameRules).</summary>
        Bomb = 3,
        /// <summary>Hits a plus shape, then flies to the most useful target.</summary>
        Homing = 4,
        /// <summary>Clears every gem of one color.</summary>
        ColorBomb = 5,
        /// <summary>Static blocker with HP. Damaged by adjacent matches and by blasts.</summary>
        Box = 6,
        /// <summary>Falls like a gem, immune to blasts; collected when it reaches the bottom.</summary>
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

        /// <summary>Whether gravity can move this piece (cover is checked separately).</summary>
        public static bool CanFall(this PieceKind kind) => kind != PieceKind.Box;

        /// <summary>Whether the player may grab this piece (cover is checked separately).</summary>
        public static bool IsSwappable(this PieceKind kind) => kind != PieceKind.Box;
    }

    /// <summary>Gem color indices. Levels use the first N colors.</summary>
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
        /// <summary>Gem color (0..5); -1 for colorless pieces.</summary>
        public int Color;
        /// <summary>Remaining hit points for blockers.</summary>
        public int Hp;
        /// <summary>Set while a special is queued for activation during resolution.</summary>
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
