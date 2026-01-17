using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Updated equipment system supporting:
    /// - 1 Weapon Relic (role-tagged weapon)
    /// - 6 Category Relics (Boots, Gloves, Hat, Coat, Trinket, Totem)
    /// - 1 Ultimate (role-locked)
    /// - 1 Passive Unique (role-locked)
    /// 
    /// Each relic (except passives) adds cards to the unit's deck.
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
        
        /// <summary>
        /// Initialize equipment for a unit.
        /// </summary>
        public void Initialize(UnitRole role, WeaponFamily family)
        {
            unitRole = role;
            weaponFamily = family;
            
            // Auto-assign role-locked relics
            AssignRoleLockedRelics();
        }
        
        /// <summary>
        /// Assign the role-locked Ultimate and Passive Unique relics.
        /// </summary>
        private void AssignRoleLockedRelics()
        {
            ultimateRelic = new EquippedRelic(RelicCategory.Ultimate, unitRole);
            passiveUniqueRelic = new EquippedRelic(RelicCategory.PassiveUnique, unitRole);
        }
        
        #endregion
        
        #region Equip Methods
        
        /// <summary>
        /// Equip a weapon relic.
        /// </summary>
        public bool EquipWeaponRelic(WeaponRelic relic)
        {
            if (relic == null) return false;
            if (!relic.MatchesFamily(weaponFamily))
            {
                Debug.LogWarning($"Weapon relic {relic.relicName} doesn't match family {weaponFamily}");
                return false;
            }
            
            weaponRelic = relic;
            return true;
        }
        
        /// <summary>
        /// Equip a category relic.
        /// </summary>
        public bool EquipRelic(EquippedRelic relic)
        {
            if (relic == null) return false;
            
            // Don't allow equipping role-locked categories
            if (relic.category == RelicCategory.Ultimate || relic.category == RelicCategory.PassiveUnique)
            {
                Debug.LogWarning($"Cannot manually equip {relic.category} - it's role-locked");
                return false;
            }
            
            switch (relic.category)
            {
                case RelicCategory.Boots:
                    bootsRelic = relic;
                    break;
                case RelicCategory.Gloves:
                    glovesRelic = relic;
                    break;
                case RelicCategory.Hat:
                    hatRelic = relic;
                    break;
                case RelicCategory.Coat:
                    coatRelic = relic;
                    break;
                case RelicCategory.Trinket:
                    trinketRelic = relic;
                    break;
                case RelicCategory.Totem:
                    totemRelic = relic;
                    break;
                default:
                    Debug.LogWarning($"Unknown relic category: {relic.category}");
                    return false;
            }
            
            Debug.Log($"<color=cyan>Equipped {relic.relicName} to {gameObject.name}</color>");
            return true;
        }
        
        /// <summary>
        /// Equip a relic by category and role.
        /// </summary>
        public bool EquipRelic(RelicCategory category, UnitRole roleTag)
        {
            var relic = new EquippedRelic(category, roleTag);
            return EquipRelic(relic);
        }
        
        /// <summary>
        /// Unequip a relic by category.
        /// </summary>
        public EquippedRelic UnequipRelic(RelicCategory category)
        {
            EquippedRelic removed = null;
            
            switch (category)
            {
                case RelicCategory.Boots:
                    removed = bootsRelic;
                    bootsRelic = null;
                    break;
                case RelicCategory.Gloves:
                    removed = glovesRelic;
                    glovesRelic = null;
                    break;
                case RelicCategory.Hat:
                    removed = hatRelic;
                    hatRelic = null;
                    break;
                case RelicCategory.Coat:
                    removed = coatRelic;
                    coatRelic = null;
                    break;
                case RelicCategory.Trinket:
                    removed = trinketRelic;
                    trinketRelic = null;
                    break;
                case RelicCategory.Totem:
                    removed = totemRelic;
                    totemRelic = null;
                    break;
            }
            
            return removed;
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Get all equipped relics (including weapon).
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
        /// Get all relics that add cards (non-passive).
        /// </summary>
        public List<EquippedRelic> GetActiveRelics()
        {
            return GetAllEquippedRelics().Where(r => !r.IsPassive()).ToList();
        }
        
        /// <summary>
        /// Get all passive relics.
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
        /// Count relics that match this unit's role (for Proficiency bonus).
        /// </summary>
        public int GetMatchingRelicCount()
        {
            int count = 0;
            foreach (var relic in GetAllEquippedRelics())
            {
                if (relic.MatchesRole(unitRole)) count++;
            }
            
            // Also count weapon relic
            if (weaponRelic != null && weaponRelic.MatchesRole(unitRole)) count++;
            
            return count;
        }
        
        /// <summary>
        /// Get total cards this equipment adds to deck.
        /// </summary>
        public int GetTotalCardCount()
        {
            int total = 0;
            
            // Weapon relic cards (from weapon data)
            if (weaponRelic != null && weaponRelic.baseWeaponData != null)
            {
                total += weaponRelic.baseWeaponData.cardCopies;
            }
            
            // Category relic cards
            foreach (var relic in GetActiveRelics())
            {
                total += relic.GetCopies();
            }
            
            return total;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Get equipment summary for debugging/UI.
        /// </summary>
        public string GetEquipmentSummary()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Unit: {unitRole} ({weaponFamily})");
            sb.AppendLine($"Matching Relics: {GetMatchingRelicCount()}");
            sb.AppendLine($"Total Cards: {GetTotalCardCount()}");
            sb.AppendLine("---");
            
            if (weaponRelic != null)
                sb.AppendLine($"Weapon: {weaponRelic.relicName}");
            
            foreach (var relic in GetAllEquippedRelics())
            {
                string match = relic.MatchesRole(unitRole) ? " ✓" : "";
                string passive = relic.IsPassive() ? " (P)" : "";
                sb.AppendLine($"{relic.category}: {relic.relicName}{passive}{match}");
            }
            
            return sb.ToString();
        }
        
        #endregion
    }
}