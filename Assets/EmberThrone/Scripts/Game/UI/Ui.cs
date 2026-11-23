using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public enum ButtonKind
    {
        Cta,
        Secondary,
    }

    public static class Ui
    {
        public const string Hidden = "et-hidden";

        public static VisualElement El(string classes, VisualElement parent = null, string name = null)
        {
            var e = new VisualElement();
            Setup(e, classes, parent, name);
            e.pickingMode = PickingMode.Ignore;
            return e;
        }

        public static Label Text(string text, string classes, VisualElement parent, string name = null)
        {
            var l = new Label(text);
            Setup(l, classes, parent, name);
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        public static VisualElement PageHeader(VisualElement parent, string title, string classes = null)
        {
            var header = El(classes == null ? "et-page-header" : "et-page-header " + classes, parent, "PageHeader");
            El("et-page-header__cap", header);
            El("et-gold-line et-page-header__line", header);
            Text(title, "et-page-header__title", header, "Title");
            return header;
        }

        public static Button Button(string text, string classes, Action onClick, VisualElement parent, string name = null)
        {
            var b = new Button(() =>
            {
                Sound.Play(Sfx.UiTap);
                onClick?.Invoke();
            }) { text = text ?? "" };
            b.RemoveFromClassList(UnityEngine.UIElements.Button.ussClassName);
            Setup(b, classes, parent, name);
            return b;
        }

        public static string ClassOf(ButtonKind kind) => kind == ButtonKind.Cta ? "et-btn et-btn--cta" : "et-btn et-btn--secondary";

        public static VisualElement Image(Sprite sprite, string classes, VisualElement parent)
        {
            var e = El("et-icon " + classes, parent);
            e.style.backgroundImage = new StyleBackground(sprite);
            return e;
        }

        static void Setup(VisualElement e, string classes, VisualElement parent, string name)
        {
            if (!string.IsNullOrEmpty(name)) e.name = name;
            if (!string.IsNullOrEmpty(classes))
                foreach (var c in classes.Split(' '))
                    if (c.Length > 0) e.AddToClassList(c);
            parent?.Add(e);
        }

        public static bool Alive(VisualElement e) => e != null && e.panel != null;
    }
}
