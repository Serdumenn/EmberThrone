namespace EmberThrone.Core
{
    /// <summary>Tunable gameplay constants. Keep balance numbers here, not scattered in code.</summary>
    public sealed class GameRules
    {
        /// <summary>Bomb blast radius (2 = 5x5).</summary>
        public int BombRadius = 2;

        /// <summary>Bomb + Bomb combo radius (3 = 7x7).</summary>
        public int BombBombRadius = 3;

        /// <summary>Number of strikes launched by a Homing + Homing combo.</summary>
        public int HomingHomingStrikes = 3;

        /// <summary>
        /// true: a horizontal line of 4 creates a vertical rocket (and vice versa).
        /// false: the rocket fires along the line it was made from.
        /// </summary>
        public bool RocketPerpendicularToMatch = true;

        /// <summary>Shuffle automatically when the settled board has no valid move.</summary>
        public bool ShuffleWhenNoMoves = true;

        public int MaxCascadeRounds = 500;
        public int MaxGravityTicks = 10000;
        public int MaxShuffleAttempts = 200;
        public int MaxInitialFillAttempts = 30;

        /// <summary>Moves left at the win needed for 2 stars (SYS-CORE-001 §4.2).</summary>
        public int TwoStarMovesLeft = 3;

        /// <summary>Moves left at the win needed for 3 stars (SYS-CORE-001 §4.2).</summary>
        public int ThreeStarMovesLeft = 6;

        /// <summary>Stars for a won level.</summary>
        public int StarsFor(int movesLeft)
        {
            if (movesLeft >= ThreeStarMovesLeft) return 3;
            return movesLeft >= TwoStarMovesLeft ? 2 : 1;
        }

        public GameRules Clone() => (GameRules)MemberwiseClone();
    }
}
