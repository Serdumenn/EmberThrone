namespace EmberThrone.Firebase
{
    public interface IRemoteConfigManager
    {
        bool IsFetched { get; }

        void FetchAsync();

        int GetInt(string key, int defaultValue = 0);

        float GetFloat(string key, float defaultValue = 0f);

        string GetString(string key, string defaultValue = "");

        bool GetBool(string key, bool defaultValue = false);
    }
}
