using System;
using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class LiveEventTests
    {
        const long Day = EconomyRules.SecondsPerDay;

        static long At(int year, int month, int day, int hour = 12) =>
            (long)(new DateTime(year, month, day, hour, 0, 0) - new DateTime(1970, 1, 1)).TotalSeconds;

        static Economy Fresh(long now)
        {
            var rules = new EconomyRules();
            var economy = new Economy(rules, Profile.New(rules, 7));
            economy.Tick(now);
            return economy;
        }

        static LiveEvent EventOf(Economy e) => new LiveEvent(new EventRules(), e);

        [Test]
        public void Months_FollowTheCalendar()
        {
            int november = EventRules.MonthOf(At(2026, 11, 12));
            Assert.AreEqual(2026 * 12 + 10, november);
            Assert.AreEqual(At(2026, 11, 1, 0), EventRules.MonthStart(november));
            Assert.AreEqual(november, EventRules.MonthOf(At(2026, 11, 30, 23)));
            Assert.AreEqual(november + 1, EventRules.MonthOf(At(2026, 12, 1, 0)));
            Assert.AreEqual(november + 2, EventRules.MonthOf(At(2027, 1, 1, 0)), "the year turns over");
            Assert.AreEqual("Hasat Şöleni", new EventRules().Of(november).Name);
        }

        [Test]
        public void Calendar_FitsItsMonths_AndStaysUnderThePrizeCaps()
        {
            var rules = new EventRules();
            Assert.AreEqual(12, rules.Calendar.Length);
            foreach (var def in rules.Calendar)
            {
                int coins, gems;
                if (def.Kind == EventKind.Sprint)
                {
                    Assert.Greater(def.Target, 0, def.Name);
                    Assert.GreaterOrEqual(def.Days, 3, def.Name);
                    Assert.LessOrEqual(def.Days, 7, def.Name);
                    Assert.LessOrEqual(def.StartDay + def.Days - 1, 28, def.Name + " ends inside the shortest month");
                    coins = def.Rewards.Where(r => r.Kind == RewardKind.Coins).Sum(r => r.Amount) * (100 + rules.EarlyBonusPercent) / 100;
                    gems = def.Rewards.Where(r => r.Kind == RewardKind.Gems).Sum(r => r.Amount);
                }
                else
                {
                    Assert.AreEqual(5, def.Tiers.Length, def.Name);
                    for (int i = 1; i < def.Tiers.Length; i++) Assert.Greater(def.Tiers[i].Points, def.Tiers[i - 1].Points, def.Name);
                    coins = def.Tiers.SelectMany(t => t.Rewards).Where(r => r.Kind == RewardKind.Coins).Sum(r => r.Amount);
                    gems = def.Tiers.SelectMany(t => t.Rewards).Where(r => r.Kind == RewardKind.Gems).Sum(r => r.Amount);
                }
                Assert.LessOrEqual(coins, 1500, def.Name + " coins");
                Assert.LessOrEqual(gems, 25, def.Name + " gems");
            }
        }

        [Test]
        public void Ladder_CountsWinsObstaclesAndBoosters_AndPaysEachTierOnce()
        {
            long now = At(2026, 11, 12);
            var e = Fresh(now);
            var ev = EventOf(e);
            Assert.IsTrue(ev.Active(now));
            Assert.AreEqual(EventKind.Ladder, ev.Current(now).Kind);
            Assert.AreEqual((int)(At(2026, 12, 1, 0) - now), ev.SecondsLeft(now));

            Assert.AreEqual(45, ev.ReportWin(3, true, now), "three stars on a hard level");
            Assert.AreEqual(15, ev.ReportWin(2, false, now));
            Assert.AreEqual(10, ev.ReportWin(1, false, now));
            Assert.AreEqual(0, ev.ReportWin(0, true, now));
            Assert.AreEqual(20, ev.Report(EventMetric.Obstacles, 10, now));
            Assert.AreEqual(10, ev.Report(EventMetric.Boosters, 2, now));
            Assert.AreEqual(0, ev.Report(EventMetric.Stars, 3, now), "stars are counted with the win");
            Assert.AreEqual(100, ev.Points);
            Assert.AreEqual(1, ev.TiersReached(now));
            Assert.AreEqual(1, ev.Claimable(now));

            int coins = e.Coins, hammers = e.Boosters(BoosterType.Hammer);
            var prize = ev.Claim(now);
            Assert.AreEqual(2, prize.Length);
            Assert.AreEqual(coins + 100, e.Coins);
            Assert.AreEqual(hammers + 1, e.Boosters(BoosterType.Hammer));
            Assert.AreEqual(0, ev.Claimable(now));
            Assert.IsNull(ev.Claim(now), "a tier pays once");
            Assert.AreEqual(100, ev.Points, "claiming does not spend the points");
        }

        [Test]
        public void NewMonth_PaysWhatWasEarned_AndStartsFromZero()
        {
            long now = At(2026, 11, 12);
            var e = Fresh(now);
            var ev = EventOf(e);
            for (int i = 0; i < 12; i++) ev.ReportWin(3, false, now);
            Assert.AreEqual(300, ev.Points);
            Assert.AreEqual(2, ev.Claimable(now));

            int coins = e.Coins;
            long december = At(2026, 12, 2);
            ev.Tick(december);
            Assert.AreEqual(coins + 300, e.Coins, "both tiers were handed over");
            Assert.AreEqual(0, ev.Points);
            Assert.AreEqual(0, ev.Claimable(december));
            Assert.AreEqual("Büyük Ocak", ev.Current(december).Name);

            ev.ReportWin(3, false, december);
            ev.Tick(now);
            Assert.AreEqual(0, ev.Points, "a clock set back starts over and pays nothing");
            Assert.AreEqual(coins + 300, e.Coins);
        }

        [Test]
        public void Sprint_RunsForItsDaysOnly_AndPaysMoreWhenFinishedEarly()
        {
            long before = At(2027, 2, 9);
            long first = At(2027, 2, 10);
            long late = At(2027, 2, 13);
            long after = At(2027, 2, 14);

            var e = Fresh(before);
            var ev = EventOf(e);
            Assert.IsFalse(ev.Active(before));
            Assert.IsNull(ev.Current(before));
            Assert.AreEqual(0, ev.ReportWin(3, false, before));
            Assert.AreEqual(0, ev.Points);

            Assert.IsTrue(ev.Active(first));
            Assert.AreEqual("Ayaz Baskını", ev.Current(first).Name);
            Assert.IsTrue(ev.BonusOpen(first));
            for (int i = 0; i < 13; i++) ev.ReportWin(3, false, first);
            Assert.AreEqual(39, ev.Points, "this sprint counts stars");
            Assert.AreEqual(0, ev.Claimable(first));
            ev.ReportWin(3, false, first);
            Assert.AreEqual(40, ev.Points, "the goal is the cap");
            Assert.AreEqual(1, ev.Claimable(first));
            Assert.IsTrue(ev.Early(first));

            int coins = e.Coins, gems = e.Gems;
            var prize = ev.Claim(first);
            Assert.AreEqual(coins + 960, e.Coins, "800 and the early bonus");
            Assert.AreEqual(gems + 10, e.Gems);
            Assert.AreEqual(3, prize.Length);
            Assert.IsNull(ev.Claim(first));
            Assert.IsFalse(ev.Active(after));

            var slow = Fresh(before);
            var other = EventOf(slow);
            Assert.IsFalse(other.BonusOpen(late));
            for (int i = 0; i < 14; i++) other.ReportWin(3, false, late);
            Assert.IsFalse(other.Early(late));
            int had = slow.Coins;
            other.Tick(after);
            Assert.AreEqual(had + 800, slow.Coins, "an unclaimed prize is handed over when the sprint ends");
            Assert.AreEqual(0, other.Claimable(after));
        }

        [Test]
        public void Profile_KeepsTheEvent()
        {
            long now = At(2026, 11, 12);
            var e = Fresh(now);
            var ev = EventOf(e);
            for (int i = 0; i < 5; i++) ev.ReportWin(3, false, now);
            ev.Claim(now);
            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(e.Profile.EventId, loaded.EventId);
            Assert.AreEqual(125, loaded.EventPoints);
            Assert.AreEqual(1, loaded.EventClaimed);
            Assert.AreEqual(-1, Profile.FromJson("{\"coins\":10}", e.Rules).EventId);
        }
    }
}
