namespace EmberThrone.Game
{
    public enum Impact
    {
        Match,
        SpecialMade,
        Rocket,
        Bomb,
        ColorBomb,
        Combo,
        BigCombo,
        Firework,
        Win,
        Lose,
    }

    public static class Feel
    {
        public static float TraumaOf(Impact impact)
        {
            switch (impact)
            {
                case Impact.Rocket: return 0.22f;
                case Impact.Bomb: return 0.38f;
                case Impact.ColorBomb: return 0.32f;
                case Impact.Combo: return 0.55f;
                case Impact.BigCombo: return 0.75f;
                case Impact.Firework: return 0.12f;
                default: return 0f;
            }
        }

        public static Sfx SoundOf(Impact impact)
        {
            switch (impact)
            {
                case Impact.Match: return Sfx.Match;
                case Impact.SpecialMade: return Sfx.SpecialMade;
                case Impact.Rocket: return Sfx.Rocket;
                case Impact.Bomb: return Sfx.Bomb;
                case Impact.ColorBomb: return Sfx.ColorBomb;
                case Impact.Combo:
                case Impact.BigCombo:
                    return Sfx.Combo;
                case Impact.Firework: return Sfx.Firework;
                case Impact.Win: return Sfx.Win;
                default: return Sfx.Lose;
            }
        }

        public static HapticLevel HapticOf(Impact impact)
        {
            switch (impact)
            {
                case Impact.Match:
                case Impact.SpecialMade:
                case Impact.Firework:
                    return HapticLevel.Light;
                case Impact.Rocket:
                case Impact.Bomb:
                case Impact.ColorBomb:
                case Impact.Lose:
                    return HapticLevel.Medium;
                default:
                    return HapticLevel.Strong;
            }
        }
    }
}
