namespace EmberThrone.Core
{
    public struct OnLevelStarted
    {
        public int LevelId;
        public int ChapterId;
    }

    public struct OnLevelCompleted
    {
        public int LevelId;
        public int ChapterId;
        public int MovesRemaining;
        public float ElapsedSeconds;
    }

    public struct OnLevelFailed
    {
        public int LevelId;
        public int ChapterId;
        public float GoalCompletionPercent;
    }

    public struct OnMatchMade
    {
        public int MatchCount;
        public int ColorIndex;
        public bool IsCascade;
    }

    public struct OnSpecialPieceCreated
    {
        public string PieceId;
        public int GridX;
        public int GridY;
    }

    public struct OnSpecialPieceActivated
    {
        public string PieceId;
        public int TilesDestroyed;
    }

    public struct OnObstacleHit
    {
        public string ObstacleId;
        public int RemainingHealth;
        public bool IsDestroyed;
    }

    public struct OnCoinChanged
    {
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Source;
    }

    public struct OnGemChanged
    {
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Source;
    }

    public struct OnLivesChanged
    {
        public int OldCount;
        public int NewCount;
        public float NextLifeRefillSeconds;
    }

    public struct OnBoosterUsed
    {
        public string BoosterId;
        public int RemainingCount;
    }

    public struct OnTaskCompleted
    {
        public int ChapterId;
        public int TaskIndex;
        public string TaskType;
    }

    public struct OnAreaRestoreStep
    {
        public int ChapterId;
        public int StepIndex;
        public string ObjectName;
    }

    public struct OnChapterCompleted
    {
        public int ChapterId;
        public bool IsMilestone;
    }

    public struct OnExtraMoveOffered
    {
        public int LevelId;
        public int OfferNumber;
        public int CostGems;
    }
}
