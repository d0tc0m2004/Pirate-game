using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Utility class to assign weapons to units during generation.
    /// Handles role-based restrictions (Master Gunner = Ranged only, etc.)
    /// </summary>
    public static class WeaponAssigner
    {
        /// <summary>
        /// Assign a random appropriate weapon to a unit based on role.
        /// </summary>
        public static WeaponData AssignWeapon(UnitRole role, out WeaponFamily assignedFamily)
        {
            var database = WeaponDatabase.Instance;
            
            if (database == null)
            {
                Debug.LogWarning("WeaponDatabase not found! Using fallback.");
                assignedFamily = WeaponFamily.Cutlass;
                return null;
            }
            
            WeaponData weapon = null;
            
            switch (role)
            {
                // Master Gunner: Ranged only
                case UnitRole.MasterGunner:
                    weapon = database.GetRandomRangedWeapon();
                    break;
                    
                // Master-at-Arms: Melee only
                case UnitRole.MasterAtArms:
                    weapon = database.GetRandomMeleeWeapon();
                    break;
                    
                // All other roles: Any weapon
                default:
                    if (Random.value > 0.5f)
                        weapon = database.GetRandomMeleeWeapon();
                    else
                        weapon = database.GetRandomRangedWeapon();
                    break;
            }
            
            assignedFamily = weapon != null ? weapon.family : GetFallbackFamily(role);
            return weapon;
        }
        
        /// <summary>
        /// Assign a specific weapon family to a unit.
        /// </summary>
        public static WeaponData AssignWeaponFamily(WeaponFamily family)
        {
            var database = WeaponDatabase.Instance;
            if (database == null) return null;
            
            return database.GetWeapon(family);
        }
        
        /// <summary>
        /// Get valid weapon families for a role.
        /// </summary>
        public static List<WeaponFamily> GetValidFamilies(UnitRole role)
        {
            var database = WeaponDatabase.Instance;
            if (database == null) return new List<WeaponFamily>();
            
            switch (role)
            {
                case UnitRole.MasterGunner:
                    return database.GetRangedFamilies();
                    
                case UnitRole.MasterAtArms:
                    return database.GetMeleeFamilies();
                    
                default:
                    return database.GetAllFamilies();
            }
        }
        
        /// <summary>
        /// Check if a weapon family is valid for a role.
        /// </summary>
        public static bool IsValidFamilyForRole(WeaponFamily family, UnitRole role)
        {
            var validFamilies = GetValidFamilies(role);
            return validFamilies.Contains(family);
        }
        
        /// <summary>
        /// Get fallback family when database is missing.
        /// </summary>
        private static WeaponFamily GetFallbackFamily(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => WeaponFamily.Pistol,
                UnitRole.MasterAtArms => WeaponFamily.Cutlass,
                _ => Random.value > 0.5f ? WeaponFamily.Cutlass : WeaponFamily.Pistol
            };
        }
        
        /// <summary>
        /// Get weapon type (Melee/Ranged) for a family.
        /// </summary>
        public static WeaponType GetWeaponType(WeaponFamily family)
        {
            switch (family)
            {
                // Melee families
                case WeaponFamily.Cutlass:
                case WeaponFamily.Machete:
                case WeaponFamily.Rapier:
                case WeaponFamily.Axe:
                case WeaponFamily.Hammer:
                case WeaponFamily.Anchor:
                case WeaponFamily.Clubs:
                case WeaponFamily.Mace:
                case WeaponFamily.Harpoon:
                case WeaponFamily.Spear:
                case WeaponFamily.BoardingPike:
                case WeaponFamily.Dagger:
                case WeaponFamily.Dirk:
                    return WeaponType.Melee;
                    
                // Ranged families
                case WeaponFamily.Pistol:
                case WeaponFamily.Musket:
                case WeaponFamily.Blunderbuss:
                case WeaponFamily.Grenade:
                case WeaponFamily.Cannonball:
                case WeaponFamily.CursedBird:
                case WeaponFamily.CursedMonkey:
                    return WeaponType.Ranged;
                    
                default:
                    return WeaponType.Melee;
            }
        }
    }
}