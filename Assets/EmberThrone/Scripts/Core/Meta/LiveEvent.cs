using System;

namespace EmberThrone.Core
{
    public enum EventKind
    {
        Ladder,
        Sprint,
    }

    public enum EventMetric
    {
        Wins,
        Stars,
        Obstacles,
        Boosters,
    }

    public sealed class EventTier
    {
        public readonly int Points;
        public readonly Reward[] Rewards;

        public EventTier(int points, params Reward[] rewards)
        {
            Points = points;
            Rewards = rewards;
        }
    }

    public sealed class EventDef
    {
        public readonly string Name;
        public readonly string Emblem;
        public readonly EventKind Kind;
        public readonly int StartDay;
        public readonly int Days;
        public readonly EventMetric Goal;
        public readonly int Target;
        public readonly Reward[] Rewards;
        public readonly EventTier[] Tiers;

        EventDef(string name, string emblem, EventKind kind, int startDay, int days, EventMetric goal, int target, Reward[] rewards, EventTier[] tiers)
        {
            Name = name;
            Emblem = emblem;
            Kind = kind;
            StartDay = startDay;
            Days = days;
            Goal = goal;
            Target = target;
            Rewards = rewards;
            Tiers = tiers;
        }

        public static EventDef Ladder(string name, string emblem, params EventTier[] tiers) =>
            new EventDef(name, emblem, EventKind.Ladder, 1, 0, EventMetric.Wins, 0, new Reward[0], tiers);

        public static EventDef Sprint(string name, string emblem, int startDay, int days, EventMetric goal, int target, params Reward[] rewards) =>
            new EventDef(name, emblem, EventKind.Sprint, startDay, days, goal, target, rewards, new EventTier[0]);
    }

    public sealed class EventRules
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public int OneStarPoints = 10;
        public int TwoStarPoints = 15;
        public int ThreeStarPoints = 25;
        public int HardLevelPoints = 20;
        public int ObstaclePoints = 2;
        public int BoosterPoints = 5;
        public int EarlyBonusPercent = 20;
        public string Token = "kor_komur";

        public EventDef[] Calendar =
        {
            EventDef.Ladder("Kış Ocağı", "kor_mum", Tiers()),
            EventDef.Sprint("Ayaz Baskını", "ayaz_asa", 10, 4, EventMetric.Stars, 40, Reward.Coins(800), Reward.Gems(10), Reward.Of(BoosterType.Hammer, 3)),
            EventDef.Ladder("Bahar Kıvılcımı", "bahce_kovan", Tiers()),
            EventDef.Sprint("Bahçe Şenliği", "bahce_aycicegi", 10, 5, EventMetric.Obstacles, 200, Reward.Coins(800), Reward.Gems(10), Reward.Of(BoosterType.Shuffle, 2)),
            EventDef.Ladder("Fener Gecesi", "fenerci_fener", Tiers()),
            EventDef.Sprint("Demirci Yarışı", "demirci_ors", 10, 6, EventMetric.Wins, 25, Reward.Coins(1000), Reward.Gems(12), Reward.Of(BoosterType.RowBlast, 2)),
            EventDef.Ladder("Yaz Ateşi", "kor_atesbocegi", Tiers()),
            EventDef.Sprint("Fırın Festivali", "sofra_simit", 10, 4, EventMetric.Stars, 40, Reward.Coins(800), Reward.Gems(10), Reward.Of(BoosterType.ColumnBlast, 2)),
            EventDef.Ladder("Güz Sofrası", "sofra_recel", Tiers()),
            EventDef.Sprint("Ayaz'ın Gecesi", "ayaz_can", 24, 5, EventMetric.Boosters, 12, Reward.Coins(800), Reward.Gems(10), Reward.Pack(2)),
            EventDef.Ladder("Hasat Şöleni", "bahce_balkabagi", Tiers()),
            EventDef.Ladder("Büyük Ocak", "kor_kalp", Tiers()),
        };

        static EventTier[] Tiers() => new[]
        {
            new EventTier(100, Reward.Coins(100), Reward.Of(BoosterType.Hammer)),
            new EventTier(300, Reward.Coins(200), Reward.Of(BoosterType.RowBlast)),
            new EventTier(700, Reward.Coins(300), Reward.Gems(5), Reward.Pack()),
            new EventTier(1500, Reward.Coins(400), Reward.Gems(8), Reward.Of(BoosterType.ColumnBlast)),
            new EventTier(3000, Reward.Coins(500), Reward.Gems(12), Reward.Pack(3)),
        };

        public static int MonthOf(long now)
        {
            var date = Epoch.AddSeconds(now);
            return date.Year * 12 + date.Month - 1;
        }

        public static long MonthStart(int month) => (long)(new DateTime(month / 12, month % 12 + 1, 1) - Epoch).TotalSeconds;

        public EventDef Of(int month) => Calendar[month % 12 % Calendar.Length];

        public long StartOf(int month) => MonthStart(month) + (Of(month).StartDay - 1L) * EconomyRules.SecondsPerDay;

        public long EndOf(int month)
        {
            var def = Of(month);
            return def.Days <= 0 ? MonthStart(month + 1) : StartOf(month) + (long)def.Days * EconomyRules.SecondsPerDay;
        }

