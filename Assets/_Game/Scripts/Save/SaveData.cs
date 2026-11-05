// TECH-001 §7 — SaveData
// Oyun durumunu tutan serializable data container.
// JsonUtility uyumlu — Dictionary kullanılmaz, array tabanlı.

using System;
using UnityEngine;

namespace EmberThrone.Save
{
    /// <summary>
    /// Serializable game state. Persisted to disk via SaveManager.
    /// Uses arrays instead of Dictionary for JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // ─── Version (migration desteği) ─────────────
        /// <summary>Save format version. Increment when schema changes.</summary>
        public int saveVersion = 1;

        // ─── Progression ─────────────────────────────
        /// <summary>Current level the player is on (1-based).</summary>
        public int currentLevelId = 1;

        /// <summary>Current chapter the player is in (1-based).</summary>
        public int currentChapterId = 1;

        /// <summary>Highest level ever completed.</summary>
        public int highestCompletedLevel = 0;

        /// <summary>Per-level completion flags. Index = levelId - 1.</summary>
        public bool[] completedLevels = new bool[150]; // 10 chapters × 15 levels

        // ─── Economy ─────────────────────────────────
        /// <summary>Soft currency (coins).</summary>
        public int coins = 500; // FTUE starting coins

        /// <summary>Hard currency (gems).</summary>
        public int gems = 50; // FTUE starting gems

        /// <summary>Current lives count.</summary>
        public int lives = 5;

        /// <summary>UTC ticks of last life refill time (for offline calculation).</summary>
        public long lastLifeRefillTicks;

        // ─── Boosters ────────────────────────────────
        /// <summary>BST-001 Hammer count.</summary>
        public int boosterHammer = 3; // FTUE starting hammers

        /// <summary>BST-002 Bomb count.</summary>
        public int boosterBomb = 0;

        /// <summary>BST-003 ColorBrush count.</summary>
        public int boosterColorBrush = 0;

        // ─── Chapter Tasks (META-001) ────────────────
        /// <summary>
        /// Per-chapter task progress. Indexed by [chapterIndex * maxTasksPerChapter + taskIndex].
        /// Max 10 chapters × 6 tasks = 60 slots.
        /// </summary>
        public int[] chapterTaskProgress = new int[60];

        // ─── Area Restore ────────────────────────────
        /// <summary>Per-chapter area restore completion flags. Same flat-array approach.</summary>
        public bool[] areaRestoreCompleted = new bool[60];

        // ─── Daily Reward ────────────────────────────
        /// <summary>UTC ticks of last daily reward claim.</summary>
        public long lastDailyRewardTicks;

        /// <summary>Consecutive login day streak.</summary>
        public int dailyStreak = 0;

        // ─── Stats ───────────────────────────────────
        /// <summary>Total play time in seconds.</summary>
        public float totalPlayTimeSeconds = 0f;

        /// <summary>Total levels attempted.</summary>
        public int totalLevelsAttempted = 0;

        /// <summary>Total levels completed.</summary>
        public int totalLevelsCompleted = 0;

        // ─── Timestamps ──────────────────────────────
        /// <summary>UTC ticks when save was first created.</summary>
        public long firstPlayTicks;

        /// <summary>UTC ticks of last save.</summary>
        public long lastSaveTicks;

        // ─── Factory ─────────────────────────────────

        /// <summary>
        /// Create a fresh SaveData with FTUE defaults (META-001 §FTUE).
        /// </summary>
        public static SaveData CreateNew()
        {
            var data = new SaveData
            {
                firstPlayTicks = DateTime.UtcNow.Ticks,
                lastLifeRefillTicks = DateTime.UtcNow.Ticks,
                lastSaveTicks = DateTime.UtcNow.Ticks,
            };
            return data;
        }
    }
}
