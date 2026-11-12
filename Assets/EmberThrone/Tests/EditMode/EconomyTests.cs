using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class EconomyTests
    {
        const long Day = EconomyRules.SecondsPerDay;
        const long Noon = 100 * Day + 12 * 3600;

        static Economy Fresh(long seed = 7)
        {
            var rules = new EconomyRules();
            var economy = new Economy(rules, Profile.New(rules, seed));
            economy.Tick(Noon);
            return economy;
        }

        [Test]
        public void NewProfile_StartsWithTheWelcomePack()
        {
            var e = Fresh();
            Assert.AreEqual(500, e.Coins);
            Assert.AreEqual(50, e.Gems);
            Assert.AreEqual(5, e.Lives);
            Assert.AreEqual(3, e.Boosters(BoosterType.Hammer));
            Assert.AreEqual(3, e.Boosters(BoosterType.Shuffle));
        }

        [Test]
        public void Lives_ComeBackOneEveryHalfHour_UpToFive()
        {
            var e = Fresh();
            Assert.IsTrue(e.LoseLife(Noon));
            Assert.IsTrue(e.LoseLife(Noon + 60));
            Assert.AreEqual(3, e.Lives);
            Assert.AreEqual(1800, e.SecondsToNextLife(Noon));

            e.Tick(Noon + 1799);
            Assert.AreEqual(3, e.Lives);
            Assert.AreEqual(1, e.SecondsToNextLife(Noon + 1799));

            e.Tick(Noon + 1800);
            Assert.AreEqual(4, e.Lives);
            Assert.AreEqual(1800, e.SecondsToNextLife(Noon + 1800));

            e.Tick(Noon + 5 * 3600);
            Assert.AreEqual(5, e.Lives);
            Assert.AreEqual(0, e.SecondsToNextLife(Noon + 5 * 3600));
            Assert.AreEqual(0, e.Profile.LifeAnchor);
        }

        [Test]
        public void Lives_AtZero_CannotBeLost_AndAClockSetBackGivesNothing()
        {
            var e = Fresh();
            for (int i = 0; i < 5; i++) Assert.IsTrue(e.LoseLife(Noon));
            Assert.IsFalse(e.LoseLife(Noon));
            Assert.AreEqual(0, e.Lives);

            e.Tick(Noon - 7200);
            Assert.AreEqual(0, e.Lives, "moving the clock back restarts the wait");
            e.Tick(Noon - 7200 + 1800);
            Assert.AreEqual(1, e.Lives);
        }

        [Test]
        public void ANewDay_RefillsLives_AndResetsAdLimits()
        {
            var e = Fresh();
            for (int i = 0; i < 4; i++) e.LoseLife(Noon);
            Assert.IsTrue(e.GrantAdLife(Noon));
            Assert.IsTrue(e.GrantAdLife(Noon));
            Assert.IsTrue(e.GrantAdLife(Noon));
            Assert.IsFalse(e.GrantAdLife(Noon), "three ad lives a day");
            Assert.AreEqual(4, e.Lives);

            e.LoseLife(Noon);
            e.LoseLife(Noon);
            e.Tick(Noon + Day);
            Assert.AreEqual(5, e.Lives);
            e.LoseLife(Noon + Day);
            Assert.IsTrue(e.CanWatchLifeAd(Noon + Day));
        }

        [Test]
        public void BuyingALife_CostsCoins_AndARefillCostsGems()
        {
            var e = Fresh();
            Assert.IsFalse(e.BuyLife(Noon), "nothing to buy when full");
            e.LoseLife(Noon);
            e.LoseLife(Noon);
            Assert.IsTrue(e.BuyLife(Noon));
            Assert.AreEqual(200, e.Coins);
            Assert.AreEqual(4, e.Lives);
            Assert.IsFalse(e.BuyLife(Noon), "not enough coins");

            Assert.IsTrue(e.RefillLives(Noon));
            Assert.AreEqual(0, e.Gems);
            Assert.AreEqual(5, e.Lives);
        }

        [Test]
        public void WinReward_GrowsWithStars_AndCoinsStopAtTheCap()
        {
            var e = Fresh();
            Assert.AreEqual(100, e.RewardWin(1));
            Assert.AreEqual(140, e.RewardWin(2));
            Assert.AreEqual(220, e.RewardWin(3));
            Assert.AreEqual(960, e.Coins);

            Assert.AreEqual(e.Rules.CoinCap - 960, e.AddCoins(1000000));
            Assert.AreEqual(e.Rules.CoinCap, e.Coins);
        }

        [Test]
        public void ExtraMoves_GetDearerEachTime()
        {
            var rules = new EconomyRules();
            Assert.AreEqual(900, rules.ExtraMovesCost(0));
            Assert.AreEqual(1350, rules.ExtraMovesCost(1));
            Assert.AreEqual(2025, rules.ExtraMovesCost(2));

            var e = Fresh();
            Assert.IsFalse(e.BuyExtraMoves(0));
            e.AddCoins(400);
            Assert.IsTrue(e.BuyExtraMoves(0));
            Assert.AreEqual(0, e.Coins);
        }

        [Test]
        public void Boosters_AreBoughtWithCoins_UpToTheStockLimit()
        {
            var e = Fresh();
            e.AddCoins(5000);
            Assert.IsTrue(e.BuyBooster(BoosterType.Hammer));
            Assert.AreEqual(4, e.Boosters(BoosterType.Hammer));
            Assert.AreEqual(4600, e.Coins);
            Assert.IsTrue(e.SpendBooster(BoosterType.Hammer));
            Assert.AreEqual(3, e.Boosters(BoosterType.Hammer));

            e.AddBoosters(BoosterType.RowBlast, 100);
            Assert.AreEqual(e.Rules.BoosterCap, e.Boosters(BoosterType.RowBlast));
            Assert.IsFalse(e.BuyBooster(BoosterType.RowBlast));
            Assert.AreEqual(4600, e.Coins, "a full stock takes no coins");
        }

        [Test]
        public void DailyGift_FollowsTheStreak_AndBreaksAfterAMissedDay()
        {
            var e = Fresh();
            Assert.IsTrue(e.DailyReady(Noon));
            Assert.AreEqual(0, e.DailyIndex(Noon));
            var first = e.ClaimDaily(Noon);
            Assert.AreEqual(1, first.Length);
            Assert.AreEqual(650, e.Coins);
            Assert.IsFalse(e.DailyReady(Noon + 3600));
            Assert.IsNull(e.ClaimDaily(Noon + 3600));
            Assert.IsFalse(e.DailyReady(Noon - Day), "the clock set back does not give a second gift");

            Assert.AreEqual(1, e.DailyIndex(Noon + Day));
            var second = e.ClaimDaily(Noon + Day);
            Assert.AreEqual(2, second.Length);
            Assert.AreEqual(850, e.Coins);
            Assert.AreEqual(4, e.Boosters(BoosterType.Hammer));
            Assert.AreEqual(2, e.Profile.DailyStreak);

            Assert.AreEqual(0, e.DailyIndex(Noon + 3 * Day), "a missed day starts over");
            e.ClaimDaily(Noon + 3 * Day);
            Assert.AreEqual(1, e.Profile.DailyStreak);
        }

        [Test]
        public void DailyGift_WrapsAroundAfterAWeek()
        {
            var e = Fresh();
            for (int day = 0; day < 7; day++) e.ClaimDaily(Noon + day * Day);
            Assert.AreEqual(60, e.Gems, "day seven brings gems");
            Assert.AreEqual(0, e.DailyIndex(Noon + 7 * Day));
            Assert.AreEqual(8, e.Profile.DailyStreak + 1);
        }

        [Test]
        public void Chest_CostsCoins_AndGivesWhatTheTableAllows()
        {
            var e = Fresh();
            e.Profile.Coins = 300;
            Assert.IsNull(e.OpenChest(true));
            Assert.AreEqual(0, e.Profile.ChestsOpened);

            for (int i = 0; i < 40; i++)
            {
                e.Profile.Coins = 0;
                for (int b = 0; b < Profile.BoosterKinds; b++) e.Profile.Boosters[b] = 0;
                int coins = e.Coins, gems = e.Gems, packs = e.Profile.Packs;
                int boosters = e.Profile.Boosters.Sum();
                var reward = e.OpenChest(false);
                Assert.IsTrue(reward.HasValue);
                var r = reward.Value;
                var drop = e.Rules.ChestDrops.First(d => d.Kind == r.Kind && (r.Kind != RewardKind.Booster || d.Booster == r.Booster));
                Assert.GreaterOrEqual(r.Amount, drop.Min / 10 * 10);
                Assert.LessOrEqual(r.Amount, drop.Max);
                int gained = (e.Coins - coins) + (e.Gems - gems) + (e.Profile.Boosters.Sum() - boosters) + (e.Profile.Packs - packs);
                Assert.Greater(gained, 0, "every chest gives something");
            }
            Assert.AreEqual(40, e.Profile.ChestsOpened);
        }

        [Test]
        public void Chest_IsTheSame_ForTheSameSeedAndCount()
        {
            var a = Fresh(99);
            var b = Fresh(99);
            for (int i = 0; i < 10; i++) Assert.AreEqual(a.OpenChest(false).ToString(), b.OpenChest(false).ToString());
        }

        [Test]
        public void Quests_AreThreeDifferentKindsADay_AndTheSameForEveryoneThatDay()
        {
            var a = Fresh(1);
            var b = Fresh(2);
            var today = a.Quests(Noon);
            Assert.AreEqual(3, today.Length);
            Assert.AreEqual(3, today.Select(q => q.Kind).Distinct().Count());
            CollectionAssert.AreEqual(today.Select(q => q.Kind + ":" + q.Target).ToArray(), b.Quests(Noon).Select(q => q.Kind + ":" + q.Target).ToArray());

            bool differs = false;
            for (int day = 1; day <= 10 && !differs; day++)
                differs = !a.Quests(Noon + day * Day).Select(q => q.Kind + ":" + q.Target).SequenceEqual(today.Select(q => q.Kind + ":" + q.Target));
            Assert.IsTrue(differs, "the set changes from day to day");
        }

        [Test]
        public void Quests_CountOnlyTheirOwnKind_PayOnce_AndGiveABonusWhenAllAreDone()
        {
            var e = Fresh();
            var quests = e.Quests(Noon);
            var kind = quests[0].Kind;
            Assert.AreEqual(0, e.ClaimQuest(0, Noon), "not done yet");

            e.Report(kind, quests[0].Target + 50, Noon);
            var after = e.Quests(Noon);
            Assert.AreEqual(quests[0].Target, after[0].Progress, "progress stops at the target");
            Assert.IsTrue(after[0].Done);
            Assert.AreEqual(0, after[1].Progress);
            Assert.AreEqual(1, e.ClaimableQuests(Noon));

            int coins = e.Coins;
            Assert.AreEqual(quests[0].Coins, e.ClaimQuest(0, Noon));
            Assert.AreEqual(coins + quests[0].Coins, e.Coins);
            Assert.AreEqual(0, e.ClaimQuest(0, Noon), "paid once");
            Assert.IsFalse(e.QuestBonusReady(Noon));

            for (int i = 1; i < quests.Length; i++)
            {
                e.Report(quests[i].Kind, quests[i].Target, Noon);
                Assert.Greater(e.ClaimQuest(i, Noon), 0);
            }
            Assert.IsTrue(e.QuestBonusReady(Noon));
            Assert.AreEqual(e.Rules.AllQuestsGems, e.ClaimQuestBonus(Noon));
            Assert.AreEqual(0, e.ClaimQuestBonus(Noon));

            Assert.AreEqual(0, e.Quests(Noon + Day)[0].Progress, "a new day starts from zero");
            Assert.IsFalse(e.Quests(Noon + Day)[0].Claimed);
        }

        [Test]
        public void Profile_SurvivesSavingAndLoading()
        {
            var e = Fresh(1234567890123L);
            e.LoseLife(Noon);
            e.ClaimDaily(Noon);
            e.Report(e.Quests(Noon)[1].Kind, 2, Noon);
            e.OpenChest(false);
            e.AddBoosters(BoosterType.ColumnBlast, 4);
            int columns = e.Boosters(BoosterType.ColumnBlast);

            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(e.Profile.ToJson(), loaded.ToJson());
            Assert.AreEqual(1234567890123L, loaded.Seed);
            Assert.AreEqual(e.Profile.LifeAnchor, loaded.LifeAnchor);
            Assert.AreEqual(columns, loaded.Boosters[(int)BoosterType.ColumnBlast]);

            var empty = Profile.FromJson("{}", e.Rules);
            Assert.AreEqual(500, empty.Coins);
            Assert.AreEqual(3, empty.Boosters[0]);
        }

        [Test]
        public void Scenes_AreMarkedOnce_AndKept()
        {
            var e = Fresh();
            Assert.IsFalse(e.SceneSeen(1));
            Assert.IsTrue(e.MarkSceneSeen(1));
            Assert.IsFalse(e.MarkSceneSeen(1), "a scene plays once");
            Assert.IsTrue(e.SceneSeen(1));
            Assert.IsFalse(e.SceneSeen(0));
            Assert.IsFalse(e.MarkSceneSeen(-1));
            Assert.IsFalse(e.MarkSceneSeen(40));
            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(2, loaded.ScenesSeen);
            Assert.AreEqual(0, Profile.FromJson("{\"coins\":10}", e.Rules).ScenesSeen);
        }

        static CardCollection Cards(Economy e) => new CardCollection(new CollectionRules(), e);

        [Test]
        public void CardPack_GivesThreeCards_AndIsTheSameForTheSameSeed()
        {
            var a = Fresh(41);
            var b = Fresh(41);
            var cards = Cards(a);
            Assert.AreEqual(2, cards.Packs, "two packs to start with");
            var first = cards.OpenPack();
            var same = Cards(b).OpenPack();
            Assert.AreEqual(3, first.Length);
            CollectionAssert.AreEqual(first.Select(d => d.Card).ToArray(), same.Select(d => d.Card).ToArray());
            Assert.AreEqual(1, cards.Packs);
            Assert.AreEqual(1, a.Profile.PacksOpened);
            Assert.GreaterOrEqual(cards.TotalOwned, 1);
            Assert.LessOrEqual(cards.TotalOwned, 3);

            cards.OpenPack();
            Assert.IsNull(cards.OpenPack(), "no packs left");
        }

        [Test]
        public void CardPack_PaysCoinsForDoubles_AndTheCollectionCanBeFinished()
        {
            var e = Fresh(7);
            var cards = Cards(e);
            int opened = 0, doubles = 0;
            e.Profile.Coins = 0;
            while (!cards.Complete && opened < 400)
            {
                cards.AddPacks(1);
                foreach (var draw in cards.OpenPack())
                {
                    if (draw.IsNew) Assert.AreEqual(0, draw.Coins);
                    else
                    {
                        doubles++;
                        Assert.AreEqual(cards.Rules.DuplicateCoinsPerStar * cards.Rules.Card(draw.Card).Stars, draw.Coins);
                    }
                }
                e.Profile.Coins = 0;
                opened++;
            }
            Assert.IsTrue(cards.Complete, "every card turns up in the end");
            Assert.Less(opened, 400);
            Assert.Greater(doubles, 0);
            Assert.AreEqual(cards.Rules.CardCount, cards.TotalOwned);
        }

        [Test]
        public void CardSet_PaysOnceWhenComplete_AndTheGrandPrizeNeedsEverySet()
        {
            var e = Fresh();
            var cards = Cards(e);
            Assert.IsNull(cards.ClaimSet(0), "not complete yet");
            for (int c = 0; c < cards.Rules.Sets[0].Cards.Length - 1; c++) e.Profile.Cards[cards.Rules.IndexOf(0, c)] = 1;
            Assert.AreEqual(5, cards.OwnedInSet(0));
            Assert.IsFalse(cards.SetComplete(0));
            e.Profile.Cards[cards.Rules.IndexOf(0, 5)] = 2;
            Assert.IsTrue(cards.SetComplete(0));
            Assert.AreEqual(1, cards.Claimable);

            int coins = e.Coins, hammers = e.Boosters(BoosterType.Hammer);
            Assert.IsNotNull(cards.ClaimSet(0));
            Assert.AreEqual(coins + 300, e.Coins);
            Assert.AreEqual(hammers + 1, e.Boosters(BoosterType.Hammer));
            Assert.IsNull(cards.ClaimSet(0), "paid once");
            Assert.IsNull(cards.ClaimGrand());

            for (int i = 0; i < e.Profile.Cards.Length; i++) e.Profile.Cards[i] = 1;
            Assert.IsTrue(cards.Complete);
            int gems = e.Gems;
            Assert.IsNotNull(cards.ClaimGrand());
            Assert.AreEqual(gems + 25, e.Gems);
            Assert.IsNull(cards.ClaimGrand());
        }

        [Test]
        public void Packs_ComeFromFirstWins_QuestsAndGifts_AndSurviveSaving()
        {
            var e = Fresh();
            int packs = e.Profile.Packs;
            e.RewardWin(3, true);
            Assert.AreEqual(packs + 1, e.Profile.Packs);
            e.RewardWin(3, false);
            Assert.AreEqual(packs + 1, e.Profile.Packs, "a replay brings no pack");
            e.Grant(Reward.Pack(2));
            Assert.AreEqual(packs + 3, e.Profile.Packs);

            var cards = Cards(e);
            cards.OpenPack();
            var loaded = Profile.FromJson(e.Profile.ToJson(), e.Rules);
            Assert.AreEqual(e.Profile.Packs, loaded.Packs);
            Assert.AreEqual(1, loaded.PacksOpened);
            CollectionAssert.AreEqual(e.Profile.Cards, loaded.Cards);
            Assert.AreEqual(cards.TotalOwned, new CardCollection(cards.Rules, new Economy(e.Rules, loaded)).TotalOwned);
        }
    }
}
