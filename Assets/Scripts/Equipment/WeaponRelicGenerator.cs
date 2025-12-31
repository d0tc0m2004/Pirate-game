using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;
using TacticalGame.Units;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Generates weapon relics for units and creates the relic pool.
    /// 
    /// Flow:
    /// 1. Unit gets weapon family assigned (e.g., Hammer)
    /// 2. Roll a random role effect (1-3) for that unit's role
    /// 3. Create the unit's default weapon relic
    /// 4. Generate relic pool with all other combinations (excluding assigned ones)
    /// </summary>
    public static class WeaponRelicGenerator
    {
        /// <summary>
        /// Generate a weapon relic for a unit based on their weapon family and role.
        /// </summary>
        public static WeaponRelic GenerateDefaultWeaponRelic(UnitData unitData)
        {
            // Get weapon data
            WeaponData weaponData = WeaponDatabase.Instance?.GetWeapon(unitData.weaponFamily);
            if (weaponData == null)
            {
                Debug.LogError($"Weapon not found for family: {unitData.weaponFamily}");
                return null;
            }

            // Get role effects database
            RoleEffectsDatabase effectsDB = RoleEffectsDatabase.Instance;
            if (effectsDB == null)
            {
                Debug.LogError("RoleEffectsDatabase not found!");
                return null;
            }

            // Roll random effect tier (1, 2, or 3)
            int effectTier = effectsDB.GetRandomEffectTier();

            // Get the effect data
            WeaponRelicEffectData effectData = effectsDB.GetEffect(unitData.role, effectTier);

            // Create the weapon relic
            return new WeaponRelic(weaponData, unitData.role, effectTier, effectData);
        }

        /// <summary>
        /// Generate a specific weapon relic.
        /// </summary>
        public static WeaponRelic GenerateWeaponRelic(WeaponFamily family, UnitRole role, int effectTier)
        {
            WeaponData weaponData = WeaponDatabase.Instance?.GetWeapon(family);
            if (weaponData == null) return null;

            RoleEffectsDatabase effectsDB = RoleEffectsDatabase.Instance;
            if (effectsDB == null) return null;

            WeaponRelicEffectData effectData = effectsDB.GetEffect(role, effectTier);
            return new WeaponRelic(weaponData, role, effectTier, effectData);
        }

        /// <summary>
        /// Generate the complete relic pool, excluding the relics assigned to units.
        /// </summary>
        public static List<WeaponRelic> GenerateRelicPool(List<WeaponRelic> excludedRelics)
        {
            List<WeaponRelic> pool = new List<WeaponRelic>();

            WeaponDatabase weaponDB = WeaponDatabase.Instance;
            RoleEffectsDatabase effectsDB = RoleEffectsDatabase.Instance;

            if (weaponDB == null || effectsDB == null)
            {
                Debug.LogError("Databases not found!");
                return pool;
            }

            // Get all weapon families
            var allFamilies = weaponDB.GetAllFamilies();

            // Get all roles (excluding Neutral if exists)
            var allRoles = System.Enum.GetValues(typeof(UnitRole))
                .Cast<UnitRole>()
                .Where(r => r != UnitRole.Captain) // Captain uses random stats, include if you want
                .ToList();

            // Actually include all roles including Captain
            allRoles = System.Enum.GetValues(typeof(UnitRole)).Cast<UnitRole>().ToList();

            // Generate all combinations
            foreach (WeaponFamily family in allFamilies)
            {
                WeaponData weaponData = weaponDB.GetWeapon(family);
                if (weaponData == null) continue;

                foreach (UnitRole role in allRoles)
                {
                    // Check if this role has effects defined
                    RoleWeaponEffects roleEffects = effectsDB.GetRoleEffects(role);
                    if (roleEffects == null) continue;

                    // Generate all 3 effect tiers
                    for (int tier = 1; tier <= 3; tier++)
                    {
                        WeaponRelicEffectData effectData = effectsDB.GetEffect(role, tier);
                        WeaponRelic relic = new WeaponRelic(weaponData, role, tier, effectData);

                        // Check if this relic is excluded (already assigned to a unit)
                        bool isExcluded = excludedRelics.Any(e => e.Equals(relic));

                        if (!isExcluded)
                        {
                            pool.Add(relic);
                        }
                    }
                }
            }

            Debug.Log($"Generated relic pool with {pool.Count} relics");
            return pool;
        }

        /// <summary>
        /// Generate relic pool for a specific weapon family only.
        /// Used for showing relics a unit can actually equip.
        /// </summary>
        public static List<WeaponRelic> GenerateRelicPoolForFamily(WeaponFamily family, List<WeaponRelic> excludedRelics)
        {
            List<WeaponRelic> pool = new List<WeaponRelic>();

            WeaponDatabase weaponDB = WeaponDatabase.Instance;
            RoleEffectsDatabase effectsDB = RoleEffectsDatabase.Instance;

            if (weaponDB == null || effectsDB == null) return pool;

            WeaponData weaponData = weaponDB.GetWeapon(family);
            if (weaponData == null) return pool;

            var allRoles = System.Enum.GetValues(typeof(UnitRole)).Cast<UnitRole>().ToList();

            foreach (UnitRole role in allRoles)
            {
                RoleWeaponEffects roleEffects = effectsDB.GetRoleEffects(role);
                if (roleEffects == null) continue;

                for (int tier = 1; tier <= 3; tier++)
                {
                    WeaponRelicEffectData effectData = effectsDB.GetEffect(role, tier);
                    WeaponRelic relic = new WeaponRelic(weaponData, role, tier, effectData);

                    bool isExcluded = excludedRelics.Any(e => e.Equals(relic));

                    if (!isExcluded)
                    {
                        pool.Add(relic);
                    }
                }
            }

            return pool;
        }

        /// <summary>
        /// Get all relics that a unit can equip (same weapon family).
        /// </summary>
        public static List<WeaponRelic> GetEquippableRelics(UnitData unitData, List<WeaponRelic> relicPool)
        {
            return relicPool.Where(r => r.MatchesFamily(unitData.weaponFamily)).ToList();
        }

        /// <summary>
        /// Filter relics by role.
        /// </summary>
        public static List<WeaponRelic> FilterByRole(List<WeaponRelic> relics, UnitRole role)
        {
            return relics.Where(r => r.MatchesRole(role)).ToList();
        }

        /// <summary>
        /// Filter relics by rarity.
        /// </summary>
        public static List<WeaponRelic> FilterByRarity(List<WeaponRelic> relics, RelicRarity rarity)
        {
            return relics.Where(r => r.effectData.rarity == rarity).ToList();
        }
    }
}