using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class SettingsPopup
    {
        public VisualElement Root { get; private set; }
        public Button SoundButton { get; private set; }
        public Button HapticsButton { get; private set; }
        public Button ShakeButton { get; private set; }
        public Button CloseButton { get; private set; }

        Action _closed;

        public static SettingsPopup Show(VisualElement parent, Action closed)
        {
            var popup = new SettingsPopup { _closed = closed };
            popup.Build(parent);
            return popup;
        }

        void Build(VisualElement parent)
        {
            Root = Ui.El("et-fill et-scrim et-result", parent, "Settings");
            Root.pickingMode = PickingMode.Position;
            var body = Ui.El("et-result__body", Root, "Body");
            var panel = Ui.El("et-panel et-result__panel", body, "Panel");
            var ribbon = Ui.El("et-ribbon", panel);
            Ui.Text("Ayarlar", "et-title", ribbon, "Title");

            SoundButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Secondary) + " et-result__button", ToggleSound, panel, "Sound");
            HapticsButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Secondary) + " et-result__button", ToggleHaptics, panel, "Haptics");
            ShakeButton = Ui.Button(null, Ui.ClassOf(ButtonKind.Secondary) + " et-result__button", ToggleShake, panel, "Shake");
            CloseButton = Ui.Button("Tamam", Ui.ClassOf(ButtonKind.Cta) + " et-result__button", Close, panel, "Close");
            Refresh();

            const float from = 1600f;
            body.style.translate = new Translate(0f, from);
            Tweens.Run(Timing.ModalIn, t =>
            {
                if (Ui.Alive(body)) body.style.translate = new Translate(0f, Mathf.LerpUnclamped(from, 0f, Ease.OutCubic(t)));
            });
        }

        void ToggleSound()
        {
            Sound.Enabled = !Sound.Enabled;
            PlayerPrefs.Save();
            Refresh();
        }

        void ToggleHaptics()
        {
            Haptics.Enabled = !Haptics.Enabled;
            PlayerPrefs.Save();
            if (Haptics.Enabled) Haptics.Play(HapticLevel.Medium);
            Refresh();
        }

        void ToggleShake()
        {
            CameraShake.Enabled = !CameraShake.Enabled;
            PlayerPrefs.Save();
            Refresh();
        }

        void Refresh()
        {
            SoundButton.text = "Ses: " + (Sound.Enabled ? "Açık" : "Kapalı");
            HapticsButton.text = "Titreşim: " + (Haptics.Enabled ? "Açık" : "Kapalı");
            ShakeButton.text = "Sarsıntı: " + (CameraShake.Enabled ? "Açık" : "Kapalı");
        }

        public void Close()
        {
            if (Root.parent == null) return;
            Root.RemoveFromHierarchy();
            _closed?.Invoke();
        }
    }
}
