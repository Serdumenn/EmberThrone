// TECH-001 §4 + META-001 — EconomyConfig ScriptableObject
// Coin/gem caps, FTUE generosity, booster fiyatları, can sistemi.
// Remote Config ile runtime override edilebilir (TECH-001 §6.3).

using System;
using UnityEngine;

namespace EmberThrone.Data
{
    /// <summary>
    /// Global economy parameters. Single instance — loaded at boot.
    /// Values can be overridden at runtime via Firebase Remote Config.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "EmberThrone/Economy Config", order = 3)]
    public class EconomyConfig : ScriptableObject
    {
        // ─── Currency Caps ───────────────────────────
        [Header("─── Currency Caps (META-001) ───")]
        [Tooltip("Soft currency (coin) cap")]
        public int coinSoftCap = 5000;

        [Tooltip("Hard currency (gem) cap")]
        public int gemSoftCap = 15000;

        // ─── Level Rewards ───────────────────────────
        [Header("─── Level Completion Rewards ───")]
        [Tooltip("Base coin reward for completing a level")]
        public int levelCompleteCoinBase = 50;

        [Tooltip("Bonus coins per remaining move")]
        public int coinsPerRemainingMove = 10;

        // ─── FTUE Generosity (META-001 §FTUE) ───────
        [Header("─── FTUE Generosity ───")]
        [Tooltip("Starting coin gift (pre-L1)")]
        public int ftueStartingCoins = 500;

        [Tooltip("Starting gem gift (pre-L1)")]
        public int ftueStartingGems = 50;

        [Tooltip("Starting booster: BST-001 Hammer count")]
        public int ftueStartingHammers = 3;

        [Tooltip("Flat coin bonus per level during FTUE (L1-10)")]
        public int ftueLevelCoinBonus = 100;

        // ─── Lives System ────────────────────────────
        [Header("─── Lives (Can) System ───")]
        [Tooltip("Maximum lives count")]
        public int maxLives = 5;

        [Tooltip("Life refill time in seconds (default: 30 min = 1800s)")]
        public int lifeRefillSeconds = 1800;

        [Tooltip("Cost in gems for full life refill")]
        public int fullRefillGemCost = 10;

        // ─── Extra Move (Spend Trigger) ──────────────
        [Header("─── Extra Move — SYS-CORE-001 ───")]
        [Tooltip("Base gem cost for +5 moves offer")]
        public int extraMoveBaseCost = 20;

        [Tooltip("Price multiplier per subsequent offer (max 3 offers)")]
        public float extraMovePriceMultiplier = 1.5f;

        [Tooltip("Max number of extra move offers per level attempt")]
        public int extraMoveMaxOffers = 3;

        [Tooltip("Minimum goal completion % to trigger offer (0.7 = 70%)")]
        [Range(0f, 1f)]
        public float extraMoveGoalThreshold = 0.7f;

        // ─── Booster Costs ───────────────────────────
        [Header("─── Booster Costs ───")]
        [Tooltip("BST-001 Hammer cost in coins")]
        public int hammerCostCoins = 200;

        [Tooltip("BST-002 Bomb cost in coins")]
        public int bombCostCoins = 400;

        [Tooltip("BST-003 ColorBrush cost in coins")]
        public int colorBrushCostCoins = 600;

        // ─── Daily Rewards ───────────────────────────
        [Header("─── Daily Rewards ───")]
        [Tooltip("Daily login coin reward")]
        public int dailyRewardCoins = 100;

        [Tooltip("Daily login gem reward (every 7th day)")]
        public int weeklyBonusGems = 5;

        // ─── Chest Rewards ───────────────────────────
        [Header("─── Chest System ───")]
        [Tooltip("Standard chest coin range (min)")]
        public int standardChestCoinMin = 50;

        [Tooltip("Standard chest coin range (max)")]
        public int standardChestCoinMax = 200;

        [Tooltip("Premium chest gem cost")]
        public int premiumChestGemCost = 30;

        // ─── Validation ──────────────────────────────
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (coinSoftCap <= 0)
                Debug.LogError("[EconomyConfig] coinSoftCap must be > 0");
            if (gemSoftCap <= 0)
                Debug.LogError("[EconomyConfig] gemSoftCap must be > 0");
            if (maxLives <= 0)
                Debug.LogError("[EconomyConfig] maxLives must be > 0");
            if (extraMoveBaseCost <= 0)
                Debug.LogError("[EconomyConfig] extraMoveBaseCost must be > 0");
            if (extraMoveGoalThreshold < 0.5f || extraMoveGoalThreshold > 1f)
                Debug.LogWarning("[EconomyConfig] extraMoveGoalThreshold should be between 0.5 and 1.0");
            if (lifeRefillSeconds < 60)
                Debug.LogWarning("[EconomyConfig] lifeRefillSeconds seems too low (< 1 min)");
        }
#endif
    }
}
