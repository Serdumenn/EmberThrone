using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class StoreTests
    {
        const long Day = EconomyRules.SecondsPerDay;
        static readonly long Now = EventRules.MonthStart(2026 * 12 + 10) + 3 * Day + 12 * 3600;

        static Economy Fresh()
        {
            var rules = new EconomyRules();
            var economy = new Economy(rules, Profile.New(rules, 7));
            economy.Tick(Now);
            return economy;
        }

        [Test]
        public void EveryProduct_HasAUniqueId_AndAFallbackPrice()
        {
            var store = new StoreRules();
            Assert.AreEqual(store.Products.Length, store.Products.Select(p => p.Id).Distinct().Count());
            foreach (var p in store.Products) StringAssert.StartsWith("$", p.Price, p.Id);
            Assert.AreEqual(30, store.PassTrack.Length);
        }

        [Test]
        public void GemPack_IsGrantedOnce_PerTransaction_EvenAboveTheCap()
        {
            var e = Fresh();
            e.Profile.Gems = e.Rules.GemCap;
            Assert.IsTrue(e.GrantPurchase("gems_200", "t1", Now, out var granted));
            Assert.AreEqual(200, granted.Single().Amount);
            Assert.AreEqual(e.Rules.GemCap + 200, e.Gems, "paid gems are never lost to the cap");
            Assert.IsTrue(e.GrantPurchase("gems_200", "t1", Now, out granted), "a re-delivered order is fine");
            Assert.AreEqual(0, granted.Length);
            Assert.AreEqual(e.Rules.GemCap + 200, e.Gems, "but not paid twice");
            Assert.IsFalse(e.GrantPurchase("nope", "t2", Now, out _));
        }

        [Test]
        public void StarterPack_IsOfferedFromLevelFive_AndSoldOnce()
        {
            var e = Fresh();
            Assert.IsFalse(e.StarterOffered(3));
            Assert.IsTrue(e.StarterOffered(5));
            Assert.IsFalse(e.StarterOffered(31));
            int coins = e.Coins;
            e.GrantPurchase(StoreRules.StarterId, "t1", Now, out _);
            Assert.AreEqual(coins + 500, e.Coins);
            Assert.AreEqual(8, e.Boosters(BoosterType.Hammer));
            Assert.IsFalse(e.StarterOffered(6), "only once");
        }

        [Test]
        public void Piggy_FillsWithWins_AndBreaksForItsCoins()
        {
            var e = Fresh();
            Assert.IsFalse(e.CanBreakPiggy);
            for (int i = 0; i < 100; i++) e.ReportStoreWin(Now);
            Assert.AreEqual(e.Store.PiggyCap, e.Profile.PiggyCoins, "the piggy has a cap");
            Assert.IsTrue(e.CanBreakPiggy);
            int coins = e.Coins;
            e.GrantPurchase(StoreRules.PiggyId, "t1", Now, out var granted);
            Assert.AreEqual(e.Store.PiggyCap, granted.Single().Amount);
            Assert.AreEqual(coins + e.Store.PiggyCap, e.Coins);
            Assert.AreEqual(0, e.Profile.PiggyCoins);
        }

        [Test]
        public void Pass_StepsFollowWins_PremiumNeedsThePurchase_AndGivesEightLives()
        {
            var e = Fresh();
            Assert.AreEqual(5, e.MaxLives);
            e.ReportStoreWin(Now);
            e.ReportStoreWin(Now);
            Assert.AreEqual(2, e.PassReached(Now));
            Assert.IsTrue(e.ClaimPass(1, false, Now).HasValue);
            Assert.IsFalse(e.ClaimPass(1, false, Now).HasValue, "once");
            Assert.IsFalse(e.ClaimPass(3, false, Now).HasValue, "not reached");
            Assert.IsFalse(e.ClaimPass(1, true, Now).HasValue, "premium needs the pass");

            e.GrantPurchase(StoreRules.PassId, "t1", Now, out _);
            Assert.IsTrue(e.PassPremium(Now));
            Assert.AreEqual(8, e.MaxLives);
            e.Tick(Now + Day);
            Assert.AreEqual(8, e.Lives, "a new day fills up to eight");
            Assert.IsTrue(e.ClaimPass(1, true, Now).HasValue);
        }

        [Test]
        public void Pass_ResetsEachMonth_AndPaysWhatWasLeftUnclaimed()
        {
            var e = Fresh();
            e.GrantPurchase(StoreRules.PassId, "t1", Now, out _);
            for (int i = 0; i < 3; i++) e.ReportStoreWin(Now);
            int coins = e.Coins;
            long next = EventRules.MonthStart(EventRules.MonthOf(Now) + 1) + Day;
            e.Tick(next);
            Assert.Greater(e.Coins, coins, "earned steps are paid at the end of the month");
            Assert.AreEqual(0, e.PassReached(next));
            Assert.IsFalse(e.PassPremium(next), "the pass is for one month");
            Assert.AreEqual(5, e.MaxLives);
        }

        [Test]
        public void StoreState_IsSavedWithTheProfile()
        {
            var e = Fresh();
            e.ReportStoreWin(Now);
            e.GrantPurchase(StoreRules.PassId, "t9", Now, out _);
            e.ClaimPass(1, true, Now);
            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(e.Profile.PiggyCoins, loaded.PiggyCoins);
            Assert.AreEqual(e.Profile.PassPremiumMonth, loaded.PassPremiumMonth);
            Assert.AreEqual(e.Profile.PassPremiumClaimed, loaded.PassPremiumClaimed);
            CollectionAssert.AreEqual(new[] { "t9" }, loaded.Purchases);
        }
    }
}
