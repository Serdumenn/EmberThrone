using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmberThrone.Game.Tests
{
    static class StarsBackup
    {
        static readonly int[] TestLevelIds = { 0, 99, 4242, 4243 };

        static IEnumerable<int> KnownIds() => LevelProgress.LoadLevels().Select(l => l.Id).Concat(TestLevelIds);

        public static Dictionary<int, int> Snapshot()
        {
            var stars = new Dictionary<int, int>();
            foreach (int id in KnownIds())
                if (PlayerPrefs.HasKey(LevelProgress.StarsKey + id)) stars[id] = LevelProgress.GetStars(id);
            return stars;
        }

        public static void Restore(Dictionary<int, int> stars)
        {
            foreach (int id in KnownIds()) PlayerPrefs.DeleteKey(LevelProgress.StarsKey + id);
            foreach (var pair in stars) PlayerPrefs.SetInt(LevelProgress.StarsKey + pair.Key, pair.Value);
            PlayerPrefs.Save();
        }

        static readonly string[] SettingKeys = { Sound.EnabledKey, Haptics.EnabledKey, CameraShake.EnabledKey };

        public static Dictionary<string, int> SnapshotSettings()
        {
            var values = new Dictionary<string, int>();
            foreach (var key in SettingKeys)
                if (PlayerPrefs.HasKey(key)) values[key] = PlayerPrefs.GetInt(key);
            return values;
        }

        public static void RestoreSettings(Dictionary<string, int> values)
        {
            foreach (var key in SettingKeys) PlayerPrefs.DeleteKey(key);
            foreach (var pair in values) PlayerPrefs.SetInt(pair.Key, pair.Value);
            PlayerPrefs.Save();
        }

        public static string SnapshotProfile() => PlayerPrefs.HasKey(Meta.ProfileKey) ? PlayerPrefs.GetString(Meta.ProfileKey) : null;

        public static void FreshProfile()
        {
            PlayerPrefs.DeleteKey(Meta.ProfileKey);
            Meta.Clock = Meta.WallClock;
            Meta.Forget();
            foreach (var type in BoosterInventory.All) BoosterInventory.Set(type, Meta.Rules.StartBoosters);
        }

        public static void RestoreProfile(string profile)
        {
            if (profile == null) PlayerPrefs.DeleteKey(Meta.ProfileKey);
            else PlayerPrefs.SetString(Meta.ProfileKey, profile);
            PlayerPrefs.Save();
            Meta.Clock = Meta.WallClock;
            Meta.Forget();
        }
    }
}
