using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EmberThrone.Game
{
    public sealed class GameApp : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";

        public static GameApp Instance { get; private set; }

        public Camera Camera { get; private set; }
        public CameraShake Shake { get; private set; }
        public UiRoot Ui { get; private set; }
        public AdService Ads { get; private set; }
        public GameController Game { get; private set; }
        public LevelSelectScreen LevelSelect { get; private set; }
        public LevelStartPopup LevelStart { get; private set; }
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
            Sound.Init(gameObject);
            Ads = gameObject.AddComponent<AdService>();
            Camera = BuildCamera();
            Shake = Camera.gameObject.AddComponent<CameraShake>();
            Shake.SetRest(Camera.transform.position);
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
            go.AddComponent<AudioListener>();
            return cam;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (LevelStart != null)
            {
                LevelStart.Close();
                return;
            }
            if (LevelSelect != null && LevelSelect.Settings != null)
            {
                LevelSelect.Settings.Close();
                return;
            }
            if (Game != null) ShowLevelSelect();
            else Application.Quit();
        }

        public bool HasLevel(int index) => index >= 0 && index < _levels.Count;

        public void ShowLevelSelect()
        {
            CloseCurrent();
            LevelSelect = LevelSelectScreen.Create(Ui, _levels, l => LevelProgress.GetStars(l.Id), ShowLevelStart);
        }

        public void ShowLevelStart(int index)
        {
            if (LevelStart != null || !HasLevel(index)) return;
            LevelStart = LevelStartPopup.Show(Ui.Popups, _levels[index], () => StartLevel(index), () => LevelStart = null);
        }

        public void StartLevel(int index) => StartLevel(_levels[index], index, Environment.TickCount);

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
            LevelStart?.Close();
            LevelSelect?.Settings?.Close();
            LevelSelect = null;
            Ui.ClearContent();
            Shake.SetRest(new Vector3(0f, 0f, -10f));
            Shake.Stop();
        }
    }

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

        public static void SaveStars(int levelId, int stars)
        {
            if (stars <= GetStars(levelId)) return;
            PlayerPrefs.SetInt(StarsKey + levelId, stars);
            PlayerPrefs.Save();
        }
    }
}
