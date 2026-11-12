namespace EmberThrone.Core
{
    public sealed class Villager
    {
        public readonly string Name;
        public readonly string Avatar;
        public readonly bool Outsider;

        public Villager(string name, string avatar, bool outsider = false)
        {
            Name = name;
            Avatar = avatar;
            Outsider = outsider;
        }
    }

    public static class Village
    {
        public const int HoursPerWeek = 7 * 24;
        public const int SecondsPerHour = 3600;

        public static readonly Villager[] People =
        {
            new Villager("Ayaz", "ayaz_asa", true),
            new Villager("Pofuduk", "ayaz_kartopu", true),
            new Villager("Lina", "fenerci_fener"),
            new Villager("Odo", "demirci_tokmak"),
            new Villager("Mira", "fenerci_pusula"),
            new Villager("Fırıncı Hatçe", "sofra_ekmek"),
            new Villager("Simitçi Cemal", "sofra_simit"),
            new Villager("Kurabiyeci Elif", "sofra_kurabiye"),
            new Villager("Reçelci Nazlı", "sofra_recel"),
            new Villager("Börekçi Sabri", "sofra_borek"),
            new Villager("Çaycı Rıza", "sofra_caydanlik"),
            new Villager("Nalbant Kadir", "demirci_nal"),
            new Villager("Çırak Mert", "demirci_masa"),
            new Villager("Körükçü Tuna", "demirci_koruk"),
            new Villager("Demirci Bora", "demirci_ors"),
            new Villager("Zırhçı Aslı", "demirci_migfer"),
            new Villager("Kibritçi Nil", "fenerci_kibrit"),
            new Villager("Fitilci Yaman", "fenerci_fitil"),
            new Villager("Örgücü Zehra", "fenerci_atki"),
            new Villager("Yağcı Selim", "fenerci_yag"),
            new Villager("Bahçıvan Ece", "bahce_aycicegi"),
            new Villager("Bostancı Ali", "bahce_kova"),
            new Villager("Elmacı Defne", "bahce_elma"),
            new Villager("Çiftçi Hasan", "bahce_balkabagi"),
            new Villager("Arıcı Zeynep", "bahce_kovan"),
            new Villager("Bekçi Kerem", "bahce_korkuluk"),
            new Villager("Kızakçı Can", "ayaz_kartopu"),
            new Villager("Buzcu Deniz", "ayaz_sarkit"),
            new Villager("Eldivenci Ayşe", "ayaz_eldiven"),
            new Villager("Camcı Umut", "ayaz_kure"),
            new Villager("Çancı Yusuf", "ayaz_can"),
            new Villager("Mumcu İpek", "kor_mum"),
            new Villager("Kömürcü Efe", "kor_komur"),
            new Villager("Pırıl", "kor_atesbocegi"),
            new Villager("Kuyumcu Asya", "kor_kolye"),
            new Villager("Bakırcı Emre", "kor_kupa"),
            new Villager("Ocakçı Derya", "kor_kalp"),
        };

        public static int PointsBy(long seed, ulong stream, long start, long now, int busyMinPercent, int busyMaxPercent, int gainMax)
        {
            var rng = new Pcg32((ulong)seed, stream);
            int busy = rng.Next(busyMinPercent, busyMaxPercent + 1);
            long hours = (now - start) / SecondsPerHour;
            if (hours > HoursPerWeek) hours = HoursPerWeek;
            int points = 0;
            for (long h = 0; h < hours; h++)
                if (rng.Next(100) < busy) points += rng.Next(1, gainMax + 1);
            return points;
        }
    }
}
