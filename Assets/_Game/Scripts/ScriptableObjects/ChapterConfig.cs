// TECH-001 §4 — ChapterConfig ScriptableObject
// Her chapter 15 level, 4-6 task, area restore adımları içerir.

using System;
using UnityEngine;

namespace EmberThrone.Data
{
    /// <summary>
    /// Defines a chapter (15 levels, tasks, area restore steps).
    /// META-001: 10 chapters at launch (150 levels total).
    /// </summary>
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

        // ─── Levels ──────────────────────────────────
        [Header("─── Levels ───")]
        [Tooltip("15 level configs per chapter (linear navigation)")]
        public LevelConfig[] levelConfigs;

        // ─── Tasks (META-001) ────────────────────────
        [Header("─── Tasks ───")]
        [Tooltip("4-6 chapter tasks: star topla, level tamamla, booster kullan")]
        public ChapterTask[] tasks;

        // ─── Area Restore (META-001) ─────────────────
        [Header("─── Area Restore ───")]
        [Tooltip("Each task completion restores one visual object")]
        public AreaRestoreStep[] areaRestoreSteps;

        // ─── Milestone ───────────────────────────────
        [Header("─── Milestone ───")]
        [Tooltip("Is this a milestone chapter? (every 5th: chapter 5, 10)")]
        public bool isMilestone;

        [Tooltip("Milestone reward: bonus gems (default +20 for milestone chapters)")]
        [Min(0)]
        public int milestoneGemReward = 0;

        // ─── Validation ──────────────────────────────
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (levelConfigs != null && levelConfigs.Length != 15)
                Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: expected 15 levels, has {levelConfigs.Length}");

            if (tasks == null || tasks.Length < 4 || tasks.Length > 6)
                Debug.LogWarning($"[ChapterConfig] Chapter {chapterId}: expected 4-6 tasks, has {tasks?.Length ?? 0}");

            // Milestone check
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

    // ─── Supporting Types ────────────────────────────

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
        CollectStars,       // Collect N stars from levels
        CompleteLevels,     // Complete N levels
        UseBooster,         // Use N boosters
        DestroyObstacles,   // Destroy N obstacles of any type
        CreateSpecialPiece, // Create N special pieces
    }
}
