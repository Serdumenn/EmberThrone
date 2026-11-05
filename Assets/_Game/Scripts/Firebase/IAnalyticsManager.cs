// TECH-001 §6.2 — IAnalyticsManager Interface
// Firebase Analytics event logging.

using System.Collections.Generic;

namespace EmberThrone.Firebase
{
    /// <summary>
    /// Analytics event logging contract.
    /// DATA-001 telemetry events are logged through this interface.
    /// </summary>
    public interface IAnalyticsManager
    {
        /// <summary>Log a simple event (no parameters).</summary>
        void LogEvent(string eventName);

        /// <summary>Log an event with string parameters.</summary>
        void LogEvent(string eventName, Dictionary<string, string> parameters);

        /// <summary>Log an event with a single string parameter.</summary>
        void LogEvent(string eventName, string paramKey, string paramValue);

        /// <summary>Log an event with a single numeric parameter.</summary>
        void LogEvent(string eventName, string paramKey, float paramValue);

        /// <summary>Set a user property for segmentation.</summary>
        void SetUserProperty(string name, string value);
    }
}
