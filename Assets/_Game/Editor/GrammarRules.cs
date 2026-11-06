#if UNITY_EDITOR
using EmberThrone.Data;

namespace EmberThrone.Editor
{
    public static class GrammarRules
    {
        public static DifficultyBand GetBandForLevel(int levelId)
        {
            if (levelId <= 10) return DifficultyBand.Tutorial;
            if (levelId <= 30) return DifficultyBand.Onboarding;
            if (levelId <= 80) return DifficultyBand.CoreCasual;
            if (levelId <= 150) return DifficultyBand.MidChallenge;
            if (levelId <= 250) return DifficultyBand.HardWall;
            return DifficultyBand.Expert;
        }

        public static (float min, float max) GetFailRateRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (0f, 0.05f),
                DifficultyBand.Onboarding => (0.10f, 0.20f),
                DifficultyBand.CoreCasual => (0.25f, 0.40f),
                DifficultyBand.MidChallenge => (0.40f, 0.55f),
                DifficultyBand.HardWall => (0.55f, 0.65f),
                DifficultyBand.Expert => (0.60f, 0.70f),
                _ => (0f, 1f),
            };
        }

        public static (int min, int max) GetGridWidthRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (7, 7),
                DifficultyBand.Onboarding => (7, 8),
                DifficultyBand.CoreCasual => (8, 9),
                DifficultyBand.MidChallenge => (8, 9),
                DifficultyBand.HardWall => (9, 9),
                DifficultyBand.Expert => (9, 9),
                _ => (7, 9),
            };
        }

        public static (int min, int max) GetGridHeightRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (7, 7),
                DifficultyBand.Onboarding => (7, 8),
                DifficultyBand.CoreCasual => (8, 9),
                DifficultyBand.MidChallenge => (8, 9),
                DifficultyBand.HardWall => (9, 9),
                DifficultyBand.Expert => (9, 9),
                _ => (7, 9),
            };
        }

        public static (int min, int max) GetColorCountRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (3, 3),
                DifficultyBand.Onboarding => (3, 4),
                DifficultyBand.CoreCasual => (4, 5),
                DifficultyBand.MidChallenge => (4, 5),
                DifficultyBand.HardWall => (5, 5),
                DifficultyBand.Expert => (5, 5),
                _ => (3, 5),
            };
        }

        public static (int min, int max) GetMoveRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (30, 40),
                DifficultyBand.Onboarding => (25, 35),
                DifficultyBand.CoreCasual => (20, 30),
                DifficultyBand.MidChallenge => (18, 25),
                DifficultyBand.HardWall => (15, 22),
                DifficultyBand.Expert => (12, 20),
                _ => (15, 40),
            };
        }

        public static ObstacleType[] GetAvailableObstacles(int levelId)
        {
            var list = new System.Collections.Generic.List<ObstacleType>();

            if (levelId >= 3)  list.Add(ObstacleType.Crate);
            if (levelId >= 8)  list.Add(ObstacleType.IceCrate);
            if (levelId >= 16) list.Add(ObstacleType.Chained);
            if (levelId >= 22) list.Add(ObstacleType.Stone);
            if (levelId >= 35) list.Add(ObstacleType.FireBox);
            if (levelId >= 50) list.Add(ObstacleType.WireCage);
            if (levelId >= 70) list.Add(ObstacleType.Crystal);

            return list.ToArray();
        }

        public static (int min, int max) GetObstacleCountRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (0, 2),
                DifficultyBand.Onboarding => (1, 4),
                DifficultyBand.CoreCasual => (3, 6),
                DifficultyBand.MidChallenge => (4, 8),
                DifficultyBand.HardWall => (5, 10),
                DifficultyBand.Expert => (6, 12),
                _ => (0, 12),
            };
        }

        public static (int min, int max) GetGoalCountRange(DifficultyBand band)
        {
            return band switch
            {
                DifficultyBand.Tutorial => (1, 1),
                DifficultyBand.Onboarding => (1, 2),
                DifficultyBand.CoreCasual => (1, 2),
                DifficultyBand.MidChallenge => (2, 3),
                DifficultyBand.HardWall => (2, 3),
                DifficultyBand.Expert => (2, 4),
                _ => (1, 4),
            };
        }

        public static (int min, int max) GetGoalTargetRange(DifficultyBand band, GoalType goalType)
        {
            return goalType switch
            {
                GoalType.CollectColor => band switch
                {
                    DifficultyBand.Tutorial => (5, 10),
                    DifficultyBand.Onboarding => (10, 20),
                    DifficultyBand.CoreCasual => (15, 30),
                    DifficultyBand.MidChallenge => (20, 40),
                    DifficultyBand.HardWall => (25, 45),
                    DifficultyBand.Expert => (30, 50),
                    _ => (5, 50),
                },
                GoalType.DestroyObstacle => band switch
                {
                    DifficultyBand.Tutorial => (1, 2),
                    DifficultyBand.Onboarding => (2, 4),
                    DifficultyBand.CoreCasual => (3, 6),
                    DifficultyBand.MidChallenge => (4, 8),
                    DifficultyBand.HardWall => (5, 10),
                    DifficultyBand.Expert => (6, 12),
                    _ => (1, 12),
                },
                GoalType.ScoreTarget => band switch
                {
                    DifficultyBand.Tutorial => (500, 1000),
                    DifficultyBand.Onboarding => (1000, 3000),
                    DifficultyBand.CoreCasual => (3000, 6000),
                    DifficultyBand.MidChallenge => (5000, 10000),
                    DifficultyBand.HardWall => (8000, 15000),
                    DifficultyBand.Expert => (10000, 20000),
                    _ => (500, 20000),
                },
                GoalType.UseSpecialPiece => band switch
                {
                    DifficultyBand.Tutorial => (0, 0),
                    DifficultyBand.Onboarding => (1, 2),
                    DifficultyBand.CoreCasual => (2, 3),
                    DifficultyBand.MidChallenge => (2, 4),
                    DifficultyBand.HardWall => (3, 5),
                    DifficultyBand.Expert => (3, 6),
                    _ => (0, 6),
                },
                _ => (1, 10),
            };
        }

        public static float EstimateSolvability(int moves, int totalGoalDifficulty, int obstacleCount, int colorCount)
        {
            if (totalGoalDifficulty <= 0) return 1f;

            float movePressure = (float)moves / totalGoalDifficulty;
            float obstaclePenalty = obstacleCount * 0.02f;
            float colorPenalty = (colorCount - 3) * 0.05f;

            float score = UnityEngine.Mathf.Clamp01(movePressure - obstaclePenalty - colorPenalty);
            return score;
        }
    }
}
#endif
