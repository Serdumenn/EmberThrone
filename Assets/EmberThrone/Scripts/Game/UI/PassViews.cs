using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class PassPopup : Popup
    {
        sealed class StepRow
        {
            public int Step;
            public Button Free;
            public Button Premium;
            public VisualElement Root;
        }

        readonly List<StepRow> _rows = new List<StepRow>();
        Label _status;
        PurchaseService _purchases;
        Action<string, Reward[], bool> _showRewards;

        public Button BuyButton { get; private set; }
        public string StatusText => _status.text;
        public int StepCount => _rows.Count;
        public Button FreeButton(int step) => _rows[step - 1].Free;
        public Button PremiumButton(int step) => _rows[step - 1].Premium;

        public static PassPopup Show(VisualElement parent, PurchaseService purchases, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new PassPopup { _purchases = purchases, _showRewards = showRewards };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Pass", "Kor Yolu", closed);
            _status = Ui.Text("", "et-ink et-pass__status", Panel, "Status");
            Ui.Text("Ücretli şerit: büyük ödüller ve bu ay en çok " + Meta.Rules.Store.PassMaxLives + " can", "et-caption et-pass__note", Panel, "Note");
            var product = Meta.Rules.Store.Find(StoreRules.PassId);
            BuyButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Buy, Panel, "Buy");
            Ui.Text("Kor Yolu'nu aç: " + (_purchases != null ? _purchases.PriceOf(product) : product.Price), "et-btn__label", BuyButton, "Label");

            var head = Ui.El("et-pass__head", Panel, "Head");
            Ui.Text("Bedava", "et-caption et-pass__col", head);
            Ui.Text("Kor Yolu", "et-caption et-pass__col et-pass__col--premium", head);

            var list = new ScrollView(ScrollViewMode.Vertical) { name = "Steps" };
            list.AddToClassList("et-pass__list");
            Panel.Add(list);
            var track = Meta.Rules.Store.PassTrack;
            for (int i = 0; i < track.Length; i++) _rows.Add(BuildRow(list, i + 1, track[i]));

            AddClose();
            Refresh();
            Meta.Changed += Refresh;
        }

        StepRow BuildRow(VisualElement list, int step, PassStep rewards)
        {
            var row = new StepRow { Step = step, Root = Ui.El("et-pass__row", list, "Step" + step) };
            row.Free = Cell(row.Root, rewards.Free, () => Claim(step, false));
            Ui.Text(step.ToString(), "et-outlined et-pass__step", row.Root, "Number");
            row.Premium = Cell(row.Root, rewards.Premium, () => Claim(step, true));
            row.Premium.AddToClassList("et-pass__cell--premium");
            return row;
        }

        static Button Cell(VisualElement parent, Reward reward, Action onTap)
        {
            var cell = Ui.Button(null, "et-pressable et-pass__cell", onTap, parent, "Cell");
            Ui.El("et-icon et-pass__icon " + IconOf(reward), cell);
            Ui.Text("+" + reward.Amount, "et-outlined et-pass__amount", cell, "Amount");
            return cell;
        }

        protected override void OnClosed() => Meta.Changed -= Refresh;

        void Refresh()
        {
            if (!IsOpen) return;
            var economy = Meta.Economy;
            long now = Meta.Now;
            int reached = economy.PassReached(now);
            bool premium = economy.PassPremium(now);
            _status.text = "Adım " + reached + "/" + _rows.Count + " · " + Meta.Span(economy.PassSecondsLeft(now));
            BuyButton.EnableInClassList(Ui.Hidden, premium);
            foreach (var row in _rows)
            {
                Mark(row.Free, row.Step <= reached, economy.PassClaimed(row.Step, false));
                Mark(row.Premium, row.Step <= reached && premium, economy.PassClaimed(row.Step, true));
                row.Premium.EnableInClassList("et-pass__cell--locked", !premium);
            }
        }

        static void Mark(Button cell, bool reached, bool claimed)
        {
            cell.EnableInClassList("et-pass__cell--ready", reached && !claimed);
            cell.EnableInClassList("et-pass__cell--claimed", claimed);
            cell.SetEnabled(reached && !claimed);
        }

        void Claim(int step, bool premium)
        {
            var reward = Meta.Economy.ClaimPass(step, premium, Meta.Now);
            if (!reward.HasValue) return;
            Meta.Save();
            _showRewards?.Invoke("Kor Yolu", new[] { reward.Value }, false);
        }

        void Buy()
        {
            if (_purchases == null) return;
            _purchases.Buy(StoreRules.PassId);
        }
    }

    public sealed class StarterPopup : Popup
    {
        PurchaseService _purchases;

        public Button BuyButton { get; private set; }

        public static StarterPopup Show(VisualElement parent, PurchaseService purchases, Action closed)
        {
            var popup = new StarterPopup { _purchases = purchases };
            popup.Build(parent, closed);
            return popup;
        }

        void Build(VisualElement parent, Action closed)
        {
            Open(parent, "Starter", "Başlangıç paketi", closed);
            var product = Meta.Rules.Store.Find(StoreRules.StarterId);
            Ui.Text("Yalnız bir kez: yolculuğa hızlı başla!", "et-ink et-result__message", Panel, "Message");
            var rewards = Ui.El("et-reward-row", Panel, "Rewards");
            foreach (var reward in product.Rewards) RewardTile(reward, rewards);
            BuyButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Buy, Panel, "Buy");
            Ui.Text("Al: " + (_purchases != null ? _purchases.PriceOf(product) : product.Price), "et-btn__label", BuyButton, "Label");
            AddClose("Sonra");
        }

        void Buy()
        {
            if (_purchases != null && _purchases.Buy(StoreRules.StarterId)) Close();
        }
    }
}
