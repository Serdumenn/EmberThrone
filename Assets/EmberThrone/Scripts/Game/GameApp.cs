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

        public static bool ShowLoading = true;

        public static GameApp Instance { get; private set; }

        public Camera Camera { get; private set; }
        public CameraShake Shake { get; private set; }
        public UiRoot Ui { get; private set; }
        public AdService Ads { get; private set; }
        public GameController Game { get; private set; }
        public LevelSelectScreen LevelSelect { get; private set; }
        public LevelStartPopup LevelStart { get; private set; }
        public Popup Modal { get; private set; }
        public RewardPopup Rewards { get; private set; }
        public LoadingScreen Loading { get; private set; }
        public bool ScreenStaysOn { get; private set; }
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
            gameObject.AddComponent<HitStop>();
            Sound.Init(gameObject);
            Ads = gameObject.AddComponent<AdService>();
            Camera = BuildCamera();
            Shake = Camera.gameObject.AddComponent<CameraShake>();
            Shake.SetRest(Camera.transform.position);
            Ui = UiRoot.Create(transform);
            _levels = LevelProgress.LoadLevels();
            ShowLevelSelect();
            if (ShowLoading) ShowLoadingScreen();
        }

        public void ShowLoadingScreen()
        {
            Loading?.Close();
            LoadingScreen screen = null;
            screen = LoadingScreen.Show(Ui.Popups, () =>
            {
                if (Loading == screen) Loading = null;
            });
            Loading = screen;
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
            if (Rewards != null)
            {
                Rewards.Close();
                return;
            }
            if (Modal != null)
            {
                Modal.Close();
                return;
            }
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
            if (Game != null) Game.OnHome();
            else if (LevelSelect != null && LevelSelect.Page != LobbyPage.Home) LevelSelect.ShowMap(false);
            else Application.Quit();
        }

        public bool HasLevel(int index) => index >= 0 && index < _levels.Count;

        public void ShowLevelSelect()
        {
            CloseCurrent();
            LevelSelect = LevelSelectScreen.Create(Ui, _levels, l => LevelProgress.GetStars(l.Id), ShowLevelStart, this);
        }

        public void ShowLevelStart(int index)
        {
            if (LevelStart != null || Modal != null || !HasLevel(index)) return;
            if (Meta.Economy.Lives <= 0)
            {
                OpenLives();
                return;
            }
            LevelStart = LevelStartPopup.Show(Ui.Popups, _levels[index], () => StartLevel(index), () => LevelStart = null);
        }

        public void StartLevel(int index)
        {
            if (Meta.Economy.Lives <= 0)
            {
                ShowLevelSelect();
                OpenLives();
                return;
            }
            StartLevel(_levels[index], index, Environment.TickCount);
        }

        public void OpenShop() => OpenModal(closed => ShopPopup.Show(Ui.Popups, Ads, ShowRewards, closed));

        public void OpenLives() => OpenModal(closed => LivesPopup.Show(Ui.Popups, Ads, closed));

        public void OpenDaily() => OpenModal(closed => DailyPopup.Show(Ui.Popups, ShowRewards, closed));

        public void OpenQuests() => OpenModal(closed => QuestsPopup.Show(Ui.Popups, closed));

        public void OpenCardSet(int set) => OpenModal(closed => SetPopup.Show(Ui.Popups, set, ShowRewards, closed));

        public void OpenPack()
        {
            if (Modal != null) return;
            var draws = Meta.Cards.OpenPack();
            if (draws == null) return;
            Meta.Save();
            OpenModal(closed => PackPopup.Show(Ui.Popups, draws, Meta.Cards.Packs > 0, OpenPack, closed));
        }

        public void OpenInfo(string title, string message) => OpenModal(closed => InfoPopup.Show(Ui.Popups, title, message, closed));

        public void Confirm(string title, string message, string yes, string no, Action confirmed)
        {
            OpenModal(closed => ConfirmPopup.Show(Ui.Popups, title, message, yes, no, confirmed, closed));
        }

        void OpenModal(Func<Action, Popup> open)
        {
            if (Modal != null) return;
            Popup popup = null;
            popup = open(() =>
            {
                if (Modal == popup) Modal = null;
            });
            if (popup.IsOpen) Modal = popup;
        }

        public void ShowRewards(string title, Reward[] rewards, bool chest)
        {
            Rewards?.Close();
            RewardPopup popup = null;
            popup = RewardPopup.Show(Ui.Popups, title, rewards, chest, () =>
            {
                if (Rewards == popup) Rewards = null;
            });
            Rewards = popup;
        }

        public void StartLevel(LevelDefinition level, int index, int seed)
        {
            CloseCurrent();
            KeepScreenOn(true);
            Game = GameController.Create(this, transform, level, index, seed);
        }

        void KeepScreenOn(bool on)
        {
            ScreenStaysOn = on;
            Screen.sleepTimeout = on ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

        void CloseCurrent()
        {
            KeepScreenOn(false);
            Tweens.KillAll();
            if (Game != null)
            {
                Destroy(Game.gameObject);
                Game = null;
            }
            LevelStart?.Close();
            Rewards?.Close();
            Modal?.Close();
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
