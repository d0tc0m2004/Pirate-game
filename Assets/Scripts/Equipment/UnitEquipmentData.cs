using System;
using System.Collections.Generic;
using TacticalGame.Enums;
using UnityEngine;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Equipment data container for managing relics and jewels.
    /// Wraps relic storage with slot-based access.
    /// </summary>
    [Serializable]
    public class UnitEquipmentData
    {
        #region Constants
        
        public const int SLOT_R1 = 0;
        public const int SLOT_R2 = 1;
        public const int SLOT_R3 = 2;
        public const int SLOT_R4 = 3;
        public const int SLOT_R5 = 4;
        public const int SLOT_R6 = 5;
        public const int SLOT_R7 = 6;
        
        public const int MAX_SLOTS = 7;
        public const int MAX_JEWELS_PER_SLOT = 3;
        
        #endregion
        
        #region Storage
        
        private WeaponRelic[] weaponRelics = new WeaponRelic[MAX_SLOTS];
        private EquippedRelic[] categoryRelics = new EquippedRelic[MAX_SLOTS];
        private JewelData[,] jewels = new JewelData[MAX_SLOTS, MAX_JEWELS_PER_SLOT];
        
        #endregion
        
        #region Constructor
        
        public UnitEquipmentData()
        {
            weaponRelics = new WeaponRelic[MAX_SLOTS];
            categoryRelics = new EquippedRelic[MAX_SLOTS];
            jewels = new JewelData[MAX_SLOTS, MAX_JEWELS_PER_SLOT];
        }
        
        #endregion
        
        #region Weapon Relic Methods
        
        /// <summary>
        /// Equip a weapon relic to a slot.
        /// </summary>
        public void EquipWeaponRelic(int slot, WeaponRelic relic)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return;
            weaponRelics[slot] = relic;
        }
        
        /// <summary>
        /// Get weapon relic from a slot.
        /// </summary>
        public WeaponRelic GetWeaponRelic(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            return weaponRelics[slot];
        }
        
        /// <summary>
        /// Unequip weapon relic from a slot.
        /// </summary>
        public WeaponRelic UnequipWeaponRelic(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            WeaponRelic relic = weaponRelics[slot];
            weaponRelics[slot] = null;
            return relic;
        }
        
        #endregion
        
        #region Category Relic Methods
        
        /// <summary>
        /// Equip a category relic to a slot.
        /// </summary>
        public void EquipCategoryRelic(int slot, EquippedRelic relic)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return;
            categoryRelics[slot] = relic;
        }
        
        /// <summary>
        /// Get category relic from a slot.
        /// </summary>
        public EquippedRelic GetCategoryRelic(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            return categoryRelics[slot];
        }
        
        /// <summary>
        /// Unequip category relic from a slot.
        /// </summary>
        public EquippedRelic UnequipCategoryRelic(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            EquippedRelic relic = categoryRelics[slot];
            categoryRelics[slot] = null;
            return relic;
        }
        
        #endregion
        
        #region Slot Query Methods
        
        /// <summary>
        /// Check if a slot is empty (no weapon or category relic).
        /// </summary>
        public bool IsSlotEmpty(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return true;
            return weaponRelics[slot] == null && categoryRelics[slot] == null;
        }
        
        /// <summary>
        /// Clear a slot (both weapon and category).
        /// </summary>
        public void ClearSlot(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return;
            weaponRelics[slot] = null;
            categoryRelics[slot] = null;
            for (int j = 0; j < MAX_JEWELS_PER_SLOT; j++)
                jewels[slot, j] = null;
        }
        
        /// <summary>
        /// Clear all equipment.
        /// </summary>
        public void UnequipAll()
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                weaponRelics[i] = null;
                categoryRelics[i] = null;
                for (int j = 0; j < MAX_JEWELS_PER_SLOT; j++)
                    jewels[i, j] = null;
            }
        }
        
        #endregion
        
        #region Jewel Methods
        
        /// <summary>
        /// Equip a jewel to a slot.
        /// </summary>
        public void EquipJewel(int slotIndex, int jewelIndex, JewelData jewel)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;
            if (jewelIndex < 0 || jewelIndex >= MAX_JEWELS_PER_SLOT) return;
            jewels[slotIndex, jewelIndex] = jewel;
        }
        
        /// <summary>
        /// Get jewel from a slot.
        /// </summary>
        public JewelData GetJewel(int slotIndex, int jewelIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return null;
            if (jewelIndex < 0 || jewelIndex >= MAX_JEWELS_PER_SLOT) return null;
            return jewels[slotIndex, jewelIndex];
        }
        
        /// <summary>
        /// Unequip jewel from a slot.
        /// </summary>
        public JewelData UnequipJewel(int slotIndex, int jewelIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return null;
            if (jewelIndex < 0 || jewelIndex >= MAX_JEWELS_PER_SLOT) return null;
            JewelData jewel = jewels[slotIndex, jewelIndex];
            jewels[slotIndex, jewelIndex] = null;
            return jewel;
        }
        
        /// <summary>
        /// Get total equipped jewel count.
        /// </summary>
        public int GetTotalEquippedJewelCount()
        {
            int count = 0;
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                for (int j = 0; j < MAX_JEWELS_PER_SLOT; j++)
                {
                    if (jewels[i, j] != null) count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Get jewel budget based on matching relics.
        /// 0 matching = 0, 1 = 3, 2 = 6, 3 = 9, 4 = 12
        /// </summary>
        public int GetJewelBudget(UnitRole role)
        {
            int matching = GetMatchingRelicCount(role);
            return matching * 3;
        }
        
        /// <summary>
        /// Count relics that match the given role.
        /// </summary>
        public int GetMatchingRelicCount(UnitRole role)
        {
            int count = 0;
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (weaponRelics[i] != null && weaponRelics[i].MatchesRole(role))
                    count++;
                if (categoryRelics[i] != null && categoryRelics[i].MatchesRole(role))
                    count++;
            }
            return count;
        }
        
        #endregion
        
        #region List Methods
        
        /// <summary>
        /// Get all equipped weapon relics.
        /// </summary>
        public List<WeaponRelic> GetAllWeaponRelics()
        {
            var list = new List<WeaponRelic>();
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (weaponRelics[i] != null)
                    list.Add(weaponRelics[i]);
            }
            return list;
        }
        
        /// <summary>
        /// Get all equipped category relics.
        /// </summary>
        public List<EquippedRelic> GetAllCategoryRelics()
        {
            var list = new List<EquippedRelic>();
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (categoryRelics[i] != null)
                    list.Add(categoryRelics[i]);
            }
            return list;
        }
        
        /// <summary>
        /// Get relic from a slot as RelicData.
        /// </summary>
        public RelicData GetRelic(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            // Return weapon relic as RelicData if present
            if (weaponRelics[slot] != null)
                return ConvertToRelicData(weaponRelics[slot]);
            // Return category relic as RelicData if present
            if (categoryRelics[slot] != null)
                return ConvertToRelicData(categoryRelics[slot]);
            return null;
        }
        
        /// <summary>
        /// Convert WeaponRelic to RelicData for compatibility.
        /// </summary>
        private RelicData ConvertToRelicData(WeaponRelic weaponRelic)
        {
            if (weaponRelic == null) return null;
            var data = ScriptableObject.CreateInstance<RelicData>();
            data.relicName = weaponRelic.relicName;
            data.roleTag = weaponRelic.roleTag;
            data.category = RelicCategory.Weapon;
            data.weaponFamily = weaponRelic.weaponFamily;
            return data;
        }
        
        /// <summary>
        /// Convert EquippedRelic to RelicData for compatibility.
        /// </summary>
        private RelicData ConvertToRelicData(EquippedRelic equippedRelic)
        {
            if (equippedRelic == null) return null;
            var data = ScriptableObject.CreateInstance<RelicData>();
            data.relicName = equippedRelic.relicName;
            data.roleTag = equippedRelic.roleTag;
            data.category = equippedRelic.category;
            return data;
        }
        
        /// <summary>
        /// Get category relic from slot (for when you need the EquippedRelic specifically).
        /// </summary>
        public EquippedRelic GetCategoryRelicAt(int slot)
        {
            if (slot < 0 || slot >= MAX_SLOTS) return null;
            return categoryRelics[slot];
        }
        
        /// <summary>
        /// Get all jewels for a slot as an array.
        /// </summary>
        public JewelData[] GetJewels(int slot)
        {
            var arr = new JewelData[MAX_JEWELS_PER_SLOT];
            if (slot < 0 || slot >= MAX_SLOTS) return arr;
            
            for (int j = 0; j < MAX_JEWELS_PER_SLOT; j++)
            {
                arr[j] = jewels[slot, j];
            }
            return arr;
        }
        
        #endregion
        
        #region Static Methods
        
        /// <summary>
        /// Get display name for a slot index.
        /// </summary>
        public static string GetSlotName(int slot)
        {
            return slot switch
            {
                SLOT_R1 => "R1",
                SLOT_R2 => "R2",
                SLOT_R3 => "R3",
                SLOT_R4 => "R4",
                SLOT_R5 => "R5",
                SLOT_R6 => "ULT",
                SLOT_R7 => "PAS",
                _ => $"Slot {slot}"
            };
        }
        
        #endregion
    }
}