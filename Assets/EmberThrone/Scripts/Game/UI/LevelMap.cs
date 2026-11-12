using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class District
    {
        public readonly string Title;
        public readonly string Style;

        public District(string title, string style)
        {
            Title = title;
            Style = style;
        }
    }

    public static class LevelMap
    {
        public const int LevelsPerChapter = 10;

        public static readonly District[] Chapters =
        {
            new District("Kıvılcım Köyü", "et-district__art--1"),
            new District("Fırın Sokağı", "et-district__art--2"),
            new District("Demirci Çarşısı", "et-district__art--3"),
        };

        public static int ChapterOf(int levelIndex) => Mathf.Min(levelIndex / LevelsPerChapter, Chapters.Length - 1);
    }
}
