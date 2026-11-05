// TECH-001 §3.3 — Game Bootstrapper
// Uygulama başlangıcında tüm servisleri register eder.
// Boot sırası: ServiceLocator → EventBus → SaveManager → PoolManager → Firebase

using UnityEngine;
using EmberThrone.Save;
using EmberThrone.Utils;
using EmberThrone.Firebase;

namespace EmberThrone.Core
{
    /// <summary>
    /// Application entry point. Creates and registers all services.
    /// Persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;

        // Not started automatically: the game runs from EmberThrone.Game.GameApp and does not use these
        // services yet. Call this from there when they are wired in (Phase 3).
        private static void Bootstrap()
        {
            // Ensure only one bootstrapper exists
            if (_instance != null) return;

            var go = new GameObject("[GameBootstrapper]");
            _instance = go.AddComponent<GameBootstrapper>();
            DontDestroyOnLoad(go);

            Debug.Log("[GameBootstrapper] Initializing EmberThrone...");
            InitializeServices(go);
            Debug.Log("[GameBootstrapper] Boot complete.");
        }

        private static void InitializeServices(GameObject bootstrapperGo)
        {
            // ─── Phase 1: Core Systems ───
            // ServiceLocator is static — no registration needed.
            // EventBus is static — no registration needed.

            // ─── Phase 2: SaveManager (TECH-001 §7) ───
            var saveManager = new SaveManager();
            ServiceLocator.Register<ISaveManager>(saveManager);

            // ─── Phase 3: PoolManager (TECH-001 §9) ───
            var poolManager = bootstrapperGo.AddComponent<PoolManager>();
            ServiceLocator.Register<IPoolManager>(poolManager);

            // ─── Phase 4: Firebase Stubs (TECH-001 §6) ───
            var firebaseStub = new FirebaseStubService();
            ServiceLocator.Register<IFirebaseService>(firebaseStub);
            ServiceLocator.Register<IAnalyticsManager>(firebaseStub);
            ServiceLocator.Register<IRemoteConfigManager>(firebaseStub);
            firebaseStub.Initialize();

            // ─── Future Phases ───
            // ServiceLocator.Register<IAudioManager>(new AudioManager());
            // ServiceLocator.Register<IEconomyManager>(new EconomyManager());

            Debug.Log("[GameBootstrapper] All services registered.");
        }

        private void OnApplicationQuit()
        {
            // Dispose SaveManager (final save + unsubscribe)
            if (ServiceLocator.TryGet<ISaveManager>(out var saveManager))
            {
                (saveManager as SaveManager)?.Dispose();
            }

            // Clean shutdown
            EventBus.Clear();
            ServiceLocator.Clear();
            Debug.Log("[GameBootstrapper] Application quit — all systems cleaned up.");
        }
    }
}
