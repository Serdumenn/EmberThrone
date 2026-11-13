using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class GameApp : MonoBehaviour
    {
        public const string BootstrapScene = "Bootstrap";

        public static bool ShowLoading = true;
        const long ArriveDelayMs = 350;
        public static bool ShowStories = true;
        public static bool StreakBoosts = true;
        public static bool WinFinale = true;
        public static bool OfferStarter = true;

        public static GameApp Instance { get; private set; }

        public Camera Camera { get; private set; }
        public CameraShake Shake { get; private set; }
        public UiRoot Ui { get; private set; }
        public AdService Ads { get; private set; }
        public PurchaseService Purchases { get; private set; }
        public GameController Game { get; private set; }
        public LevelSelectScreen LevelSelect { get; private set; }
        public LevelStartPopup LevelStart { get; private set; }
        public Popup Modal { get; private set; }
        public RewardPopup Rewards { get; private set; }
        public LoadingScreen Loading { get; private set; }
        public StoryScene Story { get; private set; }
        public RewardFlyers Flyers { get; private set; }
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
            Purchases = gameObject.AddComponent<PurchaseService>();
            Purchases.Initialize(Meta.Rules.Store.Products, GrantPurchase);
            Camera = BuildCamera();
            Shake = Camera.gameObject.AddComponent<CameraShake>();
            Shake.SetRest(Camera.transform.position);
            Ui = UiRoot.Create(transform);
            Flyers = RewardFlyers.Create(Ui.Popups);
            _levels = LevelProgress.LoadLevels();
            SeedRestore();
            ShowLevelSelect();
            if (ShowLoading) ShowLoadingScreen();
        }

        public void ShowLoadingScreen()
        {
            Loading?.Close();
            LoadingScreen screen = null;
            screen = LoadingScreen.Show(Ui.Popups, transform, () =>
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
            if (Story != null)
            {
                Story.Close();
                return;
            }
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
            OfferStarterOnce();
        }

        void OfferStarterOnce()
        {
            var economy = Meta.Economy;
            if (!OfferStarter || Modal != null || economy.Profile.StarterShown || !economy.StarterOffered(LevelSelect.CurrentIndex)) return;
            economy.Profile.StarterShown = true;
            Meta.Save();
            OpenStarter();
        }

        public void OpenStarter() => OpenModal(closed => StarterPopup.Show(Ui.Popups, Purchases, closed));

        bool GrantPurchase(string productId, string transaction)
        {
            if (!Meta.Economy.GrantPurchase(productId, transaction, Meta.Now, out var granted)) return false;
            Meta.Save();
            if (granted.Length > 0) ShowRewards("Teşekkürler!", granted, false);
            return true;
        }

        public void OpenPass() => OpenModal(closed => PassPopup.Show(Ui.Popups, Purchases, ShowRewards, closed));

        public void ShowLevelStart(int index)
        {
            if (LevelStart != null || Modal != null || Story != null || !HasLevel(index)) return;
            if (Meta.Economy.Lives <= 0)
            {
                OpenLives();
                return;
            }
            int chapter = LevelMap.ChapterStarting(index);
            if (ShowStories && StoryScene.Available(chapter) && Meta.Economy.MarkSceneSeen(chapter))
            {
                Meta.Save();
                PlayStory(chapter, () => ShowLevelStart(index));
                return;
            }
            LevelStart = LevelStartPopup.Show(Ui.Popups, _levels[index], () => StartLevel(index), () => LevelStart = null);
        }

        public void PlayStory(int chapter, Action done = null)
        {
            if (Story != null || chapter < 0 || chapter >= LevelMap.Chapters.Length) return;
            StoryScene scene = null;
            scene = StoryScene.Show(Ui.Popups, transform, chapter, finished =>
            {
                if (Story == scene) Story = null;
                if (finished) done?.Invoke();
            });
            Story = scene;
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

        public void OpenShop() => OpenModal(closed => ShopPopup.Show(Ui.Popups, Ads, Purchases, ShowRewards, closed));

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

        public void OpenContest(ContestKind kind) => OpenModal(closed => ContestPopup.Show(Ui.Popups, kind, ShowRewards, closed));

        public void OpenLadder() => OpenModal(closed => LadderPopup.Show(Ui.Popups, ShowRewards, closed));

        public void OpenEvent()
        {
            if (Meta.Event.Current(Meta.Now) == null) return;
            OpenModal(closed => EventPopup.Show(Ui.Popups, ShowRewards, closed));
        }

        void SeedRestore()
        {
            var levels = new int[LevelMap.Chapters.Length];
            var stars = new int[LevelMap.Chapters.Length];
            for (int i = 0; i < _levels.Count; i++)
            {
                int best = LevelProgress.GetStars(_levels[i].Id);
                if (best <= 0) continue;
                int chapter = LevelMap.ChapterOf(i);
                levels[chapter]++;
                stars[chapter] += best;
            }
            if (Meta.Restore.Seed(levels, stars)) Meta.Save();
        }

        public void OpenRestore()
        {
            if (LevelSelect == null || LevelSelect.Diorama.Busy) return;
            int district = LevelSelect.RestoreFocus;
            OpenModal(closed => RestorePopup.Show(Ui.Popups, district, Repair, closed));
        }

        public void Repair(int district, int task)
        {
            var rewards = Meta.Restore.Claim(district, task);
            if (rewards == null) return;
            bool restored = Meta.Restore.Restored(district);
            Meta.Save();
            var select = LevelSelect;
            if (select == null)
            {
                ShowRewards(LevelMap.Chapters[district].Title, rewards, false);
                return;
            }
            select.ShowMap(false);
            string title = restored ? LevelMap.Chapters[district].Title + " onarıldı!" : Meta.RestoreRules.Areas[district].Tasks[task].Name;
            select.Diorama.Thaw(task, restored, () => ShowRewards(title, rewards, restored));
        }

        public void ShowRaceResult()
        {
            var race = Meta.Race;
            if (!race.ResultReady) return;
            int rank = race.ResultRank;
            var prize = race.ClaimResult();
            Meta.Save();
            ShowRewards(rank + ". oldun!", prize, false);
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
            Hold(rewards);
            RewardPopup popup = null;
            popup = RewardPopup.Show(Ui.Popups, title, rewards, chest, () =>
            {
                if (Rewards == popup) Rewards = null;
                for (int i = 0; i < popup.Rewards.Length; i++) FlyHome(popup.Rewards[i], popup.OriginOf(i));
            });
            Rewards = popup;
        }

        void Hold(Reward[] rewards)
        {
            var top = LevelSelect?.Top;
            if (top == null) return;
            foreach (var reward in rewards) top.Hold(reward.Kind, reward.Amount);
        }

        VisualElement HomeOf(Reward reward)
        {
            if (LevelSelect == null) return null;
            if (reward.Kind == RewardKind.Pack) return LevelSelect.Tabs.CardsTab;
            return LevelSelect.Top.TargetOf(reward.Kind);
        }

        void FlyHome(Reward reward, Vector2 fromPanel)
        {
            var top = LevelSelect?.Top;
            var target = HomeOf(reward);
            if (top == null || target == null) return;
            int landed = 0;
            Flyers.Fly(reward, fromPanel, target, amount =>
            {
                Sound.Play(Sfx.Collect, Mathf.Min(landed++, 8));
                if (reward.Kind == RewardKind.Pack) Popup.Bump(target, 0.12f, 0.14f);
                else top.Release(reward.Kind, amount);
            });
        }

        public void ShowLevelSelect(Reward[] arriving)
        {
            ShowLevelSelect();
            if (arriving == null || arriving.Length == 0) return;
            Hold(arriving);
            var select = LevelSelect;
            select.Root.schedule.Execute(() =>
            {
                if (LevelSelect != select) return;
                var from = Ui.Content.worldBound.center;
                foreach (var reward in arriving) FlyHome(reward, from);
            }).ExecuteLater(ArriveDelayMs);
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
            Story?.Close(false);
            Story = null;
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
