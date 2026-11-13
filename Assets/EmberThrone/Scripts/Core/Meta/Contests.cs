using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum ContestKind
    {
        Rush = 0,
        Lantern = 1,
    }

    public sealed class ContestRules
    {
        public const int Kinds = 2;
        public const int Slots = 5;

        public int[] FromWins = { 8, 12 };
        public int LadderFromWins = 5;
        public int[] Seconds = { 3600, 3 * EconomyRules.SecondsPerDay };
        public int[] Targets = { 5, 15 };
        public int[] RivalCounts = { 4, 9 };
        public int[] RestSeconds = { 1800, EconomyRules.SecondsPerDay };
        public int RivalFastestPercent = 40;
        public int RivalSlowestPercent = 160;
        public Reward[][][] Prizes =
        {
            new[]
            {
                new[] { Reward.Coins(150), Reward.Of(BoosterType.Hammer, 2) },
                new[] { Reward.Coins(80), Reward.Of(BoosterType.Hammer) },
                new[] { Reward.Coins(50) },
            },
            new[]
            {
                new[] { Reward.Coins(500), Reward.Gems(10), Reward.Pack(2) },
                new[] { Reward.Coins(300), Reward.Gems(5) },
                new[] { Reward.Coins(200), Reward.Pack() },
            },
        };

        public int LadderTarget = 7;
        public int LadderPool = 10000;
        public int LadderPlayers = 100;
        public int LadderSurvivorsMin = 8;
        public int LadderSurvivorsMax = 30;

        public int[] ChestSteps = { 30, 70, 120 };
        public Reward[][] ChestRewards =
        {
            new[] { Reward.Coins(100), Reward.Of(BoosterType.Hammer) },
            new[] { Reward.Coins(200), Reward.Of(BoosterType.RowBlast), Reward.Of(BoosterType.ColumnBlast) },
            new[] { Reward.Coins(300), Reward.Gems(10), Reward.Pack(2) },
        };
        public int ChestBusyMinPercent = 3;
        public int ChestBusyMaxPercent = 9;

        public Villager[] People = Village.People;

        public Reward[] PrizeFor(ContestKind kind, int rank)
        {
            var prizes = Prizes[(int)kind];
            return rank >= 1 && rank <= prizes.Length ? prizes[rank - 1] : new Reward[0];
        }
    }

    public readonly struct ContestRow
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly string Avatar;
        public readonly int Wins;
        public readonly bool IsMe;
        public readonly bool Finished;

        public ContestRow(int rank, string name, string avatar, int wins, bool isMe, bool finished)
        {
            Rank = rank;
            Name = name;
            Avatar = avatar;
            Wins = wins;
            IsMe = isMe;
            Finished = finished;
        }
    }

    public sealed class Contests
    {
        const int Start = 0;
        const int Wins = 1;
        const int Done = 2;
        const int Claimed = 3;
        const int Rest = 4;
        const ulong RivalStream = 0xC2B2AE3D27D4EB4FUL;
        const ulong CastStream = 0x165667B19E3779F9UL;
        const ulong LadderStream = 0x27D4EB2F165667C5UL;
        const ulong MemberStream = 0x85EBCA77C2B2AE63UL;
        const int TeamMembers = 9;

        public readonly ContestRules Rules;
        readonly Economy _economy;

        Profile Profile => _economy.Profile;

        public Contests(ContestRules rules, Economy economy)
        {
            Rules = rules;
            _economy = economy;
        }

        long Get(ContestKind kind, int slot)
        {
            Ensure();
            return Profile.Contest[(int)kind * ContestRules.Slots + slot];
        }

        void Set(ContestKind kind, int slot, long value)
        {
            Ensure();
            Profile.Contest[(int)kind * ContestRules.Slots + slot] = value;
        }

        void Ensure()
        {
            int size = ContestRules.Kinds * ContestRules.Slots;
            if (Profile.Contest.Length == size) return;
            var grown = new long[size];
            Array.Copy(Profile.Contest, grown, Math.Min(size, Profile.Contest.Length));
            Profile.Contest = grown;
        }

        public void Tick(long now)
        {
            RollLadder(now);
            RollChest(now);
        }

        public bool Unlocked(ContestKind kind, int levelsWon) => levelsWon >= Rules.FromWins[(int)kind];

        public bool LadderUnlocked(int levelsWon) => levelsWon >= Rules.LadderFromWins;

        public int Target(ContestKind kind) => Rules.Targets[(int)kind];

        public long Ends(ContestKind kind) => Get(kind, Start) + Rules.Seconds[(int)kind];

        public bool Joined(ContestKind kind) => Get(kind, Start) > 0 && Get(kind, Claimed) == 0;

        public bool Running(ContestKind kind, long now) => Joined(kind) && Get(kind, Done) == 0 && now < Ends(kind);

        public bool Over(ContestKind kind, long now) => Joined(kind) && (Get(kind, Done) > 0 || now >= Ends(kind));

        public bool CanStart(ContestKind kind, long now) => !Joined(kind) && now >= Get(kind, Rest);

        public int MyWins(ContestKind kind) => Joined(kind) ? (int)Get(kind, Wins) : 0;

        public int SecondsLeft(ContestKind kind, long now)
        {
            if (Joined(kind)) return (int)Math.Max(0L, Ends(kind) - now);
            return (int)Math.Max(0L, Get(kind, Rest) - now);
        }

        public bool Begin(ContestKind kind, long now)
        {
            if (!CanStart(kind, now)) return false;
            Set(kind, Start, now);
            Set(kind, Wins, 0);
            Set(kind, Done, 0);
            Set(kind, Claimed, 0);
            return true;
        }

        public void ReportWin(long now)
        {
            for (int k = 0; k < ContestRules.Kinds; k++)
            {
                var kind = (ContestKind)k;
                if (!Running(kind, now)) continue;
                long wins = Get(kind, Wins) + 1;
                Set(kind, Wins, wins);
                if (wins >= Target(kind)) Set(kind, Done, now);
            }
            RollLadder(now);
            if (Profile.LadderWins < Rules.LadderTarget) Profile.LadderWins++;
            RollChest(now);
            if (Profile.Team >= 0) Profile.ChestWins++;
        }

        public void ReportLoss(long now)
        {
            RollLadder(now);
            if (Profile.LadderWins < Rules.LadderTarget) Profile.LadderWins = 0;
        }

        float Finish(ContestKind kind, int i)
        {
            var rng = new Pcg32((ulong)Profile.Seed ^ (ulong)Get(kind, Start), RivalStream + (ulong)((int)kind * 32 + i));
            return rng.Next(Rules.RivalFastestPercent, Rules.RivalSlowestPercent + 1) / 100f;
        }

        Villager[] Cast(ContestKind kind)
        {
            var people = new List<Villager>(Rules.People);
            new Pcg32((ulong)Profile.Seed ^ (ulong)Get(kind, Start), CastStream + (ulong)kind).Shuffle(people);
            return people.GetRange(0, Math.Min(Rules.RivalCounts[(int)kind], people.Count)).ToArray();
        }

        public ContestRow[] Standings(ContestKind kind, long now)
        {
            if (!Joined(kind)) return new ContestRow[0];
            long start = Get(kind, Start);
            long end = Ends(kind);
            long at = Math.Min(now, end);
            int target = Target(kind);
            float seconds = Rules.Seconds[(int)kind];
            var cast = Cast(kind);
            var entries = new List<(string name, string avatar, int wins, long done, bool me)>();
            long myDone = Get(kind, Done);
            entries.Add((null, null, (int)Get(kind, Wins), myDone > 0 ? myDone : long.MaxValue, true));
            for (int i = 0; i < cast.Length; i++)
            {
                float f = Finish(kind, i);
                long done = start + (long)(seconds * f);
                int wins = (int)Math.Min(target, Math.Floor(target * (at - start) / (seconds * f)));
                entries.Add((cast[i].Name, cast[i].Avatar, wins, done <= at ? done : long.MaxValue, false));
            }
            entries.Sort((a, b) =>
            {
                if (a.done != b.done) return a.done.CompareTo(b.done);
                if (a.wins != b.wins) return b.wins.CompareTo(a.wins);
                return b.me.CompareTo(a.me);
            });
            var rows = new ContestRow[entries.Count];
            for (int r = 0; r < rows.Length; r++)
            {
                var e = entries[r];
                rows[r] = new ContestRow(r + 1, e.name, e.avatar, e.wins, e.me, e.done != long.MaxValue);
            }
            return rows;
        }

        public int Rank(ContestKind kind, long now)
        {
            foreach (var row in Standings(kind, now))
                if (row.IsMe) return row.Rank;
            return 0;
        }

        public Reward[] PrizeNow(ContestKind kind, long now) =>
            Over(kind, now) && Get(kind, Done) > 0 ? Rules.PrizeFor(kind, Rank(kind, now)) : new Reward[0];

        public Reward[] Claim(ContestKind kind, long now)
        {
            if (!Over(kind, now)) return null;
            var prize = PrizeNow(kind, now);
            foreach (var reward in prize) _economy.Grant(reward);
            Set(kind, Claimed, 1);
            Set(kind, Rest, now + Rules.RestSeconds[(int)kind]);
            return prize;
        }

        void RollLadder(long now)
        {
            int day = EconomyRules.DayOf(now);
            if (Profile.LadderDay == day) return;
            if (Profile.LadderDay >= 0 && Profile.LadderWins >= Rules.LadderTarget && !Profile.LadderClaimed)
                _economy.Grant(Reward.Coins(LadderShare(Profile.LadderDay)));
            Profile.LadderDay = day;
            Profile.LadderWins = 0;
            Profile.LadderClaimed = false;
        }

        int Survivors(int day)
        {
            var rng = new Pcg32((ulong)Profile.Seed, LadderStream + (ulong)day);
            return rng.Next(Rules.LadderSurvivorsMin, Rules.LadderSurvivorsMax + 1);
        }

        int LadderShare(int day) => Rules.LadderPool / Math.Max(1, Survivors(day));

        public int LadderWins(long now)
        {
            RollLadder(now);
            return Profile.LadderWins;
        }

        public bool LadderDone(long now) => LadderWins(now) >= Rules.LadderTarget;

        public bool LadderAtRisk(long now)
        {
            int wins = LadderWins(now);
            return wins > 0 && wins < Rules.LadderTarget;
        }

        public bool LadderClaimable(long now) => LadderDone(now) && !Profile.LadderClaimed;

        public int LadderPlayersLeft(long now)
        {
            int wins = LadderWins(now);
            int survivors = Survivors(Profile.LadderDay);
            return Rules.LadderPlayers - (Rules.LadderPlayers - survivors) * wins / Rules.LadderTarget;
        }

        public int LadderPrize(long now)
        {
            RollLadder(now);
            return LadderShare(Profile.LadderDay);
        }

        public int LadderSecondsLeft(long now) => (int)(EconomyRules.SecondsPerDay - now % EconomyRules.SecondsPerDay);

        public Reward[] ClaimLadder(long now)
        {
            if (!LadderClaimable(now)) return null;
            var prize = new[] { Reward.Coins(LadderShare(Profile.LadderDay)) };
            _economy.Grant(prize[0]);
            Profile.LadderClaimed = true;
            return prize;
        }

        void RollChest(long now)
        {
            int week = RaceRules.WeekOf(now);
            if (Profile.ChestWeek == week) return;
            if (Profile.ChestWeek >= 0 && Profile.Team >= 0)
            {
                int done = TeamWinsAt(Profile.ChestWeek, Profile.ChestWins, RaceRules.WeekStart(Profile.ChestWeek + 1));
                for (int i = 0; i < Rules.ChestSteps.Length; i++)
                    if (done >= Rules.ChestSteps[i] && (Profile.ChestClaimed & (1 << i)) == 0)
                        foreach (var reward in Rules.ChestRewards[i]) _economy.Grant(reward);
            }
            Profile.ChestWeek = week;
            Profile.ChestWins = 0;
            Profile.ChestClaimed = 0;
        }

        int TeamWinsAt(int week, int mine, long now)
        {
            int total = mine;
            long start = RaceRules.WeekStart(week);
            for (int i = 0; i < TeamMembers; i++)
                total += Village.PointsBy(Profile.Seed, MemberStream + (ulong)(week * 16 + i), start, now, Rules.ChestBusyMinPercent, Rules.ChestBusyMaxPercent, 1);
            return total;
        }

        public bool ChestOpen => Profile.Team >= 0;

        public int TeamWins(long now)
        {
            RollChest(now);
            return ChestOpen ? TeamWinsAt(Profile.ChestWeek, Profile.ChestWins, now) : 0;
        }

        public bool ChestClaimed(int step) => (Profile.ChestClaimed & (1 << step)) != 0;

        public bool CanClaimChest(int step, long now) =>
            ChestOpen && step >= 0 && step < Rules.ChestSteps.Length && !ChestClaimed(step) && TeamWins(now) >= Rules.ChestSteps[step];

        public int ChestClaimable(long now)
        {
            int n = 0;
            for (int i = 0; i < Rules.ChestSteps.Length; i++)
                if (CanClaimChest(i, now)) n++;
            return n;
        }

        public Reward[] ClaimChest(int step, long now)
        {
            if (!CanClaimChest(step, now)) return null;
            Profile.ChestClaimed |= 1 << step;
            foreach (var reward in Rules.ChestRewards[step]) _economy.Grant(reward);
            return Rules.ChestRewards[step];
        }

        public int ChestSecondsLeft(long now) => (int)Math.Max(0L, RaceRules.WeekStart(RaceRules.WeekOf(now) + 1) - now);
    }
}
