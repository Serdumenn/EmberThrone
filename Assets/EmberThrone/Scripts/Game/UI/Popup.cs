using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public class Popup
    {
        const float SlideFrom = 1600f;

        Action _closed;

        public VisualElement Root { get; private set; }
        public VisualElement Body { get; private set; }
        public VisualElement Panel { get; private set; }
        public Button CloseButton { get; protected set; }
        public string Title { get; private set; }
        public bool IsOpen => Root != null && Root.parent != null;

        protected void Open(VisualElement parent, string name, string title, Action closed)
        {
            _closed = closed;
            Title = title;
            Root = Ui.El("et-fill et-scrim et-result", parent, name);
            Root.pickingMode = PickingMode.Position;
            Body = Ui.El("et-result__body", Root, "Body");
            Panel = Ui.El("et-panel et-result__panel", Body, "Panel");
            var ribbon = Ui.El("et-ribbon", Panel);
            Ui.Text(title, title.Length > 11 ? "et-title et-title--small" : "et-title", ribbon, "Title");

            var body = Body;
            body.style.translate = new Translate(0f, SlideFrom);
            Tweens.Run(Timing.ModalIn, t =>
            {
                if (Ui.Alive(body)) body.style.translate = new Translate(0f, Mathf.LerpUnclamped(SlideFrom, 0f, Ease.OutCubic(t)));
            });
        }

        protected Button AddClose(string label = "Tamam", ButtonKind kind = ButtonKind.Secondary)
        {
            CloseButton = Ui.Button(label, Ui.ClassOf(kind) + " et-result__button", Close, Panel, "Close");
            return CloseButton;
        }

        protected virtual void OnClosed()
        {
        }

        public void Close()
        {
            if (!IsOpen) return;
            Root.RemoveFromHierarchy();
            OnClosed();
            _closed?.Invoke();
        }

        public static string IconOf(Reward reward)
        {
            switch (reward.Kind)
            {
                case RewardKind.Coins: return "et-ic--coin";
                case RewardKind.Gems: return "et-ic--gem";
                case RewardKind.Life: return "et-ic--heart";
                case RewardKind.Pack: return "et-ic--cards";
                default: return "et-ic--" + BoosterInventory.Slug(reward.Booster);
            }
        }

        public static VisualElement RewardTile(Reward reward, VisualElement parent)
        {
            var tile = Ui.El("et-reward", parent, "Reward");
            Ui.El("et-icon et-reward__icon " + IconOf(reward), tile);
            Ui.Text("+" + reward.Amount, "et-outlined et-reward__amount", tile, "Amount");
            return tile;
        }

        public static void Bump(VisualElement e, float amount = 0.2f, float duration = 0.18f)
        {
            Tweens.Run(duration, t =>
            {
                if (Ui.Alive(e)) e.style.scale = new Scale(Vector2.one * (1f + amount * Mathf.Sin(t * Mathf.PI)));
            }, 0f, () =>
            {
                if (Ui.Alive(e)) e.style.scale = StyleKeyword.Null;
            });
        }
    }
}
