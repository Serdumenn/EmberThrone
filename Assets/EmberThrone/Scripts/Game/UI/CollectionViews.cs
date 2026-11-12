using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public static class CardArt
    {
        const string Folder = "UI/Cards/";
        const string Fallback = "UI/Art/ic_cards";

        public static Texture2D Of(CollectionRules rules, int card)
        {
            var set = rules.Sets[rules.SetOf(card)];
            var art = Resources.Load<Texture2D>(Folder + set.Id + "_" + rules.Card(card).Id);
            return art != null ? art : Resources.Load<Texture2D>(Fallback);
        }

        public static bool Has(CollectionRules rules, int card)
        {
            var set = rules.Sets[rules.SetOf(card)];
            return Resources.Load<Texture2D>(Folder + set.Id + "_" + rules.Card(card).Id) != null;
        }
    }

    public static class CardView
    {
        public static VisualElement Build(VisualElement parent, int card, int owned, string name = "Card")
        {
            var rules = Meta.Cards.Rules;
            var def = rules.Card(card);
            var e = Ui.El("et-card et-card--s" + def.Stars + (owned > 0 ? "" : " et-card--missing"), parent, name);
            var art = Ui.El("et-icon et-card__art", e, "Art");
            art.style.backgroundImage = new StyleBackground(CardArt.Of(rules, card));
            Ui.Text(owned > 0 ? def.Name : "?", "et-caption et-card__name", e, "Name");
            var stars = Ui.El("et-card__stars", e, "Stars");
            for (int i = 0; i < def.Stars; i++) Ui.El("et-star et-star--on et-card__star", stars);
            if (owned > 1) Ui.Text("x" + owned, "et-chip et-caption et-card__count", e, "Count");
            return e;
        }
    }

    public sealed class CollectionView
    {
        readonly List<Button> _tiles = new List<Button>();
        readonly List<Label> _progress = new List<Label>();
        readonly List<VisualElement> _fills = new List<VisualElement>();
        readonly List<VisualElement> _checks = new List<VisualElement>();
        readonly List<Label> _badges = new List<Label>();
        Label _total;
        VisualElement _totalFill;
        Label _packBadge;
        GameApp _app;

        public VisualElement Root { get; private set; }
        public Button PackButton { get; private set; }
        public Button GrandButton { get; private set; }
        public IReadOnlyList<Button> SetTiles => _tiles;
        public string TotalText => _total.text;
        public string PackBadgeText => _packBadge.ClassListContains(Ui.Hidden) ? "" : _packBadge.text;

        public static CollectionView Create(VisualElement parent, GameApp app)
        {
            var view = new CollectionView { _app = app };
            view.Build(parent);
            return view;
        }

        void Build(VisualElement parent)
        {
            var rules = Meta.Cards.Rules;
            Root = Ui.El("et-cards", parent, "Collection");

            var header = Ui.El("et-panel et-cards__header", Root, "Header");
            Ui.Text("Koleksiyon", "et-title et-title--small", header, "Title");
            var total = Ui.El("et-cards__total", header, "Total");
            var bar = Ui.El("et-progress et-cards__bar", total, "Bar");
            _totalFill = Ui.El("et-progress__fill", bar, "Fill");
            _total = Ui.Text("", "et-progress__text", bar, "Text");
            GrandButton = Rows.Price(total, "Büyük ödül", "et-ic--trophy", ButtonKind.Cta, ClaimGrand, "Grand");
            PackButton = Ui.Button("Paket aç", Ui.ClassOf(ButtonKind.Cta) + " et-cards__pack", _app.OpenPack, header, "Pack");
            _packBadge = Ui.Text("", "et-badge", PackButton, "Badge");

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "Sets" };
            scroll.AddToClassList("et-cards__scroll");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Root.Add(scroll);
            var grid = Ui.El("et-cards__grid", scroll, "Grid");

            for (int s = 0; s < rules.Sets.Length; s++)
            {
                int set = s;
                var def = rules.Sets[s];
                var tile = Ui.Button(null, "et-pressable et-set", () => _app.OpenCardSet(set), grid, "Set" + (s + 1));
                var art = Ui.El("et-icon et-set__art", tile, "Art");
                art.style.backgroundImage = new StyleBackground(CardArt.Of(rules, rules.IndexOf(s, def.Cards.Length - 1)));
                Ui.Text(def.Name, "et-ink et-set__name", tile, "Name");
                var setBar = Ui.El("et-progress et-set__bar", tile, "Bar");
                _fills.Add(Ui.El("et-progress__fill", setBar, "Fill"));
                _progress.Add(Ui.Text("", "et-progress__text", setBar, "Text"));
                _checks.Add(Ui.El("et-check et-set__check", tile, "Check"));
                _badges.Add(Ui.Text("!", "et-badge", tile, "Badge"));
                _tiles.Add(tile);
            }

            Refresh();
            Meta.Changed += Refresh;
            Root.RegisterCallback<DetachFromPanelEvent>(_ => Meta.Changed -= Refresh);
        }

        public void Refresh()
        {
            var cards = Meta.Cards;
            var rules = cards.Rules;
            int owned = cards.TotalOwned;
            _total.text = owned + "/" + rules.CardCount;
            _totalFill.style.width = Length.Percent(100f * owned / rules.CardCount);
            _totalFill.EnableInClassList(Ui.Hidden, owned == 0);
            GrandButton.SetEnabled(cards.Complete && !cards.GrandClaimed);
            PackButton.SetEnabled(cards.Packs > 0);
            _packBadge.text = cards.Packs.ToString();
            _packBadge.EnableInClassList(Ui.Hidden, cards.Packs <= 0);

            for (int s = 0; s < rules.Sets.Length; s++)
            {
                int count = rules.Sets[s].Cards.Length;
                int have = cards.OwnedInSet(s);
                _progress[s].text = have + "/" + count;
                _fills[s].style.width = Length.Percent(100f * have / count);
                _fills[s].EnableInClassList(Ui.Hidden, have == 0);
                _checks[s].EnableInClassList(Ui.Hidden, !cards.SetClaimed(s));
                _badges[s].EnableInClassList(Ui.Hidden, !cards.SetComplete(s) || cards.SetClaimed(s));
            }
        }

        void ClaimGrand()
        {
            var rewards = Meta.Cards.ClaimGrand();
            if (rewards == null) return;
            Meta.Save();
            _app.ShowRewards("Büyük Ödül", rewards, false);
        }
    }

    public sealed class SetPopup : Popup
    {
        public Button ClaimButton { get; private set; }
        public int Set { get; private set; }
        public int CardCount { get; private set; }

        public static SetPopup Show(VisualElement parent, int set, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var popup = new SetPopup { Set = set };
            popup.Build(parent, showRewards, closed);
            return popup;
        }

        void Build(VisualElement parent, Action<string, Reward[], bool> showRewards, Action closed)
        {
            var cards = Meta.Cards;
            var def = cards.Rules.Sets[Set];
            Open(parent, "CardSet", def.Name, closed);

            var grid = Ui.El("et-card-grid", Panel, "Cards");
            for (int c = 0; c < def.Cards.Length; c++)
            {
                int index = cards.Rules.IndexOf(Set, c);
                CardView.Build(grid, index, cards.Owned(index), "Card" + (c + 1));
                CardCount++;
            }

            var rewards = Ui.El("et-rewards et-rewards--small", Panel, "Rewards");
            foreach (var reward in def.Rewards) RewardTile(reward, rewards).AddToClassList("et-reward--small");

            if (cards.SetClaimed(Set)) Ui.Text("Ödül alındı", "et-ink et-result__message", Panel, "State");
            else if (cards.SetComplete(Set))
            {
                ClaimButton = Ui.Button("Ödülü al", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", () =>
                {
                    var got = Meta.Cards.ClaimSet(Set);
                    if (got == null) return;
                    Meta.Save();
                    Close();
                    showRewards(def.Name, got, false);
                }, Panel, "Claim");
            }
            else Ui.Text(cards.OwnedInSet(Set) + "/" + def.Cards.Length + " kart", "et-ink et-result__message", Panel, "State");
            AddClose();
        }
    }

    public sealed class PackPopup : Popup
    {
        public Button AgainButton { get; private set; }
        public int Count { get; private set; }
        public int NewCount { get; private set; }

        public static PackPopup Show(VisualElement parent, CardDraw[] draws, bool more, Action again, Action closed)
        {
            var popup = new PackPopup();
            popup.Build(parent, draws, more, again, closed);
            return popup;
        }

        void Build(VisualElement parent, CardDraw[] draws, bool more, Action again, Action closed)
        {
            Open(parent, "Pack", "Kart Paketi", closed);
            Count = draws.Length;
            var row = Ui.El("et-card-grid", Panel, "Cards");
            for (int i = 0; i < draws.Length; i++)
            {
                var draw = draws[i];
                var slot = Ui.El("et-pack__slot", row, "Slot" + (i + 1));
                CardView.Build(slot, draw.Card, 1);
                if (draw.IsNew)
                {
                    NewCount++;
                    Ui.Text("Yeni!", "et-tag et-outlined et-pack__tag", slot, "Tag");
                }
                else
                {
                    var coins = Ui.El("et-pack__coins", slot, "Coins");
                    Ui.El("et-icon et-pack__coin et-ic--coin", coins);
                    Ui.Text("+" + draw.Coins, "et-ink et-pack__amount", coins, "Amount");
                }

                slot.style.scale = new Scale(Vector2.zero);
                float pitch = i * 2f;
                Tweens.Run(Timing.StarFill, t =>
                {
                    if (Ui.Alive(slot)) slot.style.scale = new Scale(Vector2.one * Ease.OutBack(t));
                }, Timing.ModalIn + i * Timing.StarFill, null, () => Sound.Play(Sfx.Star, pitch));
            }

            if (more)
            {
                AgainButton = Ui.Button("Bir daha aç", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", () =>
                {
                    Close();
                    again();
                }, Panel, "Again");
            }
            AddClose("Tamam", more ? ButtonKind.Secondary : ButtonKind.Cta);
        }
    }
}
