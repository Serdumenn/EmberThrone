namespace EmberThrone.Game
{
    /// <summary>Board events that the player should feel, from smallest to biggest.</summary>
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

    /// <summary>
    /// How strongly each event shakes the camera and the phone (design-system.md, "Kutlama katmanları").
    /// Small things only buzz; specials shake a little; combos and the win shake hard. One table keeps it proportional.
    /// </summary>
    public static class Feel
    {
        /// <summary>Camera trauma 0..1 (the shake grows with its square).</summary>
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
