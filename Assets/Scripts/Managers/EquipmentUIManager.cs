using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.UI;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Equipment screen with 6 relic slots per unit, each with 3 jewel sockets.
    /// 
    /// Layout:
    /// - LEFT: Unit list
    /// - CENTER: 6 relic slots (R1-R4, ULT, PAS) with jewels
    /// - RIGHT: Relic pool (top) + Jewel pool (bottom)
    /// </summary>
    public class EquipmentUIManager : MonoBehaviour
    {
        [Header("Canvas References")]
        [SerializeField] private GameObject equipmentCanvas;
        [SerializeField] private GameObject creationCanvas;
        [SerializeField] private GameObject battleCanvas;

        [Header("Unit List (Left)")]
        [SerializeField] private Transform unitListContainer;
        [SerializeField] private GameObject unitListItemPrefab;

        [Header("Selected Unit Info")]
        [SerializeField] private TMP_Text selectedUnitNameText;
        [SerializeField] private TMP_Text selectedUnitRoleText;
        [SerializeField] private TMP_Text selectedUnitWeaponText;

        [Header("Relic Slots (Center)")]
        [SerializeField] private RelicSlotUI relicSlot1;
        [SerializeField] private RelicSlotUI relicSlot2;
        [SerializeField] private RelicSlotUI relicSlot3;
        [SerializeField] private RelicSlotUI relicSlot4;
        [SerializeField] private RelicSlotUI ultimateSlot;
        [SerializeField] private RelicSlotUI passiveSlot;

        [Header("Relic Pool (Right Top)")]
        [SerializeField] private Transform relicPoolContainer;
        [SerializeField] private GameObject relicPoolItemPrefab;
        [SerializeField] private TMP_Text relicPoolTitleText;

        [Header("Jewel Pool (Right Bottom)")]
        [SerializeField] private Transform jewelPoolContainer;
        [SerializeField] private GameObject jewelPoolItemPrefab;
        [SerializeField] private TMP_Text jewelBudgetText;

        [Header("Info Panel")]
        [SerializeField] private GameObject itemInfoPanel;
        [SerializeField] private TMP_Text itemInfoNameText;
        [SerializeField] private TMP_Text itemInfoDescText;
        [SerializeField] private TMP_Text itemInfoStatsText;

        [Header("Buttons")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button unequipAllButton;

        [Header("Managers")]
        [SerializeField] private DeploymentManager deploymentManager;

        // Data
        private List<UnitData> playerUnits = new List<UnitData>();
        private List<UnitData> enemyUnits = new List<UnitData>();
        private List<WeaponRelic> weaponRelicPool = new List<WeaponRelic>();
        private List<JewelData> jewelPool = new List<JewelData>();

        // Selection
        private UnitData selectedUnit;
        private int selectedUnitIndex = -1;
        private RelicSlotUI selectedRelicSlot;
        private int selectedJewelIndex = -1;

        // Spawned UI
        private List<GameObject> spawnedUnitItems = new List<GameObject>();
        private List<GameObject> spawnedRelicItems = new List<GameObject>();
        private List<GameObject> spawnedJewelItems = new List<GameObject>();

        void Start()
        {
            if (startBattleButton) startBattleButton.onClick.AddListener(OnStartBattle);
            if (backButton) backButton.onClick.AddListener(OnBack);
            if (unequipAllButton) unequipAllButton.onClick.AddListener(UnequipAll);

            SetupSlotCallbacks();

            if (equipmentCanvas) equipmentCanvas.SetActive(false);
            if (itemInfoPanel) itemInfoPanel.SetActive(false);
        }

        void SetupSlotCallbacks()
        {
            relicSlot1?.Setup("R1", RelicSlotType.Mixed, 0, OnRelicSlotClicked, OnJewelSlotClicked);
            relicSlot2?.Setup("R2", RelicSlotType.Mixed, 1, OnRelicSlotClicked, OnJewelSlotClicked);
            relicSlot3?.Setup("R3", RelicSlotType.Mixed, 2, OnRelicSlotClicked, OnJewelSlotClicked);
            relicSlot4?.Setup("R4", RelicSlotType.Mixed, 3, OnRelicSlotClicked, OnJewelSlotClicked);
            ultimateSlot?.Setup("ULT", RelicSlotType.Ultimate, 4, OnRelicSlotClicked, OnJewelSlotClicked);
            passiveSlot?.Setup("PAS", RelicSlotType.Passive, 5, OnRelicSlotClicked, OnJewelSlotClicked);
        }

        public void OpenEquipmentScreen(List<UnitData> players, List<UnitData> enemies)
        {
            playerUnits = players;
            enemyUnits = enemies;

            if (creationCanvas) creationCanvas.SetActive(false);
            if (equipmentCanvas) equipmentCanvas.SetActive(true);

            InitializeEquipment();
            GeneratePools();
            ClearUI();
            PopulateUnitList();

            if (playerUnits.Count > 0) SelectUnit(0);
        }

        void InitializeEquipment()
        {
            foreach (var unit in playerUnits)
            {
                if (unit.equipment == null) unit.equipment = new UnitEquipmentData();
                if (unit.defaultWeaponRelic != null && unit.equipment.IsSlotEmpty(0))
                    unit.equipment.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
            foreach (var unit in enemyUnits)
            {
                if (unit.equipment == null) unit.equipment = new UnitEquipmentData();
                if (unit.defaultWeaponRelic != null && unit.equipment.IsSlotEmpty(0))
                    unit.equipment.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
        }

        void GeneratePools()
        {
            var assigned = new List<WeaponRelic>();
            foreach (var u in playerUnits)
                if (u.equipment != null) assigned.AddRange(u.equipment.GetAllWeaponRelics());

            weaponRelicPool = WeaponRelicGenerator.GenerateRelicPool(assigned);
            
            // Load jewels from Resources
            var loaded = Resources.LoadAll<JewelData>("Jewels");
            jewelPool = loaded != null ? loaded.ToList() : new List<JewelData>();
        }

        void PopulateUnitList()
        {
            if (unitListContainer == null || unitListItemPrefab == null) return;

            for (int i = 0; i < playerUnits.Count; i++)
            {
                var item = Instantiate(unitListItemPrefab, unitListContainer);
                spawnedUnitItems.Add(item);

                var ui = item.GetComponent<UnitListItemUI>();
                if (ui != null) ui.Setup(playerUnits[i], i, this);
            }
        }

        public void SelectUnit(int index)
        {
            if (index < 0 || index >= playerUnits.Count) return;

            selectedUnitIndex = index;
            selectedUnit = playerUnits[index];
            selectedRelicSlot = null;
            selectedJewelIndex = -1;

            UpdateUnitListVisuals();
            UpdateUnitInfo();
            UpdateRelicSlots();
            UpdateJewelBudget();
            RefreshRelicPool();
            RefreshJewelPool();

            if (itemInfoPanel) itemInfoPanel.SetActive(false);
        }

        void UpdateUnitListVisuals()
        {
            for (int i = 0; i < spawnedUnitItems.Count; i++)
            {
                var ui = spawnedUnitItems[i].GetComponent<UnitListItemUI>();
                ui?.SetSelected(i == selectedUnitIndex);
            }
        }

        void UpdateUnitInfo()
        {
            if (selectedUnit == null) return;
            if (selectedUnitNameText) selectedUnitNameText.text = selectedUnit.unitName;
            if (selectedUnitRoleText) selectedUnitRoleText.text = selectedUnit.GetRoleDisplayName();
            if (selectedUnitWeaponText)
            {
                string wt = selectedUnit.weaponType == WeaponType.Melee ? "Melee" : "Ranged";
                selectedUnitWeaponText.text = $"{selectedUnit.GetWeaponFamilyDisplayName()} ({wt})";
            }
        }

        void UpdateRelicSlots()
        {
            if (selectedUnit?.equipment == null) return;
            var eq = selectedUnit.equipment;

            UpdateSlot(relicSlot1, 0, eq);
            UpdateSlot(relicSlot2, 1, eq);
            UpdateSlot(relicSlot3, 2, eq);
            UpdateSlot(relicSlot4, 3, eq);
            UpdateSlot(ultimateSlot, 4, eq);
            UpdateSlot(passiveSlot, 5, eq);
        }

        void UpdateSlot(RelicSlotUI slot, int idx, UnitEquipmentData eq)
        {
            if (slot == null) return;

            var wr = eq.GetWeaponRelic(idx);
            var r = eq.GetRelic(idx);

            if (wr != null)
                slot.DisplayWeaponRelic(wr, wr.MatchesRole(selectedUnit.role));
            else if (r != null)
                slot.DisplayRelic(r, r.MatchesRole(selectedUnit.role));
            else
                slot.SetEmpty();

            slot.UpdateJewels(eq.GetJewels(idx));
        }

        void UpdateJewelBudget()
        {
            if (jewelBudgetText == null || selectedUnit?.equipment == null) return;
            int used = selectedUnit.equipment.GetTotalEquippedJewelCount();
            int max = selectedUnit.equipment.GetJewelBudget(selectedUnit.role);
            jewelBudgetText.text = $"Jewels: {used}/{max}";
            jewelBudgetText.color = used > max ? Color.red : (used == max ? Color.yellow : Color.white);
        }

        void RefreshRelicPool()
        {
            foreach (var item in spawnedRelicItems) if (item) Destroy(item);
            spawnedRelicItems.Clear();

            if (selectedUnit == null || relicPoolContainer == null) return;

            if (relicPoolTitleText)
                relicPoolTitleText.text = selectedRelicSlot != null
                    ? $"Relics for {selectedRelicSlot.SlotLabel}:"
                    : "Available Relics:";

            var available = weaponRelicPool
                .Where(r => r.MatchesFamily(selectedUnit.weaponFamily))
                .OrderByDescending(r => r.MatchesRole(selectedUnit.role))
                .ThenByDescending(r => (int)r.effectData.rarity)
                .ToList();

            foreach (var relic in available)
            {
                if (relicPoolItemPrefab == null) continue;
                var item = Instantiate(relicPoolItemPrefab, relicPoolContainer);
                spawnedRelicItems.Add(item);

                var ui = item.GetComponent<RelicPoolItemUI>();
                ui?.SetupWeaponRelic(relic, relic.MatchesRole(selectedUnit.role), OnRelicPoolClicked, OnRelicPoolHover);
            }
        }

        void RefreshJewelPool()
        {
            foreach (var item in spawnedJewelItems) if (item) Destroy(item);
            spawnedJewelItems.Clear();

            if (jewelPoolContainer == null) return;

            foreach (var jewel in jewelPool)
            {
                if (jewelPoolItemPrefab == null) continue;
                var item = Instantiate(jewelPoolItemPrefab, jewelPoolContainer);
                spawnedJewelItems.Add(item);

                var ui = item.GetComponent<JewelPoolItemUI>();
                ui?.Setup(jewel, OnJewelPoolClicked, OnJewelPoolHover);
            }
        }

        // Slot click handlers
        void OnRelicSlotClicked(RelicSlotUI slot)
        {
            selectedRelicSlot = slot;
            selectedJewelIndex = -1;
            HighlightSlot();
            RefreshRelicPool();
        }

        void OnJewelSlotClicked(RelicSlotUI slot, int jewelIdx)
        {
            selectedRelicSlot = slot;
            selectedJewelIndex = jewelIdx;
            HighlightSlot();
        }

        void HighlightSlot()
        {
            relicSlot1?.SetHighlight(false);
            relicSlot2?.SetHighlight(false);
            relicSlot3?.SetHighlight(false);
            relicSlot4?.SetHighlight(false);
            ultimateSlot?.SetHighlight(false);
            passiveSlot?.SetHighlight(false);

            if (selectedRelicSlot != null)
            {
                selectedRelicSlot.SetHighlight(true);
                if (selectedJewelIndex >= 0)
                    selectedRelicSlot.HighlightJewelSlot(selectedJewelIndex);
            }
        }

        // Pool click handlers
        void OnRelicPoolClicked(object data)
        {
            if (selectedUnit == null || selectedRelicSlot == null) return;
            if (!(data is WeaponRelic wr)) return;
            if (!wr.MatchesFamily(selectedUnit.weaponFamily)) return;

            int idx = selectedRelicSlot.SlotIndex;
            var current = selectedUnit.equipment.GetWeaponRelic(idx);
            if (current != null) weaponRelicPool.Add(current);

            selectedUnit.equipment.EquipWeaponRelic(idx, wr);
            weaponRelicPool.Remove(wr);

            UpdateRelicSlots();
            UpdateJewelBudget();
            RefreshRelicPool();
        }

        void OnRelicPoolHover(object data, bool enter)
        {
            if (itemInfoPanel == null) return;
            if (enter && data is WeaponRelic wr)
            {
                itemInfoPanel.SetActive(true);
                if (itemInfoNameText) itemInfoNameText.text = wr.relicName;
                if (itemInfoDescText) itemInfoDescText.text = $"{wr.effectData.effectName}\n{wr.effectData.description}";
                if (itemInfoStatsText)
                {
                    string s = $"Damage: {wr.GetTotalBaseDamage()}\nCost: {wr.GetEnergyCost()}";
                    if (wr.effectData.bonusDamagePercent > 0) s += $"\n+{wr.effectData.bonusDamagePercent * 100:F0}% Dmg";
                    if (selectedUnit != null && wr.MatchesRole(selectedUnit.role)) s += "\n★ Role Match!";
                    itemInfoStatsText.text = s;
                }
            }
            else itemInfoPanel.SetActive(false);
        }

        void OnJewelPoolClicked(JewelData jewel)
        {
            if (selectedUnit == null || selectedRelicSlot == null || selectedJewelIndex < 0) return;

            var eq = selectedUnit.equipment;
            if (eq.GetTotalEquippedJewelCount() >= eq.GetJewelBudget(selectedUnit.role)) return;

            int idx = selectedRelicSlot.SlotIndex;
            var current = eq.GetJewel(idx, selectedJewelIndex);
            if (current != null) jewelPool.Add(current);

            eq.EquipJewel(idx, selectedJewelIndex, jewel);
            jewelPool.Remove(jewel);

            UpdateRelicSlots();
            UpdateJewelBudget();
            RefreshJewelPool();
        }

        void OnJewelPoolHover(JewelData jewel, bool enter)
        {
            if (itemInfoPanel == null) return;
            if (enter && jewel != null)
            {
                itemInfoPanel.SetActive(true);
                if (itemInfoNameText) itemInfoNameText.text = jewel.jewelName;
                if (itemInfoDescText) itemInfoDescText.text = jewel.effectDescription;
                if (itemInfoStatsText)
                {
                    string s = "";
                    if (jewel.flatDamageBonus > 0) s += $"+{jewel.flatDamageBonus} Dmg\n";
                    if (jewel.percentDamageBonus > 0) s += $"+{jewel.percentDamageBonus * 100:F0}% Dmg\n";
                    if (jewel.energyCostModifier != 0) s += $"{jewel.energyCostModifier:+0;-0} Cost";
                    itemInfoStatsText.text = s;
                }
            }
            else itemInfoPanel.SetActive(false);
        }

        void UnequipAll()
        {
            if (selectedUnit?.equipment == null) return;

            for (int i = 0; i < 6; i++)
            {
                var wr = selectedUnit.equipment.GetWeaponRelic(i);
                if (wr != null) weaponRelicPool.Add(wr);
                for (int j = 0; j < 3; j++)
                {
                    var jw = selectedUnit.equipment.GetJewel(i, j);
                    if (jw != null) jewelPool.Add(jw);
                }
            }
            selectedUnit.equipment.UnequipAll();

            UpdateRelicSlots();
            UpdateJewelBudget();
            RefreshRelicPool();
            RefreshJewelPool();
        }

        void ClearUI()
        {
            foreach (var i in spawnedUnitItems) if (i) Destroy(i);
            foreach (var i in spawnedRelicItems) if (i) Destroy(i);
            foreach (var i in spawnedJewelItems) if (i) Destroy(i);
            spawnedUnitItems.Clear();
            spawnedRelicItems.Clear();
            spawnedJewelItems.Clear();
        }

        void OnStartBattle()
        {
            if (equipmentCanvas) equipmentCanvas.SetActive(false);
            if (battleCanvas) battleCanvas.SetActive(true);
            deploymentManager?.StartManualDeployment(playerUnits, enemyUnits);
        }

        void OnBack()
        {
            ClearUI();
            if (equipmentCanvas) equipmentCanvas.SetActive(false);
            if (creationCanvas) creationCanvas.SetActive(true);
        }
    }

    public enum RelicSlotType { Mixed, Ultimate, Passive }
}