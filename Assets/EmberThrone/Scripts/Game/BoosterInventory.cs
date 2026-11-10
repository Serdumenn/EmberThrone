using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class BoosterInventory
    {
        public const string Key = "et.booster.";

        public static readonly BoosterType[] All =
        {
            BoosterType.Hammer,
            BoosterType.RowBlast,
            BoosterType.ColumnBlast,
            BoosterType.Shuffle,
        };

        public static int Count(BoosterType type, GameRules rules) => PlayerPrefs.GetInt(Key + type, rules.StartingBoosters);

        public static bool Spend(BoosterType type, GameRules rules)
        {
            int count = Count(type, rules);
            if (count <= 0) return false;
            PlayerPrefs.SetInt(Key + type, count - 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void Set(BoosterType type, int count)
        {
            PlayerPrefs.SetInt(Key + type, Mathf.Max(0, count));
            PlayerPrefs.Save();
        }
    }
}
