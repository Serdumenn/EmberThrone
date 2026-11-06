using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public enum ButtonKind
    {
        /// <summary>Green: the action the screen exists for ("Sonraki seviye", "Tekrar dene").</summary>
        Cta,
        /// <summary>Orange: every other choice.</summary>
        Secondary,
    }

    /// <summary>
    /// UI Toolkit building blocks. All UI is built from code; its look lives in Resources/UI/Theme
    /// (docs/design/design-system.md). Code only adds and removes classes, it never sets colors or sizes.
    /// </summary>
    public static class Ui
    {
        /// <summary>Class that takes an element out of the layout.</summary>
        public const string Hidden = "et-hidden";

        /// <summary>A VisualElement with the given classes (space separated), added to the parent.</summary>
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

        /// <summary>A button styled only by our classes: Unity's default button look is removed.</summary>
        public static Button Button(string text, string classes, Action onClick, VisualElement parent, string name = null)
        {
            var b = new Button(onClick) { text = text ?? "" };
            b.RemoveFromClassList(UnityEngine.UIElements.Button.ussClassName);
            Setup(b, classes, parent, name);
            return b;
        }

        public static string ClassOf(ButtonKind kind) => kind == ButtonKind.Cta ? "et-btn et-btn--cta" : "et-btn et-btn--secondary";

        /// <summary>An icon whose image is generated at runtime (board pieces); file-based icons use USS classes.</summary>
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

        /// <summary>True while the element is still on screen (tweens check this after a screen change).</summary>
        public static bool Alive(VisualElement e) => e != null && e.panel != null;
    }
}
