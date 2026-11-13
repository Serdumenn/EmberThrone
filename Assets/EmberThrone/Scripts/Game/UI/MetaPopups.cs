using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public static class Rows
    {
        public static VisualElement Row(VisualElement parent, string icon, string title, string sub, out Label subLabel, string name)
        {
            var row = Ui.El("et-row", parent, name);
            Ui.El("et-icon et-row__icon " + icon, row);
            var texts = Ui.El("et-row__texts", row);
            Ui.Text(title, "et-row__title", texts, "Title");
            subLabel = Ui.Text(sub, "et-caption et-row__sub", texts, "Sub");
            return row;
        }

        public static Button Price(VisualElement parent, string label, string icon, ButtonKind kind, Action onClick, string name = "Buy")
        {
            var button = Ui.Button(null, Ui.ClassOf(kind) + " et-btn--small", onClick, parent, name);
            Ui.Text(label, "et-btn__label", button, "Label");
            if (icon != null) Ui.El("et-icon et-btn__icon " + icon, button);
            return button;
        }

        public static string LabelOf(Button button) => button.Q<Label>("Label").text;
    }

    public sealed class ShopPopup : Popup
    {
        readonly Dictionary<BoosterType, Button> _buy = new Dictionary<BoosterType, Button>();
        readonly Dictionary<BoosterType, Label> _owned = new Dictionary<BoosterType, Label>();
        Label _balance;
        Label _adSub;
        AdService _ads;
        PurchaseService _purchases;
        ScrollView _list;
        readonly Dictionary<string, Button> _offers = new Dictionary<string, Button>();
        readonly Dictionary<string, Label> _offerSubs = new Dictionary<string, Label>();

        public void Reveal(VisualElement e) => _list?.ScrollTo(e);

        public Button OfferButton(string productId) => _offers.TryGetValue(productId, out var b) ? b : null;
        public string OfferSub(string productId) => _offerSubs.TryGetValue(productId, out var l) ? l.text : null;
        Action<string, Reward[], bool> _showRewards;

        public Button ChestButton { get; private set; }
        public Button AdButton { get; private set; }
        public Button BuyButton(BoosterType type) => _buy[type];
        public string BalanceText => _balance.text;

        public static ShopPopup Show(VisualElement parent, AdService ads, PurchaseService purchases, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new ShopPopup { _ads = ads, _purchases = purchases, _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Shop", "Mağaza", closed);
            var balance = Ui.El("et-balance", Panel, "Balance");
            Ui.El("et-icon et-balance__icon et-ic--coin", balance);
            _balance = Ui.Text("", "et-ink et-balance__text", balance, "Coins");

            var list = new ScrollView(ScrollViewMode.Vertical) { name = "Items" };
            list.AddToClassList("et-shop__list");
            Panel.Add(list);
            _list = list;
            foreach (var product in Meta.Rules.Store.Products)
            {
                if (product.Kind == ProductKind.Starter && Meta.Economy.Profile.StarterBought) continue;
                var p = product;
                var row = Rows.Row(list, IconOfProduct(p), p.Title, "", out var sub, p.Id);
                _offerSubs[p.Id] = sub;
                _offers[p.Id] = Rows.Price(row, _purchases != null ? _purchases.PriceOf(p) : p.Price, null, ButtonKind.Cta, () => BuyProduct(p.Id));
            }

            foreach (var type in BoosterInventory.All)
            {
                var t = type;
                var row = Rows.Row(list, "et-ic--" + BoosterInventory.Slug(type), BoosterInventory.Name(type), "", out var owned, type.ToString());
                _owned[type] = owned;
                _buy[type] = Rows.Price(row, Meta.Rules.BoosterPrice(type).ToString(), "et-ic--coin", ButtonKind.Cta, () => Buy(t));
            }

            var chest = Rows.Row(list, "et-ic--chest", "Sandık", "Sürpriz ödül", out _, "Chest");
            ChestButton = Rows.Price(chest, Meta.Rules.ChestPrice.ToString(), "et-ic--coin", ButtonKind.Cta, OpenChest);

            var ad = Rows.Row(list, "et-ic--video", "Bedava altın", "", out _adSub, "FreeCoins");
            AdButton = Rows.Price(ad, "İzle", null, ButtonKind.Secondary, WatchAd, "Watch");

            AddClose();
            Refresh();
            Meta.Changed += Refresh;
        }

        protected override void OnClosed() => Meta.Changed -= Refresh;

        void Refresh()
        {
            if (!IsOpen) return;
            var economy = Meta.Economy;
            _balance.text = economy.Coins.ToString();
            foreach (var type in BoosterInventory.All)
            {
                int count = economy.Boosters(type);
                _owned[type].text = "Sende: " + count;
                _buy[type].SetEnabled(count < Meta.Rules.BoosterCap && economy.Coins >= Meta.Rules.BoosterPrice(type));
            }
            ChestButton.SetEnabled(economy.Coins >= Meta.Rules.ChestPrice);
            int left = Meta.Rules.AdCoinsPerDay - economy.Profile.AdCoins;
            _adSub.text = "+" + Meta.Rules.AdCoins + " altın · bugün " + Mathf.Max(0, left) + " hak";
            AdButton.SetEnabled(_ads != null && _ads.IsRewardedReady && economy.CanWatchCoinAd(Meta.Now));
            long now = Meta.Now;
            foreach (var product in Meta.Rules.Store.Products)
            {
                if (!_offers.TryGetValue(product.Id, out var button)) continue;
                _offerSubs[product.Id].text = SubOf(product, now);
                bool ready = _purchases != null && _purchases.IsReady && !_purchases.IsPurchasing;
                if (product.Kind == ProductKind.Piggy) ready &= economy.CanBreakPiggy;
                if (product.Kind == ProductKind.Pass) ready &= !economy.PassPremium(now);
                if (product.Kind == ProductKind.Starter) ready &= !economy.Profile.StarterBought;
                button.SetEnabled(ready);
            }
        }

        static string IconOfProduct(StoreProduct product)
        {
            switch (product.Kind)
            {
                case ProductKind.Starter: return "et-ic--starter";
                case ProductKind.Pass: return "et-ic--pass";
                case ProductKind.Piggy: return "et-ic--piggy";
                case ProductKind.Gems: return product.Rewards[0].Amount >= 500 ? "et-ic--gems-pile" : "et-ic--gem";
                case ProductKind.Coins: return "et-ic--coins-pile";
                default: return "et-ic--booster-crate";
            }
        }

        static string SubOf(StoreProduct product, long now)
        {
            var economy = Meta.Economy;
            var store = Meta.Rules.Store;
            switch (product.Kind)
            {
                case ProductKind.Starter: return "Yalnız bir kez";
                case ProductKind.Pass: return economy.PassPremium(now) ? "Bu ay açık" : "Büyük ödüller, " + store.PassMaxLives + " can";
                case ProductKind.Piggy:
                    return economy.CanBreakPiggy ? "İçinde " + economy.Profile.PiggyCoins + " altın" : economy.Profile.PiggyCoins + "/" + store.PiggyMin + " altın, kazandıkça dolar";
                case ProductKind.Boosters: return product.Rewards.Length + " çeşit booster";
                default: return "";
            }
        }

        void BuyProduct(string productId)
        {
            if (_purchases == null || !_purchases.Buy(productId)) return;
            Refresh();
        }

        void Buy(BoosterType type)
        {
            if (!Meta.Economy.BuyBooster(type)) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
            Bump(_owned[type]);
        }

        void OpenChest()
        {
            var reward = Meta.Economy.OpenChest(true);
            if (!reward.HasValue) return;
            Meta.Save();
            _showRewards("Sandık", new[] { reward.Value }, true);
        }

        void WatchAd()
        {
            if (_ads == null || !Meta.Economy.CanWatchCoinAd(Meta.Now)) return;
            _ads.ShowRewarded(() =>
            {
                int coins = Meta.Economy.GrantAdCoins(Meta.Now);
                Meta.Save();
                if (coins > 0) _showRewards("Ödül!", new[] { Reward.Coins(coins) }, false);
            }, null);
        }
    }

    public sealed class LivesPopup : Popup
    {
        Label _count;
        Label _status;
        AdService _ads;

        public Button AdButton { get; private set; }
        public Button BuyButton { get; private set; }
        public Button RefillButton { get; private set; }
        public string CountText => _count.text;
        public string StatusText => _status.text;

        public static LivesPopup Show(VisualElement parent, AdService ads, Action closed)
        {
            var popup = new LivesPopup { _ads = ads };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Lives", "Canlar", closed);
            var heart = Ui.El("et-icon et-lives__heart et-ic--heart", Panel, "Heart");
            _count = Ui.Text("", "et-outlined et-lives__count", heart, "Count");
            _status = Ui.Text("", "et-ink et-result__message", Panel, "Status");

            AdButton = Ui.Button("İzle: +1 can", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", WatchAd, Panel, "Watch");
            var buttons = Ui.El("et-lives__buttons", Panel, "Buttons");
            BuyButton = Rows.Price(buttons, "+1 can  " + Meta.Rules.LifeCoinPrice, "et-ic--coin", ButtonKind.Secondary, Buy);
            RefillButton = Rows.Price(buttons, "Doldur  " + Meta.Rules.RefillGemPrice, "et-ic--gem", ButtonKind.Secondary, Refill, "Refill");
            AddClose("Kapat");

            Refresh();
            Meta.Changed += Refresh;
            Root.schedule.Execute(Refresh).Every(500);
        }

        protected override void OnClosed() => Meta.Changed -= Refresh;

        void Refresh()
        {
            if (!IsOpen) return;
            var economy = Meta.Economy;
            long now = Meta.Now;
            _count.text = economy.Lives.ToString();
            _status.text = economy.LivesFull ? "Canların dolu!" : "Sonraki can: " + Meta.Countdown(economy.SecondsToNextLife(now));
            bool ad = _ads != null && _ads.IsRewardedReady && economy.CanWatchLifeAd(now);
            AdButton.EnableInClassList(Ui.Hidden, !ad);
            BuyButton.SetEnabled(!economy.LivesFull && economy.Coins >= Meta.Rules.LifeCoinPrice);
            RefillButton.SetEnabled(!economy.LivesFull && economy.Gems >= Meta.Rules.RefillGemPrice);
        }

        void WatchAd()
        {
            if (_ads == null || !Meta.Economy.CanWatchLifeAd(Meta.Now)) return;
            _ads.ShowRewarded(() =>
            {
                if (Meta.Economy.GrantAdLife(Meta.Now)) Gained();
            }, null);
        }

        void Buy()
        {
            if (Meta.Economy.BuyLife(Meta.Now)) Gained();
        }

        void Refill()
        {
            if (Meta.Economy.RefillLives(Meta.Now)) Gained();
        }

        void Gained()
        {
            Sound.Play(Sfx.MovesAdded);
            Meta.Save();
            if (IsOpen) Bump(_count, 0.35f);
        }
    }

    public sealed class DailyPopup : Popup
    {
        Action<string, Reward[], bool> _showRewards;

        public Button ClaimButton { get; private set; }
        public int Today { get; private set; }

        public static DailyPopup Show(VisualElement parent, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new DailyPopup { _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Daily", "Hediye", closed);
            var economy = Meta.Economy;
            long now = Meta.Now;
            bool ready = economy.DailyReady(now);
            Today = economy.DailyIndex(now);

            Ui.Text(ready ? "Her gün yeni bir hediye!" : "Bugünkü hediyeni aldın.", "et-ink et-result__message", Panel, "Message");
            var days = Ui.El("et-days", Panel, "Days");
            var gifts = Meta.Rules.DailyGifts;
            for (int i = 0; i < gifts.Length; i++)
            {
                bool claimed = i < Today || (i == Today && !ready);
                string state = claimed ? " et-day--claimed" : i == Today ? " et-day--today" : "";
                var cell = Ui.El("et-day" + state + (i == gifts.Length - 1 ? " et-day--wide" : ""), days, "Day" + (i + 1));
                Ui.Text((i + 1) + ". gün", "et-caption et-day__title", cell);
                var icons = Ui.El("et-day__icons", cell);
                foreach (var gift in gifts[i]) Ui.El("et-icon et-day__icon " + IconOf(gift), icons);
                Ui.Text(gifts[i][0].Amount.ToString(), "et-ink et-day__amount", cell);
                if (claimed) Ui.El("et-check et-day__check", cell);
            }

            if (ready) ClaimButton = Ui.Button("Al", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Claim, Panel, "Claim");
            AddClose(ready ? "Sonra" : "Tamam");
        }

        void Claim()
        {
            var gifts = Meta.Economy.ClaimDaily(Meta.Now);
            if (gifts == null) return;
            Meta.Save();
            Close();
            _showRewards("Hediye", gifts, false);
        }
    }

    public sealed class QuestsPopup : Popup
    {
        readonly List<Button> _claims = new List<Button>();
        readonly List<Label> _progress = new List<Label>();
        readonly List<VisualElement> _fills = new List<VisualElement>();
        readonly List<VisualElement> _checks = new List<VisualElement>();
        Label _resets;
        VisualElement _bonusCheck;

        public Button BonusButton { get; private set; }
        public IReadOnlyList<Button> ClaimButtons => _claims;
        public string ProgressText(int index) => _progress[index].text;

        public static QuestsPopup Show(VisualElement parent, Action closed)
        {
            var popup = new QuestsPopup();
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Quests", "Görevler", closed);
            _resets = Ui.Text("", "et-caption et-ink et-ink--muted et-quests__resets", Panel, "Resets");

            var quests = Meta.Economy.Quests(Meta.Now);
            for (int i = 0; i < quests.Length; i++)
            {
                int index = i;
                var q = quests[i];
                var row = Ui.El("et-row et-row--tall", Panel, "Quest" + (i + 1));
                Ui.El("et-icon et-row__icon " + IconOf(q.Kind), row);
                var texts = Ui.El("et-row__texts", row);
                Ui.Text(TitleOf(q), "et-row__title", texts, "Title");
                var bar = Ui.El("et-progress", texts, "Progress");
                _fills.Add(Ui.El("et-progress__fill", bar, "Fill"));
                _progress.Add(Ui.Text("", "et-progress__text", bar, "Text"));
                _claims.Add(Rows.Price(row, "+" + q.Coins, "et-ic--coin", ButtonKind.Cta, () => Claim(index), "Claim"));
                _checks.Add(Ui.El("et-check et-row__check", row, "Check"));
            }

            var bonus = Rows.Row(Panel, "et-ic--gift", "Hepsini bitir", "Günün ödülü", out _, "Bonus");
            BonusButton = Rows.Price(bonus, "+" + Meta.Rules.AllQuestsGems, "et-ic--gem", ButtonKind.Cta, ClaimBonus, "Claim");
            _bonusCheck = Ui.El("et-check et-row__check", bonus, "Check");

            AddClose();
            Refresh();
            Meta.Changed += Refresh;
            Root.schedule.Execute(Refresh).Every(1000);
        }

        protected override void OnClosed() => Meta.Changed -= Refresh;

        public static string IconOf(QuestKind kind)
        {
            switch (kind)
            {
                case QuestKind.WinLevels: return "et-ic--trophy";
                case QuestKind.EarnStars: return "et-ic--star";
                case QuestKind.CollectGems: return "et-ic--piece";
                case QuestKind.FireSpecials: return "et-ic--rocket";
                default: return "et-ic--box";
            }
        }

        public static string TitleOf(Quest quest)
        {
            switch (quest.Kind)
            {
                case QuestKind.WinLevels: return quest.Target + " seviye kazan";
                case QuestKind.EarnStars: return quest.Target + " yıldız topla";
                case QuestKind.CollectGems: return quest.Target + " taş patlat";
                case QuestKind.FireSpecials: return quest.Target + " özel taş kullan";
                default: return quest.Target + " engel kır";
            }
        }

        void Refresh()
        {
            if (!IsOpen) return;
            long now = Meta.Now;
            var economy = Meta.Economy;
            var quests = economy.Quests(now);
            for (int i = 0; i < quests.Length && i < _claims.Count; i++)
            {
                var q = quests[i];
                _progress[i].text = q.Progress + "/" + q.Target;
                _fills[i].style.width = Length.Percent(100f * q.Progress / Mathf.Max(1, q.Target));
                _fills[i].EnableInClassList(Ui.Hidden, q.Progress <= 0);
                _claims[i].EnableInClassList(Ui.Hidden, q.Claimed);
                _claims[i].SetEnabled(q.Done && !q.Claimed);
                _checks[i].EnableInClassList(Ui.Hidden, !q.Claimed);
            }
            bool bonusTaken = economy.Profile.QuestBonusClaimed;
            BonusButton.EnableInClassList(Ui.Hidden, bonusTaken);
            BonusButton.SetEnabled(economy.QuestBonusReady(now));
            _bonusCheck.EnableInClassList(Ui.Hidden, !bonusTaken);

            long left = EconomyRules.SecondsPerDay - now % EconomyRules.SecondsPerDay;
            _resets.text = "Yeni görevler: " + Meta.Countdown((int)left);
        }

        void Claim(int index)
        {
            if (Meta.Economy.ClaimQuest(index, Meta.Now) <= 0) return;
            Sound.Play(Sfx.Collect);
            Meta.Save();
        }

        void ClaimBonus()
        {
            if (Meta.Economy.ClaimQuestBonus(Meta.Now) <= 0) return;
            Sound.Play(Sfx.Star);
            Meta.Save();
        }
    }

    public sealed class RewardPopup : Popup
    {
        public const float ChestShake = 0.8f;
        public const float ChestSquash = 0.14f;
        const float RaySpin = 6f;
        const float GlowTime = 0.55f;
        const float PopTime = 0.45f;
        const float ConfettiTime = 1.4f;
        const int ConfettiCount = 34;

        readonly List<VisualElement> _tiles = new List<VisualElement>();
        Reward[] _rewards;
        Vector2[] _origins;

        public int Count { get; private set; }
        public bool Opened { get; private set; }
        public Reward[] Rewards => _rewards;
        public Vector2 OriginOf(int index) => _origins != null && index < _origins.Length ? _origins[index] : Vector2.zero;

        public static float ChestTime => Timing.ModalIn + ChestShake + ChestSquash;

        public static RewardPopup Show(VisualElement parent, string title, Reward[] rewards, bool chest, Action closed)
        {
            var popup = new RewardPopup();
            popup.Build(parent, title, rewards, chest, closed);
            return popup;
        }

        void Build(VisualElement parent, string title, Reward[] rewards, bool chest, Action closed)
        {
            Open(parent, "Rewards", title, closed);
            Count = rewards.Length;
            _rewards = rewards;
            Opened = !chest;

            var rays = Ui.El("et-fx-rays et-reward__rays", null, "Rays");
            Body.Insert(0, rays);
            float angle = 0f;
            rays.schedule.Execute(t =>
            {
                angle += RaySpin * t.deltaTime / 1000f;
                rays.style.rotate = new Rotate(angle);
            }).Every(16);

            var row = Ui.El("et-rewards", Panel, "Row");
            for (int i = 0; i < rewards.Length; i++)
            {
                var tile = RewardTile(rewards[i], row);
                tile.style.scale = new Scale(Vector2.zero);
                _tiles.Add(tile);
            }

            if (chest) BuildChest(row);
            else
            {
                for (int i = 0; i < _tiles.Count; i++)
                {
                    var tile = _tiles[i];
                    float pitch = i * 2f;
                    Tweens.Run(Timing.StarFill, t =>
                    {
                        if (Ui.Alive(tile)) tile.style.scale = new Scale(Vector2.one * Ease.OutBack(t));
                    }, Timing.ModalIn + i * Timing.StarFill, null, () => Sound.Play(Sfx.Star, pitch));
                }
            }
            AddClose("Harika!", ButtonKind.Cta);
        }

        void BuildChest(VisualElement row)
        {
            var holder = Ui.El("et-fill et-reward__chest-holder", row, "ChestHolder");
            var box = Ui.El("et-icon et-reward__chest et-ic--chest", holder, "Chest");
            var glow = Ui.El("et-fx-glow et-reward__flash", row, "Flash");
            glow.style.opacity = 0f;
            Tweens.Run(ChestShake, t =>
            {
                if (!Ui.Alive(box)) return;
                float strength = 0.35f + 0.65f * t;
                box.style.rotate = new Rotate(Mathf.Sin(t * Mathf.PI * 9f) * 10f * strength);
                box.style.translate = new Translate(0f, -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 26f * strength);
            }, Timing.ModalIn, () =>
            {
                if (!Ui.Alive(box)) return;
                box.style.rotate = StyleKeyword.Null;
                box.style.translate = StyleKeyword.Null;
                Tweens.Run(ChestSquash, t =>
                {
                    if (Ui.Alive(box)) box.style.scale = new Scale(new Vector2(1f + 0.28f * t, 1f - 0.24f * t));
                }, 0f, () => Burst(box, glow));
            }, () => Sound.Play(Sfx.Shuffle));
        }

        void Burst(VisualElement box, VisualElement glow)
        {
            if (!Ui.Alive(box)) return;
            Opened = true;
            var center = box.worldBound.center;
            Sound.Play(Sfx.Firework);
            Haptics.Play(HapticLevel.Medium);
            UiBurst.Confetti(Root, center, ConfettiCount, ConfettiTime);
            Tweens.Run(GlowTime, t =>
            {
                if (!Ui.Alive(glow)) return;
                glow.style.opacity = Mathf.Sin(t * Mathf.PI);
                glow.style.scale = new Scale(Vector2.one * Mathf.Lerp(0.4f, 2.4f, Ease.OutCubic(t)));
            });
            Tweens.Run(0.22f, t =>
            {
                if (!Ui.Alive(box)) return;
                box.style.scale = new Scale(Vector2.one * Mathf.Lerp(1.3f, 1.8f, t));
                box.style.opacity = 1f - t;
            }, 0f, () =>
            {
                if (Ui.Alive(box)) box.AddToClassList(Ui.Hidden);
            });
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                var offset = center - tile.worldBound.center;
                float pitch = i * 2f;
                Tweens.Run(PopTime, t =>
                {
                    if (!Ui.Alive(tile)) return;
                    float k = Ease.OutBack(t);
                    tile.style.translate = new Translate(offset.x * (1f - k), offset.y * (1f - k));
                    tile.style.scale = new Scale(Vector2.one * k);
                }, i * 0.08f, () =>
                {
                    if (Ui.Alive(tile)) tile.style.translate = StyleKeyword.Null;
                }, () => Sound.Play(Sfx.Star, pitch));
            }
        }

        protected override void OnClosing()
        {
            _origins = new Vector2[_tiles.Count];
            var fallback = Panel.worldBound.center;
            for (int i = 0; i < _tiles.Count; i++)
            {
                var bound = _tiles[i].worldBound;
                _origins[i] = bound.width > 1f && !float.IsNaN(bound.x) ? bound.center : fallback;
            }
        }
    }

    public sealed class InfoPopup : Popup
    {
        public static InfoPopup Show(VisualElement parent, string title, string message, Action closed)
        {
            var popup = new InfoPopup();
            popup.Open(parent, "Info", title, closed);
            Ui.Text(message, "et-ink et-result__message", popup.Panel, "Message");
            popup.AddClose("Tamam", ButtonKind.Cta);
            return popup;
        }
    }

    public sealed class ConfirmPopup : Popup
    {
        public Button YesButton { get; private set; }

        public static ConfirmPopup Show(VisualElement parent, string title, string message, string yes, string no, Action confirmed, Action closed)
        {
            var popup = new ConfirmPopup();
            popup.Open(parent, "Confirm", title, closed);
            Ui.Text(message, "et-ink et-result__message", popup.Panel, "Message");
            popup.YesButton = Ui.Button(yes, Ui.ClassOf(ButtonKind.Secondary) + " et-result__button", () =>
            {
                popup.Close();
                confirmed();
            }, popup.Panel, "Yes");
            popup.AddClose(no, ButtonKind.Cta);
            return popup;
        }
    }
}
