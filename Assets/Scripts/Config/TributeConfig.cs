using UnityEngine;

namespace TacticalGame.Config
{
    public enum Difficulty { Easy, Normal, Hard }
    public enum TributeState { Filling, Paid, Cracked }

    [System.Serializable]
    public class TributeConfig 
    {
        public int   battleTurnCount       = 5;
        public float hpToMoraleRatio       = 0.60f;
        public float meleeMoraleMult       = 1.10f;
        public float rangedMoraleMult      = 1.00f;
        public float spillRate             = 1.00f;
        public float freshPlunderMult      = 1.50f;
        public float lootBurstChance       = 0.15f;
        public float chestMissChance       = 0.20f;
        public float shockRatio            = 0.25f;
        public float shockCapPct           = 0.05f;
        public float surrenderSpillPct     = 0.50f;
        public float deathSpillPct         = 0.25f;
        public float leakPctPerPip         = 0.10f;
        public float chestDestroyLostPct   = 0.50f;
        public int   chestBasePips         = 3;
        public float fortifyThresholdPct   = 0.25f;
        public int   maxFortifyPips        = 2;
        public float looseAutoSweepEff     = 0.50f;
        public float surrenderThreshold    = 0.20f;
        public float greedTickInterval     = 0.25f;

        public static float GetQuotaPct(Difficulty d) => d switch {
            Difficulty.Easy   => 0.24f,
            Difficulty.Normal => 0.30f,
            Difficulty.Hard   => 0.36f,
            _ => 0.30f
        };
    }
}
