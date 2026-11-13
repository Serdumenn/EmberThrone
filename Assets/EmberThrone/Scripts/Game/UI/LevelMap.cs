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
            new District("Fener Limanı", "et-district__art--4", "Ayaz limanın fenerini söndürdü. Emby gemilere yolu gösteriyor!"),
            new District("Bilge Kütüphanesi", "et-district__art--5", "Ayaz kütüphaneyi kırağıyla örttü. Emby okuma lambalarını yakıyor!"),
            new District("Çiçek Bahçeleri", "et-district__art--6", "Ayaz bahçeleri kırağıyla dondurdu. Emby çiçekleri yeniden açtırıyor!"),
            new District("Kristal Maden", "et-district__art--7", "Ayaz madenin lambalarını söndürdü. Emby kristalleri yeniden parlatıyor!"),
            new District("Karlı Geçit", "et-district__art--8", "Ayaz geçidin gözcü ateşini söndürdü. Emby yolcuların yolunu aydınlatıyor!"),
            new District("Donmuş Göl", "et-district__art--9", "Ayaz gölü buzla kapladı. Emby çan kulesini yeniden çaldırıyor!"),
            new District("Kor Tahtı", "et-district__art--10", "Ayaz Büyük Ocak'ı söndürdü. Emby krallığın kalbini yeniden yakıyor!"),
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
