using System;
using UnityEngine;

namespace EmberThrone.Save
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;

        public int currentLevelId = 1;

        public int currentChapterId = 1;

        public int highestCompletedLevel = 0;

        public bool[] completedLevels = new bool[150];

        public int coins = 500;

        public int gems = 50;

        public int lives = 5;

        public long lastLifeRefillTicks;

        public int boosterHammer = 3;

        public int boosterBomb = 0;

        public int boosterColorBrush = 0;

        public int[] chapterTaskProgress = new int[60];

        public bool[] areaRestoreCompleted = new bool[60];

        public long lastDailyRewardTicks;

        public int dailyStreak = 0;

        public float totalPlayTimeSeconds = 0f;

        public int totalLevelsAttempted = 0;

        public int totalLevelsCompleted = 0;

        public long firstPlayTicks;

        public long lastSaveTicks;

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
