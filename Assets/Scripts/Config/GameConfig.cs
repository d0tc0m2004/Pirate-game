using UnityEngine;

namespace TacticalGame.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tactical/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== COMBAT BALANCE ===")]
        
        [Header("Base Damage")]
        [Tooltip("Base damage for melee attacks before stat scaling")]
        public int meleeBaseDamage = 10;
        
        [Tooltip("Base damage for ranged attacks before stat scaling")]
        public int rangedBaseDamage = 8;
        
        [Tooltip("How much Power stat contributes to melee damage (multiplier)")]
        [Range(0f, 1f)]
        public float powerScaling = 0.4f;
        
        [Tooltip("How much Aim stat contributes to ranged damage (multiplier)")]
        [Range(0f, 1f)]
        public float aimScaling = 0.4f;

        [Header("Damage Modifiers")]
        [Tooltip("Damage multiplier when unit is too drunk")]
        [Range(0.5f, 1f)]
        public float drunkDamageMultiplier = 0.8f;
        
        [Tooltip("Extra damage multiplier for ranged attacks on HP")]
        public float rangedHPMultiplier = 1.1f;
        
        [Tooltip("Extra damage multiplier for melee attacks on Morale")]
        public float meleeMoraleMultiplier = 1.1f;
        
        [Tooltip("Damage reduction when adjacent to hazard (cover)")]
        [Range(0f, 0.5f)]
        public float adjacencyCoverReduction = 0.1f;
        
        [Tooltip("Extra damage taken when exposed after swap")]
        [Range(1f, 2f)]
        public float exposedDamageMultiplier = 1.2f;

        [Header("Curse System")]
        [Tooltip("Default curse damage multiplier")]
        public float defaultCurseMultiplier = 1.5f;
        
        [Tooltip("How many hits before curse wears off")]
        public int defaultCurseCharges = 2;

        [Header("Focus Fire System")]
        [Tooltip("Morale damage bonus per focus fire stack (index = stack count)")]
        public float[] focusFireMultipliers = { 0f, 0f, 0.10f, 0.25f, 0.45f, 0.65f };

        [Header("=== BUZZ/RUM SYSTEM ===")]
        
        [Tooltip("Buzz gained per rum drink")]
        public int buzzPerDrink = 30;
        
        [Tooltip("HP restored by Health rum")]
        public int healthRumRestore = 20;
        
        [Tooltip("Morale restored by Morale rum")]
        public int moraleRumRestore = 20;
        
        [Tooltip("Buzz decay per turn")]
        public int buzzDecayPerTurn = 15;
        
        [Tooltip("Buzz decay on attack")]
        public int buzzDecayOnAttack = 25;
        
        [Tooltip("Max buzz before 'too drunk'")]
        public int maxBuzz = 100;

        [Header("=== SWAP SYSTEM ===")]
        
        [Tooltip("Energy cost to swap positions")]
        public int swapEnergyCost = 1;
        
        [Tooltip("Cooldown turns after swapping")]
        public int swapCooldownTurns = 3;
        
        [Tooltip("Max swaps allowed per round")]
        public int maxSwapsPerRound = 1;
        
        [Tooltip("Morale penalty percentage when swapping (non-captain)")]
        [Range(0f, 0.5f)]
        public float swapMoralePenalty = 0.15f;
        
        [Tooltip("Minimum HP percentage required to swap")]
        [Range(0f, 0.5f)]
        public float minHPPercentToSwap = 0.2f;

        [Header("=== SURRENDER SYSTEM ===")]
        
        [Tooltip("Morale threshold below which units surrender")]
        public int surrenderThreshold = 20;

        [Header("=== ENERGY SYSTEM ===")]
        
        [Tooltip("Energy gained per turn")]
        public int energyPerTurn = 3;
        
        [Tooltip("Energy cost per attack")]
        public int attackEnergyCost = 1;

        [Header("=== UNIT GENERATION ===")]
        
        [Tooltip("Minimum random stat value")]
        public int minBaseStat = 80;
        
        [Tooltip("Maximum random stat value")]
        public int maxBaseStat = 100;
        
        [Tooltip("Multiplier for main stats based on role")]
        public float mainStatMultiplier = 1.2f;
        
        [Tooltip("Multiplier for secondary stat")]
        public float secondaryStatMultiplier = 1.1f;

        [Header("=== OBSTACLE SYSTEM ===")]
        
        [Tooltip("Damage dealt to obstacles when attack is blocked")]
        public int obstacleBlockDamage = 100;

        [Header("=== TIMING ===")]
        
        [Tooltip("Seconds to wait during enemy turn")]
        public float enemyTurnDelay = 1.5f;
        
        [Tooltip("Unit movement animation speed")]
        public float moveAnimationSpeed = 5f;

        [Header("=== ARROWS ===")]
        
        [Tooltip("Default max arrows for ranged units")]
        public int defaultMaxArrows = 10;

        // Singleton access for easy retrieval
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
    }
}