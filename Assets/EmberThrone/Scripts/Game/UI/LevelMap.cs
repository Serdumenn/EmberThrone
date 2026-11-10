using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class MapChapter
    {
        public readonly string Title;
        public readonly string Style;
        public readonly float Aspect;
        public readonly bool BannerRight;
        public readonly Vector2[] Path;

        public MapChapter(string title, string style, float aspect, bool bannerRight, params float[] path)
        {
            Title = title;
            Style = style;
            Aspect = aspect;
            BannerRight = bannerRight;
            Path = new Vector2[path.Length / 2];
            for (int i = 0; i < Path.Length; i++) Path[i] = new Vector2(path[i * 2], path[i * 2 + 1]);
        }

        public List<Vector2> Spots(int count)
        {
            if (count == Path.Length) return new List<Vector2>(Path);
            const float lead = 0f;
            var lengths = new float[Path.Length];
            for (int i = 1; i < Path.Length; i++)
                lengths[i] = lengths[i - 1] + Vector2.Distance(Scaled(Path[i - 1]), Scaled(Path[i]));
            float total = lengths[Path.Length - 1];

            var spots = new List<Vector2>(count);
            for (int n = 0; n < count; n++)
            {
                float t = count == 1 ? 0.5f : lead + (1f - 2f * lead) * n / (count - 1);
                float target = t * total;
                int seg = 1;
                while (seg < Path.Length - 1 && lengths[seg] < target) seg++;
                float span = lengths[seg] - lengths[seg - 1];
                float k = span <= 0f ? 0f : (target - lengths[seg - 1]) / span;
                spots.Add(Vector2.Lerp(Path[seg - 1], Path[seg], k));
            }
            return spots;
        }

        Vector2 Scaled(Vector2 p) => new Vector2(p.x, p.y * Aspect);
    }

    public static class LevelMap
    {
        public const int LevelsPerChapter = 10;

        public static readonly MapChapter[] Chapters =
        {
            new MapChapter("Kıvılcım Köyü", "et-map__chapter--1", 0.9357f, false,
                58, 88, 42, 77, 62, 67, 40, 58, 51, 45, 31, 38.5f, 15.5f, 30, 32, 22.5f, 44.5f, 14.5f, 60.5f, 9),
            new MapChapter("Fırın Sokağı", "et-map__chapter--2", 0.8672f, false,
                49, 92.5f, 36, 83.8f, 23.5f, 72, 33.6f, 60.3f, 50.5f, 60, 47, 43.5f, 31, 42, 14, 35, 17, 18, 33, 5.5f),
            new MapChapter("Demirci Çarşısı", "et-map__chapter--3", 1.7917f, true,
                49.2f, 94, 47.5f, 85, 44.2f, 76, 46, 67, 53, 59, 52, 50.8f, 39, 44, 46, 36.5f, 39.5f, 29, 48.5f, 21.5f),
        };

        public static int ChapterOf(int levelIndex) => Mathf.Min(levelIndex / LevelsPerChapter, Chapters.Length - 1);
    }
}
