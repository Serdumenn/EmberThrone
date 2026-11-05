using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EmberThrone.Game
{
    /// <summary>
    /// The canvas, the event system and the safe area. Canvas units are a virtual 1080x1920 screen that
    /// only ever grows (Expand), so a layout that fits 1080x1920 fits every phone and tablet.
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

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

        public event Action LayoutChanged;

        /// <summary>Parent of the current screen (level select, HUD).</summary>
        public RectTransform Content { get; private set; }
        /// <summary>Parent of modal windows; drawn over the content.</summary>
        public RectTransform Popups { get; private set; }

        /// <summary>Screen pixels per canvas unit. Computed here because CanvasScaler updates a frame late.</summary>
        public static float Scale => Mathf.Min(ScreenWidth / ReferenceWidth, ScreenHeight / ReferenceHeight);

        RectTransform _safe;
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
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            _safe = Ui.Stretch(Ui.Rect("SafeArea", transform));
            Content = Ui.Stretch(Ui.Rect("Content", _safe));
            // Dim layers must also cover the notch, so popups sit outside the safe area.
            Popups = Ui.Stretch(Ui.Rect("Popups", transform));

            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            ApplyLayout();
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
            _safe.anchorMin = new Vector2(_lastSafe.xMin / _width, _lastSafe.yMin / _height);
            _safe.anchorMax = new Vector2(_lastSafe.xMax / _width, _lastSafe.yMax / _height);
            LayoutChanged?.Invoke();
        }

        public void ClearContent()
        {
            for (int i = Content.childCount - 1; i >= 0; i--) Destroy(Content.GetChild(i).gameObject);
        }
    }
}
