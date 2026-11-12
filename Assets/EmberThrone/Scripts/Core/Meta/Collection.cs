using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class CardDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Stars;

        public CardDef(string id, string name, int stars)
        {
            Id = id;
            Name = name;
            Stars = stars;
        }
    }

    public sealed class CardSet
    {
        public readonly string Id;
        public readonly string Name;
        public readonly CardDef[] Cards;
        public readonly Reward[] Rewards;

        public CardSet(string id, string name, Reward[] rewards, params CardDef[] cards)
        {
            Id = id;
            Name = name;
            Rewards = rewards;
            Cards = cards;
        }
    }

    public sealed class CollectionRules
    {
        public const int MaxStars = 3;

        public int CardsPerPack = 3;
        public int[] StarWeights = { 70, 25, 5 };
        public int NewCardChancePercent = 60;
        public int DuplicateCoinsPerStar = 15;
        public Reward[] GrandPrize = { Reward.Coins(3000), Reward.Gems(25) };

        public CardSet[] Sets =
        {
            new CardSet("sofra", "Köy Sofrası", new[] { Reward.Coins(300), Reward.Of(BoosterType.Hammer) },
                new CardDef("ekmek", "Köy Ekmeği", 1), new CardDef("simit", "Simit", 1), new CardDef("kurabiye", "Kurabiye", 1),
                new CardDef("recel", "Reçel", 2), new CardDef("borek", "Börek", 2), new CardDef("caydanlik", "Çaydanlık", 3)),
            new CardSet("demirci", "Demirci Tezgahı", new[] { Reward.Coins(400), Reward.Of(BoosterType.RowBlast) },
                new CardDef("nal", "Nal", 1), new CardDef("tokmak", "Tokmak", 1), new CardDef("masa", "Maşa", 1),
                new CardDef("koruk", "Körük", 2), new CardDef("ors", "Örs", 2), new CardDef("migfer", "Miğfer", 3)),
            new CardSet("fenerci", "Fenerci Çantası", new[] { Reward.Coins(400), Reward.Of(BoosterType.ColumnBlast) },
                new CardDef("kibrit", "Kibrit", 1), new CardDef("fitil", "Fitil", 1), new CardDef("atki", "Atkı", 1),
                new CardDef("yag", "Kandil Yağı", 2), new CardDef("pusula", "Pusula", 2), new CardDef("fener", "Pirinç Fener", 3)),
            new CardSet("bahce", "Bahçe", new[] { Reward.Coins(500), Reward.Of(BoosterType.Shuffle) },
                new CardDef("aycicegi", "Ayçiçeği", 1), new CardDef("kova", "Sulama Kovası", 1), new CardDef("elma", "Elma Sepeti", 1),
                new CardDef("balkabagi", "Balkabağı", 2), new CardDef("kovan", "Arı Kovanı", 2), new CardDef("korkuluk", "Korkuluk", 3)),
            new CardSet("ayaz", "Ayaz'ın Hileleri", new[] { Reward.Coins(600), Reward.Of(BoosterType.Hammer, 2) },
                new CardDef("kartopu", "Kartopu", 1), new CardDef("sarkit", "Buz Sarkıtı", 1), new CardDef("eldiven", "Eldiven", 1),
                new CardDef("kure", "Kar Küresi", 2), new CardDef("can", "Donmuş Çan", 2), new CardDef("asa", "Buz Asası", 3)),
            new CardSet("kor", "Kor Hazineleri", new[] { Reward.Coins(800), Reward.Gems(5) },
                new CardDef("mum", "Mum", 1), new CardDef("komur", "Kor Parçası", 1), new CardDef("atesbocegi", "Ateş Böceği", 1),
                new CardDef("kolye", "Kor Kolye", 2), new CardDef("kupa", "Kor Kupası", 2), new CardDef("kalp", "Kor Kalbi", 3)),
        };

        public int CardCount
        {
            get
            {
                int n = 0;
                foreach (var set in Sets) n += set.Cards.Length;
                return n;
            }
        }

        public int IndexOf(int set, int card)
        {
            int index = 0;
            for (int s = 0; s < set; s++) index += Sets[s].Cards.Length;
            return index + card;
        }

        public CardDef Card(int index) => Sets[SetOf(index)].Cards[index - IndexOf(SetOf(index), 0)];

        public int SetOf(int index)
        {
            for (int s = 0; s < Sets.Length; s++)
            {
                if (index < Sets[s].Cards.Length) return s;
                index -= Sets[s].Cards.Length;
            }
            return Sets.Length - 1;
        }
    }

    public readonly struct CardDraw
    {
        public readonly int Card;
        public readonly bool IsNew;
        public readonly int Coins;

        public CardDraw(int card, bool isNew, int coins)
        {
            Card = card;
            IsNew = isNew;
            Coins = coins;
        }
    }

    public sealed class CardCollection
    {
        const ulong PackStream = 0xD1B54A32D192ED03UL;

        public readonly CollectionRules Rules;
        readonly Economy _economy;

        Profile Profile => _economy.Profile;

        public CardCollection(CollectionRules rules, Economy economy)
        {
            Rules = rules;
            _economy = economy;
            Fit();
        }

        void Fit()
        {
            int cards = Rules.CardCount;
            if (Profile.Cards.Length != cards)
            {
                var grown = new int[cards];
                System.Array.Copy(Profile.Cards, grown, System.Math.Min(cards, Profile.Cards.Length));
                Profile.Cards = grown;
            }
            if (Profile.SetsClaimed.Length != Rules.Sets.Length)
            {
                var grown = new bool[Rules.Sets.Length];
                System.Array.Copy(Profile.SetsClaimed, grown, System.Math.Min(grown.Length, Profile.SetsClaimed.Length));
                Profile.SetsClaimed = grown;
            }
        }

        public int Packs => Profile.Packs;

        public int Owned(int card) => Profile.Cards[card];

        public int OwnedInSet(int set)
        {
            int n = 0;
            for (int c = 0; c < Rules.Sets[set].Cards.Length; c++)
                if (Profile.Cards[Rules.IndexOf(set, c)] > 0) n++;
            return n;
        }

        public bool SetComplete(int set) => OwnedInSet(set) >= Rules.Sets[set].Cards.Length;

        public bool SetClaimed(int set) => Profile.SetsClaimed[set];

        public int TotalOwned
        {
            get
            {
                int n = 0;
                foreach (int count in Profile.Cards)
                    if (count > 0) n++;
                return n;
            }
        }

        public bool Complete => TotalOwned >= Rules.CardCount;

        public bool GrandClaimed => Profile.GrandClaimed;

        public int Claimable
        {
            get
            {
                int n = 0;
                for (int s = 0; s < Rules.Sets.Length; s++)
                    if (SetComplete(s) && !SetClaimed(s)) n++;
                if (Complete && !GrandClaimed) n++;
                return n;
            }
        }

        public void AddPacks(int count)
        {
            if (count > 0) Profile.Packs += count;
        }

        public CardDraw[] OpenPack()
        {
            if (Profile.Packs <= 0) return null;
            Profile.Packs--;
            var rng = new Pcg32((ulong)Profile.Seed, PackStream + (ulong)Profile.PacksOpened * 2UL);
            Profile.PacksOpened++;

            var draws = new CardDraw[Rules.CardsPerPack];
            for (int i = 0; i < draws.Length; i++)
            {
                bool wantNew = i == 0 && rng.Next(100) < Rules.NewCardChancePercent;
                int card = Pick(rng, wantNew);
                bool isNew = Profile.Cards[card] == 0;
                Profile.Cards[card]++;
                int coins = isNew ? 0 : _economy.AddCoins(Rules.DuplicateCoinsPerStar * Rules.Card(card).Stars);
                draws[i] = new CardDraw(card, isNew, coins);
            }
            return draws;
        }

        int Pick(Pcg32 rng, bool onlyMissing)
        {
            var pool = new List<int>();
            int total = 0;
            for (int pass = 0; pass < 2 && pool.Count == 0; pass++)
            {
                bool missing = onlyMissing && pass == 0;
                for (int card = 0; card < Profile.Cards.Length; card++)
                {
                    if (missing && Profile.Cards[card] > 0) continue;
                    pool.Add(card);
                    total += Weight(card);
                }
            }
            int roll = rng.Next(total);
            foreach (int card in pool)
            {
                roll -= Weight(card);
                if (roll < 0) return card;
            }
            return pool[pool.Count - 1];
        }

        int Weight(int card)
        {
            int stars = Rules.Card(card).Stars;
            return Rules.StarWeights[System.Math.Min(stars, Rules.StarWeights.Length) - 1];
        }

        public Reward[] ClaimSet(int set)
        {
            if (set < 0 || set >= Rules.Sets.Length || Profile.SetsClaimed[set] || !SetComplete(set)) return null;
            Profile.SetsClaimed[set] = true;
            foreach (var reward in Rules.Sets[set].Rewards) _economy.Grant(reward);
            return Rules.Sets[set].Rewards;
        }

        public Reward[] ClaimGrand()
        {
            if (Profile.GrandClaimed || !Complete) return null;
            Profile.GrandClaimed = true;
            foreach (var reward in Rules.GrandPrize) _economy.Grant(reward);
            return Rules.GrandPrize;
        }
    }
}
