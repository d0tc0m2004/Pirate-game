using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Database containing all weapon definitions.
    /// Create one in Resources folder for easy access.
    /// Create via: Create -> Tactical -> Equipment -> Weapon Database
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Tactical/Equipment/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        [Header("All Weapons")]
        public List<WeaponData> allWeapons = new List<WeaponData>();
        
        // Singleton access
        private static WeaponDatabase _instance;
        public static WeaponDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                    if (_instance == null)
                    {
                        Debug.LogError("WeaponDatabase not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Get a weapon by family.
        /// </summary>
        public WeaponData GetWeapon(WeaponFamily family)
        {
            return allWeapons.FirstOrDefault(w => w != null && w.family == family);
        }
        
        /// <summary>
        /// Get all melee weapons.
        /// </summary>
        public List<WeaponData> GetMeleeWeapons()
        {
            return allWeapons.Where(w => w != null && w.attackType == WeaponType.Melee).ToList();
        }
        
        /// <summary>
        /// Get all ranged weapons.
        /// </summary>
        public List<WeaponData> GetRangedWeapons()
        {
            return allWeapons.Where(w => w != null && w.attackType == WeaponType.Ranged).ToList();
        }
        
        /// <summary>
        /// Get all weapons of a specific subtype.
        /// </summary>
        public List<WeaponData> GetWeaponsBySubType(WeaponSubType subType)
        {
            return allWeapons.Where(w => w != null && w.subType == subType).ToList();
        }
        
        /// <summary>
        /// Get a random melee weapon.
        /// </summary>
        public WeaponData GetRandomMeleeWeapon()
        {
            var melee = GetMeleeWeapons();
            if (melee.Count == 0) return null;
            return melee[Random.Range(0, melee.Count)];
        }
        
        /// <summary>
        /// Get a random ranged weapon.
        /// </summary>
        public WeaponData GetRandomRangedWeapon()
        {
            var ranged = GetRangedWeapons();
            if (ranged.Count == 0) return null;
            return ranged[Random.Range(0, ranged.Count)];
        }
        
        /// <summary>
        /// Get a random weapon based on weapon type (Melee/Ranged).
        /// </summary>
        public WeaponData GetRandomWeaponByType(WeaponType type)
        {
            return type == WeaponType.Melee ? GetRandomMeleeWeapon() : GetRandomRangedWeapon();
        }
        
        /// <summary>
        /// Get all weapon families.
        /// </summary>
        public List<WeaponFamily> GetAllFamilies()
        {
            return allWeapons
                .Where(w => w != null)
                .Select(w => w.family)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Get all melee families.
        /// </summary>
        public List<WeaponFamily> GetMeleeFamilies()
        {
            return allWeapons
                .Where(w => w != null && w.attackType == WeaponType.Melee)
                .Select(w => w.family)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Get all ranged families.
        /// </summary>
        public List<WeaponFamily> GetRangedFamilies()
        {
            return allWeapons
                .Where(w => w != null && w.attackType == WeaponType.Ranged)
                .Select(w => w.family)
                .Distinct()
                .ToList();
        }
    }
}