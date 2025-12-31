using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Component attached to units to manage their equipped items.
    /// Handles weapon family lock, relic slots, and jewel budget.
    /// </summary>
    public class UnitEquipment : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Locked Weapon Family")]
        [SerializeField] private WeaponFamily lockedWeaponFamily;
        [SerializeField] private WeaponData defaultWeapon;
        
        [Header("Fixed Slots")]
        [SerializeField] private RelicData ultimateRelic;
        [SerializeField] private RelicData passiveRelic;
        
        [Header("Mixed Relic Slots (4 max)")]
        [SerializeField] private List<RelicData> mixedRelics = new List<RelicData>(4);
        
        [Header("Jewels")]
        [SerializeField] private List<JewelData> equippedJewels = new List<JewelData>();
        
        #endregion
        
        #region Private State
        
        private UnitRole unitRole;
        
        #endregion
        
        #region Public Properties
        
        public WeaponFamily LockedWeaponFamily => lockedWeaponFamily;
        public WeaponData DefaultWeapon => defaultWeapon;
        public RelicData UltimateRelic => ultimateRelic;
        public RelicData PassiveRelic => passiveRelic;
        public IReadOnlyList<RelicData> MixedRelics => mixedRelics;
        public IReadOnlyList<JewelData> EquippedJewels => equippedJewels;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize equipment with a weapon family and role.
        /// </summary>
        public void Initialize(WeaponFamily family, UnitRole role, WeaponData weapon = null)
        {
            lockedWeaponFamily = family;
            unitRole = role;
            defaultWeapon = weapon;
            
            // Clear slots
            mixedRelics.Clear();
            equippedJewels.Clear();
            ultimateRelic = null;
            passiveRelic = null;
        }
        
        #endregion
        
        #region Weapon Management
        
        /// <summary>
        /// Set the default weapon (must match locked family).
        /// </summary>
        public bool SetDefaultWeapon(WeaponData weapon)
        {
            if (weapon == null) return false;
            if (weapon.family != lockedWeaponFamily)
            {
                Debug.LogWarning($"Weapon {weapon.weaponName} doesn't match locked family {lockedWeaponFamily}");
                return false;
            }
            
            defaultWeapon = weapon;
            return true;
        }
        
        #endregion
        
        #region Relic Management
        
        /// <summary>
        /// Equip a relic to a mixed slot.
        /// </summary>
        public bool EquipMixedRelic(RelicData relic, int slotIndex)
        {
            if (relic == null) return false;
            if (slotIndex < 0 || slotIndex >= 4) return false;
            
            // Check weapon family restriction
            if (relic.category == RelicCategory.Weapon)
            {
                if (!relic.MatchesWeaponFamily(lockedWeaponFamily))
                {
                    Debug.LogWarning($"Weapon relic {relic.relicName} doesn't match family {lockedWeaponFamily}");
                    return false;
                }
            }
            
            // Check for duplicates (only weapons can duplicate)
            if (relic.category != RelicCategory.Weapon)
            {
                foreach (var equipped in mixedRelics)
                {
                    if (equipped != null && equipped.category == relic.category)
                    {
                        Debug.LogWarning($"Cannot equip duplicate {relic.category} relic");
                        return false;
                    }
                }
            }
            
            // Expand list if needed
            while (mixedRelics.Count <= slotIndex)
            {
                mixedRelics.Add(null);
            }
            
            mixedRelics[slotIndex] = relic;
            return true;
        }
        
        /// <summary>
        /// Remove a relic from a mixed slot.
        /// </summary>
        public RelicData UnequipMixedRelic(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= mixedRelics.Count) return null;
            
            RelicData removed = mixedRelics[slotIndex];
            mixedRelics[slotIndex] = null;
            return removed;
        }
        
        /// <summary>
        /// Set the ultimate relic (role-specific slot).
        /// </summary>
        public bool SetUltimateRelic(RelicData relic)
        {
            if (relic != null && !relic.MatchesRole(unitRole))
            {
                Debug.LogWarning($"Ultimate relic {relic.relicName} doesn't match role {unitRole}");
                return false;
            }
            
            ultimateRelic = relic;
            return true;
        }
        
        /// <summary>
        /// Set the passive relic (role-specific slot).
        /// </summary>
        public bool SetPassiveRelic(RelicData relic)
        {
            if (relic != null && !relic.MatchesRole(unitRole))
            {
                Debug.LogWarning($"Passive relic {relic.relicName} doesn't match role {unitRole}");
                return false;
            }
            
            passiveRelic = relic;
            return true;
        }
        
        #endregion
        
        #region Jewel Budget System
        
        /// <summary>
        /// Get the number of matching relics (determines jewel budget).
        /// </summary>
        public int GetMatchingRelicCount()
        {
            int count = 0;
            foreach (var relic in mixedRelics)
            {
                if (relic != null && relic.MatchesRole(unitRole))
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Get the total jewel budget based on matching relics.
        /// 0 matching = 0, 1 = 3, 2 = 6, 3 = 9, 4 = 12
        /// </summary>
        public int GetJewelBudget()
        {
            int matching = GetMatchingRelicCount();
            return matching * 3;
        }
        
        /// <summary>
        /// Get the total socket count across all relics.
        /// </summary>
        public int GetTotalSocketCount()
        {
            int total = 0;
            
            // Default weapon: 1 socket (can be unlocked to 3)
            if (defaultWeapon != null) total += 1;
            
            // Ultimate and Passive: 1 socket each (can be unlocked to 3)
            if (ultimateRelic != null) total += ultimateRelic.GetSocketCount();
            if (passiveRelic != null) total += passiveRelic.GetSocketCount();
            
            // Mixed relics
            foreach (var relic in mixedRelics)
            {
                if (relic != null) total += relic.GetSocketCount();
            }
            
            return total;
        }
        
        /// <summary>
        /// Check if a jewel can be added (within budget).
        /// </summary>
        public bool CanAddJewel()
        {
            return equippedJewels.Count < GetJewelBudget();
        }
        
        /// <summary>
        /// Add a jewel to the equipment.
        /// </summary>
        public bool AddJewel(JewelData jewel)
        {
            if (jewel == null) return false;
            if (!CanAddJewel())
            {
                Debug.LogWarning("Jewel budget exceeded!");
                return false;
            }
            
            equippedJewels.Add(jewel);
            return true;
        }
        
        /// <summary>
        /// Remove a jewel from equipment.
        /// </summary>
        public bool RemoveJewel(JewelData jewel)
        {
            return equippedJewels.Remove(jewel);
        }
        
        #endregion
        
        #region Stat Bonuses
        
        /// <summary>
        /// Get total secondary stat bonus from non-matching relics.
        /// </summary>
        public float GetNonMatchingStatBonus()
        {
            float bonus = 0f;
            foreach (var relic in mixedRelics)
            {
                if (relic != null && !relic.MatchesRole(unitRole))
                {
                    bonus += relic.GetNonMatchingBonus();
                }
            }
            return bonus;
        }
        
        /// <summary>
        /// Get equipment summary for UI display.
        /// </summary>
        public string GetEquipmentSummary()
        {
            string summary = $"Weapon Family: {lockedWeaponFamily}\n";
            summary += $"Default Weapon: {(defaultWeapon != null ? defaultWeapon.weaponName : "None")}\n";
            summary += $"Mixed Relics: {mixedRelics.FindAll(r => r != null).Count}/4\n";
            summary += $"Matching Relics: {GetMatchingRelicCount()}\n";
            summary += $"Jewel Budget: {equippedJewels.Count}/{GetJewelBudget()}\n";
            summary += $"Non-Matching Bonus: +{GetNonMatchingStatBonus() * 100:F0}% Secondary";
            return summary;
        }
        
        #endregion
    }
}