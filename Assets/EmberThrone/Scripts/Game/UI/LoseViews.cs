using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class OutOfMovesBanner
    {
        const float Above = -1414f;
        const float Below = 1312f;
        const float Overshoot = 69f;
        const float Rebound = -12f;
        const float Lift = -51f;

        public VisualElement Root { get; private set; }
        public VisualElement Band { get; private set; }
        public bool IsOpen => Root != null && Root.parent != null;

        public static float Duration => Timing.BannerStop + Timing.BannerHold + Timing.BannerOut;

        public static OutOfMovesBanner Show(VisualElement parent, Action done)
        {
            var banner = new OutOfMovesBanner();
            banner.Build(parent, done);
            return banner;
        }

        void Build(VisualElement parent, Action done)
        {
            Root = Ui.El("et-fill", parent, "OutOfMoves");
            var stage = Ui.El("et-stage", Root, "Stage");
            Band = Ui.El("et-banner", stage, "Band");
            Ui.El("et-gold-line et-banner__line et-banner__line--top", Band);
            Ui.El("et-banner__body", Band);
            Ui.El("et-gold-line et-banner__line et-banner__line--bottom", Band);
            Ui.Text("Hamlen Kalmadı!", "et-banner__text", Band, "Text");

            var band = Band;
            band.style.translate = new Translate(0f, Above);
            Tweens.Run(Duration, t => Move(band, t * Duration), 0f, () =>
            {
                if (Ui.Alive(Root)) Root.RemoveFromHierarchy();
                Tweens.After(Timing.BannerGap, () => done?.Invoke());
            });
        }

        static void Move(VisualElement band, float time)
        {
            if (!Ui.Alive(band)) return;
            band.style.translate = new Translate(0f, OffsetAt(time));
        }

        public static float OffsetAt(float time)
        {
            if (time < Timing.BannerIn) return Mathf.LerpUnclamped(Above, Overshoot, Ease.OutQuad(time / Timing.BannerIn));
            if (time < Timing.BannerBack) return Mathf.LerpUnclamped(Overshoot, Rebound, Ease.InOutSine((time - Timing.BannerIn) / (Timing.BannerBack - Timing.BannerIn)));
            if (time < Timing.BannerStop) return Mathf.LerpUnclamped(Rebound, 0f, Ease.InOutSine((time - Timing.BannerBack) / (Timing.BannerStop - Timing.BannerBack)));
            float hold = Timing.BannerStop + Timing.BannerHold;
            if (time < hold) return 0f;
            float exit = time - hold;
            if (exit < Timing.BannerLift) return Mathf.LerpUnclamped(0f, Lift, Ease.OutQuad(exit / Timing.BannerLift));
            return Mathf.LerpUnclamped(Lift, Below, Ease.InQuad(Mathf.Clamp01((exit - Timing.BannerLift) / (Timing.BannerOut - Timing.BannerLift))));
        }
    }

    public sealed class ContinuePopup
    {
        const float DropFrom = -500f;

        readonly List<string> _warnings = new List<string>();
        VisualElement _offer;
        VisualElement _warning;
        Action _giveUp;

        public VisualElement Root { get; private set; }
        public VisualElement Window { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button AdButton { get; private set; }
        public Button CloseButton { get; private set; }
        public bool Warning { get; private set; }
        public string Message { get; private set; }
        public string Title => "Devam Et?";
        public bool IsOpen => Root != null && Root.parent != null;

        public static ContinuePopup Show(VisualElement parent, int moves, int price, int coins, bool adReady, IEnumerable<string> warnings,
            Action onContinue, Action onAd, Action giveUp)
        {
            var popup = new ContinuePopup { _giveUp = giveUp };
            popup._warnings.AddRange(warnings);
            popup.Build(parent, moves, price, coins, adReady, onContinue, onAd);
            return popup;
        }

        void Build(VisualElement parent, int moves, int price, int coins, bool adReady, Action onContinue, Action onAd)
        {
            Root = Ui.El("et-fill et-scrim", parent, "Continue");
            Root.pickingMode = PickingMode.Position;
            var stage = Ui.El("et-stage", Root, "Stage");
            Window = Ui.El("et-continue", stage, "Window");

            var purse = Ui.El("et-pill et-continue__purse", Window, "Coins");
            Ui.El("et-icon et-pill__icon et-ic--coin", purse);
            Ui.Text(coins.ToString(), "et-ink et-pill__text et-continue__purse-text", purse, "Text");
            Ui.El("et-icon et-pill__plus et-ic--plus", purse);

            Ui.El("et-gold-line et-continue__line et-continue__line--top", Window);
            var header = Ui.El("et-continue__header", Window, "Header");
            Ui.Text(Title, "et-continue__title", header, "Title");

            var cream = Ui.El("et-continue__cream", Window, "Cream");
            _offer = Ui.El("et-continue__offer", cream, "Offer");
            var hex = Ui.El("et-continue__hex", _offer, "Moves");
            Ui.Text("+" + moves, "et-continue__hex-text", hex, "Text");
            Message = "Devam etmek için " + moves + " hamle daha ekle!";
            var line = Ui.El("et-continue__message", _offer, "Message");
            Ui.Text("Devam etmek için", "et-continue__ink", line);
            Ui.Text(moves.ToString(), "et-continue__ink et-continue__ink--red", line);
            Ui.Text("hamle daha ekle!", "et-continue__ink", line);

            _warning = Ui.El("et-continue__warning " + Ui.Hidden, cream, "Warning");
            Ui.Text("Bunları kaybedeceksin!", "et-continue__ink et-continue__warning-title", _warning, "Title");
            foreach (var text in _warnings) Ui.Text(text, "et-continue__ink et-continue__warning-line", _warning, "Line");

            var band = Ui.El("et-continue__band", Window, "Band");
            ContinueButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Cta) + " et-btn--framed et-continue__buy", () => onContinue?.Invoke(), band, "Continue");
            Ui.Text("Devam Et", "et-continue__buy-text", ContinueButton, "Label");
            Ui.El("et-icon et-continue__buy-coin et-ic--coin", ContinueButton);
            Ui.Text(price.ToString(), "et-continue__buy-text", ContinueButton, "Price");
            Ui.El("et-gold-line et-continue__line et-continue__line--bottom", Window);

            if (adReady)
            {
                AdButton = Ui.Button(null, "et-btn et-btn--blue et-continue__ad", () => onAd?.Invoke(), Window, "Ad");
                Ui.El("et-icon et-continue__ad-icon et-ic--video", AdButton);
                Ui.Text("İzle: +" + moves + " hamle", "et-continue__ad-text", AdButton, "Label");
            }

            CloseButton = Ui.Button(null, "et-pressable et-continue__close", OnClose, Window, "Close");

            Root.style.opacity = 0f;
            var root = Root;
            Tweens.Run(Timing.ScrimFade, t =>
            {
                if (Ui.Alive(root)) root.style.opacity = t;
            }, 0f, () =>
            {
                if (Ui.Alive(root)) root.style.opacity = 1f;
            });
            var window = Window;
            window.style.translate = new Translate(0f, DropFrom);
            Tweens.Run(Timing.ModalDrop, t =>
            {
                if (Ui.Alive(window)) window.style.translate = new Translate(0f, DropFrom * Ease.Spring(t * Timing.ModalDrop, Timing.ModalIn, Timing.ModalBounce));
            }, 0f, () =>
            {
                if (Ui.Alive(window)) window.style.translate = new Translate(0f, 0f);
            });
        }

        public string ButtonText(Button button) => button.Q<Label>("Label").text;

        public string PriceText => ContinueButton.Q<Label>("Price").text;

        public void TapClose() => OnClose();

        void OnClose()
        {
            if (!Warning && _warnings.Count > 0)
            {
                Warning = true;
                Message = string.Join("\n", _warnings);
                _offer.AddToClassList(Ui.Hidden);
                _warning.RemoveFromClassList(Ui.Hidden);
                return;
            }
            Close();
            _giveUp?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            Root.RemoveFromHierarchy();
        }
    }
}
