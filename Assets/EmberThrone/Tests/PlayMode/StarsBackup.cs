using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EmberThrone.Game.Tests
{
    /// <summary>Keeps the bot's wins out of the player's saved stars.</summary>
    static class StarsBackup
    {
        /// <summary>Ids of the levels tests build by hand.</summary>
        static readonly int[] TestLevelIds = { 0, 99, 4242 };

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
    }
}
