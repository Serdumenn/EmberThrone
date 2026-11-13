using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmberThrone.Game
{
    public static class RestoreArt
    {
        public const string Folder = "UI/Restore/";

        public sealed class Area
        {
            public string Image;
            public Rect Box;
        }

        public sealed class District
        {
            public string Warm;
            public string Frozen;
            public Area[] Areas;
        }

        static District[] _districts;

        public static District[] Districts => _districts ?? (_districts = Load());

        public static District Of(int district)
        {
            var all = Districts;
            return all.Length == 0 ? null : all[Mathf.Clamp(district, 0, all.Length - 1)];
        }

        public static Texture2D Texture(string name) => Resources.Load<Texture2D>(Folder + name);

        static District[] Load()
        {
            var asset = Resources.Load<TextAsset>(Folder + "restore");
            if (asset == null) return new District[0];
            var root = MiniJson.AsObject(MiniJson.Parse(asset.text), "restore");
            MiniJson.TryGet(root, "districts", out var raw);
            var list = MiniJson.AsArray(raw, "districts");
            var districts = new District[list.Count];
            for (int d = 0; d < list.Count; d++)
            {
                var o = MiniJson.AsObject(list[d], "district");
                MiniJson.TryGet(o, "areas", out var areasRaw);
                var areas = MiniJson.AsArray(areasRaw, "areas");
                var parsed = new Area[areas.Count];
                for (int a = 0; a < areas.Count; a++)
                {
                    var area = MiniJson.AsObject(areas[a], "area");
                    MiniJson.TryGet(area, "box", out var boxRaw);
                    var box = MiniJson.AsArray(boxRaw, "box");
                    float x0 = Convert.ToSingle(box[0]), y0 = Convert.ToSingle(box[1]), x1 = Convert.ToSingle(box[2]), y1 = Convert.ToSingle(box[3]);
                    parsed[a] = new Area { Image = MiniJson.GetString(area, "image", ""), Box = Rect.MinMaxRect(x0, y0, x1, y1) };
                }
                districts[d] = new District { Warm = MiniJson.GetString(o, "warm", ""), Frozen = MiniJson.GetString(o, "frozen", ""), Areas = parsed };
            }
            return districts;
        }
    }

    public sealed class HomeDiorama
    {
        const float ThawSeconds = 1.1f;
        const float WarmSeconds = 1.4f;
        const float GlowScale = 1.6f;

        VisualElement _frame;
        VisualElement _base;
        VisualElement _warm;
        readonly List<VisualElement> _areas = new List<VisualElement>();
        float _aspect = 1536f / 2752f;

        public VisualElement Root { get; private set; }
        public int District { get; private set; } = -1;
        public bool Warm { get; private set; }
        public int ThawedCount { get; private set; }
        public bool Busy { get; private set; }

        public static HomeDiorama Create(VisualElement backdrop)
        {
            var diorama = new HomeDiorama();
            diorama.Root = Ui.El("et-fill et-diorama", backdrop, "Diorama");
            diorama._frame = Ui.El("et-diorama__frame", diorama.Root, "Frame");
            diorama._base = Ui.El("et-fill et-diorama__image", diorama._frame, "Base");
            diorama._warm = Ui.El("et-fill et-diorama__image", diorama._frame, "Warm");
            diorama.Root.RegisterCallback<GeometryChangedEvent>(_ => diorama.Layout());
            return diorama;
        }

        void Layout()
        {
            float w = Root.layout.width, h = Root.layout.height;
            if (float.IsNaN(w) || float.IsNaN(h) || w <= 0f || h <= 0f) return;
            float fw = Mathf.Max(w, h * _aspect);
            float fh = fw / _aspect;
            _frame.style.left = (w - fw) * 0.5f;
            _frame.style.top = (h - fh) * 0.5f;
            _frame.style.width = fw;
            _frame.style.height = fh;
        }

        public void Show(int district)
        {
            District = district;
            var art = RestoreArt.Of(district);
            var restore = Meta.Restore;
            Warm = restore.Restored(district);
            foreach (var area in _areas) area.RemoveFromHierarchy();
            _areas.Clear();
            ThawedCount = 0;
            if (art == null) return;

            var warm = RestoreArt.Texture(art.Warm);
            var frozen = RestoreArt.Texture(art.Frozen);
            if (warm != null) _aspect = (float)warm.width / warm.height;
            _base.style.backgroundImage = new StyleBackground(Warm ? warm : frozen);
            _warm.style.backgroundImage = new StyleBackground(warm);
            _warm.style.opacity = 0f;
            _warm.EnableInClassList(Ui.Hidden, true);

            for (int i = 0; i < art.Areas.Length; i++)
            {
                var a = art.Areas[i];
                var e = Ui.El("et-diorama__area", _frame, "Area" + (i + 1));
                e.style.left = Length.Percent(a.Box.xMin * 100f);
                e.style.top = Length.Percent(a.Box.yMin * 100f);
                e.style.width = Length.Percent(a.Box.width * 100f);
                e.style.height = Length.Percent(a.Box.height * 100f);
                e.style.backgroundImage = new StyleBackground(RestoreArt.Texture(a.Image));
                bool thawed = !Warm && restore.IsClaimed(district, i);
                e.EnableInClassList(Ui.Hidden, !thawed);
                if (thawed) ThawedCount++;
                _areas.Add(e);
            }
            Layout();
        }

        public void Thaw(int area, bool all, Action done)
        {
            if (area < 0 || area >= _areas.Count)
            {
                done?.Invoke();
                return;
            }
            Busy = true;
            var e = _areas[area];
            e.RemoveFromClassList(Ui.Hidden);
            e.style.opacity = 0f;
            ThawedCount++;
            var glow = Ui.El("et-fx-glow et-diorama__glow", _frame, "Glow");
            glow.style.left = e.style.left;
            glow.style.top = e.style.top;
            glow.style.width = e.style.width;
            glow.style.height = e.style.height;
            Sound.Play(Sfx.Hammer);

            float time = 0f;
            Root.schedule.Execute(t =>
            {
                time += t.deltaTime / 1000f;
                float u = Mathf.Clamp01(time / ThawSeconds);
                if (Ui.Alive(e))
                {
                    e.style.opacity = Ease.OutCubic(u);
                    e.style.scale = new Scale(Vector2.one * Mathf.LerpUnclamped(1.12f, 1f, Ease.OutBack(u)));
                }
                if (Ui.Alive(glow))
                {
                    glow.style.opacity = Mathf.Sin(u * Mathf.PI) * 0.9f;
                    glow.style.scale = new Scale(Vector2.one * Mathf.Lerp(0.6f, GlowScale, u));
                }
                if (u < 1f) return;
                glow.RemoveFromHierarchy();
                Sound.Play(Sfx.Star, 4f);
                if (all) WarmAll(done);
                else
                {
                    Busy = false;
                    done?.Invoke();
                }
            }).Every(16).Until(() => time >= ThawSeconds || !Ui.Alive(Root));
        }

        void WarmAll(Action done)
        {
            _warm.RemoveFromClassList(Ui.Hidden);
            _warm.BringToFront();
            float time = 0f;
            Sound.Play(Sfx.Win);
            Root.schedule.Execute(t =>
            {
                time += t.deltaTime / 1000f;
                float u = Mathf.Clamp01(time / WarmSeconds);
                if (Ui.Alive(_warm)) _warm.style.opacity = Ease.OutCubic(u);
                if (u < 1f) return;
                Warm = true;
                Busy = false;
                done?.Invoke();
            }).Every(16).Until(() => time >= WarmSeconds || !Ui.Alive(Root));
        }
    }

    public sealed class RestorePopup : Popup
    {
        readonly List<Button> _repairs = new List<Button>();
        Action<int, int> _repair;

        public int District { get; private set; }
        public int TaskRows { get; private set; }
        public IReadOnlyList<Button> RepairButtons => _repairs;
        public string ProgressText { get; private set; }

        public static RestorePopup Show(VisualElement parent, int district, Action<int, int> repair, Action closed)
        {
            var popup = new RestorePopup { District = district, _repair = repair };
            popup.Build(parent, closed);
            return popup;
        }

        static string GoalText(RestoreTask task)
        {
            switch (task.Goal)
            {
                case RestoreGoal.Levels: return task.Target + " seviye geç";
                case RestoreGoal.Stars: return task.Target + " yıldız kazan";
                default: return task.Target + " booster kullan";
            }
        }

        void Build(VisualElement parent, Action closed)
        {
            var restore = Meta.Restore;
            var art = RestoreArt.Of(District);
            Open(parent, "Restore", LevelMap.Chapters[Mathf.Min(District, LevelMap.Chapters.Length - 1)].Title, closed);
            var states = restore.Tasks(District);
            ProgressText = restore.ClaimedCount(District) + "/" + states.Length;
            Ui.Text("Ayaz'ın buzunu çöz, bölgeyi onar · " + ProgressText, "et-caption et-ink et-ink--muted et-restore__hint", Panel, "Hint");

            for (int i = 0; i < states.Length; i++)
            {
                int index = i;
                var state = states[i];
                var row = Ui.El("et-row et-restore__row", Panel, "Task" + (i + 1));
                var thumb = Ui.El("et-avatar et-avatar--row et-restore__thumb", row, "Thumb");
                var image = Ui.El("et-restore__thumb-art", thumb, "Art");
                if (art != null && i < art.Areas.Length) image.style.backgroundImage = new StyleBackground(RestoreArt.Texture(art.Areas[i].Image));
                thumb.EnableInClassList("et-restore__thumb--frozen", !state.Claimed);
                var texts = Ui.El("et-row__texts", row);
                Ui.Text(state.Task.Name, "et-row__title", texts, "Title");
                Ui.Text(GoalText(state.Task) + " · " + state.Progress + "/" + state.Task.Target, "et-caption et-row__sub", texts, "Sub");
                if (state.Claimed) Ui.El("et-check et-row__check", row, "Done");
                else if (state.Done) _repairs.Add(RepairButton(row, index));
                else
                {
                    var prize = Ui.El("et-restore__prize", row, "Prize");
                    var first = state.Task.Rewards[0];
                    Ui.El("et-icon et-restore__prize-icon " + IconOf(first), prize);
                    Ui.Text(first.Amount.ToString(), "et-ink et-restore__prize-text", prize, "Amount");
                }
                TaskRows++;
            }

            var bonus = Ui.El("et-restore__bonus", Panel, "Bonus");
            Ui.Text("Bölge onarılınca", "et-caption et-ink et-restore__bonus-title", bonus, "Title");
            var tiles = Ui.El("et-restore__bonus-row", bonus, "Tiles");
            foreach (var reward in restore.Rules.Areas[District].Bonus)
            {
                var tile = Ui.El("et-restore__prize", tiles, "Reward");
                Ui.El("et-icon et-restore__prize-icon " + IconOf(reward), tile);
                Ui.Text(reward.Amount.ToString(), "et-ink et-restore__prize-text", tile, "Amount");
            }
            AddClose();
        }

        Button RepairButton(VisualElement row, int index)
        {
            var button = Rows.Price(row, "Onar", null, ButtonKind.Cta, () =>
            {
                Close();
                _repair(District, index);
            }, "Repair");
            return button;
        }
    }
}
