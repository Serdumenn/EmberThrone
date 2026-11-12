using System;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public static class Meta
    {
        public const string ProfileKey = "et.profile";
        const string OldBoosterKey = "et.booster.";

        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        static Economy _economy;
        static CardCollection _cards;
        static Race _race;
        static Team _team;
        static LiveEvent _event;

        public static readonly EconomyRules Rules = new EconomyRules();
        public static readonly CollectionRules CardRules = new CollectionRules();
        public static readonly RaceRules RaceRules = new RaceRules();
        public static readonly TeamRules TeamRules = new TeamRules();
        public static readonly EventRules EventRules = new EventRules();

        public static Func<long> Clock = WallClock;

        public static event Action Changed;

        public static long Now => Clock();

        public static Economy Economy
        {
            get
            {
                if (_economy == null) Load();
                Tick();
                return _economy;
            }
        }

        public static Race Race
        {
            get
            {
                if (_economy == null) Load();
                Tick();
                return _race;
            }
        }

        public static Team Team
        {
            get
            {
                if (_economy == null) Load();
                Tick();
                return _team;
            }
        }

        public static LiveEvent Event
        {
            get
            {
                if (_economy == null) Load();
                Tick();
                return _event;
            }
        }

        static void Tick()
        {
            long now = Now;
            _economy.Tick(now);
            _race.Tick(now);
            _team.Tick(now);
            _event.Tick(now);
        }

        public static CardCollection Cards
        {
            get
            {
                if (_economy == null) Load();
                return _cards;
            }
        }

        public static long WallClock() => (long)(DateTime.Now - Epoch).TotalSeconds;

        public static void Load()
        {
            Profile profile = null;
            if (PlayerPrefs.HasKey(ProfileKey))
            {
                try
                {
                    profile = Profile.FromJson(PlayerPrefs.GetString(ProfileKey), Rules);
                }
                catch (FormatException e)
                {
                    Debug.LogWarning("[EmberThrone] Saved profile could not be read, starting a new one: " + e.Message);
                }
            }
            if (profile == null)
            {
                profile = Profile.New(Rules, DateTime.UtcNow.Ticks);
                foreach (var type in BoosterInventory.All)
                    if (PlayerPrefs.HasKey(OldBoosterKey + type))
                        profile.Boosters[(int)type] = Mathf.Clamp(PlayerPrefs.GetInt(OldBoosterKey + type), 0, Rules.BoosterCap);
            }
            _economy = new Economy(Rules, profile);
            _cards = new CardCollection(CardRules, _economy);
            _race = new Race(RaceRules, _economy);
            _team = new Team(TeamRules, _economy, _race);
            _event = new LiveEvent(EventRules, _economy);
            Tick();
        }

        public static void Save()
        {
            if (_economy == null) return;
            PlayerPrefs.SetString(ProfileKey, _economy.Profile.ToJson());
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Forget()
        {
            _economy = null;
            _cards = null;
            _race = null;
            _team = null;
            _event = null;
        }

        public static string Span(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int days = seconds / 86400, hours = seconds / 3600 % 24, minutes = seconds / 60 % 60;
            if (days > 0) return days + "g " + hours + "s";
            if (hours > 0) return hours + "s " + minutes + "dk";
            return Mathf.Max(1, minutes) + "dk";
        }

        public static string Countdown(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            string tail = (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            return seconds >= 3600 ? (seconds / 3600).ToString("00") + ":" + tail : tail;
        }
    }
}
