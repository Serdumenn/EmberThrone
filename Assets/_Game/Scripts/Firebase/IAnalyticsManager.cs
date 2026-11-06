using System.Collections.Generic;

namespace EmberThrone.Firebase
{
    public interface IAnalyticsManager
    {
        void LogEvent(string eventName);

        void LogEvent(string eventName, Dictionary<string, string> parameters);

        void LogEvent(string eventName, string paramKey, string paramValue);

        void LogEvent(string eventName, string paramKey, float paramValue);

        void SetUserProperty(string name, string value);
    }
}
