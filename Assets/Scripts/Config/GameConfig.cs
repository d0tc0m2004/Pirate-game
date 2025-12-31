using UnityEngine;

namespace TacticalGame.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tactical/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== STAT GENERATION RANGES ===")]
        
        [Header("Health Ranges")]
        public int healthLowMin = 510;
        public int healthLowMax = 600;
        public int healthMidMin = 600;
        public int healthMidMax = 720;
        public int healthHighMin = 720;
        public int healthHighMax = 840;

        [Header("Morale Ranges")]
        public int moraleLowMin = 765;
        public int moraleLowMax = 900;
        public int moraleMidMin = 900;
        public int moraleMidMax = 1080;
        public int moraleHighMin = 1080;
        public int moraleHighMax = 1260;

        [Header("Buzz Ranges")]
        public int buzzLowMin = 80;
        public int buzzLowMax = 100;
        public int buzzMidMin = 100;
        public int buzzMidMax = 130;
        public int buzzHighMin = 130;
        public int buzzHighMax = 160;

        [Header("Power Ranges")]
        public int powerLowMin = 15;
        public int powerLowMax = 25;
        public int powerMidMin = 26;
        public int powerMidMax = 35;
        public int powerHighMin = 36;
        public int powerHighMax = 50;

        [Header("Aim Ranges")]
        public int aimLowMin = 15;
        public int aimLowMax = 25;
        public int aimMidMin = 26;
        public int aimMidMax = 35;
        public int aimHighMin = 36;
        public int aimHighMax = 50;

        [Header("Tactics Ranges")]
        public int tacticsLowMin = 15;
        public int tacticsLowMax = 25;
        public int tacticsMidMin = 26;
        public int tacticsMidMax = 35;
        public int tacticsHighMin = 36;
        public int tacticsHighMax = 50;

        [Header("Skill Ranges")]
        public int skillLowMin = 15;
        public int skillLowMax = 25;
        public int skillMidMin = 26;
        public int skillMidMax = 35;
        public int skillHighMin = 36;
        public int skillHighMax = 50;

        [Header("Proficiency Ranges (as percentage, e.g., 105 = 1.05x)")]
        public int proficiencyLowMin = 105;
        public int proficiencyLowMax = 125;
        public int proficiencyMidMin = 125;
        public int proficiencyMidMax = 150;
        public int proficiencyHighMin = 150;
        public int proficiencyHighMax = 200;

        [Header("Grit Ranges")]
        public int gritLowMin = 15;
        public int gritLowMax = 25;
        public int gritMidMin = 26;
        public int gritMidMax = 35;
        public int gritHighMin = 36;
        public int gritHighMax = 50;

        [Header("Hull Ranges")]
        public int hullLowMin = 15;
        public int hullLowMax = 25;
        public int hullMidMin = 26;
        public int hullMidMax = 35;
        public int hullHighMin = 36;
        public int hullHighMax = 50;

        [Header("Speed Ranges")]
        public int speedLowMin = 15;
        public int speedLowMax = 25;
        public int speedMidMin = 26;
        public int speedMidMax = 35;
        public int speedHighMin = 36;
        public int speedHighMax = 50;

        [Header("=== COMBAT BALANCE ===")]
        
        [Header("Base Damage (from weapons)")]
        public int meleeBaseDamage = 10;
        public int rangedBaseDamage = 8;
        
        [Header("Stat Scaling (from stat table)")]
        [Tooltip("MeleeOutput = Base × (1 + Power × 0.03)")]
        public float powerScalingPercent = 0.03f;  // +3% per Power point
        
        [Tooltip("RangedOutput = Base × (1 + Aim × 0.03)")]
        public float aimScalingPercent = 0.03f;    // +3% per Aim point
        
        [Tooltip("Potency = Base × (1 + Tactics × 0.04) for heals/buffs/debuffs")]
        public float tacticsScalingPercent = 0.04f; // +4% per Tactics point
        
        [Header("Skill - Combo System")]
        [Tooltip("ComboStep = clamp(Skill × skillComboMultiplier, comboStepMin, comboStepMax)")]
        public float skillComboMultiplier = 0.003f;
        public float comboStepMin = 0.02f;   // 2% minimum combo bonus
        public float comboStepMax = 0.12f;   // 12% maximum combo bonus
        [Tooltip("Maximum combo chain count (recommended n ≤ 6)")]
        public int maxComboChain = 6;

        [Header("Damage Modifiers")]
        [Range(0.5f, 1f)]
        public float drunkDamageMultiplier = 0.8f;
        
        public float rangedHPMultiplier = 1.1f;
        public float meleeMoraleMultiplier = 1.1f;
        
        [Range(0f, 0.5f)]
        public float adjacencyCoverReduction = 0.1f;
        
        [Range(1f, 2f)]
        public float exposedDamageMultiplier = 1.2f;

        [Header("Curse System")]
        public float defaultCurseMultiplier = 1.5f;
        public int defaultCurseCharges = 2;

        [Header("Focus Fire System")]
        public float[] focusFireMultipliers = { 0f, 0f, 0.10f, 0.25f, 0.45f, 0.65f };

        [Header("=== BUZZ/RUM SYSTEM ===")]
        public int buzzPerDrink = 30;
        public int healthRumRestore = 20;
        public int moraleRumRestore = 20;
        public int buzzDecayPerTurn = 15;
        public int buzzDecayOnAttack = 25;
        public int maxBuzz = 100;

        [Header("=== SWAP SYSTEM ===")]
        public int swapEnergyCost = 1;
        public int swapCooldownTurns = 3;
        public int maxSwapsPerRound = 1;
        
        [Range(0f, 0.5f)]
        public float swapMoralePenalty = 0.15f;
        
        [Range(0f, 0.5f)]
        public float minHPPercentToSwap = 0.2f;

        [Header("=== SURRENDER SYSTEM ===")]
        public int surrenderThreshold = 20;

        [Header("=== ENERGY SYSTEM ===")]
        public int energyPerTurn = 3;
        public int attackEnergyCost = 1;

        [Header("=== SPEED/INITIATIVE SYSTEM ===")]
        public float speedToInitiative = 1f;        // Each +1 Speed = +1 Team Initiative
        public float firstActionBonusPerSpeed = 0.002f;  // +0.2% damage per Speed if going first
        public float firstActionBonusCap = 0.15f;   // Cap at 15% bonus damage
        
        [Header("=== GRIT SYSTEM (Damage Reduction) ===")]
        [Tooltip("GritFactor = (1-HP%) × gritLowHPWeight + (Morale%) × gritMoraleWeight")]
        public float gritLowHPWeight = 0.50f;      // 50% weight for low HP
        public float gritMoraleWeight = 0.40f;     // 40% weight for morale
        [Tooltip("DR = min(DRCap, GritFactor × (Grit/100))")]
        public float gritDRCap = 0.40f;            // 40% max damage reduction
        [Tooltip("+1% of GritFactor per Grit point")]
        public float gritPerPointPercent = 0.01f;  // Each Grit adds 1% of GritFactor to DR
        
        [Header("=== HULL SYSTEM ===")]
        public int baseHull = 50;
        public int hullPerPoint = 10;
        [Range(0.1f, 0.5f)]
        public float hullAbsorbPercent = 0.30f; // Absorbs up to 30% of incoming damage
        
        [Header("=== OBSTACLE SYSTEM ===")]
        public int obstacleBlockDamage = 100;

        [Header("=== TIMING ===")]
        public float enemyTurnDelay = 1.5f;
        public float moveAnimationSpeed = 5f;

        [Header("=== ARROWS ===")]
        public int defaultMaxArrows = 10;

        // Singleton access
        private static GameConfig _instance;
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("GameConfig not found in Resources folder! Creating default.");
                        _instance = CreateInstance<GameConfig>();
                    }
                }
                return _instance;
            }
        }

        #region Stat Range Helpers

        public (int min, int max) GetHealthRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (healthLowMin, healthLowMax),
                StatRangeType.Mid => (healthMidMin, healthMidMax),
                StatRangeType.High => (healthHighMin, healthHighMax),
                _ => (healthLowMin, healthLowMax)
            };
        }

        public (int min, int max) GetMoraleRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (moraleLowMin, moraleLowMax),
                StatRangeType.Mid => (moraleMidMin, moraleMidMax),
                StatRangeType.High => (moraleHighMin, moraleHighMax),
                _ => (moraleLowMin, moraleLowMax)
            };
        }

        public (int min, int max) GetBuzzRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (buzzLowMin, buzzLowMax),
                StatRangeType.Mid => (buzzMidMin, buzzMidMax),
                StatRangeType.High => (buzzHighMin, buzzHighMax),
                _ => (buzzLowMin, buzzLowMax)
            };
        }

        public (int min, int max) GetPowerRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (powerLowMin, powerLowMax),
                StatRangeType.Mid => (powerMidMin, powerMidMax),
                StatRangeType.High => (powerHighMin, powerHighMax),
                _ => (powerLowMin, powerLowMax)
            };
        }

        public (int min, int max) GetAimRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (aimLowMin, aimLowMax),
                StatRangeType.Mid => (aimMidMin, aimMidMax),
                StatRangeType.High => (aimHighMin, aimHighMax),
                _ => (aimLowMin, aimLowMax)
            };
        }

        public (int min, int max) GetTacticsRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (tacticsLowMin, tacticsLowMax),
                StatRangeType.Mid => (tacticsMidMin, tacticsMidMax),
                StatRangeType.High => (tacticsHighMin, tacticsHighMax),
                _ => (tacticsLowMin, tacticsLowMax)
            };
        }

        public (int min, int max) GetSkillRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (skillLowMin, skillLowMax),
                StatRangeType.Mid => (skillMidMin, skillMidMax),
                StatRangeType.High => (skillHighMin, skillHighMax),
                _ => (skillLowMin, skillLowMax)
            };
        }

        public (int min, int max) GetProficiencyRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (proficiencyLowMin, proficiencyLowMax),
                StatRangeType.Mid => (proficiencyMidMin, proficiencyMidMax),
                StatRangeType.High => (proficiencyHighMin, proficiencyHighMax),
                _ => (proficiencyLowMin, proficiencyLowMax)
            };
        }

        public (int min, int max) GetGritRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (gritLowMin, gritLowMax),
                StatRangeType.Mid => (gritMidMin, gritMidMax),
                StatRangeType.High => (gritHighMin, gritHighMax),
                _ => (gritLowMin, gritLowMax)
            };
        }

        public (int min, int max) GetHullRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (hullLowMin, hullLowMax),
                StatRangeType.Mid => (hullMidMin, hullMidMax),
                StatRangeType.High => (hullHighMin, hullHighMax),
                _ => (hullLowMin, hullLowMax)
            };
        }

        public (int min, int max) GetSpeedRange(StatRangeType rangeType)
        {
            return rangeType switch
            {
                StatRangeType.Low => (speedLowMin, speedLowMax),
                StatRangeType.Mid => (speedMidMin, speedMidMax),
                StatRangeType.High => (speedHighMin, speedHighMax),
                _ => (speedLowMin, speedLowMax)
            };
        }

        #endregion
    }

    public enum StatRangeType
    {
        Low,
        Mid,
        High
    }
}