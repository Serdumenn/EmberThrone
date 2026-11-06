#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using EmberThrone.Data;

namespace EmberThrone.Editor
{
    public class GrammarSimulator : EditorWindow
    {
        private int _chapterId = 1;
        private int _startLevel = 1;
        private int _endLevel = 15;
        private bool _overwriteExisting = false;
        private string _outputFolder = "Assets/_Game/ScriptableObjects/Definitions/Levels";

        private Vector2 _scrollPos;
        private string _lastLog = "";
        private int _generatedCount = 0;

        [MenuItem("EmberThrone/Grammar Simulator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GrammarSimulator>("Grammar Simulator");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            EditorGUILayout.Space(10);
            DrawConfiguration();
            EditorGUILayout.Space(10);
            DrawGenerateButton();
            EditorGUILayout.Space(10);
            DrawDifficultyPreview();
            EditorGUILayout.Space(10);
            DrawLogSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("🔥 EmberThrone Grammar Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "LevelConfig SO'larını zorluk eğrisine göre otomatik üretir.\n" +
                "TECH-001 §5 — LVL-DESIGN-001 spesifikasyonuna uygun.",
                MessageType.Info
            );
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("─── Konfigürasyon ───", EditorStyles.boldLabel);

            _chapterId = EditorGUILayout.IntSlider("Chapter ID", _chapterId, 1, 10);

            EditorGUILayout.BeginHorizontal();
            _startLevel = EditorGUILayout.IntField("Başlangıç Level", _startLevel);
            _endLevel = EditorGUILayout.IntField("Bitiş Level", _endLevel);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Chapter'a Göre Otomatik Ayarla"))
            {
                _startLevel = (_chapterId - 1) * 15 + 1;
                _endLevel = _chapterId * 15;
            }

            _overwriteExisting = EditorGUILayout.Toggle("Mevcut SO'ları Üstüne Yaz", _overwriteExisting);
            _outputFolder = EditorGUILayout.TextField("Çıktı Klasörü", _outputFolder);

            if (_startLevel > _endLevel)
            {
                EditorGUILayout.HelpBox("Başlangıç level, bitiş level'dan büyük olamaz!", MessageType.Error);
            }

            if (_endLevel - _startLevel + 1 > 150)
            {
                EditorGUILayout.HelpBox("Tek seferde en fazla 150 level üretilebilir.", MessageType.Warning);
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(_startLevel > _endLevel);

            var color = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);

            if (GUILayout.Button($"🎲 {_endLevel - _startLevel + 1} Level Üret", GUILayout.Height(35)))
            {
                GenerateLevels();
            }

            GUI.backgroundColor = color;
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDifficultyPreview()
        {
            EditorGUILayout.LabelField("─── Zorluk Önizlemesi ───", EditorStyles.boldLabel);

            var band = GrammarRules.GetBandForLevel(_startLevel);
            var (failMin, failMax) = GrammarRules.GetFailRateRange(band);
            var (gridWMin, gridWMax) = GrammarRules.GetGridWidthRange(band);
            var (moveMin, moveMax) = GrammarRules.GetMoveRange(band);
            var (colorMin, colorMax) = GrammarRules.GetColorCountRange(band);
            var obstacles = GrammarRules.GetAvailableObstacles(_startLevel);

            EditorGUILayout.LabelField($"Band: {band}");
            EditorGUILayout.LabelField($"Beklenen Başarısızlık: {failMin:P0} - {failMax:P0}");
            EditorGUILayout.LabelField($"Grid: {gridWMin}×{gridWMin} — {gridWMax}×{gridWMax}");
            EditorGUILayout.LabelField($"Hamle: {moveMin} — {moveMax}");
            EditorGUILayout.LabelField($"Renk: {colorMin} — {colorMax}");
            EditorGUILayout.LabelField($"Açık Engeller: {obstacles.Length} tip ({string.Join(", ", obstacles)})");
        }

        private void DrawLogSection()
        {
            if (string.IsNullOrEmpty(_lastLog)) return;

            EditorGUILayout.LabelField("─── Son İşlem ───", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_lastLog, MessageType.None);
        }

        private void GenerateLevels()
        {
            if (!AssetDatabase.IsValidFolder(_outputFolder))
            {
                CreateFolderRecursive(_outputFolder);
            }

            _generatedCount = 0;
            int skippedCount = 0;

            for (int levelId = _startLevel; levelId <= _endLevel; levelId++)
            {
                string assetPath = $"{_outputFolder}/Level_{levelId:D3}.asset";

                if (!_overwriteExisting && AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath) != null)
                {
                    skippedCount++;
                    continue;
                }

                var config = GenerateSingleLevel(levelId);

                if (_overwriteExisting && AssetDatabase.LoadAssetAtPath<LevelConfig>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(config, assetPath);
                _generatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _lastLog = $"✅ {_generatedCount} level üretildi, {skippedCount} atlandı.\n" +
                       $"Klasör: {_outputFolder}\n" +
                       $"Aralık: Level {_startLevel} — {_endLevel}";

            Debug.Log($"[GrammarSimulator] {_lastLog}");
        }

        private LevelConfig GenerateSingleLevel(int levelId)
        {
            var config = ScriptableObject.CreateInstance<LevelConfig>();

            var rng = new System.Random(levelId * 31337);

            var band = GrammarRules.GetBandForLevel(levelId);
            int chapterId = ((levelId - 1) / 15) + 1;
            int posInChapter = ((levelId - 1) % 15) + 1;

            config.levelId = levelId;
            config.chapterId = chapterId;

            var (gridWMin, gridWMax) = GrammarRules.GetGridWidthRange(band);
            var (gridHMin, gridHMax) = GrammarRules.GetGridHeightRange(band);
            config.gridWidth = rng.Next(gridWMin, gridWMax + 1);
            config.gridHeight = rng.Next(gridHMin, gridHMax + 1);

            var (colorMin, colorMax) = GrammarRules.GetColorCountRange(band);
            config.availableColors = rng.Next(colorMin, colorMax + 1);

            var (moveMin, moveMax) = GrammarRules.GetMoveRange(band);
            config.maxMoves = rng.Next(moveMin, moveMax + 1);

            config.gravityDirection = GravityDirection.Down;

            config.difficultyBand = band;
            var (failMin, failMax) = GrammarRules.GetFailRateRange(band);
            config.expectedFailRate = failMin + (float)(rng.NextDouble() * (failMax - failMin));

            config.goals = GenerateGoals(rng, band, levelId);

            config.obstacles = GenerateObstacles(rng, band, levelId);

            config.specialPieceWeights = GenerateSpecialPieceWeights(rng, band);

            config.isGrammarGenerated = true;
            config.isOverridden = false;
            config.isCriticalLevel = (posInChapter == 5 || posInChapter == 10 || posInChapter == 15);

            int totalGoalDifficulty = 0;
            foreach (var goal in config.goals)
                totalGoalDifficulty += goal.targetCount;

            int obstacleTotal = 0;
            foreach (var obs in config.obstacles)
                obstacleTotal += obs.count;

            config.solvabilityRate = GrammarRules.EstimateSolvability(
                config.maxMoves, totalGoalDifficulty, obstacleTotal, config.availableColors
            );

            if (config.isCriticalLevel)
            {
                config.maxMoves = Mathf.Max(config.maxMoves - 2, moveMin);
            }

            return config;
        }

        private LevelGoal[] GenerateGoals(System.Random rng, DifficultyBand band, int levelId)
        {
            var (goalMin, goalMax) = GrammarRules.GetGoalCountRange(band);
            int goalCount = rng.Next(goalMin, goalMax + 1);

            var goals = new LevelGoal[goalCount];
            var usedTypes = new System.Collections.Generic.HashSet<GoalType>();

            for (int i = 0; i < goalCount; i++)
            {
                GoalType type;
                int safetyBreak = 0;
                do
                {
                    type = GetRandomGoalType(rng, band, levelId);
                    safetyBreak++;
                } while (usedTypes.Contains(type) && safetyBreak < 10);

                usedTypes.Add(type);

                var (targetMin, targetMax) = GrammarRules.GetGoalTargetRange(band, type);
                int target = rng.Next(targetMin, Mathf.Max(targetMin + 1, targetMax + 1));

                goals[i] = new LevelGoal
                {
                    goalType = type,
                    targetCount = target,
                    targetVariant = (type == GoalType.CollectColor) ? rng.Next(0, 5) : 0,
                };
            }

            return goals;
        }

        private GoalType GetRandomGoalType(System.Random rng, DifficultyBand band, int levelId)
        {
            if (band == DifficultyBand.Tutorial)
            {
                return rng.Next(0, 2) == 0 ? GoalType.CollectColor : GoalType.ScoreTarget;
            }

            var available = new System.Collections.Generic.List<GoalType>
            {
                GoalType.CollectColor,
                GoalType.ScoreTarget,
            };

            if (levelId >= 3)
                available.Add(GoalType.DestroyObstacle);

            if (band >= DifficultyBand.Onboarding)
                available.Add(GoalType.UseSpecialPiece);

            return available[rng.Next(0, available.Count)];
        }

        private ObstaclePlacement[] GenerateObstacles(System.Random rng, DifficultyBand band, int levelId)
        {
            var availableTypes = GrammarRules.GetAvailableObstacles(levelId);

            if (availableTypes.Length == 0)
                return new ObstaclePlacement[0];

            int typeCount = Mathf.Min(rng.Next(1, 3), availableTypes.Length);
            var placements = new ObstaclePlacement[typeCount];

            var (obsMin, obsMax) = GrammarRules.GetObstacleCountRange(band);
            int totalBudget = rng.Next(obsMin, obsMax + 1);

            for (int i = 0; i < typeCount; i++)
            {
                int countForThis = (i == typeCount - 1) ? totalBudget : rng.Next(1, totalBudget);
                totalBudget -= countForThis;
                if (totalBudget < 0) totalBudget = 0;

                placements[i] = new ObstaclePlacement
                {
                    obstacleType = availableTypes[rng.Next(0, availableTypes.Length)],
                    count = Mathf.Max(1, countForThis),
                    positions = new Vector2Int[0],
                };
            }

            return placements;
        }

        private SpecialPieceWeight[] GenerateSpecialPieceWeights(System.Random rng, DifficultyBand band)
        {
            return new SpecialPieceWeight[]
            {
                new SpecialPieceWeight { pieceType = SpecialPieceType.RocketHorizontal, weight = 1f },
                new SpecialPieceWeight { pieceType = SpecialPieceType.RocketVertical,   weight = 1f },
                new SpecialPieceWeight { pieceType = SpecialPieceType.Bomb,
                    weight = band >= DifficultyBand.Onboarding ? 1f : 0f },
                new SpecialPieceWeight { pieceType = SpecialPieceType.TNT,
                    weight = band >= DifficultyBand.CoreCasual ? 1f : 0f },
                new SpecialPieceWeight { pieceType = SpecialPieceType.Rainbow,
                    weight = band >= DifficultyBand.MidChallenge ? 0.8f : 0f },
            };
        }

        private void CreateFolderRecursive(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
