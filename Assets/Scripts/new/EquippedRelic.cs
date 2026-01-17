using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Runtime representation of an equipped relic.
    /// Similar to WeaponRelic but for non-weapon equipment.
    /// 
    /// Example: A unit equips "Captain Boots" relic
    /// - Category = Boots
    /// - RoleTag = Captain
    /// - Effect = Swap location with another unit
    /// - If unit is Captain, gets Proficiency bonus
    /// </summary>
    [System.Serializable]
    public class EquippedRelic
    {
        [Header("Identity")]
        public RelicCategory category;
        public UnitRole roleTag;
        
        [Header("Effect Data")]
        public RelicEffectData effectData;
        
        [Header("Generated Info")]
        public string relicName;
        public string fullDescription;
        
        /// <summary>
        /// Create an equipped relic from effect data.
        /// </summary>
        public EquippedRelic(RelicEffectData data)
        {
            effectData = data;
            category = data.category;
            roleTag = data.roleTag;
            relicName = data.GetDisplayName();
            fullDescription = GenerateDescription();
        }
        
        /// <summary>
        /// Create an equipped relic from category and role.
        /// Looks up effect data from database.
        /// </summary>
        public EquippedRelic(RelicCategory cat, UnitRole role)
        {
            category = cat;
            roleTag = role;
            
            var db = RelicEffectsDatabase.Instance;
            if (db != null)
            {
                effectData = db.GetEffect(cat, role);
            }
            
            if (effectData != null)
            {
                relicName = effectData.GetDisplayName();
                fullDescription = GenerateDescription();
            }
            else
            {
                relicName = $"{role} {cat}";
                fullDescription = "Effect data not found";
                Debug.LogWarning($"No effect data found for {cat} + {role}");
            }
        }
        
        private string GenerateDescription()
        {
            if (effectData == null) return "";
            
            string copyInfo = effectData.isPassive ? "Passive" : $"{effectData.copies} copies";
            string costInfo = effectData.isPassive ? "" : $" • {effectData.energyCost} Energy";
            
            return $"{relicName}\n{copyInfo}{costInfo}\n{effectData.description}";
        }
        
        /// <summary>
        /// Get number of card copies this relic adds to deck.
        /// </summary>
        public int GetCopies()
        {
            return effectData?.copies ?? 0;
        }
        
        /// <summary>
        /// Get energy cost to play this relic's card.
        /// </summary>
        public int GetEnergyCost()
        {
            return effectData?.energyCost ?? 1;
        }
        
        /// <summary>
        /// Check if this is a passive relic (always active, no card).
        /// </summary>
        public bool IsPassive()
        {
            return effectData?.isPassive ?? false;
        }
        
        /// <summary>
        /// Check if this relic matches a unit's role (for Proficiency bonus).
        /// </summary>
        public bool MatchesRole(UnitRole unitRole)
        {
            return roleTag == unitRole;
        }
        
        /// <summary>
        /// Get the effect type for execution.
        /// </summary>
        public RelicEffectType GetEffectType()
        {
            return effectData?.effectType ?? RelicEffectType.None;
        }
        
        /// <summary>
        /// Get unique ID for this relic.
        /// </summary>
        public string GetUniqueId()
        {
            return $"{category}_{roleTag}";
        }
        
        /// <summary>
        /// Check equality.
        /// </summary>
        public bool Equals(EquippedRelic other)
        {
            if (other == null) return false;
            return GetUniqueId() == other.GetUniqueId();
        }
    }
}