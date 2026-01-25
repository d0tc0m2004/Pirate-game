using UnityEngine;
using TacticalGame.Enums;
using TacticalGame.Units;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Represents a single card in the shared battle deck.
    /// Each card is tied to a specific unit (the owner who equipped the relic).
    /// </summary>
    [System.Serializable]
    public class BattleCard
    {
        [Header("Identity")]
        public string cardId;           // Unique ID for this card instance
        public string cardName;         // Display name
        
        [Header("Source")]
        public UnitStatus ownerUnit;    // The unit who equipped this relic
        public EquippedRelic sourceRelic;
        public WeaponRelic sourceWeaponRelic;
        
        [Header("Card Data")]
        public RelicCategory category;
        public UnitRole roleTag;
        public int energyCost;
        public RelicEffectType effectType;
        public string description;
        
        [Header("State")]
        public bool isStowed = false;   // If true, won't be discarded at end of turn
        
        /// <summary>
        /// Is this a weapon card?
        /// </summary>
        public bool IsWeaponCard => sourceWeaponRelic != null;
        
        /// <summary>
        /// Get display name for the card.
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(cardName))
                return cardName;
            if (IsWeaponCard)
                return sourceWeaponRelic.relicName;
            return sourceRelic?.relicName ?? $"{roleTag} {category}";
        }
        
        /// <summary>
        /// Get the owner unit's name.
        /// </summary>
        public string GetOwnerName()
        {
            return ownerUnit?.UnitName ?? "Unknown";
        }
        
        /// <summary>
        /// Check if this card belongs to a specific unit.
        /// </summary>
        public bool BelongsTo(UnitStatus unit)
        {
            return ownerUnit == unit;
        }
        
        /// <summary>
        /// Check if this card requires a target.
        /// </summary>
        public bool RequiresTarget()
        {
            // Weapon cards always need target
            if (IsWeaponCard) return true;
            
            // Check by category
            switch (category)
            {
                case RelicCategory.Gloves:  // Attacks need targets
                case RelicCategory.Totem:   // Some totems need placement
                case RelicCategory.Ultimate: // Some ultimates need targets
                    return true;
                    
                case RelicCategory.Boots:   // Movement - needs tile
                    return true;
                    
                case RelicCategory.Hat:     // Usually self-buffs
                case RelicCategory.Coat:    // Usually self-buffs
                    return false;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get what type of target this card needs.
        /// </summary>
        public CardTargetType GetTargetType()
        {
            if (IsWeaponCard)
            {
                return sourceWeaponRelic.baseWeaponData.attackType == WeaponType.Melee 
                    ? CardTargetType.AdjacentEnemy 
                    : CardTargetType.RangedEnemy;
            }
            
            switch (category)
            {
                case RelicCategory.Boots:
                    // Check for swap effects
                    if (effectType == RelicEffectType.Boots_SwapWithUnit ||
                        effectType == RelicEffectType.Boots_MoveAlly)
                        return CardTargetType.Ally;
                    if (effectType == RelicEffectType.Boots_V2_SwapWithEnemy)
                        return CardTargetType.Enemy;
                    return CardTargetType.Tile;
                    
                case RelicCategory.Gloves:
                    return CardTargetType.Enemy;
                    
                case RelicCategory.Totem:
                    // Curses target enemies, summons target tiles
                    if (effectType.ToString().Contains("Curse"))
                        return CardTargetType.Enemy;
                    return CardTargetType.Tile;
                    
                case RelicCategory.Ultimate:
                    // Most ultimates target enemies or are AoE
                    return CardTargetType.Enemy;
                    
                default:
                    return CardTargetType.None;
            }
        }
        
        /// <summary>
        /// Create a card from an EquippedRelic.
        /// </summary>
        public static BattleCard FromRelic(EquippedRelic relic, UnitStatus owner, int copyIndex)
        {
            return new BattleCard
            {
                cardId = $"{owner.GetInstanceID()}_{relic.category}_{relic.roleTag}_{copyIndex}",
                cardName = relic.relicName,
                ownerUnit = owner,
                sourceRelic = relic,
                sourceWeaponRelic = null,
                category = relic.category,
                roleTag = relic.roleTag,
                energyCost = relic.GetEnergyCost(),
                effectType = relic.GetEffectType(),
                description = relic.effectData?.description ?? "",
                isStowed = false
            };
        }
        
        /// <summary>
        /// Create a card from a WeaponRelic.
        /// </summary>
        public static BattleCard FromWeaponRelic(WeaponRelic relic, UnitStatus owner, int copyIndex)
        {
            return new BattleCard
            {
                cardId = $"{owner.GetInstanceID()}_Weapon_{relic.roleTag}_{copyIndex}",
                cardName = relic.relicName,
                ownerUnit = owner,
                sourceRelic = null,
                sourceWeaponRelic = relic,
                category = RelicCategory.Weapon,
                roleTag = relic.roleTag,
                energyCost = relic.GetEnergyCost(),
                effectType = RelicEffectType.None,
                description = $"Attack with {relic.relicName}",
                isStowed = false
            };
        }
    }
    
    /// <summary>
    /// Types of targets a card can require.
    /// (Named CardTargetType to avoid conflict with WeaponData.TargetType)
    /// </summary>
    public enum CardTargetType
    {
        None,           // No target needed (self-buff)
        Tile,           // Target a grid tile
        Ally,           // Target an allied unit
        Enemy,          // Target an enemy unit
        AdjacentEnemy,  // Target adjacent enemy (melee)
        RangedEnemy,    // Target enemy in range
        AnyUnit         // Target any unit
    }
}