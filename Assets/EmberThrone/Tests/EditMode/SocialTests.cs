using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class SocialTests
    {
        const long Day = EconomyRules.SecondsPerDay;
        const long Monday = 20003 * Day;
        const long Noon = Monday + 12 * 3600;
        const long NextMonday = Monday + 7 * Day;

        static Economy Fresh(long seed = 7)
        {
            var rules = new EconomyRules();
            var economy = new Economy(rules, Profile.New(rules, seed));
            economy.Tick(Noon);
            return economy;
        }

        static Race RaceOf(Economy e) => new Race(new RaceRules(), e);

        static Team TeamOf(Economy e, Race race = null) => new Team(new TeamRules(), e, race ?? RaceOf(e));

        [Test]
        public void Week_StartsOnMonday()
        {
            Assert.AreEqual(0, RaceRules.WeekOf(4 * Day - 1), "the first Sunday night");
            Assert.AreEqual(1, RaceRules.WeekOf(4 * Day), "1970-01-05 was a Monday");
            Assert.AreEqual(4 * Day, RaceRules.WeekStart(1));
            Assert.AreEqual(Monday, RaceRules.WeekStart(RaceRules.WeekOf(Noon)));
            Assert.AreEqual(RaceRules.WeekOf(Noon) + 1, RaceRules.WeekOf(NextMonday));
        }

        [Test]
        public void Win_AddsTrophies_AndAFirstWinCountsDouble()
        {
            var race = RaceOf(Fresh());
            Assert.AreEqual(6, race.AddWin(3, true, Noon));
            Assert.AreEqual(2, race.AddWin(2, false, Noon));
            Assert.AreEqual(8, race.Points);
            Assert.AreEqual((int)(NextMonday - Noon), race.SecondsLeft(Noon));
        }

        [Test]
        public void Rivals_AreTheSameForTheSameSeed_AndNeverLosePoints()
        {
            var a = RaceOf(Fresh(31));
            var b = RaceOf(Fresh(31));
            var early = a.Standings(Noon);
            var same = b.Standings(Noon);
            var late = a.Standings(Noon + 3 * Day);
            Assert.AreEqual(a.Rules.Rivals + 1, early.Length);
            CollectionAssert.AreEqual(early.Select(r => r.Name + r.Points).ToArray(), same.Select(r => r.Name + r.Points).ToArray());

            for (int i = 0; i < late.Length; i++)
            {
                Assert.AreEqual(i + 1, late[i].Rank);
                if (i > 0) Assert.GreaterOrEqual(late[i - 1].Points, late[i].Points, "sorted by points");
            }
            foreach (var row in early)
            {
                if (row.IsMe) continue;
                Assert.GreaterOrEqual(late.First(r => r.Name == row.Name).Points, row.Points, row.Name);
            }
            Assert.AreEqual(1, late.Count(r => r.IsMe));
            Assert.Greater(late[0].Points, 0, "somebody played in three days");
            Assert.AreEqual(early.Length, early.Select(r => r.Name).Distinct().Count(), "nobody is listed twice");
        }

        [Test]
        public void Race_OnMondayMorning_HasEverybodyAtZero_WithThePlayerOnTop()
        {
            var race = RaceOf(Fresh());
            Assert.AreEqual(1, race.Rank(Monday));
            Assert.AreEqual(0, race.Standings(Monday).Sum(r => r.Points));
        }

        [Test]
        public void NewWeek_KeepsLastWeeksResult_AndPaysByRank()
        {
            var e = Fresh();
            var race = RaceOf(e);
            for (int i = 0; i < 200; i++) race.AddWin(3, true, Noon);
            Assert.IsFalse(race.ResultReady);

            race.Tick(NextMonday + 60);
            Assert.IsTrue(race.ResultReady);
            Assert.AreEqual(0, race.Points, "the new week starts from zero");
            Assert.AreEqual(1200, race.ResultPoints);
            Assert.AreEqual(1, race.ResultRank);

            int coins = e.Coins, gems = e.Gems, packs = e.Profile.Packs;
            var prize = race.ClaimResult();
            Assert.AreEqual(3, prize.Length);
            Assert.AreEqual(coins + 1500, e.Coins);
            Assert.AreEqual(gems + 15, e.Gems);
            Assert.AreEqual(packs + 3, e.Profile.Packs);
            Assert.IsFalse(race.ResultReady);
            Assert.IsNull(race.ClaimResult(), "paid once");
        }

        [Test]
        public void LastPlace_StillGetsAConsolationPrize()
        {
            var e = Fresh();
            var race = RaceOf(e);
            race.AddWin(1, false, Noon);
            race.Tick(NextMonday);
            Assert.IsTrue(race.ResultReady);
            Assert.Greater(race.ResultRank, 20);
            int coins = e.Coins;
            race.ClaimResult();
            Assert.AreEqual(coins + 50, e.Coins);
        }

        [Test]
        public void NewWeek_WithoutPoints_OrAfterAClockSetBack_LeavesNoResult()
        {
            var idle = RaceOf(Fresh());
            idle.Tick(Noon);
            idle.Tick(NextMonday);
            Assert.IsFalse(idle.ResultReady);

            var cheat = RaceOf(Fresh());
            cheat.AddWin(3, true, NextMonday + 60);
            cheat.Tick(Noon);
            Assert.IsFalse(cheat.ResultReady);
            Assert.AreEqual(0, cheat.Points);
        }

        [Test]
        public void Prizes_FollowTheRankTable()
        {
            var rules = new RaceRules();
            Assert.AreSame(rules.Prizes[0], rules.PrizeFor(1));
            Assert.AreSame(rules.Prizes[2], rules.PrizeFor(3));
            Assert.AreSame(rules.Prizes[3], rules.PrizeFor(4));
            Assert.AreSame(rules.Prizes[3], rules.PrizeFor(10));
            Assert.AreSame(rules.Prizes[4], rules.PrizeFor(11));
            Assert.AreSame(rules.Prizes[5], rules.PrizeFor(30));
        }

        [Test]
        public void Team_IsJoinedAndLeft()
        {
            var e = Fresh();
            var team = TeamOf(e);
            Assert.IsFalse(team.Joined);
            Assert.AreEqual(0, team.Members(Noon).Length);
            Assert.IsFalse(team.Join(99));
            Assert.IsTrue(team.Join(2));
            Assert.AreEqual("Fener Alayı", team.Current.Name);
            Assert.AreEqual(team.Current.Size + 1, team.MemberCount(2), "the player counts");
            Assert.AreEqual(team.Rules.Teams[0].Size, team.MemberCount(0));
            Assert.IsTrue(team.Leave());
            Assert.IsFalse(team.Joined);
        }

        [Test]
        public void Members_IncludeThePlayer_AndAddUpToTheTeamScore()
        {
            var e = Fresh();
            var race = RaceOf(e);
            var team = TeamOf(e, race);
            team.Join(0);
            race.AddWin(3, true, Noon);
            long later = Noon + 2 * Day;
            var members = team.Members(later);
            Assert.AreEqual(team.Current.Size + 1, members.Length);
            Assert.AreEqual(1, members.Count(m => m.IsMe));
            Assert.AreEqual(6, members.First(m => m.IsMe).Points);
            Assert.AreEqual(members.Sum(m => m.Points), team.PointsOf(0, later));
            Assert.IsFalse(members.Any(m => m.Name == "Ayaz"), "the villain is in nobody's team");
            for (int i = 1; i < members.Length; i++) Assert.GreaterOrEqual(members[i - 1].Points, members[i].Points);
        }

        [Test]
        public void Request_NeedsATeamAndAMissingLife_ThenWaitsFourHours()
        {
            var e = Fresh();
            var team = TeamOf(e);
            e.LoseLife(Noon);
            Assert.IsFalse(team.Request(Noon), "no team yet");
            team.Join(1);
            Assert.IsTrue(team.Request(Noon));
            Assert.IsFalse(team.Request(Noon + 60), "one request at a time");
            Assert.AreEqual(4 * 3600 - 60, team.RequestCooldown(Noon + 60));

            long later = Noon + 4 * 3600;
            e.LoseLife(later);
            Assert.AreEqual(0, team.RequestCooldown(later));
            Assert.IsTrue(team.Request(later));

            var full = Fresh();
            var other = TeamOf(full);
            other.Join(1);
            Assert.IsFalse(other.Request(Noon), "lives are full");
        }

        [Test]
        public void Gifts_ArriveWithinMinutes_AndNeverPassFiveLives()
        {
            var e = Fresh();
            var team = TeamOf(e);
            team.Join(0);
            for (int i = 0; i < 3; i++) e.LoseLife(Noon);
            Assert.AreEqual(2, e.Lives);
            Assert.IsTrue(team.Request(Noon));
            Assert.AreEqual(0, team.RequestReceived(Noon));

            team.Tick(Noon + 1100);
            Assert.AreEqual(5, team.RequestReceived(Noon + 1100), "all five come in the first twenty minutes");
            Assert.AreEqual(5, e.Lives);
            Assert.AreEqual(2, team.GiftsWaiting(Noon + 1100), "two gifts wait for room");

            e.LoseLife(Noon + 1200);
            team.Tick(Noon + 1200);
            Assert.AreEqual(5, e.Lives);
            Assert.AreEqual(1, team.GiftsWaiting(Noon + 1200));

            int before = 0;
            for (long t = Noon; t <= Noon + 1100; t += 20)
            {
                int now = team.RequestReceived(t);
                Assert.GreaterOrEqual(now, before);
                before = now;
            }
        }

        [Test]
        public void Help_PaysOnce_PerAsk()
        {
            var e = Fresh();
            var team = TeamOf(e);
            team.Join(3);
            long at = Noon;
            while (team.Asks(at).Length == 0 && at < Noon + 2 * Day) at += 300;
            var asks = team.Asks(at);
            Assert.AreEqual(1, asks.Length, "somebody asks for lives within two days");
            Assert.AreEqual(1, team.OpenAsks(at));
            Assert.IsFalse(asks[0].Helped);
            Assert.Less(asks[0].Received, asks[0].Needed);

            int coins = e.Coins;
            Assert.AreEqual(20, team.Help(asks[0].Id, at));
            Assert.AreEqual(coins + 20, e.Coins);
            Assert.AreEqual(0, team.Help(asks[0].Id, at), "one help per ask");
            Assert.AreEqual(1, team.HelpsThisWeek(at));
            var after = team.Asks(at);
            Assert.AreEqual(1, after.Length);
            Assert.IsTrue(after[0].Helped);
            Assert.AreEqual(asks[0].Received + 1, after[0].Received);
            Assert.AreEqual(0, team.OpenAsks(at));
            Assert.AreEqual(0, team.Help(12345, at), "an ask that does not exist");
            Assert.AreEqual(0, team.HelpsThisWeek(at + 7 * Day), "helps are counted per week");
        }

        [Test]
        public void Profile_KeepsRaceAndTeam()
        {
            var e = Fresh();
            var race = RaceOf(e);
            var team = TeamOf(e, race);
            team.Join(4);
            race.AddWin(3, true, Noon);
            e.LoseLife(Noon);
            team.Request(Noon);
            race.Tick(NextMonday);

            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(4, loaded.Team);
            Assert.AreEqual(Noon, loaded.TeamRequestAt);
            Assert.AreEqual(e.Profile.RaceWeek, loaded.RaceWeek);
            Assert.AreEqual(e.Profile.RaceResultWeek, loaded.RaceResultWeek);
            Assert.AreEqual(6, loaded.RaceResultPoints);
            CollectionAssert.AreEqual(e.Profile.TeamHelped, loaded.TeamHelped);

            var old = Profile.FromJson("{\"coins\":10}", e.Rules);
            Assert.AreEqual(-1, old.Team);
            Assert.AreEqual(-1, old.RaceWeek);
            Assert.AreEqual(-1, old.RaceResultWeek);
        }
    }
}
