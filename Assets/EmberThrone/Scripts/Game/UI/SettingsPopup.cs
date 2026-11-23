using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class SettingsPopup
    {
        public const string PrivacyPolicyUrl = "https://serdumenn.github.io/ember-throne/privacy/";
        public const string Off = "et-toggle--off";

        public static Action<string> OpenUrl = Application.OpenURL;

        public VisualElement Root { get; private set; }
        public Button SoundButton { get; private set; }
        public Button HapticsButton { get; private set; }
        public Button ShakeButton { get; private set; }
        public Button PrivacyButton { get; private set; }
        public Button PolicyButton { get; private set; }
        public Button CloseButton { get; private set; }
        public string Title => "Ayarlar";

        Action _closed;

        public static SettingsPopup Show(VisualElement parent, Action closed)
        {
            var popup = new SettingsPopup { _closed = closed };
            popup.Build(parent);
            return popup;
        }

        void Build(VisualElement parent)
        {
            Root = Ui.El("et-fill et-settings", parent, "Settings");
            Root.pickingMode = PickingMode.Position;
            Ui.PageHeader(Root, Title, "et-page-header--screen");
            CloseButton = Ui.Button(null, "et-pressable et-settings__close", Close, Root, "Close");

            var toggles = Ui.El("et-settings__toggles", Root, "Toggles");
            SoundButton = Toggle(toggles, "Sound", "et-ic--sound", "Ses", ToggleSound);
            HapticsButton = Toggle(toggles, "Haptics", "et-ic--vibrate", "Titreşim", ToggleHaptics);
            ShakeButton = Toggle(toggles, "Shake", "et-ic--shake", "Sarsıntı", ToggleShake);

            var list = Ui.El("et-settings__list", Root, "List");
            var ads = GameApp.Instance != null ? GameApp.Instance.Ads : null;
            if (ads != null && ads.PrivacyOptionsRequired)
                PrivacyButton = Ui.Button("Gizlilik seçenekleri", "et-btn et-btn--blue et-settings__button", () => ads.ShowPrivacyOptions(null), list, "Privacy");
            PolicyButton = Ui.Button("Gizlilik politikası", Ui.ClassOf(ButtonKind.Cta) + " et-settings__button", () => OpenUrl(PrivacyPolicyUrl), list, "Policy");
            Refresh();
        }

        static Button Toggle(VisualElement parent, string name, string icon, string caption, Action onTap)
        {
            var cell = Ui.El("et-settings__cell", parent, name + "Cell");
            var button = Ui.Button(null, "et-pressable et-toggle", onTap, cell, name);
            Ui.El("et-icon et-toggle__icon " + icon, button);
            Ui.El("et-toggle__slash", button, "Slash");
            Ui.Text(caption, "et-outlined et-settings__caption", cell, "Caption");
            return button;
        }

        public bool IsOn(Button toggle) => !toggle.ClassListContains(Off);

        public string Caption(Button toggle) => toggle.parent.Q<Label>("Caption").text;

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
            SoundButton.EnableInClassList(Off, !Sound.Enabled);
            HapticsButton.EnableInClassList(Off, !Haptics.Enabled);
            ShakeButton.EnableInClassList(Off, !CameraShake.Enabled);
        }

        public void Close()
        {
            if (Root.parent == null) return;
            Root.RemoveFromHierarchy();
            _closed?.Invoke();
        }
    }
}
