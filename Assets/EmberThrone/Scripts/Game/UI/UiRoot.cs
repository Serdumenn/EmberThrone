using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    /// <summary>
    /// The UI Toolkit panel, the event system and the safe area. Panel units are a virtual 1080x1920 screen that
    /// only ever grows (Expand), so a layout that fits 1080x1920 fits every phone and tablet.
    /// The panel settings are built here, not stored in an asset: the scene stays empty.
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const string ThemePath = "UI/Theme/EmberThrone";

        /// <summary>Test hook: lay out for this screen size, so tests can try any phone shape.</summary>
        public static Vector2Int? ScreenSizeOverride;

        /// <summary>Test hook to try notches on any screen. Null uses the device's safe area.</summary>
        public static Rect? SafeAreaOverride;

        public static int ScreenWidth => ScreenSizeOverride?.x ?? Screen.width;
        public static int ScreenHeight => ScreenSizeOverride?.y ?? Screen.height;

        public static Rect SafeArea
        {
            get
            {
                if (SafeAreaOverride.HasValue) return SafeAreaOverride.Value;
                return ScreenSizeOverride.HasValue ? new Rect(0f, 0f, ScreenWidth, ScreenHeight) : Screen.safeArea;
            }
        }

        /// <summary>Screen pixels per panel unit, the same formula PanelSettings uses for Expand.</summary>
        public static float Scale => Mathf.Min(ScreenWidth / ReferenceWidth, ScreenHeight / ReferenceHeight);

        /// <summary>A panel point (y down) in screen pixels (y up), e.g. to tap a button from a test.</summary>
        public static Vector2 PanelToScreen(Vector2 panel) => new Vector2(panel.x * Scale, ScreenHeight - panel.y * Scale);

        public event Action LayoutChanged;

        public PanelSettings Settings { get; private set; }
        /// <summary>Full-screen picture behind the current screen (also under the notch).</summary>
        public VisualElement Backdrop { get; private set; }
        /// <summary>Parent of the current screen (level select, HUD); inside the safe area.</summary>
        public VisualElement Content { get; private set; }
        /// <summary>Parent of modal windows; drawn over the content and over the notch.</summary>
        public VisualElement Popups { get; private set; }

        VisualElement _safe;
        int _width;
        int _height;
        Rect _lastSafe;

        public static UiRoot Create(Transform parent)
        {
            var go = new GameObject("UiRoot");
            go.transform.SetParent(parent, false);
            return go.AddComponent<UiRoot>();
        }

        void Awake()
        {
            // UI Toolkit reads the Input System through the event system, like the old canvas did.
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            Settings = ScriptableObject.CreateInstance<PanelSettings>();
            Settings.name = "EmberThrone UI";
            Settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemePath);
            if (Settings.themeStyleSheet == null) Debug.LogError("[EmberThrone] UI theme missing: Resources/" + ThemePath);
            Settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            Settings.referenceResolution = new Vector2Int((int)ReferenceWidth, (int)ReferenceHeight);
            Settings.screenMatchMode = PanelScreenMatchMode.Expand;
            Settings.clearColor = false;

            // Inactive until the settings are in, so the document never starts without a panel.
            var docObject = new GameObject("UIDocument");
            docObject.SetActive(false);
            docObject.transform.SetParent(transform, false);
            var document = docObject.AddComponent<UIDocument>();
            document.panelSettings = Settings;
            docObject.SetActive(true);

            var root = document.rootVisualElement;
            root.AddToClassList("et-root");
            root.pickingMode = PickingMode.Ignore;
            Backdrop = Ui.El("et-fill", root, "Backdrop");
            _safe = Ui.El("et-safe", root, "SafeArea");
            Content = Ui.El("et-fill", _safe, "Content");
            Popups = Ui.El("et-fill", root, "Popups");

            ApplyLayout();
        }

        void OnDestroy()
        {
            if (Settings != null) Destroy(Settings);
        }

        void LateUpdate()
        {
            if (ScreenWidth != _width || ScreenHeight != _height || SafeArea != _lastSafe) ApplyLayout();
        }

        void ApplyLayout()
        {
            _width = ScreenWidth;
            _height = ScreenHeight;
            _lastSafe = SafeArea;
            // The safe area is runtime data, so it is the one layout value set from code.
            float s = Scale;
            _safe.style.left = _lastSafe.xMin / s;
            _safe.style.right = (_width - _lastSafe.xMax) / s;
            _safe.style.top = (_height - _lastSafe.yMax) / s;
            _safe.style.bottom = _lastSafe.yMin / s;
            LayoutChanged?.Invoke();
        }

        /// <summary>Removes the current screen and its backdrop.</summary>
        public void ClearContent()
        {
            Content.Clear();
            Backdrop.ClearClassList();
            Backdrop.AddToClassList("et-fill");
        }
    }
}
