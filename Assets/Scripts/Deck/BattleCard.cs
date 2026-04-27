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
            // Let the target type govern targeting requirements entirely
            // (Weapons now return None to auto-target closest enemy)

            // Use the resolved target type — None means no target needed
            return GetTargetType() != CardTargetType.None;
        }
        
        /// <summary>
        /// Get what type of target this card needs.
        /// </summary>
        public CardTargetType GetTargetType()
        {
            if (IsWeaponCard)
            {
                // Weapons now auto-evaluate the nearest enemy
                return CardTargetType.None;
            }

            // Check specific effect types that override category defaults
            CardTargetType? specific = GetEffectSpecificTargetType();
            if (specific.HasValue)
                return specific.Value;

            switch (category)
            {
                case RelicCategory.Boots:
                    return CardTargetType.Tile;

                case RelicCategory.Gloves:
                    // Default: auto-target closest enemy (executor handles it)
                    return CardTargetType.None;

                case RelicCategory.Totem:
                    if (effectType.ToString().Contains("Curse") ||
                        effectType.ToString().Contains("Disable"))
                        return CardTargetType.Enemy;
                    return CardTargetType.Tile;

                case RelicCategory.Ultimate:
                    // Default: auto-target closest enemy (executor handles it)
                    return CardTargetType.None;

                default:
                    return CardTargetType.None;
            }
        }

        /// <summary>
        /// Override target type for specific effects that don't follow category defaults.
        /// </summary>
        private CardTargetType? GetEffectSpecificTargetType()
        {
            switch (effectType)
            {
                // === Boots that target allies ===
                case RelicEffectType.Boots_SwapWithUnit:
                case RelicEffectType.Boots_MoveAlly:
                case RelicEffectType.Boots_V2_MoveAllyGainShield:
                    return CardTargetType.Ally;

                // === Boots that target enemies ===
                case RelicEffectType.Boots_V2_SwapWithEnemy:
                    return CardTargetType.Enemy;

                // === Gloves that target allies ===
                case RelicEffectType.Gloves_V2_AttackHealAlly:
                    return CardTargetType.Enemy; // Still attacks enemy, heal is secondary

                // === Coat effects that target allies ===
                case RelicEffectType.Coat_DoubleAllyStats:
                case RelicEffectType.Coat_PreventSurrender:
                    return CardTargetType.Ally;

                // === Hat effects that target allies ===
                case RelicEffectType.Hat_V2_MoveForwardHeal:
                    return CardTargetType.Ally;

                // === Coat effects that target tiles ===
                case RelicEffectType.Coat_V2_CurseEmptyTile:
                case RelicEffectType.Coat_V2_BuffTileDamageExchange:
                    return CardTargetType.Tile;

                // === Auto-cast / Random Target Effects (No UI Target Prompt) ===
                case RelicEffectType.Totem_RallyNoMoraleDamage:
                case RelicEffectType.Totem_CurseCaptainReflect:
                case RelicEffectType.Ultimate_MarkCaptainOnly:
                case RelicEffectType.Totem_ConvertGrogToEnergy:
                case RelicEffectType.Ultimate_ShipCannon:
                case RelicEffectType.Totem_SummonCannon:
                case RelicEffectType.Boots_AllyFreeMoveLowestMorale:
                case RelicEffectType.Totem_SummonHighQualityRum:
                    return CardTargetType.None;

                // BOATSWAIN AUTO-CASTS
                case RelicEffectType.Hat_ReturnDamage:
                case RelicEffectType.Hat_IncreaseHealthStat:
                case RelicEffectType.Coat_PreventDisplacement:
                case RelicEffectType.Coat_ProtectLowHP:
                case RelicEffectType.Totem_StunOnKnockback:
                case RelicEffectType.Ultimate_SummonHardObstacles:
                case RelicEffectType.Ultimate_IgnoreHighestHP:
                case RelicEffectType.Boots_V2_SwapLowestHealthAlly:
                case RelicEffectType.Totem_V2_SummonHealingPotions:
                    return CardTargetType.None;

                // === Ultimates that target allies ===
                case RelicEffectType.Ultimate_ReviveAlly:
                case RelicEffectType.Ultimate_V2_MassRevive:
                case RelicEffectType.Ultimate_PreventDeath:
                case RelicEffectType.Ultimate_V2_FullHealthRestore:
                case RelicEffectType.Ultimate_MassiveHullBuff:
                case RelicEffectType.Ultimate_V2_Teleport:
                    return CardTargetType.Ally;

                // === Ultimates that need a specific enemy target (player chooses) ===
                case RelicEffectType.Ultimate_FullBuzzAttack:
                case RelicEffectType.Ultimate_KnockbackToLastColumn:
                case RelicEffectType.Ultimate_AttackKnockbackNearby:
                case RelicEffectType.Ultimate_StunAoE:
                case RelicEffectType.Ultimate_MassiveSingleTarget:
                case RelicEffectType.Ultimate_V2_AttackRowDamage:
                case RelicEffectType.Ultimate_MarkReflectToCaptain:
                    return CardTargetType.Enemy;

                // === Ultimates that target tiles ===
                case RelicEffectType.Ultimate_RumBottleAoE:
                    return CardTargetType.Tile;

                default:
                    return null; // Use category default
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