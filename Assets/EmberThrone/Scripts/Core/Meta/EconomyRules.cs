namespace EmberThrone.Core
{
    public enum RewardKind
    {
        Coins,
        Gems,
        Life,
        Booster,
        Pack,
    }

    public readonly struct Reward
    {
        public readonly RewardKind Kind;
        public readonly int Amount;
        public readonly BoosterType Booster;

        public Reward(RewardKind kind, int amount, BoosterType booster = BoosterType.Hammer)
        {
            Kind = kind;
            Amount = amount;
            Booster = booster;
        }

        public static Reward Coins(int amount) => new Reward(RewardKind.Coins, amount);
        public static Reward Gems(int amount) => new Reward(RewardKind.Gems, amount);
        public static Reward Of(BoosterType type, int amount = 1) => new Reward(RewardKind.Booster, amount, type);
        public static Reward Pack(int amount = 1) => new Reward(RewardKind.Pack, amount);

        public override string ToString() => Kind == RewardKind.Booster ? Amount + "x" + Booster : Amount + " " + Kind;
    }

    public sealed class ChestDrop
    {
        public readonly int Weight;
        public readonly RewardKind Kind;
        public readonly BoosterType Booster;
        public readonly int Min;
        public readonly int Max;

        public ChestDrop(int weight, RewardKind kind, int min, int max, BoosterType booster = BoosterType.Hammer)
        {
            Weight = weight;
            Kind = kind;
            Min = min;
            Max = max;
            Booster = booster;
        }
    }

    public enum QuestKind
    {
        WinLevels,
        EarnStars,
        CollectGems,
        FireSpecials,
        BreakObstacles,
    }

    public sealed class QuestTemplate
    {
        public readonly QuestKind Kind;
        public readonly int Target;
        public readonly int Coins;

        public QuestTemplate(QuestKind kind, int target, int coins)
        {
            Kind = kind;
            Target = target;
            Coins = coins;
        }
    }

    public sealed class EconomyRules
    {
        public const int SecondsPerDay = 86400;

        public int StartCoins = 500;
        public int StartGems = 50;
        public int CoinCap = 15000;
        public int GemCap = 9999;

        public int WinCoins = 100;
        public int TwoStarBonus = 40;
        public int ThreeStarBonus = 80;
        public int ReplayCoins = 25;

        public int MaxLives = 5;
        public int LifeSeconds = 1800;
        public int LifeCoinPrice = 300;
        public int RefillGemPrice = 50;
        public int AdLivesPerDay = 3;

        public int ExtraMovesPrice = 900;
        public int ExtraMovesGrowthPercent = 150;

        public int AdCoins = 60;
        public int AdCoinsPerDay = 5;

        public int StartPacks = 2;
        public int PacksOnFirstWin = 1;
        public int PacksOnAllQuests = 1;

        public int StartBoosters = 3;
        public int BoosterCap = 20;
        public int[] BoosterPrices = { 900, 1200, 1200, 900 };

        public int ChestPrice = 400;
        public ChestDrop[] ChestDrops =
        {
            new ChestDrop(42, RewardKind.Coins, 150, 400),
            new ChestDrop(10, RewardKind.Pack, 1, 1),
            new ChestDrop(15, RewardKind.Booster, 1, 2, BoosterType.Hammer),
            new ChestDrop(10, RewardKind.Booster, 1, 1, BoosterType.RowBlast),
            new ChestDrop(10, RewardKind.Booster, 1, 1, BoosterType.ColumnBlast),
            new ChestDrop(8, RewardKind.Booster, 1, 2, BoosterType.Shuffle),
            new ChestDrop(5, RewardKind.Gems, 5, 10),
        };

        public Reward[][] DailyGifts =
        {
            new[] { Reward.Coins(150) },
            new[] { Reward.Coins(200), Reward.Of(BoosterType.Hammer) },
            new[] { Reward.Coins(250), Reward.Pack() },
            new[] { Reward.Coins(300), Reward.Of(BoosterType.Shuffle) },
            new[] { Reward.Coins(350), Reward.Of(BoosterType.RowBlast) },
            new[] { Reward.Coins(400), Reward.Pack() },
            new[] { Reward.Coins(500), Reward.Gems(10), Reward.Of(BoosterType.ColumnBlast) },
        };

        public int QuestsPerDay = 3;
        public int AllQuestsGems = 3;
        public QuestTemplate[] Quests =
        {
            new QuestTemplate(QuestKind.WinLevels, 3, 200),
            new QuestTemplate(QuestKind.WinLevels, 5, 300),
            new QuestTemplate(QuestKind.EarnStars, 6, 200),
            new QuestTemplate(QuestKind.EarnStars, 10, 300),
            new QuestTemplate(QuestKind.CollectGems, 120, 150),
            new QuestTemplate(QuestKind.CollectGems, 250, 250),
            new QuestTemplate(QuestKind.FireSpecials, 8, 200),
            new QuestTemplate(QuestKind.FireSpecials, 15, 300),
            new QuestTemplate(QuestKind.BreakObstacles, 20, 200),
        };

        public int WinReward(int stars) => WinCoins + (stars >= 2 ? TwoStarBonus : 0) + (stars >= 3 ? ThreeStarBonus : 0);

        public int ExtraMovesCost(int taken)
        {
            long price = ExtraMovesPrice;
            for (int i = 0; i < taken; i++) price = price * ExtraMovesGrowthPercent / 100;
            return (int)price;
        }

        public int BoosterPrice(BoosterType type) => BoosterPrices[(int)type];

        public static int DayOf(long now) => (int)(now / SecondsPerDay);
    }
}
