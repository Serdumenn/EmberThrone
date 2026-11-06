using UnityEngine;
using EmberThrone.Save;
using EmberThrone.Utils;
using EmberThrone.Firebase;

namespace EmberThrone.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper _instance;

        private static void Bootstrap()
        {
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
            var saveManager = new SaveManager();
            ServiceLocator.Register<ISaveManager>(saveManager);

            var poolManager = bootstrapperGo.AddComponent<PoolManager>();
            ServiceLocator.Register<IPoolManager>(poolManager);

            var firebaseStub = new FirebaseStubService();
            ServiceLocator.Register<IFirebaseService>(firebaseStub);
            ServiceLocator.Register<IAnalyticsManager>(firebaseStub);
            ServiceLocator.Register<IRemoteConfigManager>(firebaseStub);
            firebaseStub.Initialize();

            Debug.Log("[GameBootstrapper] All services registered.");
        }

        private void OnApplicationQuit()
        {
            if (ServiceLocator.TryGet<ISaveManager>(out var saveManager))
            {
                (saveManager as SaveManager)?.Dispose();
            }

            EventBus.Clear();
            ServiceLocator.Clear();
            Debug.Log("[GameBootstrapper] Application quit — all systems cleaned up.");
        }
    }
}
