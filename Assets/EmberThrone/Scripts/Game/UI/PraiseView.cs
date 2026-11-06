using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public sealed class PraiseView
    {
        public static readonly string[] Words = { null, "Harika!", "Muhteşem!", "İnanılmaz!" };

        const float Rise = 70f;

        Label _label;
        int _run;

        public string Text { get; private set; }

        public static PraiseView Create(VisualElement parent)
        {
            var view = new PraiseView();
            view._label = Ui.Text("", "et-title et-praise " + Ui.Hidden, parent, "Praise");
            return view;
        }

        public void Show(int level)
        {
            if (level <= 0 || level >= Words.Length) return;
            Text = Words[level];
            var label = _label;
            label.text = Text;
            label.RemoveFromClassList(Ui.Hidden);
            label.style.opacity = 1f;
            label.style.translate = new Translate(0f, 0f);
            label.style.scale = new Scale(Vector2.zero);

            int run = ++_run;
            Tweens.Run(Timing.PraisePop, t =>
            {
                if (run == _run && Ui.Alive(label)) label.style.scale = new Scale(Vector2.one * Ease.OutBack(t));
            });
            Tweens.Run(Timing.PraiseOut, t =>
            {
                if (run != _run || !Ui.Alive(label)) return;
                label.style.opacity = 1f - t;
                label.style.translate = new Translate(0f, -Rise * Ease.OutCubic(t));
            }, Timing.PraisePop + Timing.PraiseHold, () =>
            {
                if (run == _run && Ui.Alive(label)) label.AddToClassList(Ui.Hidden);
            });
        }
    }
}
