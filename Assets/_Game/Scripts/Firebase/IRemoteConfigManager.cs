// TECH-001 §6.3 — IRemoteConfigManager Interface
// Firebase Remote Config — runtime değer override.
// EconomyConfig değerlerini sunucudan güncellemek için kullanılır.

namespace EmberThrone.Firebase
{
    /// <summary>
    /// Remote Config contract for runtime parameter overrides.
    /// Used to A/B test economy values, difficulty curves, etc.
    /// </summary>
    public interface IRemoteConfigManager
    {
        /// <summary>Has Remote Config been fetched at least once?</summary>
        bool IsFetched { get; }

        /// <summary>Fetch latest config from server.</summary>
        void FetchAsync();

        /// <summary>Get an integer value with fallback.</summary>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>Get a float value with fallback.</summary>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>Get a string value with fallback.</summary>
        string GetString(string key, string defaultValue = "");

        /// <summary>Get a boolean value with fallback.</summary>
        bool GetBool(string key, bool defaultValue = false);
    }
}
