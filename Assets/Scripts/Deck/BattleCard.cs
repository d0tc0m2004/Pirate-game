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
                if (sourceWeaponRelic.baseWeaponData == null)
                {
                    Debug.LogWarning($"BattleCard {cardName} (Weapon) has missing baseWeaponData! Defaulting to AdjacentEnemy.");
                    return CardTargetType.AdjacentEnemy;
                }

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
            Debug.Log($"<color=cyan>BattleCard.FromRelic: Creating card for {relic.category}+{relic.roleTag}</color>");

            // Get description and effect type from effectData
            string desc = relic.effectData?.description;
            RelicEffectType effectType = relic.effectData?.effectType ?? RelicEffectType.None;
            int energyCost = relic.effectData?.energyCost ?? 1;

            Debug.Log($"<color=cyan>  Initial: desc='{desc}', effectType={effectType}, energyCost={energyCost}</color>");

            // If effectData is missing (e.g., relic created via Inspector serialization),
            // look up from the database directly
            if (string.IsNullOrEmpty(desc) || effectType == RelicEffectType.None)
            {
                Debug.Log($"<color=yellow>  Description or effectType missing, looking up from database...</color>");
                var db = RelicEffectsDatabase.Instance;
                if (db != null)
                {
                    var dbEffect = db.GetEffect(relic.category, relic.roleTag);
                    if (dbEffect != null)
                    {
                        desc = dbEffect.description;
                        effectType = dbEffect.effectType;
                        energyCost = dbEffect.energyCost;
                        Debug.Log($"<color=green>  Found in DB: desc='{desc}'</color>");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=red>  NOT found in database!</color>");
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=red>  Database instance is NULL!</color>");
                }
            }

            // Final fallback if still no description
            if (string.IsNullOrEmpty(desc))
            {
                desc = GenerateFallbackDescription(relic.category, effectType);
                Debug.Log($"<color=orange>  Using fallback: '{desc}'</color>");
            }

            return new BattleCard
            {
                cardId = $"{owner.GetInstanceID()}_{relic.category}_{relic.roleTag}_{copyIndex}",
                cardName = relic.relicName,
                ownerUnit = owner,
                sourceRelic = relic,
                sourceWeaponRelic = null,
                category = relic.category,
                roleTag = relic.roleTag,
                energyCost = energyCost,
                effectType = effectType,
                description = desc,
                isStowed = false
            };
        }

        /// <summary>
        /// Generate a fallback description when effectData is missing.
        /// </summary>
        private static string GenerateFallbackDescription(RelicCategory category, RelicEffectType effectType)
        {
            // Try to generate from effect type name
            if (effectType != RelicEffectType.None)
            {
                string typeName = effectType.ToString();
                // Convert camelCase to readable: "Boots_SwapWithUnit" -> "Swap with unit"
                typeName = typeName.Replace("_", " ");
                // Remove category prefix
                foreach (var cat in System.Enum.GetNames(typeof(RelicCategory)))
                {
                    if (typeName.StartsWith(cat + " "))
                    {
                        typeName = typeName.Substring(cat.Length + 1);
                        break;
                    }
                }
                // Remove V2 suffix for cleaner display
                typeName = typeName.Replace(" V2 ", " ").Replace("V2", "");
                return typeName.Trim();
            }

            // Fallback based on category
            return category switch
            {
                RelicCategory.Boots => "Move to a new position",
                RelicCategory.Gloves => "Attack an enemy",
                RelicCategory.Hat => "Gain a buff or resource",
                RelicCategory.Coat => "Defensive ability",
                RelicCategory.Totem => "Summon or curse effect",
                RelicCategory.Ultimate => "Powerful ability",
                RelicCategory.Trinket => "Passive effect",
                RelicCategory.PassiveUnique => "Role passive",
                RelicCategory.Weapon => "Attack with weapon",
                _ => "Special ability"
            };
        }
        
        /// <summary>
        /// Create a card from a WeaponRelic.
        /// </summary>
        public static BattleCard FromWeaponRelic(WeaponRelic relic, UnitStatus owner, int copyIndex)
        {
            Debug.Log($"<color=cyan>BattleCard.FromWeaponRelic: Creating card for {relic.relicName} (role={relic.roleTag})</color>");

            // Build a detailed weapon description
            string desc = "";

            // effectData is a struct, so check description directly
            if (!string.IsNullOrEmpty(relic.effectData.description))
            {
                desc = relic.effectData.description;
                Debug.Log($"<color=green>  Using effectData description: '{desc}'</color>");
            }
            else
            {
                // Try to look up from RoleEffectsDatabase
                var roleDB = RoleEffectsDatabase.Instance;
                if (roleDB != null)
                {
                    var roleEffect = roleDB.GetEffect(relic.roleTag, relic.effectTier);
                    if (!string.IsNullOrEmpty(roleEffect.description))
                    {
                        desc = roleEffect.description;
                        Debug.Log($"<color=green>  Looked up from RoleEffectsDatabase: '{desc}'</color>");
                    }
                }
            }

            // Fallback to weapon stats
            if (string.IsNullOrEmpty(desc))
            {
                if (relic.baseWeaponData != null)
                {
                    string attackType = relic.baseWeaponData.attackType == WeaponType.Melee ? "Melee" : "Ranged";
                    desc = $"{attackType} attack - {relic.baseWeaponData.baseDamage} base damage";
                }
                else
                {
                    desc = $"Attack with {relic.relicName}";
                }
                Debug.Log($"<color=yellow>  Using fallback: '{desc}'</color>");
            }

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
                description = desc,
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