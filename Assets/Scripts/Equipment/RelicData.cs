using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// ScriptableObject defining a relic's stats and effects.
    /// Create via: Create -> Tactical -> Equipment -> Relic
    /// </summary>
    [CreateAssetMenu(fileName = "New Relic", menuName = "Tactical/Equipment/Relic")]
    public class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public string relicName;
        public RelicCategory category;
        public RelicRarity rarity = RelicRarity.Common;
        
        [Tooltip("Which role this relic is tagged for (affects Proficiency scaling)")]
        public UnitRole roleTag;
        
        [Header("Slot Info")]
        [Tooltip("For weapons only - must match unit's weapon family")]
        public WeaponFamily weaponFamily;
        
        [Tooltip("Current level (1-3), determines socket count")]
        [Range(1, 3)]
        public int level = 1;
        
        [Header("Card Info (if active relic)")]
        public bool isActiveRelic = true; // false = passive only
        
        [Tooltip("Number of cards this relic adds to deck")]
        public int cardCopies = 2;
        
        [Tooltip("Energy cost per use")]
        [Range(0, 5)]
        public int energyCost = 1;
        
        [Header("Effect")]
        [TextArea(2, 4)]
        public string effectDescription;
        
        [Header("Scaling")]
        public ScalingStat primaryScalingStat = ScalingStat.Power;
        public float scalingCoefficient = 1.0f;
        
        [Header("Base Values")]
        public int baseValue = 0; // Base damage/heal/shield amount
        public int baseDuration = 0; // Effect duration in turns
        
        [Header("Visuals")]
        public Sprite relicIcon;
        
        /// <summary>
        /// Get the number of jewel sockets based on level.
        /// Level 1 = 1 socket, Level 2 = 2 sockets, Level 3 = 3 sockets
        /// </summary>
        public int GetSocketCount()
        {
            return level;
        }
        
        /// <summary>
        /// Get secondary stat bonus for non-matching relics.
        /// </summary>
        public float GetNonMatchingBonus()
        {
            return rarity switch
            {
                RelicRarity.Common => 0.02f,    // +2%
                RelicRarity.Uncommon => 0.04f,  // +4%
                RelicRarity.Rare => 0.06f,      // +6%
                RelicRarity.Unique => 0.08f,    // +8%
                _ => 0.02f
            };
        }
        
        /// <summary>
        /// Check if this relic matches a unit's role (for Proficiency bonus).
        /// </summary>
        public bool MatchesRole(UnitRole unitRole)
        {
            return roleTag == unitRole;
        }
        
        /// <summary>
        /// Check if this weapon relic matches a unit's weapon family.
        /// </summary>
        public bool MatchesWeaponFamily(WeaponFamily unitFamily)
        {
            if (category != RelicCategory.Weapon) return true; // Non-weapons always "match"
            return weaponFamily == unitFamily;
        }
    }
}