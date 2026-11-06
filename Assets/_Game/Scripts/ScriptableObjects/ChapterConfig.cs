using System;
using UnityEngine;

namespace EmberThrone.Data
{
    [CreateAssetMenu(fileName = "Chapter_00", menuName = "EmberThrone/Chapter Config", order = 2)]
    public class ChapterConfig : ScriptableObject
    {
        [Header("─── Identity ───")]
        [Tooltip("Unique chapter ID (1-10)")]
        [Range(1, 10)]
        public int chapterId;

        [Tooltip("Display name for the chapter")]
        public string chapterName;

        [Tooltip("Short description shown in the chapter intro")]
        [TextArea(1, 3)]
        public string chapterDescription;

        [Header("─── Levels ───")]
        [Tooltip("15 level configs per chapter (linear navigation)")]
        public LevelConfig[] levelConfigs;

        [Header("─── Tasks ───")]
        [Tooltip("4-6 chapter tasks: star topla, level tamamla, booster kullan")]
        public ChapterTask[] tasks;

        [Header("─── Area Restore ───")]
        [Tooltip("Each task completion restores one visual object")]
        public AreaRestoreStep[] areaRestoreSteps;

        [Header("─── Milestone ───")]
        [Tooltip("Is this a milestone chapter? (every 5th: chapter 5, 10)")]
        public bool isMilestone;

        [Tooltip("Milestone reward: bonus gems (default +20 for milestone chapters)")]
        [Min(0)]
        public int milestoneGemReward = 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (levelConfigs != null && levelConfigs.Length != 15)
                Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: expected 15 levels, has {levelConfigs.Length}");

            if (tasks == null || tasks.Length < 4 || tasks.Length > 6)
                Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: expected 4-6 tasks, has {tasks?.Length ?? 0}");

            if (chapterId == 5 || chapterId == 10)
            {
                if (!isMilestone)
                    Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: should be marked as milestone");
                if (milestoneGemReward <= 0)
                    Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: milestone gem reward should be > 0 (default: 20)");
            }
        }
#endif
    }

    [Serializable]
    public struct ChapterTask
    {
        public ChapterTaskType taskType;
        public int targetCount;

        [Tooltip("Display description for the task")]
        public string displayText;
    }

    [Serializable]
    public struct AreaRestoreStep
    {
        [Tooltip("Display name of the object being restored")]
        public string objectName;

        [Tooltip("Prefab or sprite reference for the restored object")]
        public Sprite restoreSprite;

        [Tooltip("Grid position in the area restore view")]
        public Vector2 position;
    }

    public enum ChapterTaskType
    {
        CollectStars,
        CompleteLevels,
        UseBooster,
        DestroyObstacles,
        CreateSpecialPiece,
    }
}
