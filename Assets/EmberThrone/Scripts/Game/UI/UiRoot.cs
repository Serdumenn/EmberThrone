using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class UiRoot : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;
        public const string ThemePath = "UI/Theme/EmberThrone";

        public static Vector2Int? ScreenSizeOverride;

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

        public static float Scale => Mathf.Min(ScreenWidth / ReferenceWidth, ScreenHeight / ReferenceHeight);

        public static Vector2 PanelToScreen(Vector2 panel) => new Vector2(panel.x * Scale, ScreenHeight - panel.y * Scale);

        public static Vector2 ScreenToPanel(Vector2 screen) => new Vector2(screen.x / Scale, (ScreenHeight - screen.y) / Scale);

        public event Action LayoutChanged;

        public PanelSettings Settings { get; private set; }
        public VisualElement Backdrop { get; private set; }
        public VisualElement Content { get; private set; }
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
            float s = Scale;
            _safe.style.left = _lastSafe.xMin / s;
            _safe.style.right = (_width - _lastSafe.xMax) / s;
            _safe.style.top = (_height - _lastSafe.yMax) / s;
            _safe.style.bottom = _lastSafe.yMin / s;
            LayoutChanged?.Invoke();
        }

        public void ClearContent()
        {
            Content.Clear();
            Backdrop.ClearClassList();
            Backdrop.AddToClassList("et-fill");
        }
    }
}
