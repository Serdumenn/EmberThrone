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

        public static readonly EconomyRules Rules = new EconomyRules();
        public static readonly CollectionRules CardRules = new CollectionRules();

        public static Func<long> Clock = WallClock;

        public static event Action Changed;

        public static long Now => Clock();

        public static Economy Economy
        {
            get
            {
                if (_economy == null) Load();
                _economy.Tick(Now);
                return _economy;
            }
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
            _economy.Tick(Now);
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
        }

        public static string Countdown(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            string tail = (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            return seconds >= 3600 ? (seconds / 3600).ToString("00") + ":" + tail : tail;
        }
    }
}
