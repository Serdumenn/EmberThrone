using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class Profile
    {
        public const int BoosterKinds = 4;

        public int Coins;
        public int Gems;
        public int Lives;
        public long LifeAnchor;
        public long Seed;
        public int[] Boosters = new int[BoosterKinds];

        public int DailyDay = -1;
        public int DailyStreak;

        public int AdDay = -1;
        public int AdLives;
        public int AdCoins;

        public int QuestDay = -1;
        public int[] QuestProgress = new int[0];
        public bool[] QuestClaimed = new bool[0];
        public bool QuestBonusClaimed;

        public int ChestsOpened;

        public int Packs;
        public int PacksOpened;
        public int[] Cards = new int[0];
        public bool[] SetsClaimed = new bool[0];
        public bool GrandClaimed;

        public int RaceWeek = -1;
        public int RacePoints;
        public int RaceResultWeek = -1;
        public int RaceResultPoints;

        public int Team = -1;
        public long TeamRequestAt;
        public int TeamGifts;
        public int[] TeamHelped = new int[0];
        public int TeamHelps;
        public int TeamHelpsWeek = -1;

        public int EventId = -1;
        public int EventPoints;
        public int EventClaimed;
        public long EventDoneAt;

        public int ScenesSeen;

        public static Profile New(EconomyRules rules, long seed)
        {
            var p = new Profile
            {
                Coins = rules.StartCoins,
                Gems = rules.StartGems,
                Lives = rules.MaxLives,
                Seed = seed,
                Packs = rules.StartPacks,
            };
            for (int i = 0; i < BoosterKinds; i++) p.Boosters[i] = rules.StartBoosters;
            return p;
        }

        public string ToJson()
        {
            var o = new Dictionary<string, object>
            {
                ["coins"] = Coins,
                ["gems"] = Gems,
                ["lives"] = Lives,
                ["lifeAnchor"] = LifeAnchor.ToString(),
                ["seed"] = Seed.ToString(),
                ["boosters"] = ToList(Boosters),
                ["dailyDay"] = DailyDay,
                ["dailyStreak"] = DailyStreak,
                ["adDay"] = AdDay,
                ["adLives"] = AdLives,
                ["adCoins"] = AdCoins,
                ["questDay"] = QuestDay,
                ["questProgress"] = ToList(QuestProgress),
                ["questClaimed"] = ToList(Array.ConvertAll(QuestClaimed, c => c ? 1 : 0)),
                ["questBonus"] = QuestBonusClaimed,
                ["chests"] = ChestsOpened,
                ["packs"] = Packs,
                ["packsOpened"] = PacksOpened,
                ["cards"] = ToList(Cards),
                ["setsClaimed"] = ToList(Array.ConvertAll(SetsClaimed, c => c ? 1 : 0)),
                ["grandClaimed"] = GrandClaimed,
                ["raceWeek"] = RaceWeek,
                ["racePoints"] = RacePoints,
                ["raceResultWeek"] = RaceResultWeek,
                ["raceResultPoints"] = RaceResultPoints,
                ["team"] = Team,
                ["teamRequestAt"] = TeamRequestAt.ToString(),
                ["teamGifts"] = TeamGifts,
                ["teamHelped"] = ToList(TeamHelped),
                ["teamHelps"] = TeamHelps,
                ["teamHelpsWeek"] = TeamHelpsWeek,
                ["eventId"] = EventId,
                ["eventPoints"] = EventPoints,
                ["eventClaimed"] = EventClaimed,
                ["eventDoneAt"] = EventDoneAt.ToString(),
                ["scenesSeen"] = ScenesSeen,
            };
            return MiniJson.Serialize(o);
        }

        public static Profile FromJson(string json, EconomyRules rules)
        {
            var o = MiniJson.AsObject(MiniJson.Parse(json), "profile");
            var p = new Profile
            {
                Coins = MiniJson.GetInt(o, "coins", rules.StartCoins),
                Gems = MiniJson.GetInt(o, "gems", rules.StartGems),
                Lives = MiniJson.GetInt(o, "lives", rules.MaxLives),
                LifeAnchor = ParseLong(MiniJson.GetString(o, "lifeAnchor", "0")),
                Seed = ParseLong(MiniJson.GetString(o, "seed", "1")),
                DailyDay = MiniJson.GetInt(o, "dailyDay", -1),
                DailyStreak = MiniJson.GetInt(o, "dailyStreak", 0),
                AdDay = MiniJson.GetInt(o, "adDay", -1),
                AdLives = MiniJson.GetInt(o, "adLives", 0),
                AdCoins = MiniJson.GetInt(o, "adCoins", 0),
                QuestDay = MiniJson.GetInt(o, "questDay", -1),
                QuestBonusClaimed = MiniJson.GetBool(o, "questBonus", false),
                ChestsOpened = MiniJson.GetInt(o, "chests", 0),
                Packs = MiniJson.GetInt(o, "packs", rules.StartPacks),
                PacksOpened = MiniJson.GetInt(o, "packsOpened", 0),
                GrandClaimed = MiniJson.GetBool(o, "grandClaimed", false),
                RaceWeek = MiniJson.GetInt(o, "raceWeek", -1),
                RacePoints = MiniJson.GetInt(o, "racePoints", 0),
                RaceResultWeek = MiniJson.GetInt(o, "raceResultWeek", -1),
                RaceResultPoints = MiniJson.GetInt(o, "raceResultPoints", 0),
                Team = MiniJson.GetInt(o, "team", -1),
                TeamRequestAt = ParseLong(MiniJson.GetString(o, "teamRequestAt", "0")),
                TeamGifts = MiniJson.GetInt(o, "teamGifts", 0),
                TeamHelps = MiniJson.GetInt(o, "teamHelps", 0),
                TeamHelpsWeek = MiniJson.GetInt(o, "teamHelpsWeek", -1),
                EventId = MiniJson.GetInt(o, "eventId", -1),
                EventPoints = MiniJson.GetInt(o, "eventPoints", 0),
                EventClaimed = MiniJson.GetInt(o, "eventClaimed", 0),
                EventDoneAt = ParseLong(MiniJson.GetString(o, "eventDoneAt", "0")),
                ScenesSeen = MiniJson.GetInt(o, "scenesSeen", 0),
            };
            p.TeamHelped = ReadInts(o, "teamHelped");
            p.Cards = ReadInts(o, "cards");
            p.SetsClaimed = Array.ConvertAll(ReadInts(o, "setsClaimed"), v => v != 0);
            var boosters = ReadInts(o, "boosters");
            for (int i = 0; i < BoosterKinds; i++) p.Boosters[i] = i < boosters.Length ? boosters[i] : rules.StartBoosters;
            p.QuestProgress = ReadInts(o, "questProgress");
            p.QuestClaimed = Array.ConvertAll(ReadInts(o, "questClaimed"), v => v != 0);
            return p;
        }

        static List<object> ToList(int[] values)
        {
            var list = new List<object>(values.Length);
            foreach (int v in values) list.Add(v);
            return list;
        }

        static int[] ReadInts(Dictionary<string, object> o, string key)
        {
            if (!MiniJson.TryGet(o, key, out var raw)) return new int[0];
            var list = MiniJson.AsArray(raw, key);
            var result = new int[list.Count];
            for (int i = 0; i < result.Length; i++) result[i] = Convert.ToInt32(list[i]);
            return result;
        }

        static long ParseLong(string text) => long.TryParse(text, out long value) ? value : 0L;
    }
}
