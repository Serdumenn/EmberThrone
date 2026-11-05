// TECH-001 §3.3 — Game Event Definitions
// Tüm event struct'ları burada tanımlanır. EventBus ile kullanılır.

namespace EmberThrone.Core
{
    // ───────────────────────────────────────────
    // LEVEL EVENTS
    // ───────────────────────────────────────────

    /// <summary>Level started — fired after board is initialized and playable.</summary>
    public struct OnLevelStarted
    {
        public int LevelId;
        public int ChapterId;
    }

    /// <summary>Level completed — all goals met.</summary>
    public struct OnLevelCompleted
    {
        public int LevelId;
        public int ChapterId;
        public int MovesRemaining;
        public float ElapsedSeconds;
    }

    /// <summary>Level failed — move counter hit zero, goals not met.</summary>
    public struct OnLevelFailed
    {
        public int LevelId;
        public int ChapterId;
        public float GoalCompletionPercent; // 0-1, for spend trigger (%70+ check)
    }

    // ───────────────────────────────────────────
    // BOARD / MATCH EVENTS
    // ───────────────────────────────────────────

    /// <summary>A valid match was made on the board.</summary>
    public struct OnMatchMade
    {
        public int MatchCount;   // Number of tiles in the match
        public int ColorIndex;   // Which color was matched
        public bool IsCascade;   // True if this match was from gravity, not player input
    }

    /// <summary>A special piece was created (rocket, bomb, TNT, rainbow).</summary>
    public struct OnSpecialPieceCreated
    {
        public string PieceId;  // SP-001 through SP-005
        public int GridX;
        public int GridY;
    }

    /// <summary>A special piece was activated.</summary>
    public struct OnSpecialPieceActivated
    {
        public string PieceId;
        public int TilesDestroyed;
    }

    /// <summary>An obstacle was damaged or destroyed.</summary>
    public struct OnObstacleHit
    {
        public string ObstacleId;   // OBS-001 through OBS-007
        public int RemainingHealth;
        public bool IsDestroyed;
    }

    // ───────────────────────────────────────────
    // ECONOMY EVENTS
    // ───────────────────────────────────────────

    /// <summary>Coin balance changed.</summary>
    public struct OnCoinChanged
    {
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Source; // "level_complete", "daily_reward", "chest", etc.
    }

    /// <summary>Gem balance changed.</summary>
    public struct OnGemChanged
    {
        public int OldAmount;
        public int NewAmount;
        public int Delta;
        public string Source;
    }

    /// <summary>Lives count changed.</summary>
    public struct OnLivesChanged
    {
        public int OldCount;
        public int NewCount;
        public float NextLifeRefillSeconds; // Seconds until next life refill
    }

    // ───────────────────────────────────────────
    // BOOSTER EVENTS
    // ───────────────────────────────────────────

    /// <summary>A booster was used during gameplay.</summary>
    public struct OnBoosterUsed
    {
        public string BoosterId; // BST-001 through BST-003
        public int RemainingCount;
    }

    // ───────────────────────────────────────────
    // META / PROGRESSION EVENTS
    // ───────────────────────────────────────────

    /// <summary>A chapter task was completed.</summary>
    public struct OnTaskCompleted
    {
        public int ChapterId;
        public int TaskIndex;
        public string TaskType;
    }

    /// <summary>An area restore step was completed.</summary>
    public struct OnAreaRestoreStep
    {
        public int ChapterId;
        public int StepIndex;
        public string ObjectName;
    }

    /// <summary>A chapter was completed (all 15 levels done).</summary>
    public struct OnChapterCompleted
    {
        public int ChapterId;
        public bool IsMilestone; // Every 5th chapter = milestone
    }

    // ───────────────────────────────────────────
    // SPEND TRIGGER — SYS-CORE-001
    // ───────────────────────────────────────────

    /// <summary>Extra move offer triggered (goal ≥ %70 complete).</summary>
    public struct OnExtraMoveOffered
    {
        public int LevelId;
        public int OfferNumber;     // 1, 2, or 3 (max 3 per level)
        public int CostGems;        // ×1.5 price scaling
    }
}
