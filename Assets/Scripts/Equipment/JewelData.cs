using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// ScriptableObject defining a jewel that can be socketed into relics.
    /// Jewels modify abilities (PoE-style support logic).
    /// Create via: Create -> Tactical -> Equipment -> Jewel
    /// </summary>
    [CreateAssetMenu(fileName = "New Jewel", menuName = "Tactical/Equipment/Jewel")]
    public class JewelData : ScriptableObject
    {
        [Header("Identity")]
        public string jewelName;
        public JewelType type;
        public JewelRarity rarity = JewelRarity.Common;
        
        [Header("Effect")]
        [TextArea(2, 4)]
        public string effectDescription;
        
        [Header("Modifiers")]
        [Tooltip("Flat damage added to ability")]
        public int flatDamageBonus = 0;
        
        [Tooltip("Percentage damage increase (0.1 = +10%)")]
        public float percentDamageBonus = 0f;
        
        [Tooltip("Energy cost modifier (-1 = costs 1 less)")]
        public int energyCostModifier = 0;
        
        [Tooltip("Additional effect duration in turns")]
        public int durationBonus = 0;
        
        [Header("Special Modifiers")]
        public bool addsLifesteal = false;
        public float lifestealPercent = 0f;
        
        public bool addsPierce = false;
        public float piercePercent = 0f; // % damage that ignores Hull
        
        public bool addsChainHit = false;
        public int chainTargets = 0;
        public float chainDamageFalloff = 0.5f; // Each chain does X% of previous
        
        public bool addsAreaEffect = false;
        public TargetArea areaType = TargetArea.Single;
        public float areaDamagePercent = 0.5f; // Secondary targets take X%
        
        [Header("Visuals")]
        public Sprite jewelIcon;
        public Color jewelColor = Color.white;
    }
    
    /// <summary>
    /// Types of jewels (categories for sorting/filtering).
    /// </summary>
    public enum JewelType
    {
        Damage,     // Increases damage output
        Defense,    // Adds defensive properties
        Utility,    // Adds utility effects
        Duration,   // Extends effect durations
        Cost,       // Modifies energy costs
        Target,     // Changes targeting behavior
        Special     // Unique effects
    }
    
    /// <summary>
    /// Jewel rarity (affects drop rates, not stats directly).
    /// </summary>
    public enum JewelRarity
    {
        Common,
        Uncommon,
        Rare,
        Unique
    }
}