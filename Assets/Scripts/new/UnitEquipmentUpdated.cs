using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Equipment system supporting:
    /// - 1 Weapon Relic (role-tagged weapon)
    /// - 6 Category Relics (Boots, Gloves, Hat, Coat, Trinket, Totem)
    /// - 1 Ultimate (role-locked)
    /// - 1 Passive Unique (role-locked)
    /// </summary>
    public class UnitEquipmentUpdated : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Unit Info")]
        [SerializeField] private UnitRole unitRole;
        [SerializeField] private WeaponFamily weaponFamily;
        
        [Header("Weapon Relic")]
        [SerializeField] private WeaponRelic weaponRelic;
        
        [Header("Category Relics (Player Choice)")]
        [SerializeField] private EquippedRelic bootsRelic;
        [SerializeField] private EquippedRelic glovesRelic;
        [SerializeField] private EquippedRelic hatRelic;
        [SerializeField] private EquippedRelic coatRelic;
        [SerializeField] private EquippedRelic trinketRelic;  // Passive
        [SerializeField] private EquippedRelic totemRelic;
        
        [Header("Role-Locked Relics (Auto-assigned)")]
        [SerializeField] private EquippedRelic ultimateRelic;
        [SerializeField] private EquippedRelic passiveUniqueRelic;
        
        #endregion
        
        #region Public Properties
        
        public UnitRole UnitRole => unitRole;
        public WeaponFamily WeaponFamily => weaponFamily;
        public WeaponRelic WeaponRelic => weaponRelic;
        
        public EquippedRelic BootsRelic => bootsRelic;
        public EquippedRelic GlovesRelic => glovesRelic;
        public EquippedRelic HatRelic => hatRelic;
        public EquippedRelic CoatRelic => coatRelic;
        public EquippedRelic TrinketRelic => trinketRelic;
        public EquippedRelic TotemRelic => totemRelic;
        public EquippedRelic UltimateRelic => ultimateRelic;
        public EquippedRelic PassiveUniqueRelic => passiveUniqueRelic;
        
        #endregion
        
        #region Initialization
        
        public void Initialize(UnitRole role, WeaponFamily family)
        {
            unitRole = role;
            weaponFamily = family;
            AssignRoleLockedRelics();
        }
        
        private void AssignRoleLockedRelics()
        {
            ultimateRelic = new EquippedRelic(RelicCategory.Ultimate, unitRole);
            passiveUniqueRelic = new EquippedRelic(RelicCategory.PassiveUnique, unitRole);
        }
        
        #endregion
        
        #region Equip Methods
        
        public bool EquipWeaponRelic(WeaponRelic relic)
        {
            if (relic == null) return false;
            weaponRelic = relic;
            return true;
        }
        
        public bool EquipRelic(EquippedRelic relic)
        {
            if (relic == null) return false;
            
            switch (relic.category)
            {
                case RelicCategory.Boots: bootsRelic = relic; break;
                case RelicCategory.Gloves: glovesRelic = relic; break;
                case RelicCategory.Hat: hatRelic = relic; break;
                case RelicCategory.Coat: coatRelic = relic; break;
                case RelicCategory.Trinket: trinketRelic = relic; break;
                case RelicCategory.Totem: totemRelic = relic; break;
                case RelicCategory.Ultimate: ultimateRelic = relic; break;
                case RelicCategory.PassiveUnique: passiveUniqueRelic = relic; break;
                default: return false;
            }
            
            Debug.Log($"<color=cyan>Equipped {relic.relicName}</color>");
            return true;
        }
        
        public bool EquipRelic(RelicCategory category, UnitRole roleTag)
        {
            var relic = new EquippedRelic(category, roleTag);
            return EquipRelic(relic);
        }
        
        public EquippedRelic UnequipRelic(RelicCategory category)
        {
            EquippedRelic removed = null;
            
            switch (category)
            {
                case RelicCategory.Boots: removed = bootsRelic; bootsRelic = null; break;
                case RelicCategory.Gloves: removed = glovesRelic; glovesRelic = null; break;
                case RelicCategory.Hat: removed = hatRelic; hatRelic = null; break;
                case RelicCategory.Coat: removed = coatRelic; coatRelic = null; break;
                case RelicCategory.Trinket: removed = trinketRelic; trinketRelic = null; break;
                case RelicCategory.Totem: removed = totemRelic; totemRelic = null; break;
            }
            
            return removed;
        }
        
        #endregion
        
        #region Query Methods (for RelicCardUI)
        
        /// <summary>
        /// Get all weapon relics as a list.
        /// </summary>
        public List<WeaponRelic> GetAllWeaponRelics()
        {
            var list = new List<WeaponRelic>();
            if (weaponRelic != null) list.Add(weaponRelic);
            return list;
        }
        
        /// <summary>
        /// Get all category relics (including passive ones).
        /// </summary>
        public List<EquippedRelic> GetAllEquippedRelics()
        {
            var list = new List<EquippedRelic>();
            if (bootsRelic != null) list.Add(bootsRelic);
            if (glovesRelic != null) list.Add(glovesRelic);
            if (hatRelic != null) list.Add(hatRelic);
            if (coatRelic != null) list.Add(coatRelic);
            if (trinketRelic != null) list.Add(trinketRelic);
            if (totemRelic != null) list.Add(totemRelic);
            if (ultimateRelic != null) list.Add(ultimateRelic);
            if (passiveUniqueRelic != null) list.Add(passiveUniqueRelic);
            return list;
        }
        
        /// <summary>
        /// Get active (non-passive) relics.
        /// </summary>
        public List<EquippedRelic> GetActiveRelics()
        {
            return GetAllEquippedRelics().Where(r => !r.IsPassive()).ToList();
        }
        
        /// <summary>
        /// Get passive relics.
        /// </summary>
        public List<EquippedRelic> GetPassiveRelics()
        {
            return GetAllEquippedRelics().Where(r => r.IsPassive()).ToList();
        }
        
        /// <summary>
        /// Get relic by category.
        /// </summary>
        public EquippedRelic GetRelic(RelicCategory category)
        {
            return category switch
            {
                RelicCategory.Boots => bootsRelic,
                RelicCategory.Gloves => glovesRelic,
                RelicCategory.Hat => hatRelic,
                RelicCategory.Coat => coatRelic,
                RelicCategory.Trinket => trinketRelic,
                RelicCategory.Totem => totemRelic,
                RelicCategory.Ultimate => ultimateRelic,
                RelicCategory.PassiveUnique => passiveUniqueRelic,
                _ => null
            };
        }
        
        /// <summary>
        /// Count matching relics.
        /// </summary>
        public int GetMatchingRelicCount()
        {
            int count = 0;
            if (weaponRelic != null && weaponRelic.MatchesRole(unitRole)) count++;
            foreach (var r in GetAllEquippedRelics())
            {
                if (r.MatchesRole(unitRole)) count++;
            }
            return count;
        }
        
        /// <summary>
        /// Get total card count.
        /// </summary>
        public int GetTotalCardCount()
        {
            int total = 0;
            if (weaponRelic?.baseWeaponData != null)
                total += weaponRelic.baseWeaponData.cardCopies;
            foreach (var r in GetActiveRelics())
                total += r.GetCopies();
            return total;
        }
        
        #endregion
        
        #region Debug
        
        public string GetEquipmentSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Unit: {unitRole} ({weaponFamily})");
            sb.AppendLine($"Matching: {GetMatchingRelicCount()}");
            sb.AppendLine("---");
            if (weaponRelic != null) sb.AppendLine($"Weapon: {weaponRelic.relicName}");
            foreach (var r in GetAllEquippedRelics())
            {
                string passive = r.IsPassive() ? " (P)" : "";
                string match = r.MatchesRole(unitRole) ? " ✓" : "";
                sb.AppendLine($"{r.category}: {r.relicName}{passive}{match}");
            }
            return sb.ToString();
        }
        
        #endregion
    }
}