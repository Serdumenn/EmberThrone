namespace EmberThrone.Core
{
    public sealed class GameRules
    {
        public int BombRadius = 2;

        public int BombBombRadius = 3;

        public int HomingHomingStrikes = 3;

        public bool RocketPerpendicularToMatch = true;

        public bool ShuffleWhenNoMoves = true;

        public int MaxCascadeRounds = 500;
        public int MaxGravityTicks = 10000;
        public int MaxShuffleAttempts = 200;
        public int MaxInitialFillAttempts = 30;

        public int TwoStarMovesLeft = 3;

        public int ThreeStarMovesLeft = 6;

        public int StarsFor(int movesLeft)
        {
            if (movesLeft >= ThreeStarMovesLeft) return 3;
            return movesLeft >= TwoStarMovesLeft ? 2 : 1;
        }

        public GameRules Clone() => (GameRules)MemberwiseClone();
    }
}
