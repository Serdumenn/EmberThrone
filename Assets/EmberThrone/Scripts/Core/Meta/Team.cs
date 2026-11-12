using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class TeamDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Emblem;
        public readonly string Motto;
        public readonly int Size;

        public TeamDef(string id, string name, string emblem, string motto, int size)
        {
            Id = id;
            Name = name;
            Emblem = emblem;
            Motto = motto;
            Size = size;
        }
    }

    public sealed class TeamRules
    {
        public int Capacity = 30;
        public int LivesPerRequest = 5;
        public int RequestCooldownSeconds = 4 * 3600;
        public int FirstGiftMinSeconds = 15;
        public int FirstGiftMaxSeconds = 60;
        public int NextGiftMinSeconds = 30;
        public int NextGiftMaxSeconds = 240;

        public int HelpCoins = 20;
        public int AskSlotSeconds = 2 * 3600;
        public int AskLifetimeSeconds = 3600;
        public int AskHelpMinSeconds = 240;
        public int AskHelpMaxSeconds = 900;
        public int HelpedMemory = 4;
        public int PointsPerHelp = 5;

        public Villager[] People = Village.People;

        public TeamDef[] Teams =
        {
            new TeamDef("kor", "Kor Bekçileri", "kor_kalp", "Ocağı hep sıcak tutarız.", 22),
            new TeamDef("demir", "Demir Yumruk", "demirci_migfer", "Örsün başında buluşalım.", 19),
            new TeamDef("fener", "Fener Alayı", "fenerci_fener", "Karanlığa bir ışık.", 17),
            new TeamDef("sofra", "Sofra Ustaları", "sofra_caydanlik", "Çay demli, can hazır.", 21),
            new TeamDef("bahce", "Bahçe Cinleri", "bahce_korkuluk", "Tohum ek, yıldız biç.", 15),
            new TeamDef("ayaz", "Ayaz Avcıları", "ayaz_asa", "Buzu biz kırarız.", 24),
        };
    }

    public readonly struct TeamMember
    {
        public readonly string Name;
        public readonly string Avatar;
        public readonly int Points;
        public readonly int Helps;
        public readonly bool IsMe;

        public TeamMember(string name, string avatar, int points, int helps, bool isMe)
        {
            Name = name;
            Avatar = avatar;
            Points = points;
            Helps = helps;
            IsMe = isMe;
        }
    }

    public readonly struct TeamAsk
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string Avatar;
        public readonly int Received;
        public readonly int Needed;
        public readonly bool Helped;
        public readonly int SecondsLeft;

        public TeamAsk(int id, string name, string avatar, int received, int needed, bool helped, int secondsLeft)
        {
            Id = id;
            Name = name;
            Avatar = avatar;
            Received = received;
            Needed = needed;
            Helped = helped;
            SecondsLeft = secondsLeft;
        }
    }

    public sealed class Team
    {
        const ulong RosterStream = 0xC2B2AE3D27D4EB4FUL;
        const ulong MemberStream = 0x165667B19E3779F9UL;
        const ulong GiftStream = 0x27D4EB2F165667C5UL;
        const ulong AskStream = 0x85EBCA77C2B2AE63UL;
        const ulong StreamsPerTeam = 64UL;

        public readonly TeamRules Rules;
        readonly Economy _economy;
        readonly Race _race;

        Profile Profile => _economy.Profile;

        public Team(TeamRules rules, Economy economy, Race race)
        {
            Rules = rules;
            _economy = economy;
            _race = race;
            if (Profile.Team >= Rules.Teams.Length) Profile.Team = -1;
        }

        public bool Joined => Profile.Team >= 0;
        public int Index => Profile.Team;
        public TeamDef Current => Joined ? Rules.Teams[Profile.Team] : null;

        public bool Join(int index)
        {
            if (index < 0 || index >= Rules.Teams.Length || Profile.Team == index) return false;
            Profile.Team = index;
            return true;
        }

        public bool Leave()
        {
            if (!Joined) return false;
            Profile.Team = -1;
            return true;
        }

        Villager[] Roster(int team)
        {
            var people = new List<Villager>();
            foreach (var person in Rules.People)
                if (!person.Outsider) people.Add(person);
            new Pcg32((ulong)Profile.Seed, RosterStream + (ulong)team).Shuffle(people);
            int count = Math.Min(Rules.Teams[team].Size, people.Count);
            return people.GetRange(0, count).ToArray();
        }

        int MemberPoints(int team, int member, int week, long now) =>
            _race.PointsOf(MemberStream + (ulong)team * StreamsPerTeam + (ulong)member, week, now);

        public int MemberCount(int team) => Roster(team).Length + (Profile.Team == team ? 1 : 0);

        public int PointsOf(int team, long now)
        {
            int week = RaceRules.WeekOf(now);
            int count = Roster(team).Length;
            int total = Profile.Team == team ? _race.Points : 0;
            for (int m = 0; m < count; m++) total += MemberPoints(team, m, week, now);
            return total;
        }

        public TeamMember[] Members(long now)
        {
            if (!Joined) return new TeamMember[0];
            _race.Tick(now);
            RollWeek(now);
            int week = RaceRules.WeekOf(now);
            var roster = Roster(Profile.Team);
            var members = new List<TeamMember>(roster.Length + 1) { new TeamMember(null, null, _race.Points, Profile.TeamHelps, true) };
            for (int m = 0; m < roster.Length; m++)
            {
                int points = MemberPoints(Profile.Team, m, week, now);
                members.Add(new TeamMember(roster[m].Name, roster[m].Avatar, points, points / Rules.PointsPerHelp, false));
            }
            members.Sort((a, b) => a.Points != b.Points ? b.Points.CompareTo(a.Points) : b.IsMe.CompareTo(a.IsMe));
            return members.ToArray();
        }

        void RollWeek(long now)
        {
            int week = RaceRules.WeekOf(now);
            if (Profile.TeamHelpsWeek == week) return;
            Profile.TeamHelpsWeek = week;
            Profile.TeamHelps = 0;
        }

        public int HelpsThisWeek(long now)
        {
            RollWeek(now);
            return Profile.TeamHelps;
        }

        int[] GiftOffsets(long requestAt)
        {
            var rng = new Pcg32((ulong)Profile.Seed, GiftStream + (ulong)requestAt);
            var offsets = new int[Rules.LivesPerRequest];
            int at = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                at += i == 0 ? rng.Next(Rules.FirstGiftMinSeconds, Rules.FirstGiftMaxSeconds + 1) : rng.Next(Rules.NextGiftMinSeconds, Rules.NextGiftMaxSeconds + 1);
                offsets[i] = at;
            }
            return offsets;
        }

        public bool RequestOpen(long now) => Joined && Profile.TeamRequestAt > 0 && now - Profile.TeamRequestAt < Rules.RequestCooldownSeconds;

        public int RequestReceived(long now)
        {
            if (!RequestOpen(now)) return 0;
            int arrived = 0;
            foreach (int offset in GiftOffsets(Profile.TeamRequestAt))
                if (now - Profile.TeamRequestAt >= offset) arrived++;
            return arrived;
        }

        public int RequestCooldown(long now)
        {
            if (Profile.TeamRequestAt <= 0) return 0;
            long left = Profile.TeamRequestAt + Rules.RequestCooldownSeconds - now;
            return (int)Math.Max(0L, Math.Min(left, Rules.RequestCooldownSeconds));
        }

        public bool CanRequest(long now)
        {
            Tick(now);
            return Joined && !_economy.LivesFull && RequestCooldown(now) <= 0;
        }

        public bool Request(long now)
        {
            if (!CanRequest(now)) return false;
            Profile.TeamRequestAt = now;
            Profile.TeamGifts = 0;
            return true;
        }

        public void Tick(long now)
        {
            if (Profile.TeamRequestAt > now) Profile.TeamRequestAt = now;
            if (!Joined || Profile.TeamRequestAt <= 0) return;
            int arrived = RequestReceived(now);
            while (Profile.TeamGifts < arrived && !_economy.LivesFull)
            {
                _economy.Grant(new Reward(RewardKind.Life, 1));
                Profile.TeamGifts++;
            }
        }

        public int GiftsWaiting(long now) => Math.Max(0, RequestReceived(now) - Profile.TeamGifts);

        bool HasHelped(int ask)
        {
            foreach (int id in Profile.TeamHelped)
                if (id == ask) return true;
            return false;
        }

        bool TryAsk(int slot, long now, out TeamAsk ask)
        {
            ask = default;
            var roster = Roster(Profile.Team);
            if (roster.Length == 0) return false;
            var rng = new Pcg32((ulong)Profile.Seed, AskStream + (ulong)Profile.Team * StreamsPerTeam + (ulong)slot * 4096UL);
            var asker = roster[rng.Next(roster.Length)];
            long start = (long)slot * Rules.AskSlotSeconds + rng.Next(Rules.AskSlotSeconds / 2);
            if (now < start || now >= start + Rules.AskLifetimeSeconds) return false;

            bool helped = HasHelped(slot);
            int received = helped ? 1 : 0;
            long at = start;
            for (int i = 0; i < Rules.LivesPerRequest; i++)
            {
                at += rng.Next(Rules.AskHelpMinSeconds, Rules.AskHelpMaxSeconds + 1);
                if (at <= now) received++;
            }
            if (received >= Rules.LivesPerRequest && !helped) return false;
            received = Math.Min(received, Rules.LivesPerRequest);
            ask = new TeamAsk(slot, asker.Name, asker.Avatar, received, Rules.LivesPerRequest, helped, (int)(start + Rules.AskLifetimeSeconds - now));
            return true;
        }

        public TeamAsk[] Asks(long now)
        {
            var asks = new List<TeamAsk>();
            if (!Joined || now < 0) return asks.ToArray();
            int slot = (int)(now / Rules.AskSlotSeconds);
            for (int s = slot; s >= slot - 1 && s >= 0; s--)
                if (TryAsk(s, now, out var ask)) asks.Add(ask);
            return asks.ToArray();
        }

        public int OpenAsks(long now)
        {
            int open = 0;
            foreach (var ask in Asks(now))
                if (!ask.Helped) open++;
            return open;
        }

        public int Help(int ask, long now)
        {
            if (!Joined || HasHelped(ask)) return 0;
            bool found = false;
            foreach (var open in Asks(now))
                if (open.Id == ask && !open.Helped) found = true;
            if (!found) return 0;

            var helped = new List<int>(Profile.TeamHelped) { ask };
            while (helped.Count > Rules.HelpedMemory) helped.RemoveAt(0);
            Profile.TeamHelped = helped.ToArray();
            RollWeek(now);
            Profile.TeamHelps++;
            return _economy.AddCoins(Rules.HelpCoins);
        }
    }
}
