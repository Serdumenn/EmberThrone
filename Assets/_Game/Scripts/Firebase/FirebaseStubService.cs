// TECH-001 §6 — Firebase Stub Service
// Firebase SDK yüklenmeden çalışan stub implementasyonlar.
// Tüm çağrıları loglar, varsayılan değerler döner.
// SDK yüklendiğinde bu sınıf concrete implementasyonla değiştirilecek.

using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Firebase
{
    /// <summary>
    /// Stub implementation of all Firebase interfaces.
    /// Logs all calls for debugging. Returns safe defaults.
    /// Replace with real implementations when Firebase SDK is installed.
    /// </summary>
    public class FirebaseStubService : IFirebaseService, IAnalyticsManager, IRemoteConfigManager
    {
        private const string TAG = "[FirebaseStub]";

        // ─── IFirebaseService ────────────────────────

        public bool IsInitialized => true; // Stub always "ready"
        public bool IsAuthenticated => false;
        public string UserId => "stub_user_offline";

        public void Initialize()
        {
            Debug.Log($"{TAG} Initialize() — stub mode (no Firebase SDK).");
        }

        // ─── IAnalyticsManager ───────────────────────

        public void LogEvent(string eventName)
        {
            Debug.Log($"{TAG} LogEvent: {eventName}");
        }

        public void LogEvent(string eventName, Dictionary<string, string> parameters)
        {
            string paramStr = "";
            foreach (var kvp in parameters)
                paramStr += $"{kvp.Key}={kvp.Value}, ";

            Debug.Log($"{TAG} LogEvent: {eventName} [{paramStr}]");
        }

        public void LogEvent(string eventName, string paramKey, string paramValue)
        {
            Debug.Log($"{TAG} LogEvent: {eventName} [{paramKey}={paramValue}]");
        }

        public void LogEvent(string eventName, string paramKey, float paramValue)
        {
            Debug.Log($"{TAG} LogEvent: {eventName} [{paramKey}={paramValue}]");
        }

        public void SetUserProperty(string name, string value)
        {
            Debug.Log($"{TAG} SetUserProperty: {name}={value}");
        }

        // ─── IRemoteConfigManager ────────────────────

        public bool IsFetched => false; // Stub never "fetched"

        public void FetchAsync()
        {
            Debug.Log($"{TAG} FetchAsync() — stub mode, using local defaults.");
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            Debug.Log($"{TAG} GetInt({key}) → {defaultValue} (default)");
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            Debug.Log($"{TAG} GetFloat({key}) → {defaultValue} (default)");
            return defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            Debug.Log($"{TAG} GetString({key}) → \"{defaultValue}\" (default)");
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            Debug.Log($"{TAG} GetBool({key}) → {defaultValue} (default)");
            return defaultValue;
        }
    }
}
