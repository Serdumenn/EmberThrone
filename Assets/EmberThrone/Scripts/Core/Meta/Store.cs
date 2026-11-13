using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum ProductKind
    {
        Gems,
        Coins,
        Starter,
        Boosters,
        Piggy,
        Pass,
    }

    public sealed class StoreProduct
    {
        public readonly string Id;
        public readonly ProductKind Kind;
        public readonly string Title;
        public readonly string Price;
        public readonly Reward[] Rewards;

        public StoreProduct(string id, ProductKind kind, string title, string price, params Reward[] rewards)
        {
            Id = id;
            Kind = kind;
            Title = title;
            Price = price;
            Rewards = rewards ?? new Reward[0];
        }
    }

    public readonly struct PassStep
    {
        public readonly Reward Free;
        public readonly Reward Premium;

        public PassStep(Reward free, Reward premium)
        {
            Free = free;
            Premium = premium;
        }
    }

    public sealed class StoreRules
    {
        public const string PiggyId = "piggy_bank";
        public const string PassId = "ember_pass";
        public const string StarterId = "starter_pack";

        public StoreProduct[] Products =
        {
            new StoreProduct(StarterId, ProductKind.Starter, "Başlangıç paketi", "$1.99",
                Reward.Coins(500), Reward.Gems(50), Reward.Of(BoosterType.Hammer, 5), Reward.Of(BoosterType.RowBlast, 3), Reward.Of(BoosterType.ColumnBlast, 3)),
            new StoreProduct(PassId, ProductKind.Pass, "Kor Yolu", "$4.99"),
            new StoreProduct(PiggyId, ProductKind.Piggy, "Kumbara", "$2.99"),
            new StoreProduct("gems_080", ProductKind.Gems, "80 elmas", "$0.99", Reward.Gems(80)),
            new StoreProduct("gems_200", ProductKind.Gems, "200 elmas", "$1.99", Reward.Gems(200)),
            new StoreProduct("gems_500", ProductKind.Gems, "500 elmas", "$4.99", Reward.Gems(500)),
            new StoreProduct("gems_1200", ProductKind.Gems, "1200 elmas", "$9.99", Reward.Gems(1200)),
            new StoreProduct("coins_1000", ProductKind.Coins, "1000 altın", "$1.99", Reward.Coins(1000)),
            new StoreProduct("coins_3000", ProductKind.Coins, "3000 altın", "$4.99", Reward.Coins(3000)),
            new StoreProduct("booster_pack", ProductKind.Boosters, "Booster paketi", "$2.99",
                Reward.Of(BoosterType.Hammer, 5), Reward.Of(BoosterType.RowBlast, 3), Reward.Of(BoosterType.ColumnBlast, 3), Reward.Of(BoosterType.ColorBrush, 2)),
        };

        public int StarterFromLevel = 5;
        public int StarterUntilLevel = 30;

        public int PiggyPerWin = 50;
        public int PiggyMin = 1000;
        public int PiggyCap = 3000;

        public int PassMaxLives = 8;
        public int PassPointsPerWin = 1;
        public PassStep[] PassTrack = BuildTrack(30);

        public int RememberedPurchases = 64;

        public StoreProduct Find(string id)
        {
            foreach (var p in Products)
                if (p.Id == id) return p;
            return null;
        }

        static PassStep[] BuildTrack(int steps)
        {
            var boosters = new[] { BoosterType.Hammer, BoosterType.RowBlast, BoosterType.ColumnBlast, BoosterType.Shuffle, BoosterType.ColorBrush };
            var track = new PassStep[steps];
            for (int i = 0; i < steps; i++)
            {
                int step = i + 1;
                var free = step % 5 == 0 ? Reward.Of(boosters[(step / 5 - 1) % boosters.Length]) : Reward.Coins(30 + 10 * (step % 3));
                Reward premium;
                if (step % 10 == 0) premium = Reward.Gems(25);
                else if (step % 5 == 0) premium = Reward.Pack(2);
                else if (step % 3 == 0) premium = Reward.Of(boosters[(step / 3) % boosters.Length], 2);
                else premium = Reward.Coins(120);
                track[i] = new PassStep(free, premium);
            }
            return track;
        }
    }
}
