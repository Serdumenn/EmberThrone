using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class RestorationTests
    {
        static Economy Fresh()
        {
            var rules = new EconomyRules();
            var economy = new Economy(rules, Profile.New(rules, 7));
            economy.Tick(100 * EconomyRules.SecondsPerDay);
            return economy;
        }

        static Restoration RestoreOf(Economy e) => new Restoration(new RestoreRules(), e);

        [Test]
        public void Areas_HaveFiveTasks_ThatTheirTenLevelsCanFinish()
        {
            var rules = new RestoreRules();
            Assert.AreEqual(10, rules.Areas.Length);
            foreach (var area in rules.Areas)
            {
                Assert.AreEqual(5, area.Tasks.Length);
                foreach (var task in area.Tasks)
                {
                    Assert.Greater(task.Target, 0, task.Name);
                    if (task.Goal == RestoreGoal.Levels) Assert.LessOrEqual(task.Target, 10, task.Name);
                    if (task.Goal == RestoreGoal.Stars) Assert.LessOrEqual(task.Target, 25, task.Name + " leaves room for misses");
                    Assert.IsNotEmpty(task.Rewards, task.Name);
                }
                Assert.IsNotEmpty(area.Bonus);
                Assert.AreEqual(area.Tasks.Length, area.Tasks.Select(t => t.Name).Distinct().Count());
            }
        }

        [Test]
        public void Wins_CountNewLevelsAndNewStars_PerArea()
        {
            var e = Fresh();
            var r = RestoreOf(e);
            r.ReportWin(0, 3, true);
            r.ReportWin(0, 0, false);
            r.ReportWin(0, 1, false);
            r.ReportWin(1, 2, true);
            r.ReportWin(99, 3, true);
            r.ReportBooster(0);
            r.ReportBooster(-1);
            var tasks = r.Tasks(0);
            Assert.AreEqual(1, tasks[0].Progress, "one level finished");
            Assert.AreEqual(4, tasks[1].Progress, "three stars, then one more");
            Assert.AreEqual(1, tasks[2].Progress);
            Assert.AreEqual(1, r.Tasks(1)[0].Progress, "the other district counts apart");
            Assert.AreEqual(0, r.Tasks(99).Length);
        }

        [Test]
        public void Claim_PaysOnce_AndTheLastTaskPaysTheBonus()
        {
            var e = Fresh();
            var r = RestoreOf(e);
            Assert.IsNull(r.Claim(0, 0), "not done yet");
            for (int i = 0; i < 10; i++) r.ReportWin(0, 3, true);
            r.ReportBooster(0);
            r.ReportBooster(0);
            Assert.AreEqual(5, r.Claimable(0));

            int coins = e.Coins;
            var first = r.Claim(0, 1);
            Assert.AreEqual(1, first.Length);
            Assert.AreEqual(coins + 150, e.Coins);
            Assert.IsTrue(r.IsClaimed(0, 1));
            Assert.IsNull(r.Claim(0, 1), "paid once");
            Assert.AreEqual(4, r.Claimable(0));
            Assert.IsFalse(r.Restored(0));

            int hammers = e.Boosters(BoosterType.Hammer);
            r.Claim(0, 2);
            Assert.AreEqual(hammers + 1, e.Boosters(BoosterType.Hammer));
            r.Claim(0, 0);
            r.Claim(0, 3);
            int packs = e.Profile.Packs;
            coins = e.Coins;
            var last = r.Claim(0, 4);
            Assert.AreEqual(3, last.Length, "the task and the district bonus");
            Assert.AreEqual(coins + 300 + 300, e.Coins);
            Assert.AreEqual(packs + 1, e.Profile.Packs);
            Assert.IsTrue(r.Restored(0));
            Assert.AreEqual(5, r.ClaimedCount(0));
            Assert.AreEqual(0, r.Claimable(0));
        }

        [Test]
        public void Focus_IsTheFirstUnfinishedDistrictTheyHaveReached()
        {
            var e = Fresh();
            var r = RestoreOf(e);
            Assert.AreEqual(0, r.Focus(0));
            Assert.AreEqual(0, r.Focus(2), "district one still waits");
            for (int i = 0; i < 10; i++) r.ReportWin(0, 3, true);
            r.ReportBooster(0);
            r.ReportBooster(0);
            for (int t = 0; t < 5; t++) r.Claim(0, t);
            Assert.AreEqual(0, r.Focus(0), "a finished district stays on show until the next opens");
            Assert.AreEqual(1, r.Focus(1));
            Assert.AreEqual(1, r.Focus(7));
        }

        [Test]
        public void Seed_TakesEarlierProgressOnce_AndTheProfileKeepsIt()
        {
            var e = Fresh();
            var r = RestoreOf(e);
            Assert.IsTrue(r.Seed(new[] { 4, 1 }, new[] { 10, 2 }));
            Assert.IsFalse(r.Seed(new[] { 9, 9 }, new[] { 30, 30 }), "only once");
            Assert.AreEqual(4, r.Tasks(0)[3].Progress, "four levels from before");
            Assert.AreEqual(10, e.Profile.RestoreStars[0]);
            r.Claim(0, 0);

            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            CollectionAssert.AreEqual(e.Profile.RestoreLevels, loaded.RestoreLevels);
            CollectionAssert.AreEqual(e.Profile.RestoreStars, loaded.RestoreStars);
            CollectionAssert.AreEqual(e.Profile.RestoreClaimed, loaded.RestoreClaimed);
            Assert.IsTrue(loaded.RestoreSeeded);
            var again = RestoreOf(new Economy(e.Rules, loaded));
            Assert.IsTrue(again.IsClaimed(0, 0));

            var old = Profile.FromJson("{\"coins\":10}", e.Rules);
            Assert.IsFalse(old.RestoreSeeded);
            RestoreOf(new Economy(e.Rules, old));
            Assert.AreEqual(new RestoreRules().Areas.Length, old.RestoreClaimed.Length, "an old profile grows to fit");
        }
    }
}
