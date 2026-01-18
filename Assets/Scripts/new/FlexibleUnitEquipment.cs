using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// FLEXIBLE slot-based equipment system.
    /// 
    /// Players can equip ANY relic to ANY slot (except Ultimate/Passive which are role-locked).
    /// 
    /// Slot Layout:
    /// [0-4] = Flexible slots - can hold Weapon OR Category relics
    /// [5]   = Ultimate (auto-assigned based on role)
    /// [6]   = Passive (auto-assigned based on role)
    /// </summary>
    public class FlexibleUnitEquipment : MonoBehaviour
    {
        #region Constants
        public const int SLOT_COUNT = 7;
        public const int FLEXIBLE_SLOTS = 5;  // Slots 0-4
        public const int ULTIMATE_SLOT = 5;
        public const int PASSIVE_SLOT = 6;
        #endregion

        #region Serialized Fields
        [Header("Unit Info")]
        [SerializeField] private UnitRole unitRole;
        [SerializeField] private WeaponFamily weaponFamily;

        [Header("Equipment Slots")]
        [SerializeField] private RelicSlot[] slots = new RelicSlot[SLOT_COUNT];
        #endregion

        #region Nested Class - Relic Slot
        [System.Serializable]
        public class RelicSlot
        {
            public bool hasWeapon;
            public WeaponRelic weaponRelic;
            public EquippedRelic categoryRelic;

            public bool IsEmpty => !hasWeapon && weaponRelic == null && categoryRelic == null;
            
            public void Clear()
            {
                hasWeapon = false;
                weaponRelic = null;
                categoryRelic = null;
            }

            public void SetWeapon(WeaponRelic relic)
            {
                Clear();
                hasWeapon = true;
                weaponRelic = relic;
            }

            public void SetCategory(EquippedRelic relic)
            {
                Clear();
                hasWeapon = false;
                categoryRelic = relic;
            }

            public string GetDisplayName()
            {
                if (hasWeapon && weaponRelic != null)
                    return weaponRelic.relicName;
                if (categoryRelic != null)
                    return categoryRelic.relicName;
                return "Empty";
            }

            public int GetEnergyCost()
            {
                if (hasWeapon && weaponRelic != null)
                    return weaponRelic.GetEnergyCost();
                if (categoryRelic != null)
                    return categoryRelic.GetEnergyCost();
                return 0;
            }

            public bool IsPassive()
            {
                if (hasWeapon) return false;
                return categoryRelic?.IsPassive() ?? false;
            }
        }
        #endregion

        #region Properties
        public UnitRole UnitRole => unitRole;
        public WeaponFamily WeaponFamily => weaponFamily;
        
        /// <summary>Get a slot by index (0-6)</summary>
        public RelicSlot GetSlot(int index)
        {
            if (index < 0 || index >= SLOT_COUNT) return null;
            return slots[index];
        }

        /// <summary>Get all non-empty slots</summary>
        public List<RelicSlot> GetEquippedSlots()
        {
            var list = new List<RelicSlot>();
            foreach (var slot in slots)
            {
                if (slot != null && !slot.IsEmpty)
                    list.Add(slot);
            }
            return list;
        }

        /// <summary>Get flexible slots (0-4) that aren't empty</summary>
        public List<RelicSlot> GetActiveSlots()
        {
            var list = new List<RelicSlot>();
            for (int i = 0; i < FLEXIBLE_SLOTS; i++)
            {
                if (slots[i] != null && !slots[i].IsEmpty)
                    list.Add(slots[i]);
            }
            return list;
        }

        /// <summary>Get the Ultimate slot</summary>
        public RelicSlot UltimateSlot => slots[ULTIMATE_SLOT];

        /// <summary>Get the Passive slot</summary>
        public RelicSlot PassiveSlot => slots[PASSIVE_SLOT];
        #endregion

        #region Initialization
        private void Awake()
        {
            // Ensure slots array is initialized
            if (slots == null || slots.Length != SLOT_COUNT)
            {
                slots = new RelicSlot[SLOT_COUNT];
            }
            
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                if (slots[i] == null)
                    slots[i] = new RelicSlot();
            }
        }

        /// <summary>
        /// Initialize with unit info and auto-assign role-locked relics.
        /// </summary>
        public void Initialize(UnitRole role, WeaponFamily family)
        {
            unitRole = role;
            weaponFamily = family;

            // Ensure slots exist
            if (slots == null || slots.Length != SLOT_COUNT)
            {
                slots = new RelicSlot[SLOT_COUNT];
                for (int i = 0; i < SLOT_COUNT; i++)
                    slots[i] = new RelicSlot();
            }

            // Auto-assign Ultimate (slot 5)
            var ultimateRelic = new EquippedRelic(RelicCategory.Ultimate, unitRole);
            slots[ULTIMATE_SLOT].SetCategory(ultimateRelic);

            // Auto-assign PassiveUnique (slot 6)
            var passiveRelic = new EquippedRelic(RelicCategory.PassiveUnique, unitRole);
            slots[PASSIVE_SLOT].SetCategory(passiveRelic);

            Debug.Log($"<color=cyan>[FlexibleEquipment] Initialized {role} with Ultimate and Passive</color>");
        }
        #endregion

        #region Equip Methods
        /// <summary>
        /// Equip a weapon relic to a specific slot (0-4 only).
        /// </summary>
        public bool EquipWeapon(int slotIndex, WeaponRelic relic)
        {
            if (slotIndex < 0 || slotIndex >= FLEXIBLE_SLOTS)
            {
                Debug.LogWarning($"Cannot equip weapon to slot {slotIndex} - only slots 0-4 are flexible");
                return false;
            }

            if (relic == null)
            {
                slots[slotIndex].Clear();
                return true;
            }

            slots[slotIndex].SetWeapon(relic);
            Debug.Log($"<color=cyan>[FlexibleEquipment] Slot {slotIndex}: Equipped weapon {relic.relicName}</color>");
            return true;
        }

        /// <summary>
        /// Equip a category relic to a specific slot (0-4 only, Ultimate/Passive are auto-assigned).
        /// </summary>
        public bool EquipCategory(int slotIndex, EquippedRelic relic)
        {
            if (slotIndex < 0 || slotIndex >= FLEXIBLE_SLOTS)
            {
                Debug.LogWarning($"Cannot equip category relic to slot {slotIndex} - only slots 0-4 are flexible");
                return false;
            }

            if (relic == null)
            {
                slots[slotIndex].Clear();
                return true;
            }

            slots[slotIndex].SetCategory(relic);
            Debug.Log($"<color=cyan>[FlexibleEquipment] Slot {slotIndex}: Equipped {relic.category} - {relic.relicName}</color>");
            return true;
        }

        /// <summary>
        /// Clear a slot.
        /// </summary>
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT) return;
            
            // Don't clear Ultimate/Passive
            if (slotIndex == ULTIMATE_SLOT || slotIndex == PASSIVE_SLOT)
            {
                Debug.LogWarning("Cannot clear Ultimate or Passive slots");
                return;
            }

            slots[slotIndex].Clear();
        }

        /// <summary>
        /// Clear all flexible slots (0-4).
        /// </summary>
        public void ClearAllFlexible()
        {
            for (int i = 0; i < FLEXIBLE_SLOTS; i++)
            {
                slots[i].Clear();
            }
        }
        #endregion

        #region Transfer from UnitData
        /// <summary>
        /// Transfer equipment from UnitData (used during deployment).
        /// </summary>
        public void TransferFromUnitData(UnitData data)
        {
            Debug.Log($"<color=yellow>[FlexibleEquipment] Transferring from UnitData for {data.unitName}</color>");

            // Transfer weapon relics
            if (data.weaponRelics != null)
            {
                for (int i = 0; i < Mathf.Min(data.weaponRelics.Length, FLEXIBLE_SLOTS); i++)
                {
                    if (data.weaponRelics[i] != null)
                    {
                        EquipWeapon(i, data.weaponRelics[i]);
                    }
                }
            }

            // Transfer category relics (these go by slot index, not category!)
            if (data.categoryRelics != null)
            {
                for (int i = 0; i < Mathf.Min(data.categoryRelics.Length, FLEXIBLE_SLOTS); i++)
                {
                    // Only transfer to slot if it's empty (weapon might have filled it)
                    if (data.categoryRelics[i] != null && slots[i].IsEmpty)
                    {
                        EquipCategory(i, data.categoryRelics[i]);
                    }
                }
            }

            // Log final state
            LogEquipmentState();
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Check if unit has a matching role relic for Proficiency bonus.
        /// </summary>
        public int CountMatchingRelics()
        {
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                
                if (slot.hasWeapon && slot.weaponRelic != null)
                {
                    if (slot.weaponRelic.MatchesRole(unitRole)) count++;
                }
                else if (slot.categoryRelic != null)
                {
                    if (slot.categoryRelic.MatchesRole(unitRole)) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Get total card count for deck building.
        /// </summary>
        public int GetTotalCardCount()
        {
            int total = 0;
            for (int i = 0; i < FLEXIBLE_SLOTS; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.IsEmpty) continue;

                if (slot.hasWeapon && slot.weaponRelic?.baseWeaponData != null)
                {
                    total += slot.weaponRelic.baseWeaponData.cardCopies;
                }
                else if (slot.categoryRelic != null && !slot.categoryRelic.IsPassive())
                {
                    total += slot.categoryRelic.GetCopies();
                }
            }
            return total;
        }
        #endregion

        #region Debug
        public void LogEquipmentState()
        {
            Debug.Log($"<color=green>[FlexibleEquipment] Equipment state for {unitRole}:</color>");
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                var slot = slots[i];
                string slotLabel = i switch
                {
                    ULTIMATE_SLOT => "ULT",
                    PASSIVE_SLOT => "PAS",
                    _ => $"S{i}"
                };

                if (slot == null || slot.IsEmpty)
                {
                    Debug.Log($"<color=gray>  [{slotLabel}] Empty</color>");
                }
                else if (slot.hasWeapon)
                {
                    Debug.Log($"<color=yellow>  [{slotLabel}] WEAPON: {slot.weaponRelic?.relicName}</color>");
                }
                else
                {
                    string passive = slot.categoryRelic?.IsPassive() == true ? " (Passive)" : "";
                    Debug.Log($"<color=cyan>  [{slotLabel}] {slot.categoryRelic?.category}: {slot.categoryRelic?.relicName}{passive}</color>");
                }
            }
        }

        public string GetEquipmentSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Unit: {unitRole} ({weaponFamily})");
            sb.AppendLine($"Matching Relics: {CountMatchingRelics()}");
            sb.AppendLine("---");
            
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                var slot = slots[i];
                string label = i switch
                {
                    ULTIMATE_SLOT => "Ultimate",
                    PASSIVE_SLOT => "Passive",
                    _ => $"Slot {i + 1}"
                };

                if (slot == null || slot.IsEmpty)
                    sb.AppendLine($"{label}: Empty");
                else
                    sb.AppendLine($"{label}: {slot.GetDisplayName()}");
            }
            
            return sb.ToString();
        }
        #endregion
    }
}