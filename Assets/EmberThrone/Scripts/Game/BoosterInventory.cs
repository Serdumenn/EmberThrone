using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class BoosterInventory
    {
        public static readonly BoosterType[] All =
        {
            BoosterType.Hammer,
            BoosterType.RowBlast,
            BoosterType.ColumnBlast,
            BoosterType.Shuffle,
            BoosterType.ColorBrush,
        };

        public static int Count(BoosterType type) => Meta.Economy.Boosters(type);

        public static bool Spend(BoosterType type)
        {
            if (!Meta.Economy.SpendBooster(type)) return false;
            Meta.Save();
            return true;
        }

        public static void Set(BoosterType type, int count)
        {
            Meta.Economy.Profile.Boosters[(int)type] = Mathf.Clamp(count, 0, Meta.Rules.BoosterCap);
            Meta.Save();
        }

        public static string Slug(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer: return "hammer";
                case BoosterType.RowBlast: return "row";
                case BoosterType.ColumnBlast: return "column";
                case BoosterType.ColorBrush: return "brush";
                default: return "shuffle";
            }
        }

        public static string Name(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer: return "Çekiç";
                case BoosterType.RowBlast: return "Satır";
                case BoosterType.ColumnBlast: return "Sütun";
                case BoosterType.ColorBrush: return "Fırça";
                default: return "Karıştır";
            }
        }
    }
}
