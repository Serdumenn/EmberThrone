using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public readonly struct Quest
    {
        public readonly QuestKind Kind;
        public readonly int Target;
        public readonly int Coins;
        public readonly int Progress;
        public readonly bool Claimed;

        public Quest(QuestTemplate template, int progress, bool claimed)
        {
            Kind = template.Kind;
            Target = template.Target;
            Coins = template.Coins;
            Progress = Math.Min(progress, template.Target);
            Claimed = claimed;
        }

        public bool Done => Progress >= Target;
    }

    public sealed class Economy
    {
        const ulong ChestStream = 0x9E3779B97F4A7C15UL;

        public readonly EconomyRules Rules;
        public readonly Profile Profile;

        public Economy(EconomyRules rules, Profile profile)
        {
            Rules = rules;
            Profile = profile;
        }

        public void Tick(long now)
        {
            RollDay(now);
            RegenLives(now);
        }

        void RollDay(long now)
        {
            int day = EconomyRules.DayOf(now);
            if (day == Profile.AdDay) return;
            bool newDay = day > Profile.AdDay;
            Profile.AdDay = day;
            if (!newDay) return;
            Profile.AdLives = 0;
            Profile.AdCoins = 0;
            Profile.Lives = Math.Max(Profile.Lives, Rules.MaxLives);
            Profile.LifeAnchor = 0;
        }

        void RegenLives(long now)
        {
            var p = Profile;
            if (p.Lives >= Rules.MaxLives)
            {
                p.LifeAnchor = 0;
                return;
            }
            if (p.LifeAnchor <= 0 || now < p.LifeAnchor)
            {
                p.LifeAnchor = now;
                return;
            }
            long gained = (now - p.LifeAnchor) / Rules.LifeSeconds;
            if (gained <= 0) return;
            p.Lives = (int)Math.Min(Rules.MaxLives, p.Lives + gained);
            p.LifeAnchor = p.Lives >= Rules.MaxLives ? 0 : p.LifeAnchor + gained * Rules.LifeSeconds;
        }

        public int Coins => Profile.Coins;
        public int Gems => Profile.Gems;
        public int Lives => Profile.Lives;
        public bool LivesFull => Profile.Lives >= Rules.MaxLives;

        public int SecondsToNextLife(long now)
        {
            if (LivesFull) return 0;
            if (Profile.LifeAnchor <= 0 || now < Profile.LifeAnchor) return Rules.LifeSeconds;
            long left = Rules.LifeSeconds - (now - Profile.LifeAnchor) % Rules.LifeSeconds;
            return (int)left;
        }

        public bool LoseLife(long now)
        {
            Tick(now);
            if (Profile.Lives <= 0) return false;
            if (LivesFull) Profile.LifeAnchor = now;
            Profile.Lives--;
            return true;
        }

        void AddLives(int amount)
        {
            Profile.Lives = Math.Min(Rules.MaxLives, Profile.Lives + amount);
            if (LivesFull) Profile.LifeAnchor = 0;
        }

        public bool BuyLife(long now)
        {
            Tick(now);
            if (LivesFull || !SpendCoins(Rules.LifeCoinPrice)) return false;
            AddLives(1);
            return true;
        }

        public bool RefillLives(long now)
        {
            Tick(now);
            if (LivesFull || !SpendGems(Rules.RefillGemPrice)) return false;
            AddLives(Rules.MaxLives);
            return true;
        }

        public bool CanWatchLifeAd(long now)
        {
            Tick(now);
            return !LivesFull && Profile.AdLives < Rules.AdLivesPerDay;
        }

        public bool GrantAdLife(long now)
        {
            if (!CanWatchLifeAd(now)) return false;
            Profile.AdLives++;
            AddLives(1);
            return true;
        }

        public bool CanWatchCoinAd(long now)
        {
            Tick(now);
            return Profile.AdCoins < Rules.AdCoinsPerDay && Profile.Coins < Rules.CoinCap;
        }

        public int GrantAdCoins(long now)
        {
            if (!CanWatchCoinAd(now)) return 0;
            Profile.AdCoins++;
            return AddCoins(Rules.AdCoins);
        }

        public int AddCoins(int amount)
        {
            int before = Profile.Coins;
            Profile.Coins = Math.Min(Rules.CoinCap, before + Math.Max(0, amount));
            return Profile.Coins - before;
        }

        public bool SpendCoins(int amount)
        {
            if (amount < 0 || Profile.Coins < amount) return false;
            Profile.Coins -= amount;
            return true;
        }

        public int AddGems(int amount)
        {
            int before = Profile.Gems;
            Profile.Gems = Math.Min(Rules.GemCap, before + Math.Max(0, amount));
            return Profile.Gems - before;
        }

        public bool SpendGems(int amount)
        {
            if (amount < 0 || Profile.Gems < amount) return false;
            Profile.Gems -= amount;
            return true;
        }

        public int Boosters(BoosterType type) => Profile.Boosters[(int)type];

        public int AddBoosters(BoosterType type, int amount)
        {
            int before = Profile.Boosters[(int)type];
            Profile.Boosters[(int)type] = Math.Max(0, Math.Min(Rules.BoosterCap, before + amount));
            return Profile.Boosters[(int)type] - before;
        }

        public bool SpendBooster(BoosterType type)
        {
            if (Profile.Boosters[(int)type] <= 0) return false;
            Profile.Boosters[(int)type]--;
            return true;
        }

        public bool BuyBooster(BoosterType type)
        {
            if (Boosters(type) >= Rules.BoosterCap || !SpendCoins(Rules.BoosterPrice(type))) return false;
            AddBoosters(type, 1);
            return true;
        }

        public bool BuyExtraMoves(int taken) => SpendCoins(Rules.ExtraMovesCost(taken));

        public int RewardWin(int stars, bool firstTime = true)
        {
            if (firstTime) Profile.Packs += Rules.PacksOnFirstWin;
            return AddCoins(firstTime ? Rules.WinReward(stars) : Rules.ReplayCoins);
        }

        public void Grant(Reward reward)
        {
            switch (reward.Kind)
            {
                case RewardKind.Coins: AddCoins(reward.Amount); break;
                case RewardKind.Gems: AddGems(reward.Amount); break;
                case RewardKind.Life: AddLives(reward.Amount); break;
                case RewardKind.Pack: Profile.Packs += Math.Max(0, reward.Amount); break;
                default: AddBoosters(reward.Booster, reward.Amount); break;
            }
        }

        public bool DailyReady(long now) => EconomyRules.DayOf(now) > Profile.DailyDay;

        public int DailyIndex(long now)
        {
            int count = Rules.DailyGifts.Length;
            int day = EconomyRules.DayOf(now);
            if (!DailyReady(now)) return Math.Max(0, Profile.DailyStreak - 1) % count;
            int streak = Profile.DailyDay == day - 1 ? Profile.DailyStreak + 1 : 1;
            return (streak - 1) % count;
        }

        public Reward[] ClaimDaily(long now)
        {
            if (!DailyReady(now)) return null;
            int index = DailyIndex(now);
            int day = EconomyRules.DayOf(now);
            Profile.DailyStreak = Profile.DailyDay == day - 1 ? Profile.DailyStreak + 1 : 1;
            Profile.DailyDay = day;
            var gifts = Rules.DailyGifts[index];
            foreach (var gift in gifts) Grant(gift);
            return gifts;
        }

        public Reward? OpenChest(bool paid)
        {
            if (paid && !SpendCoins(Rules.ChestPrice)) return null;
            var rng = new Pcg32((ulong)Profile.Seed, ChestStream + (ulong)Profile.ChestsOpened * 2UL);
            Profile.ChestsOpened++;
            int total = 0;
            foreach (var drop in Rules.ChestDrops) total += drop.Weight;
            int roll = rng.Next(total);
            foreach (var drop in Rules.ChestDrops)
            {
                roll -= drop.Weight;
                if (roll >= 0) continue;
                int amount = rng.Next(drop.Min, drop.Max + 1);
                if (drop.Kind == RewardKind.Coins) amount = amount / 10 * 10;
                var reward = new Reward(drop.Kind, amount, drop.Booster);
                Grant(reward);
                return reward;
            }
            return null;
        }

        QuestTemplate[] TemplatesFor(int day)
        {
            var order = new List<QuestTemplate>(Rules.Quests);
            new Pcg32((ulong)day).Shuffle(order);
            var picked = new List<QuestTemplate>();
            foreach (var template in order)
            {
                if (picked.Count >= Rules.QuestsPerDay) break;
                bool taken = false;
                foreach (var p in picked)
                    if (p.Kind == template.Kind) taken = true;
                if (!taken) picked.Add(template);
            }
            return picked.ToArray();
        }

        QuestTemplate[] Today(long now)
        {
            int day = EconomyRules.DayOf(now);
            var templates = TemplatesFor(day);
            if (Profile.QuestDay != day || Profile.QuestProgress.Length != templates.Length || Profile.QuestClaimed.Length != templates.Length)
            {
                Profile.QuestDay = day;
                Profile.QuestProgress = new int[templates.Length];
                Profile.QuestClaimed = new bool[templates.Length];
                Profile.QuestBonusClaimed = false;
            }
            return templates;
        }

        public Quest[] Quests(long now)
        {
            var templates = Today(now);
            var quests = new Quest[templates.Length];
            for (int i = 0; i < quests.Length; i++) quests[i] = new Quest(templates[i], Profile.QuestProgress[i], Profile.QuestClaimed[i]);
            return quests;
        }

        public void Report(QuestKind kind, int amount, long now)
        {
            if (amount <= 0) return;
            var templates = Today(now);
            for (int i = 0; i < templates.Length; i++)
                if (templates[i].Kind == kind)
                    Profile.QuestProgress[i] = Math.Min(templates[i].Target, Profile.QuestProgress[i] + amount);
        }

        public int ClaimQuest(int index, long now)
        {
            var templates = Today(now);
            if (index < 0 || index >= templates.Length || Profile.QuestClaimed[index]) return 0;
            if (Profile.QuestProgress[index] < templates[index].Target) return 0;
            Profile.QuestClaimed[index] = true;
            return AddCoins(templates[index].Coins);
        }

        public int ClaimableQuests(long now)
        {
            int count = 0;
            foreach (var quest in Quests(now))
                if (quest.Done && !quest.Claimed) count++;
            if (QuestBonusReady(now)) count++;
            return count;
        }

        public bool QuestBonusReady(long now)
        {
            Today(now);
            if (Profile.QuestBonusClaimed || Profile.QuestClaimed.Length == 0) return false;
            foreach (bool claimed in Profile.QuestClaimed)
                if (!claimed) return false;
            return true;
        }

        public int ClaimQuestBonus(long now)
        {
            if (!QuestBonusReady(now)) return 0;
            Profile.QuestBonusClaimed = true;
            Profile.Packs += Rules.PacksOnAllQuests;
            return AddGems(Rules.AllQuestsGems);
        }
    }
}
