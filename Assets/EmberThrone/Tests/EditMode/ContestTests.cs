using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class ContestTests
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

        static Contests Of(Economy e) => new Contests(new ContestRules(), e);

        [Test]
        public void Rush_StartsOnTap_CountsWins_AndPaysTheWinner()
        {
            var economy = Fresh();
            var contests = Of(economy);
            Assert.IsTrue(contests.CanStart(ContestKind.Rush, Noon));
            Assert.IsFalse(contests.Running(ContestKind.Rush, Noon));
            Assert.IsTrue(contests.Begin(ContestKind.Rush, Noon));
            Assert.IsFalse(contests.Begin(ContestKind.Rush, Noon), "one race at a time");
            Assert.AreEqual(3600, contests.SecondsLeft(ContestKind.Rush, Noon));

            for (int i = 1; i <= 5; i++) contests.ReportWin(Noon + i * 60);
            Assert.AreEqual(5, contests.MyWins(ContestKind.Rush));
            Assert.IsTrue(contests.Over(ContestKind.Rush, Noon + 300), "five wins end the race");
            Assert.AreEqual(1, contests.Rank(ContestKind.Rush, Noon + 300), "no rival is that fast");
            Assert.AreEqual(1, contests.Rank(ContestKind.Rush, Noon + 3000), "a finished place is kept");

            int coins = economy.Coins;
            int hammers = economy.Boosters(BoosterType.Hammer);
            var prize = contests.Claim(ContestKind.Rush, Noon + 400);
            CollectionAssert.AreEqual(contests.Rules.PrizeFor(ContestKind.Rush, 1), prize);
            Assert.AreEqual(coins + 150, economy.Coins);
            Assert.AreEqual(hammers + 2, economy.Boosters(BoosterType.Hammer));
            Assert.IsNull(contests.Claim(ContestKind.Rush, Noon + 400), "paid once");

            Assert.IsFalse(contests.CanStart(ContestKind.Rush, Noon + 401), "a rest before the next race");
            Assert.AreEqual(contests.Rules.RestSeconds[0], contests.SecondsLeft(ContestKind.Rush, Noon + 400));
            Assert.IsTrue(contests.CanStart(ContestKind.Rush, Noon + 400 + contests.Rules.RestSeconds[0]));
        }

        [Test]
        public void Rush_RunningOutOfTime_PaysNothing()
        {
            var economy = Fresh();
            var contests = Of(economy);
            contests.ReportWin(Noon);
            contests.Begin(ContestKind.Rush, Noon + 10);
            Assert.AreEqual(0, contests.MyWins(ContestKind.Rush), "wins before the start do not count");
            contests.ReportWin(Noon + 100);
            Assert.IsTrue(contests.Running(ContestKind.Rush, Noon + 3609));
            Assert.IsTrue(contests.Over(ContestKind.Rush, Noon + 3610));
            contests.ReportWin(Noon + 3700);
            Assert.AreEqual(1, contests.MyWins(ContestKind.Rush), "wins after the end do not count");
            CollectionAssert.IsEmpty(contests.PrizeNow(ContestKind.Rush, Noon + 3700));
            int coins = economy.Coins;
            CollectionAssert.IsEmpty(contests.Claim(ContestKind.Rush, Noon + 3700));
            Assert.AreEqual(coins, economy.Coins);
            Assert.IsFalse(contests.CanStart(ContestKind.Rush, Noon + 3700));
        }

        [Test]
        public void Rivals_AreTheSameForTheSameSeed_AndFinishersLead()
        {
            var a = Of(Fresh(31));
            var b = Of(Fresh(31));
            a.Begin(ContestKind.Lantern, Noon);
            b.Begin(ContestKind.Lantern, Noon);
            long late = Noon + 2 * Day;
            var rows = a.Standings(ContestKind.Lantern, late);
            CollectionAssert.AreEqual(rows.Select(r => r.Name + r.Wins).ToArray(), b.Standings(ContestKind.Lantern, late).Select(r => r.Name + r.Wins).ToArray());
            Assert.AreEqual(a.Rules.RivalCounts[1] + 1, rows.Length);
            Assert.AreEqual(1, rows.Count(r => r.IsMe));
            Assert.AreEqual(rows.Length, rows.Select(r => r.Name).Distinct().Count(), "no rival twice");

            bool seenUnfinished = false;
            for (int i = 0; i < rows.Length; i++)
            {
                Assert.AreEqual(i + 1, rows[i].Rank);
                Assert.LessOrEqual(rows[i].Wins, a.Target(ContestKind.Lantern));
                if (!rows[i].Finished) seenUnfinished = true;
                else Assert.IsFalse(seenUnfinished, "finishers come first");
            }

            var early = a.Standings(ContestKind.Lantern, Noon + 3600);
            foreach (var row in rows)
            {
                var before = early.First(r => r.Name == row.Name);
                Assert.LessOrEqual(before.Wins, row.Wins, "rivals never lose wins");
            }
        }

        [Test]
        public void Lantern_LateFinish_GetsNoPodium()
        {
            var economy = Fresh(5);
            var contests = Of(economy);
            contests.Begin(ContestKind.Lantern, Noon);
            long end = Noon + contests.Rules.Seconds[1] - 60;
            for (int i = 0; i < contests.Target(ContestKind.Lantern); i++) contests.ReportWin(end);
            int rank = contests.Rank(ContestKind.Lantern, end);
            int faster = contests.Standings(ContestKind.Lantern, end).Count(r => r.Finished && !r.IsMe);
            Assert.AreEqual(faster + 1, rank);
            CollectionAssert.AreEqual(contests.Rules.PrizeFor(ContestKind.Lantern, rank), contests.PrizeNow(ContestKind.Lantern, end));
        }

        [Test]
        public void Ladder_SevenInARow_PaysAShare_AndALossStartsOver()
        {
            var economy = Fresh();
            var contests = Of(economy);
            for (int i = 0; i < 3; i++) contests.ReportWin(Noon);
            Assert.AreEqual(3, contests.LadderWins(Noon));
            Assert.IsTrue(contests.LadderAtRisk(Noon));
            contests.ReportLoss(Noon);
            Assert.AreEqual(0, contests.LadderWins(Noon));
            Assert.AreEqual(contests.Rules.LadderPlayers, contests.LadderPlayersLeft(Noon));

            for (int i = 0; i < 9; i++) contests.ReportWin(Noon);
            Assert.AreEqual(7, contests.LadderWins(Noon));
            Assert.IsTrue(contests.LadderDone(Noon));
            Assert.IsFalse(contests.LadderAtRisk(Noon));
            int survivors = contests.LadderPlayersLeft(Noon);
            Assert.GreaterOrEqual(survivors, contests.Rules.LadderSurvivorsMin);
            Assert.LessOrEqual(survivors, contests.Rules.LadderSurvivorsMax);
            Assert.AreEqual(contests.Rules.LadderPool / survivors, contests.LadderPrize(Noon));

            contests.ReportLoss(Noon);
            Assert.IsTrue(contests.LadderDone(Noon), "a loss after the top changes nothing");
            int coins = economy.Coins;
            var prize = contests.ClaimLadder(Noon);
            Assert.AreEqual(coins + prize[0].Amount, economy.Coins);
            Assert.IsNull(contests.ClaimLadder(Noon));

            Assert.AreEqual(0, contests.LadderWins(Noon + Day), "a new climb every day");
            Assert.AreEqual((int)(Monday + Day - Noon), contests.LadderSecondsLeft(Noon));
        }

        [Test]
        public void Ladder_UnclaimedPrize_ArrivesTheNextDay()
        {
            var economy = Fresh();
            var contests = Of(economy);
            for (int i = 0; i < 7; i++) contests.ReportWin(Noon);
            int prize = contests.LadderPrize(Noon);
            int coins = economy.Coins;
            contests.Tick(Noon + Day);
            Assert.AreEqual(coins + prize, economy.Coins);
            Assert.AreEqual(0, contests.LadderWins(Noon + Day));
        }

        [Test]
        public void TeamChest_NeedsATeam_FillsWithTheWeek_AndPaysEachStep()
        {
            var economy = Fresh();
            var contests = Of(economy);
            Assert.IsFalse(contests.ChestOpen);
            Assert.AreEqual(0, contests.TeamWins(Noon));
            contests.ReportWin(Noon);
            economy.Profile.Team = 2;
            Assert.IsTrue(contests.ChestOpen);
            Assert.AreEqual(0, economy.Profile.ChestWins, "wins without a team do not count");

            int monday = contests.TeamWins(Monday);
            contests.ReportWin(Noon);
            Assert.AreEqual(monday + 1, contests.TeamWins(Monday), "my wins count for the team");
            int sunday = contests.TeamWins(NextMonday - 60);
            Assert.Greater(sunday, contests.TeamWins(Noon), "teammates win during the week");
            Assert.GreaterOrEqual(sunday, contests.Rules.ChestSteps[0]);

            long when = Noon;
            while (!contests.CanClaimChest(0, when)) when += 3600;
            Assert.IsFalse(contests.CanClaimChest(0, when - 3600));
            int coins = economy.Coins;
            var rewards = contests.ClaimChest(0, when);
            CollectionAssert.AreEqual(contests.Rules.ChestRewards[0], rewards);
            Assert.AreEqual(coins + 100, economy.Coins);
            Assert.IsTrue(contests.ChestClaimed(0));
            Assert.IsNull(contests.ClaimChest(0, when));
            Assert.AreEqual((int)(NextMonday - when), contests.ChestSecondsLeft(when));

            int reached = 0;
            for (int i = 0; i < contests.Rules.ChestSteps.Length; i++)
                if (sunday >= contests.Rules.ChestSteps[i]) reached = i + 1;
            coins = economy.Coins;
            contests.Tick(NextMonday + 60);
            int expected = 0;
            for (int i = 1; i < reached; i++)
                foreach (var reward in contests.Rules.ChestRewards[i])
                    if (reward.Kind == RewardKind.Coins) expected += reward.Amount;
            Assert.AreEqual(coins + expected, economy.Coins, "unclaimed steps arrive with the new week");
            Assert.IsFalse(contests.ChestClaimed(0), "a new week, a new chest");
            Assert.AreEqual(0, economy.Profile.ChestWins);
        }

        [Test]
        public void Unlocks_FollowTheLevelsWon()
        {
            var contests = Of(Fresh());
            Assert.IsFalse(contests.Unlocked(ContestKind.Rush, contests.Rules.FromWins[0] - 1));
            Assert.IsTrue(contests.Unlocked(ContestKind.Rush, contests.Rules.FromWins[0]));
            Assert.IsFalse(contests.LadderUnlocked(contests.Rules.LadderFromWins - 1));
            Assert.IsTrue(contests.LadderUnlocked(contests.Rules.LadderFromWins));
        }

        [Test]
        public void Profile_KeepsContests_ThroughJson()
        {
            var rules = new EconomyRules();
            var economy = Fresh();
            var contests = Of(economy);
            economy.Profile.Team = 1;
            contests.Begin(ContestKind.Lantern, Noon);
            contests.ReportWin(Noon + 5);
            contests.ReportWin(Noon + 9);

            var copy = Profile.FromJson(economy.Profile.ToJson(), rules);
            CollectionAssert.AreEqual(economy.Profile.Contest, copy.Contest);
            Assert.AreEqual(economy.Profile.LadderDay, copy.LadderDay);
            Assert.AreEqual(2, copy.LadderWins);
            Assert.AreEqual(economy.Profile.ChestWeek, copy.ChestWeek);
            Assert.AreEqual(2, copy.ChestWins);

            var again = Of(new Economy(rules, copy));
            Assert.AreEqual(2, again.MyWins(ContestKind.Lantern));
            Assert.IsTrue(again.Running(ContestKind.Lantern, Noon + 60));
        }
    }
}
