using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class RaceRules
    {
        public const int SecondsPerWeek = 7 * EconomyRules.SecondsPerDay;

        public int Rivals = 29;
        public int FirstWinFactor = 2;
        public int BusyHoursMinPercent = 4;
        public int BusyHoursMaxPercent = 20;
        public int HourGainMax = 6;
        public Villager[] People = Village.People;

        public int[] PrizeRanks = { 1, 2, 3, 10, 20 };
        public Reward[][] Prizes =
        {
            new[] { Reward.Coins(1500), Reward.Gems(15), Reward.Pack(3) },
            new[] { Reward.Coins(1000), Reward.Gems(10), Reward.Pack(2) },
            new[] { Reward.Coins(700), Reward.Gems(5), Reward.Pack() },
            new[] { Reward.Coins(400), Reward.Pack() },
            new[] { Reward.Coins(200) },
            new[] { Reward.Coins(50) },
        };

        public static int WeekOf(long now) => (EconomyRules.DayOf(now) + 3) / 7;

        public static long WeekStart(int week) => (week * 7L - 3L) * EconomyRules.SecondsPerDay;

        public Reward[] PrizeFor(int rank)
        {
            for (int i = 0; i < PrizeRanks.Length; i++)
                if (rank <= PrizeRanks[i]) return Prizes[i];
            return Prizes[Prizes.Length - 1];
        }

        public int WinPoints(int stars, bool firstTime) => Math.Max(0, stars) * (firstTime ? FirstWinFactor : 1);
    }

    public readonly struct RaceRow
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly string Avatar;
        public readonly int Points;
        public readonly bool IsMe;

        public RaceRow(int rank, string name, string avatar, int points, bool isMe)
        {
            Rank = rank;
            Name = name;
            Avatar = avatar;
            Points = points;
            IsMe = isMe;
        }
    }

    public sealed class Race
    {
        const ulong RivalStream = 0xA24BAED4963EE407UL;
        const ulong CastStream = 0x9FB21C651E98DF25UL;
        const ulong StreamsPerWeek = 64UL;

        public readonly RaceRules Rules;
        readonly Economy _economy;

        Profile Profile => _economy.Profile;

        public Race(RaceRules rules, Economy economy)
        {
            Rules = rules;
            _economy = economy;
        }

        public int Points => Profile.RacePoints;

        public void Tick(long now)
        {
            int week = RaceRules.WeekOf(now);
            if (Profile.RaceWeek == week) return;
            if (Profile.RaceWeek >= 0 && Profile.RaceWeek < week && Profile.RacePoints > 0)
            {
                Profile.RaceResultWeek = Profile.RaceWeek;
                Profile.RaceResultPoints = Profile.RacePoints;
            }
            Profile.RaceWeek = week;
            Profile.RacePoints = 0;
        }

        public int AddWin(int stars, bool firstTime, long now)
        {
            Tick(now);
            int gained = Rules.WinPoints(stars, firstTime);
            Profile.RacePoints += gained;
            return gained;
        }

        public int SecondsLeft(long now)
        {
            long left = RaceRules.WeekStart(RaceRules.WeekOf(now) + 1) - now;
            return (int)Math.Max(0L, left);
        }

        public int PointsOf(ulong stream, int week, long now)
        {
            return Village.PointsBy(Profile.Seed, stream + (ulong)week * StreamsPerWeek, RaceRules.WeekStart(week), now,
                Rules.BusyHoursMinPercent, Rules.BusyHoursMaxPercent, Rules.HourGainMax);
        }

        Villager[] Cast(int week)
        {
            var people = new List<Villager>(Rules.People);
            new Pcg32((ulong)Profile.Seed, CastStream + (ulong)week).Shuffle(people);
            int count = Math.Min(Rules.Rivals, people.Count);
            return people.GetRange(0, count).ToArray();
        }

        RaceRow[] Board(int week, int mine, long now)
        {
            var cast = Cast(week);
            var names = new List<Villager>(cast.Length + 1);
            var points = new List<int>(cast.Length + 1);
            names.Add(null);
            points.Add(mine);
            for (int i = 0; i < cast.Length; i++)
            {
                names.Add(cast[i]);
                points.Add(PointsOf(RivalStream + (ulong)i, week, now));
            }

            var order = new List<int>(names.Count);
            for (int i = 0; i < names.Count; i++) order.Add(i);
            order.Sort((a, b) => points[a] != points[b] ? points[b].CompareTo(points[a]) : a.CompareTo(b));

            var rows = new RaceRow[order.Count];
            for (int r = 0; r < rows.Length; r++)
            {
                int i = order[r];
                rows[r] = new RaceRow(r + 1, names[i]?.Name, names[i]?.Avatar, points[i], i == 0);
            }
            return rows;
        }

        static int RankIn(RaceRow[] rows)
        {
            foreach (var row in rows)
                if (row.IsMe) return row.Rank;
            return rows.Length;
        }

        public RaceRow[] Standings(long now)
        {
            Tick(now);
            return Board(Profile.RaceWeek, Profile.RacePoints, now);
        }

        public int Rank(long now) => RankIn(Standings(now));

        public bool ResultReady => Profile.RaceResultWeek >= 0;

        public int ResultPoints => ResultReady ? Profile.RaceResultPoints : 0;

        public RaceRow[] ResultStandings
        {
            get
            {
                if (!ResultReady) return new RaceRow[0];
                return Board(Profile.RaceResultWeek, Profile.RaceResultPoints, RaceRules.WeekStart(Profile.RaceResultWeek + 1));
            }
        }

        public int ResultRank => ResultReady ? RankIn(ResultStandings) : 0;

        public Reward[] ResultPrize => ResultReady ? Rules.PrizeFor(ResultRank) : null;

        public Reward[] ClaimResult()
        {
            if (!ResultReady) return null;
            var prize = Rules.PrizeFor(ResultRank);
            Profile.RaceResultWeek = -1;
            Profile.RaceResultPoints = 0;
            foreach (var reward in prize) _economy.Grant(reward);
            return prize;
        }
    }
}
