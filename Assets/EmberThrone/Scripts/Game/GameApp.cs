using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EmberThrone.Game
{
    /// <summary>
    /// Entry point. The Bootstrap scene is empty on purpose: this class builds the camera, the UI and
    /// every screen from code, so there are no prefabs or scene references to break.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";

        public static GameApp Instance { get; private set; }

        public Camera Camera { get; private set; }
        public UiRoot Ui { get; private set; }
        public GameController Game { get; private set; }
        public LevelSelectScreen LevelSelect { get; private set; }
        public IReadOnlyList<LevelDefinition> Levels => _levels;

        List<LevelDefinition> _levels;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name == BootstrapScene) Create();
        }

        public static GameApp Create()
        {
            return new GameObject("GameApp").AddComponent<GameApp>();
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Art.Preload();
            gameObject.AddComponent<Tweens>();
            Camera = BuildCamera();
            Ui = UiRoot.Create(transform);
            _levels = LevelProgress.LoadLevels();
            ShowLevelSelect();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Camera BuildCamera()
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            go.tag = "MainCamera";
            return cam;
        }

        void Update()
        {
            // Android's back button arrives as Escape.
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (Game != null) ShowLevelSelect();
            else Application.Quit();
        }

        public bool HasLevel(int index) => index >= 0 && index < _levels.Count;

        public void ShowLevelSelect()
        {
            CloseCurrent();
            LevelSelect = LevelSelectScreen.Create(Ui.Content, _levels, l => LevelProgress.GetStars(l.Id), StartLevel);
        }

        public void StartLevel(int index) => StartLevel(_levels[index], index, Environment.TickCount);

        /// <summary>Starts any level definition. The seed makes the game reproducible.</summary>
        public void StartLevel(LevelDefinition level, int index, int seed)
        {
            CloseCurrent();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Game = GameController.Create(this, transform, level, index, seed);
        }

        void CloseCurrent()
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Tweens.KillAll();
            if (Game != null)
            {
                Destroy(Game.gameObject);
                Game = null;
            }
            LevelSelect = null;
            Ui.ClearContent();
            Camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }

    /// <summary>Levels from Resources/Levels, and the player's stars (local for now; cloud save is Phase 4).</summary>
    public static class LevelProgress
    {
        const string LevelsFolder = "Levels";
        public const string StarsKey = "et.stars.";

        public static List<LevelDefinition> LoadLevels()
        {
            var levels = new List<LevelDefinition>();
            foreach (var asset in Resources.LoadAll<TextAsset>(LevelsFolder))
            {
                try
                {
                    levels.Add(LevelParser.FromJson(asset.text));
                }
                catch (LevelFormatException e)
                {
                    Debug.LogError("[EmberThrone] Bad level file " + asset.name + ": " + e.Message);
                }
            }
            levels.Sort((a, b) => a.Id.CompareTo(b.Id));
            return levels;
        }

        public static int GetStars(int levelId) => PlayerPrefs.GetInt(StarsKey + levelId, 0);

        /// <summary>Keeps the best result.</summary>
        public static void SaveStars(int levelId, int stars)
        {
            if (stars <= GetStars(levelId)) return;
            PlayerPrefs.SetInt(StarsKey + levelId, stars);
            PlayerPrefs.Save();
        }
    }
}
