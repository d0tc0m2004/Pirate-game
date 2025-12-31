using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Database containing all role weapon effects.
    /// Used to generate weapon relics with role-specific effects.
    /// </summary>
    [CreateAssetMenu(fileName = "RoleEffectsDatabase", menuName = "Tactical/Equipment/Role Effects Database")]
    public class RoleEffectsDatabase : ScriptableObject
    {
        [Header("Role Effect Definitions")]
        public List<RoleWeaponEffects> allRoleEffects = new List<RoleWeaponEffects>();

        // Singleton
        private static RoleEffectsDatabase _instance;
        public static RoleEffectsDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<RoleEffectsDatabase>("RoleEffectsDatabase");
                    if (_instance == null)
                    {
                        Debug.LogError("RoleEffectsDatabase not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get effects for a specific role.
        /// </summary>
        public RoleWeaponEffects GetRoleEffects(UnitRole role)
        {
            return allRoleEffects.FirstOrDefault(r => r != null && r.role == role);
        }

        /// <summary>
        /// Get a specific effect for a role and tier.
        /// </summary>
        public WeaponRelicEffectData GetEffect(UnitRole role, int tier)
        {
            RoleWeaponEffects roleEffects = GetRoleEffects(role);
            if (roleEffects != null)
            {
                return roleEffects.GetEffect(tier);
            }
            
            // Return empty effect if not found
            return new WeaponRelicEffectData
            {
                effectName = "Unknown",
                description = "Effect not found",
                rarity = RelicRarity.Common
            };
        }

        /// <summary>
        /// Get a random effect tier (1, 2, or 3) with weighted probability.
        /// Common: 50%, Uncommon: 35%, Rare: 15%
        /// </summary>
        public int GetRandomEffectTier()
        {
            float roll = Random.value;
            if (roll < 0.50f) return 1; // Common
            if (roll < 0.85f) return 2; // Uncommon
            return 3; // Rare
        }

        /// <summary>
        /// Get a completely random effect tier (equal probability).
        /// </summary>
        public int GetRandomEffectTierEqual()
        {
            return Random.Range(1, 4); // 1, 2, or 3
        }
    }
}