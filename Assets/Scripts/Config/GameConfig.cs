using UnityEngine;

namespace TacticalGame.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tactical/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== STAT GENERATION RANGES ===")]
        
        [Header("Health Ranges")]
        public int healthLowMin = 18;
        public int healthLowMax = 22;
        public int healthMidMin = 23;
        public int healthMidMax = 27;
        public int healthHighMin = 28;
        public int healthHighMax = 32;

        [Header("Morale Ranges")]
        public int moraleLowMin = 30;
        public int moraleLowMax = 36;
        public int moraleMidMin = 37;
        public int moraleMidMax = 44;
        public int moraleHighMin = 45;
        public int moraleHighMax = 50;

        [Header("Buzz Ranges")]
        public int buzzLowMin = 3;
        public int buzzLowMax = 4;
        public int buzzMidMin = 5;
        public int buzzMidMax = 6;
        public int buzzHighMin = 7;
        public int buzzHighMax = 8;

        [Header("Power Ranges")]
        public int powerLowMin = 1;
        public int powerLowMax = 4;
        public int powerMidMin = 5;
        public int powerMidMax = 7;
        public int powerHighMin = 8;
        public int powerHighMax = 10;

        [Header("Aim Ranges")]
        public int aimLowMin = 1;
        public int aimLowMax = 4;
        public int aimMidMin = 5;
        public int aimMidMax = 7;
        public int aimHighMin = 8;
        public int aimHighMax = 10;

        [Header("Tactics Ranges")]
        public int tacticsLowMin = 1;
        public int tacticsLowMax = 4;
        public int tacticsMidMin = 5;
        public int tacticsMidMax = 7;
        public int tacticsHighMin = 8;
        public int tacticsHighMax = 10;

        [Header("Skill Ranges")]
        public int skillLowMin = 1;
        public int skillLowMax = 4;
        public int skillMidMin = 5;
        public int skillMidMax = 7;
        public int skillHighMin = 8;
        public int skillHighMax = 10;

        [Header("Proficiency Ranges")]
        public int proficiencyLowMin = 1;
        public int proficiencyLowMax = 4;
        public int proficiencyMidMin = 5;
        public int proficiencyMidMax = 7;
        public int proficiencyHighMin = 8;
        public int proficiencyHighMax = 10;

        [Header("Grit Ranges")]
        public int gritLowMin = 1;
        public int gritLowMax = 4;
        public int gritMidMin = 5;
        public int gritMidMax = 7;
        public int gritHighMin = 8;
        public int gritHighMax = 10;

        [Header("Hull Ranges")]
        public int hullLowMin = 0;
        public int hullLowMax = 3;
        public int hullMidMin = 4;
        public int hullMidMax = 6;
        public int hullHighMin = 7;
        public int hullHighMax = 10;

        [Header("Speed Ranges")]
        public int speedLowMin = 1;
        public int speedLowMax = 4;
        public int speedMidMin = 5;
        public int speedMidMax = 7;
        public int speedHighMin = 8;
        public int speedHighMax = 10;

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
        public int buzzPerDrink = 2;
        public int healthRumRestore = 4;
        public int moraleRumRestore = 6;
        public int buzzDecayPerTurn = 1;
        public int buzzDecayOnAttack = 1;
        public int maxBuzz = 8;

        [Header("=== SWAP SYSTEM ===")]
        public int swapEnergyCost = 1;
        public int swapCooldownTurns = 3;
        public int maxSwapsPerRound = 1;
        
        [Range(0f, 0.5f)]
        public float swapMoralePenalty = 0.15f;
        
        [Range(0f, 0.5f)]
        public float minHPPercentToSwap = 0.2f;

        [Header("=== SURRENDER SYSTEM ===")]
        public float surrenderThreshold = 20;

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
        public int baseHull = 0;
        public int hullPerPoint = 1;
        [Range(0.1f, 1f)]
        public float hullAbsorbPercent = 1.00f; // Hull absorbs damage fully before HP now
        
        [Header("=== OBSTACLE SYSTEM ===")]
        public int obstacleBlockDamage = 100;

        [Header("=== TIMING ===")]
        public float enemyTurnDelay = 1.5f;
        public float moveAnimationSpeed = 5f;

        [Header("=== DEAD MAN'S LOCKER ===")]
        [Tooltip("Base hit pips per locker")]
        public int lockerBasePips = 3;
        [Tooltip("Max bonus pips from fortification")]
        public int lockerMaxFortifyPips = 2;
        [Tooltip("Min lockers for smallest grid (7-wide)")]
        public int lockerMinCount = 1;
        [Tooltip("Max lockers for largest grid (15-wide)")]
        public int lockerMaxCount = 4;
        [Tooltip("Min Manhattan distance between lockers")]
        public int lockerMinDistance = 2;
        [Tooltip("Min tiles away from any player unit at spawn")]
        public int lockerMinDistanceFromUnits = 1;
        [Tooltip("Tribute lost permanently when locker destroyed (0-1)")]
        public float lockerDestroyTributeLoss = 0.50f;
        [Tooltip("Tribute leaked per hit (0-1)")]
        public float lockerHitTributeLeak = 0.10f;
        [Tooltip("Base tribute quota per battle")]
        public float lockerTributeQuota = 100f;

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