        public int WinPoints(int stars, bool hard)
        {
            int points = stars >= 3 ? ThreeStarPoints : stars == 2 ? TwoStarPoints : stars == 1 ? OneStarPoints : 0;
            return points + (hard && stars > 0 ? HardLevelPoints : 0);
        }
    }

    public sealed class LiveEvent
    {
        public readonly EventRules Rules;
        readonly Economy _economy;

        Profile Profile => _economy.Profile;

        public LiveEvent(EventRules rules, Economy economy)
        {
            Rules = rules;
            _economy = economy;
        }

        public int Points => Profile.EventPoints;
        public int Claimed => Profile.EventClaimed;

        public void Tick(long now)
        {
            int month = EventRules.MonthOf(now);
            if (Profile.EventId != month)
            {
                if (Profile.EventId >= 0 && Profile.EventId < month) GrantEarned(Profile.EventId);
                Profile.EventId = month;
                Profile.EventPoints = 0;
                Profile.EventClaimed = 0;
                Profile.EventDoneAt = 0;
                return;
            }
            if (now >= Rules.EndOf(month)) GrantEarned(month);
        }

        void GrantEarned(int month)
        {
            while (true)
            {
                var rewards = Take(month);
                if (rewards == null) return;
            }
        }

        public bool Active(long now)
        {
            Tick(now);
            int month = EventRules.MonthOf(now);
            return now >= Rules.StartOf(month) && now < Rules.EndOf(month);
        }

        public EventDef Current(long now) => Active(now) ? Rules.Of(EventRules.MonthOf(now)) : null;

        public int SecondsLeft(long now)
        {
            if (!Active(now)) return 0;
            return (int)Math.Min(int.MaxValue, Rules.EndOf(EventRules.MonthOf(now)) - now);
        }

        public int ReportWin(int stars, bool hard, long now)
        {
            var def = Current(now);
            if (def == null || stars <= 0) return 0;
            if (def.Kind == EventKind.Ladder) return Add(Rules.WinPoints(stars, hard), def, now);
            Report(EventMetric.Wins, 1, now);
            Report(EventMetric.Stars, stars, now);
            return 0;
        }

        public int Report(EventMetric metric, int amount, long now)
        {
            var def = Current(now);
            if (def == null || amount <= 0) return 0;
            if (def.Kind == EventKind.Sprint) return metric == def.Goal ? Add(amount, def, now) : 0;
            if (metric == EventMetric.Obstacles) return Add(amount * Rules.ObstaclePoints, def, now);
            if (metric == EventMetric.Boosters) return Add(amount * Rules.BoosterPoints, def, now);
            return 0;
        }

        int Add(int amount, EventDef def, long now)
        {
            int before = Profile.EventPoints;
            int after = before + amount;
            if (def.Kind == EventKind.Sprint)
            {
                after = Math.Min(after, def.Target);
                if (before < def.Target && after >= def.Target) Profile.EventDoneAt = now;
            }
            Profile.EventPoints = after;
            return after - before;
        }

        int Earned(EventDef def)
        {
            if (def.Kind == EventKind.Sprint) return Profile.EventPoints >= def.Target ? 1 : 0;
            int reached = 0;
            foreach (var tier in def.Tiers)
                if (Profile.EventPoints >= tier.Points) reached++;
            return reached;
        }

        public int TiersReached(long now)
        {
            var def = Current(now);
            return def == null ? 0 : Earned(def);
        }

        public int Claimable(long now)
        {
            var def = Current(now);
            return def == null ? 0 : Math.Max(0, Earned(def) - Profile.EventClaimed);
        }

        bool EarlyIn(int month)
        {
            if (Profile.EventDoneAt <= 0) return false;
            long start = Rules.StartOf(month), end = Rules.EndOf(month);
            return Profile.EventDoneAt - start < (end - start) / 2;
        }

        public bool Early(long now) => Profile.EventId == EventRules.MonthOf(now) && EarlyIn(EventRules.MonthOf(now));

        public bool BonusOpen(long now)
        {
            int month = EventRules.MonthOf(now);
            long start = Rules.StartOf(month), end = Rules.EndOf(month);
            return Active(now) && now - start < (end - start) / 2;
        }

        Reward[] SprintRewards(int month, bool early)
        {
            var rewards = (Reward[])Rules.Of(month).Rewards.Clone();
            for (int i = 0; early && i < rewards.Length; i++)
                if (rewards[i].Kind == RewardKind.Coins)
                    rewards[i] = Reward.Coins(rewards[i].Amount * (100 + Rules.EarlyBonusPercent) / 100);
            return rewards;
        }

        public Reward[] GoalRewards(long now) => SprintRewards(EventRules.MonthOf(now), Early(now));

        Reward[] Take(int month)
        {
            var def = Rules.Of(month);
            if (Profile.EventClaimed >= Earned(def)) return null;
            var rewards = def.Kind == EventKind.Ladder ? def.Tiers[Profile.EventClaimed].Rewards : SprintRewards(month, EarlyIn(month));
            Profile.EventClaimed++;
            foreach (var reward in rewards) _economy.Grant(reward);
            return rewards;
        }

        public Reward[] Claim(long now)
        {
            if (Current(now) == null) return null;
            return Take(EventRules.MonthOf(now));
        }
    }
}
