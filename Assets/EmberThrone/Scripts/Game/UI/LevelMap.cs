using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class District
    {
        public readonly string Title;
        public readonly string Style;
        public readonly string Story;

        public District(string title, string style, string story)
        {
            Title = title;
            Style = style;
            Story = story;
        }
    }

    public static class LevelMap
    {
        public const int LevelsPerChapter = 10;

        public static readonly District[] Chapters =
        {
            new District("Kıvılcım Köyü", "et-district__art--1", "Ayaz köyün ocağını buzla tıkadı. Emby onu yeniden yakıyor!"),
            new District("Fırın Sokağı", "et-district__art--2", "Ayaz fırının bacasını dondurdu. Emby fırını yakıyor!"),
            new District("Demirci Çarşısı", "et-district__art--3", "Ayaz demirci ocağını söndürdü. Emby koru geri getiriyor!"),
        };

        public static string SceneOf(int chapter) => "scene_" + (chapter + 1);

        public static int ChapterStarting(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex % LevelsPerChapter != 0) return -1;
            int chapter = levelIndex / LevelsPerChapter;
            return chapter < Chapters.Length ? chapter : -1;
        }

        public static int ChapterOf(int levelIndex) => Mathf.Min(levelIndex / LevelsPerChapter, Chapters.Length - 1);
    }
}
