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
        
        [Header("Base Damage")]
        public int meleeBaseDamage = 10;
        public int rangedBaseDamage = 8;
        
        [Range(0f, 1f)]
        public float powerScaling = 0.4f;
        
        [Range(0f, 1f)]
        public float aimScaling = 0.4f;

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