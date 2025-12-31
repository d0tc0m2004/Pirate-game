using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// ScriptableObject defining a weapon's stats and effects.
    /// Create via: Create -> Tactical -> Equipment -> Weapon
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Tactical/Equipment/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        public WeaponFamily family;
        public WeaponSubType subType;
        public WeaponType attackType; // Melee or Ranged (from existing enum)
        
        [Header("Card Info")]
        [Tooltip("Number of attack cards this weapon adds to deck")]
        public int cardCopies = 2;
        
        [Tooltip("Energy cost per attack")]
        [Range(1, 3)]
        public int energyCost = 1;
        
        [Header("Damage")]
        [Tooltip("Base HP damage before stat scaling")]
        public int baseDamage = 60;
        
        [Tooltip("Which stat scales this weapon's damage")]
        public ScalingStat scalingStat = ScalingStat.Power;
        
        [Tooltip("Scaling coefficient (e.g., 1.25 means +1.25 damage per stat point)")]
        public float scalingCoefficient = 1.25f;
        
        [Header("Targeting")]
        public TargetType defaultTarget = TargetType.Closest;
        public TargetArea defaultArea = TargetArea.Single;
        
        [Header("Special Effect")]
        public WeaponEffectType effectType = WeaponEffectType.None;
        
        [Tooltip("Description of the special effect")]
        [TextArea(2, 4)]
        public string effectDescription;
        
        [Header("Effect Parameters")]
        [Tooltip("Generic value for effect (damage %, duration, etc.)")]
        public float effectValue1 = 0f;
        public float effectValue2 = 0f;
        public int effectDuration = 0;
        
        [Header("Visuals")]
        public Sprite weaponIcon;
        
        /// <summary>
        /// Calculate total damage with stat scaling.
        /// </summary>
        public int CalculateDamage(int statValue)
        {
            return baseDamage + Mathf.RoundToInt(statValue * scalingCoefficient);
        }
        
        /// <summary>
        /// Get a formatted description of this weapon.
        /// </summary>
        public string GetDescription()
        {
            string scaling = scalingStat == ScalingStat.Power ? "Power" : "Aim";
            return $"{cardCopies} copies • {energyCost} Energy • Base {baseDamage} HP dmg • {scaling} scaling\n" +
                   $"Effect: {effectDescription}";
        }
    }
    
    /// <summary>
    /// Which stat scales weapon damage.
    /// </summary>
    public enum ScalingStat
    {
        Power,  // Melee weapons
        Aim     // Ranged weapons
    }
    
    /// <summary>
    /// How the weapon selects its target.
    /// </summary>
    public enum TargetType
    {
        Closest,    // Nearest enemy
        Furthest,   // Furthest enemy
        LowestHP,   // Enemy with lowest HP
        LowestMorale, // Enemy with lowest morale
        Random,     // Random enemy
        Manual      // Player chooses
    }
    
    /// <summary>
    /// Area of effect for the weapon.
    /// </summary>
    public enum TargetArea
    {
        Single,     // One target
        Row,        // All enemies in same row
        Column,     // All enemies in same column
        Adjacent,   // Target + adjacent enemies
        All         // All enemies
    }
}