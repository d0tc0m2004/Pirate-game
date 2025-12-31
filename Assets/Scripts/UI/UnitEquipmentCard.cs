using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI component for a single unit row in the Equipment screen.
    /// Shows: Role, Weapon, 4 Relic slots, Ultimate slot, Passive slot
    /// Each slot has 3 jewel slots underneath.
    /// 
    /// Structure:
    /// - UnitInfoText (shows "Role: xxx    Weapon: xxx")
    /// - SlotsContainer
    ///   - RelicSlot1 (R1) with 3 jewels
    ///   - RelicSlot2 (R2) with 3 jewels
    ///   - RelicSlot3 (R3) with 3 jewels
    ///   - RelicSlot4 (R4) with 3 jewels
    ///   - UltimateSlot (ULT) with 3 jewels
    ///   - PassiveSlot (PAS) with 3 jewels
    /// </summary>
    public class UnitEquipmentCard : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Unit Info Display")]
        [SerializeField] private TMP_Text unitInfoText;

        [Header("Relic Slots (4 mixed slots)")]
        [SerializeField] private RelicSlotWithJewels relicSlot1;
        [SerializeField] private RelicSlotWithJewels relicSlot2;
        [SerializeField] private RelicSlotWithJewels relicSlot3;
        [SerializeField] private RelicSlotWithJewels relicSlot4;

        [Header("Special Slots")]
        [SerializeField] private RelicSlotWithJewels ultimateSlot;
        [SerializeField] private RelicSlotWithJewels passiveSlot;

        [Header("Team Colors")]
        [SerializeField] private Image teamColorBar; // Optional: colored bar on the side
        [SerializeField] private Color playerTeamColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color enemyTeamColor = new Color(1f, 0.3f, 0.3f);

        #endregion

        #region Private State

        private UnitData unitData;
        private WeaponData weaponData;
        private bool isPlayerUnit;

        #endregion

        #region Public Properties

        public UnitData UnitData => unitData;
        public bool IsPlayerUnit => isPlayerUnit;

        #endregion

        #region Public Methods

        /// <summary>
        /// Setup the card with unit data.
        /// </summary>
        public void Setup(UnitData data, bool isPlayer)
        {
            unitData = data;
            isPlayerUnit = isPlayer;

            // Get weapon data from database
            if (WeaponDatabase.Instance != null)
            {
                weaponData = WeaponDatabase.Instance.GetWeapon(data.weaponFamily);
            }

            // Setup unit info text
            SetupUnitInfoText();

            // Setup team color
            SetupTeamColor();

            // Initialize all slots with labels
            InitializeSlots();
        }

        /// <summary>
        /// Get all relic slots as a list.
        /// </summary>
        public List<RelicSlotWithJewels> GetAllRelicSlots()
        {
            return new List<RelicSlotWithJewels>
            {
                relicSlot1, relicSlot2, relicSlot3, relicSlot4,
                ultimateSlot, passiveSlot
            };
        }

        /// <summary>
        /// Get mixed relic slots only (R1-R4).
        /// </summary>
        public List<RelicSlotWithJewels> GetMixedRelicSlots()
        {
            return new List<RelicSlotWithJewels>
            {
                relicSlot1, relicSlot2, relicSlot3, relicSlot4
            };
        }

        #endregion

        #region Private Methods

        private void SetupUnitInfoText()
        {
            if (unitInfoText == null) return;

            string roleName = unitData.GetRoleDisplayName();
            string weaponName = unitData.GetWeaponFamilyDisplayName();
            string weaponType = unitData.weaponType == WeaponType.Melee ? "Melee" : "Ranged";

            unitInfoText.text = $"Role: {roleName}          Weapon: {weaponName} ({weaponType})";
        }

        private void SetupTeamColor()
        {
            if (teamColorBar != null)
            {
                teamColorBar.color = isPlayerUnit ? playerTeamColor : enemyTeamColor;
            }
        }

        private void InitializeSlots()
        {
            // Initialize mixed relic slots (R1-R4)
            if (relicSlot1 != null) relicSlot1.Initialize("R1", isPlayerUnit);
            if (relicSlot2 != null) relicSlot2.Initialize("R2", isPlayerUnit);
            if (relicSlot3 != null) relicSlot3.Initialize("R3", isPlayerUnit);
            if (relicSlot4 != null) relicSlot4.Initialize("R4", isPlayerUnit);

            // Initialize special slots
            if (ultimateSlot != null) ultimateSlot.Initialize("ULT", isPlayerUnit);
            if (passiveSlot != null) passiveSlot.Initialize("PAS", isPlayerUnit);

            // Enemy slots are not interactable (view only)
            if (!isPlayerUnit)
            {
                SetAllSlotsInteractable(false);
            }
        }

        private void SetAllSlotsInteractable(bool interactable)
        {
            if (relicSlot1 != null) relicSlot1.SetInteractable(interactable);
            if (relicSlot2 != null) relicSlot2.SetInteractable(interactable);
            if (relicSlot3 != null) relicSlot3.SetInteractable(interactable);
            if (relicSlot4 != null) relicSlot4.SetInteractable(interactable);
            if (ultimateSlot != null) ultimateSlot.SetInteractable(interactable);
            if (passiveSlot != null) passiveSlot.SetInteractable(interactable);
        }

        #endregion
    }
}