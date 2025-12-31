using UnityEngine;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// A Weapon Relic is a combination of:
    /// - Base Weapon (Hammer, Cutlass, Pistol, etc.)
    /// - Role Tag (Surgeon, Cook, Captain, etc.)
    /// - Effect Tier (1=Common, 2=Uncommon, 3=Rare)
    /// 
    /// Example: "Surgeon Hammer (Uncommon)" = Hammer + Surgeon + Effect 2
    /// </summary>
    [System.Serializable]
    public class WeaponRelic
    {
        [Header("Base Weapon")]
        public WeaponFamily weaponFamily;
        public WeaponData baseWeaponData;

        [Header("Role & Effect")]
        public UnitRole roleTag;
        public int effectTier; // 1, 2, or 3
        public WeaponRelicEffectData effectData;

        [Header("Generated Info")]
        public string relicName;
        public string fullDescription;

        /// <summary>
        /// Create a new weapon relic.
        /// </summary>
        public WeaponRelic(WeaponData weapon, UnitRole role, int tier, WeaponRelicEffectData effect)
        {
            baseWeaponData = weapon;
            weaponFamily = weapon.family;
            roleTag = role;
            effectTier = tier;
            effectData = effect;

            // Generate name
            string roleName = GetRoleDisplayName(role);
            string rarityName = effect.GetRarityName();
            relicName = $"{roleName} {weapon.weaponName} ({rarityName})";

            // Generate description
            fullDescription = $"{weapon.weaponName} - {weapon.baseDamage} Base Damage\n" +
                              $"Role: {roleName}\n" +
                              $"Effect: {effect.effectName}\n" +
                              $"{effect.description}";

            if (effect.bonusDamagePercent > 0)
            {
                fullDescription += $"\n+{effect.bonusDamagePercent * 100:F0}% Base Damage";
            }
        }

        /// <summary>
        /// Get the total base damage including rarity bonus.
        /// </summary>
        public int GetTotalBaseDamage()
        {
            if (baseWeaponData == null) return 0;
            
            float bonusMultiplier = 1f + effectData.bonusDamagePercent;
            return Mathf.RoundToInt(baseWeaponData.baseDamage * bonusMultiplier);
        }

        /// <summary>
        /// Get the energy cost.
        /// </summary>
        public int GetEnergyCost()
        {
            return baseWeaponData != null ? baseWeaponData.energyCost : 1;
        }

        /// <summary>
        /// Check if this relic matches a weapon family.
        /// </summary>
        public bool MatchesFamily(WeaponFamily family)
        {
            return weaponFamily == family;
        }

        /// <summary>
        /// Check if this relic matches a role.
        /// </summary>
        public bool MatchesRole(UnitRole role)
        {
            return roleTag == role;
        }

        /// <summary>
        /// Get display name for role.
        /// </summary>
        private string GetRoleDisplayName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => role.ToString()
            };
        }

        /// <summary>
        /// Create a unique ID for this relic combination.
        /// </summary>
        public string GetUniqueId()
        {
            return $"{weaponFamily}_{roleTag}_{effectTier}";
        }

        /// <summary>
        /// Check if two relics are the same.
        /// </summary>
        public bool Equals(WeaponRelic other)
        {
            if (other == null) return false;
            return GetUniqueId() == other.GetUniqueId();
        }
    }
}