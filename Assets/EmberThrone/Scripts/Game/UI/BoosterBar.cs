using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class BoosterBar
    {
        public const float Height = 236f;

        const string Active = "et-booster--active";
        const string Empty = "et-booster--empty";

        readonly Dictionary<BoosterType, Button> _buttons = new Dictionary<BoosterType, Button>();
        readonly Dictionary<BoosterType, Label> _counts = new Dictionary<BoosterType, Label>();

        public VisualElement Root { get; private set; }
        public Button HomeButton { get; private set; }

        public Button ButtonOf(BoosterType type) => _buttons[type];

        public string CountText(BoosterType type) => _counts[type].text;

        public static BoosterBar Create(VisualElement parent, Action<BoosterType> onPick, Action onHome)
        {
            var bar = new BoosterBar();
            bar.Build(parent, onPick, onHome);
            return bar;
        }

        void Build(VisualElement parent, Action<BoosterType> onPick, Action onHome)
        {
            Root = Ui.El("et-boosters", parent, "Boosters");
            foreach (var type in BoosterInventory.All)
            {
                var t = type;
                var button = Ui.Button(null, "et-pressable et-booster", () => onPick(t), Root, type.ToString());
                Ui.El("et-icon et-booster__icon et-booster__icon--" + BoosterInventory.Slug(type), button);
                _counts[type] = Ui.Text("0", "et-chip et-caption et-outlined et-booster__count", button, "Count");
                _buttons[type] = button;
            }
            HomeButton = Ui.Button(null, "et-ring et-pressable et-boosters__home", onHome, Root, "Home");
            Ui.El("et-icon et-boosters__home-icon", HomeButton);
        }

        public void SetCount(BoosterType type, int count)
        {
            _counts[type].text = count > 0 ? count.ToString() : "+";
            _buttons[type].EnableInClassList(Empty, count <= 0);
        }

        public void SetArmed(BoosterType? armed)
        {
            foreach (var pair in _buttons) pair.Value.EnableInClassList(Active, armed.HasValue && pair.Key == armed.Value);
        }

        public void Shake(BoosterType type)
        {
            var e = _buttons[type];
            Tweens.Run(Timing.CounterBounce * 2f, t =>
            {
                if (Ui.Alive(e)) e.style.translate = new Translate(Mathf.Sin(t * Mathf.PI * 4f) * 10f * (1f - t), 0f);
            }, 0f, () =>
            {
                if (Ui.Alive(e)) e.style.translate = StyleKeyword.Null;
            });
        }

        public void Remove() => Root.RemoveFromHierarchy();
    }
}
