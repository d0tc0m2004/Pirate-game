using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Equipment UI that builds itself entirely through code.
    /// Now includes category relics (Boots, Gloves, Hat, Coat, Trinket, Totem) with filter tabs!
    /// 
    /// Click tabs at top of relic pool to switch between Weapon and Category relics.
    /// </summary>
    public class EquipmentUIBuilder : MonoBehaviour
    {
        #region Colors
        
        private Color bgColor = new Color(0.08f, 0.08f, 0.12f);
        private Color panelColor = new Color(0.12f, 0.12f, 0.18f);
        private Color slotEmptyColor = new Color(0.15f, 0.15f, 0.22f);
        private Color slotFilledColor = new Color(0.2f, 0.2f, 0.3f);
        private Color buttonColor = new Color(0.2f, 0.2f, 0.28f);
        private Color buttonHoverColor = new Color(0.25f, 0.25f, 0.35f);
        private Color accentColor = new Color(1f, 0.84f, 0f); // Gold
        private Color textColor = Color.white;
        private Color textDimColor = new Color(0.6f, 0.6f, 0.6f);
        private Color jewelEmptyColor = new Color(0.1f, 0.1f, 0.12f);
        
        private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
        private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        private Color rareColor = new Color(0.4f, 0.6f, 1f);
        
        // Category tab colors
        private Color weaponTabColor = new Color(0.5f, 0.5f, 0.6f);
        private Color bootsTabColor = new Color(0.3f, 0.7f, 0.3f);
        private Color glovesTabColor = new Color(0.8f, 0.5f, 0.2f);
        private Color hatTabColor = new Color(0.5f, 0.3f, 0.7f);
        private Color coatTabColor = new Color(0.2f, 0.5f, 0.8f);
        private Color trinketTabColor = new Color(0.8f, 0.8f, 0.2f);
        private Color totemTabColor = new Color(0.8f, 0.2f, 0.2f);
        private Color ultimateTabColor = new Color(1f, 0.5f, 0f);      // Orange
        private Color passiveTabColor = new Color(0.6f, 0.4f, 0.8f);   // Light purple
        
        #endregion
        
        #region References (Auto-created)
        
        private Canvas mainCanvas;
        private GameObject equipmentPanel;
        
        // Left Panel
        private Transform unitListContainer;
        private List<GameObject> unitListItems = new List<GameObject>();
        
        // Center Panel
        private TMP_Text unitInfoText;
        private Transform relicSlotContainer;
        private TMP_Text infoText;
        private List<RelicSlotData> relicSlots = new List<RelicSlotData>();
        
        // Right Panel
        private Transform filterTabContainer;
        private Transform relicPoolContainer;
        private Transform jewelPoolContainer;
        private TMP_Text jewelBudgetText;
        private TMP_Text relicPoolTitleText;
        private List<GameObject> relicPoolItems = new List<GameObject>();
        private List<GameObject> jewelPoolItems = new List<GameObject>();
        private List<Button> filterTabButtons = new List<Button>();
        
        #endregion
        
        #region State
        
        private List<UnitData> playerUnits = new List<UnitData>();
        private List<UnitData> enemyUnits = new List<UnitData>();
        private int selectedUnitIndex = -1;
        private int selectedSlotIndex = -1;
        private int selectedJewelIndex = -1;
        
        // Filter state: -1 = Weapon, 0-7 = category index
        private int selectedFilterIndex = -1;
        private RelicCategory[] filterCategories = {
            RelicCategory.Boots, RelicCategory.Gloves, RelicCategory.Hat,
            RelicCategory.Coat, RelicCategory.Trinket, RelicCategory.Totem,
            RelicCategory.Ultimate, RelicCategory.PassiveUnique
        };
        
        // Category relic pool (generated once)
        private List<EquippedRelic> categoryRelicPool = new List<EquippedRelic>();
        
        // Callbacks
        public System.Action onBackClicked;
        public System.Action<List<UnitData>, List<UnitData>> onStartBattle;
        
        #endregion
        
        #region Data Classes
        
        private class RelicSlotData
        {
            public GameObject root;
            public Image background;
            public TMP_Text labelText;
            public TMP_Text nameText;
            public TMP_Text effectText;
            public Image[] jewelImages = new Image[3];
            public Button[] jewelButtons = new Button[3];
            public Image selectionOutline;
            public int slotIndex;
        }
        
        #endregion
        
        #region Category Relic Storage Helpers
        
        // Now uses UnitData directly instead of local dictionary
        
        private EquippedRelic GetCategoryRelic(UnitData unit, int slotIndex)
        {
            return unit?.GetCategoryRelic(slotIndex);
        }
        
        private void SetCategoryRelic(UnitData unit, int slotIndex, EquippedRelic relic)
        {
            unit?.EquipCategoryRelic(slotIndex, relic);
        }
        
        private void ClearCategoryRelic(UnitData unit, int slotIndex)
        {
            unit?.EquipCategoryRelic(slotIndex, null);
        }
        
        private void ClearAllCategoryRelics(UnitData unit)
        {
            if (unit == null) return;
            for (int i = 0; i < 7; i++)
                unit.EquipCategoryRelic(i, null);
        }
        
        #endregion
        
        #region Public Methods
        
        public void Open(List<UnitData> players, List<UnitData> enemies)
        {
            playerUnits = players;
            enemyUnits = enemies;
            
            // Initialize relic arrays and set default weapon relics
            foreach (var unit in playerUnits)
            {
                if (unit.weaponRelics == null) unit.weaponRelics = new WeaponRelic[7];
                if (unit.categoryRelics == null) unit.categoryRelics = new EquippedRelic[7];
                
                // Equip default weapon to slot 0 if empty
                if (unit.defaultWeaponRelic != null && unit.GetWeaponRelic(0) == null)
                    unit.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
            
            foreach (var unit in enemyUnits)
            {
                if (unit.weaponRelics == null) unit.weaponRelics = new WeaponRelic[7];
                if (unit.categoryRelics == null) unit.categoryRelics = new EquippedRelic[7];
                
                if (unit.defaultWeaponRelic != null && unit.GetWeaponRelic(0) == null)
                    unit.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
            
            // Generate category relic pool
            GenerateCategoryRelicPool();
            
            gameObject.SetActive(true);
            BuildUI();
            PopulateUnitList();
            
            if (playerUnits.Count > 0)
                SelectUnit(0);
        }
        
        public void Close()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Get all category relics equipped on a unit (for deck building).
        /// </summary>
        public List<EquippedRelic> GetEquippedCategoryRelics(UnitData unit)
        {
            return unit?.GetAllCategoryRelics() ?? new List<EquippedRelic>();
        }
        
        #endregion
        
        #region Category Relic Pool Generation
        
        private void GenerateCategoryRelicPool()
        {
            categoryRelicPool.Clear();
            
            var effectsDB = RelicEffectsDatabase.Instance;
            if (effectsDB == null)
            {
                Debug.LogWarning("[EquipmentUI] RelicEffectsDatabase not found - category relics unavailable");
                return;
            }
            
            var allRoles = System.Enum.GetValues(typeof(UnitRole)).Cast<UnitRole>().ToList();
            
            foreach (var category in filterCategories)
            {
                foreach (var role in allRoles)
                {
                    var effectData = effectsDB.GetEffect(category, role);
                    if (effectData != null)
                    {
                        categoryRelicPool.Add(new EquippedRelic(effectData));
                    }
                }
            }
            
            Debug.Log($"<color=cyan>[EquipmentUI] Generated {categoryRelicPool.Count} category relics</color>");
        }
        
        #endregion
        
        #region UI Building
        
        private void BuildUI()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            
            relicSlots.Clear();
            unitListItems.Clear();
            relicPoolItems.Clear();
            jewelPoolItems.Clear();
            filterTabButtons.Clear();
            
            GameObject canvasObj = new GameObject("EquipmentCanvas");
            canvasObj.transform.SetParent(transform);
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            equipmentPanel = CreatePanel(canvasObj.transform, "Background", bgColor);
            SetRectFill(equipmentPanel);
            
            GameObject header = CreateText(equipmentPanel.transform, "Header", "EQUIPMENT LOADOUT", 32, TextAlignmentOptions.Center);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, -15);
            headerRt.sizeDelta = new Vector2(0, 50);
            header.GetComponent<TMP_Text>().color = accentColor;
            
            CreateLeftPanel();
            CreateCenterPanel();
            CreateRightPanel();
            CreateButtons();
        }
        
        private void CreateLeftPanel()
        {
            GameObject leftPanel = CreatePanel(equipmentPanel.transform, "LeftPanel", panelColor);
            RectTransform rt = leftPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(20, 70);
            rt.offsetMax = new Vector2(270, -70);
            
            GameObject title = CreateText(leftPanel.transform, "Title", "CREW ROSTER", 18, TextAlignmentOptions.Center);
            RectTransform titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -10);
            titleRt.sizeDelta = new Vector2(0, 35);
            title.GetComponent<TMP_Text>().color = accentColor;
            
            GameObject container = new GameObject("UnitListContainer");
            container.transform.SetParent(leftPanel.transform);
            RectTransform contRt = container.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0, 0);
            contRt.anchorMax = new Vector2(1, 1);
            contRt.offsetMin = new Vector2(10, 70);
            contRt.offsetMax = new Vector2(-10, -50);
            
            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            
            unitListContainer = container.transform;
            
            GameObject budgetObj = CreateText(leftPanel.transform, "JewelBudget", "Jewel Budget: 0 / 0", 13, TextAlignmentOptions.Center);
            RectTransform budgetRt = budgetObj.GetComponent<RectTransform>();
            budgetRt.anchorMin = new Vector2(0, 0);
            budgetRt.anchorMax = new Vector2(1, 0);
            budgetRt.pivot = new Vector2(0.5f, 0);
            budgetRt.anchoredPosition = new Vector2(0, 15);
            budgetRt.sizeDelta = new Vector2(0, 30);
            jewelBudgetText = budgetObj.GetComponent<TMP_Text>();
            jewelBudgetText.color = textDimColor;
        }
        
        private void CreateCenterPanel()
        {
            GameObject centerPanel = CreatePanel(equipmentPanel.transform, "CenterPanel", panelColor);
            RectTransform rt = centerPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(290, 70);
            rt.offsetMax = new Vector2(-360, -70);
            
            GameObject unitInfo = CreateText(centerPanel.transform, "UnitInfo", "| Role | [Type] Weapon", 16, TextAlignmentOptions.Center);
            RectTransform uiRt = unitInfo.GetComponent<RectTransform>();
            uiRt.anchorMin = new Vector2(0, 1);
            uiRt.anchorMax = new Vector2(1, 1);
            uiRt.pivot = new Vector2(0.5f, 1);
            uiRt.anchoredPosition = new Vector2(0, -15);
            uiRt.sizeDelta = new Vector2(0, 35);
            unitInfoText = unitInfo.GetComponent<TMP_Text>();
            
            GameObject slotArea = new GameObject("SlotArea");
            slotArea.transform.SetParent(centerPanel.transform);
            RectTransform saRt = slotArea.AddComponent<RectTransform>();
            saRt.anchorMin = new Vector2(0, 0.25f);
            saRt.anchorMax = new Vector2(1, 1);
            saRt.offsetMin = new Vector2(20, 0);
            saRt.offsetMax = new Vector2(-20, -60);
            
            HorizontalLayoutGroup hlg = slotArea.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            
            relicSlotContainer = slotArea.transform;
            
            string[] labels = { "R1", "R2", "R3", "R4", "R5", "ULT", "PAS" };
            for (int i = 0; i < labels.Length; i++)
                CreateRelicSlot(labels[i], i);
            
            GameObject infoPanel = CreatePanel(centerPanel.transform, "InfoPanel", slotEmptyColor);
            RectTransform ipRt = infoPanel.GetComponent<RectTransform>();
            ipRt.anchorMin = new Vector2(0, 0);
            ipRt.anchorMax = new Vector2(1, 0.25f);
            ipRt.offsetMin = new Vector2(20, 15);
            ipRt.offsetMax = new Vector2(-20, -5);
            
            GameObject infoTextObj = CreateText(infoPanel.transform, "InfoText", "Select a slot to view details", 13, TextAlignmentOptions.Left);
            infoText = infoTextObj.GetComponent<TMP_Text>();
            SetRectFill(infoTextObj);
            infoTextObj.GetComponent<RectTransform>().offsetMin = new Vector2(15, 10);
            infoTextObj.GetComponent<RectTransform>().offsetMax = new Vector2(-15, -10);
            infoText.color = textDimColor;
        }
        
        private void CreateRelicSlot(string label, int index)
        {
            RelicSlotData slotData = new RelicSlotData();
            slotData.slotIndex = index;
            
            GameObject slot = CreatePanel(relicSlotContainer, $"Slot_{label}", slotEmptyColor);
            slotData.root = slot;
            slotData.background = slot.GetComponent<Image>();
            
            LayoutElement le = slot.AddComponent<LayoutElement>();
            le.minWidth = 110; le.preferredWidth = 130;
            le.minHeight = 160; le.preferredHeight = 180;
            
            GameObject outline = CreatePanel(slot.transform, "Outline", accentColor);
            SetRectFill(outline);
            outline.GetComponent<RectTransform>().offsetMin = new Vector2(-3, -3);
            outline.GetComponent<RectTransform>().offsetMax = new Vector2(3, 3);
            outline.GetComponent<Image>().raycastTarget = false;
            outline.SetActive(false);
            slotData.selectionOutline = outline.GetComponent<Image>();
            
            GameObject inner = CreatePanel(slot.transform, "Inner", slotEmptyColor);
            SetRectFill(inner);
            
            Button btn = inner.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = slotEmptyColor;
            colors.highlightedColor = buttonHoverColor;
            btn.colors = colors;
            int capturedIndex = index;
            btn.onClick.AddListener(() => OnSlotClicked(capturedIndex));
            
            GameObject labelObj = CreateText(inner.transform, "Label", label, 16, TextAlignmentOptions.Center);
            RectTransform lbRt = labelObj.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 1); lbRt.anchorMax = new Vector2(1, 1);
            lbRt.pivot = new Vector2(0.5f, 1);
            lbRt.anchoredPosition = new Vector2(0, -8); lbRt.sizeDelta = new Vector2(0, 25);
            slotData.labelText = labelObj.GetComponent<TMP_Text>();
            slotData.labelText.fontStyle = FontStyles.Bold;
            
            GameObject nameObj = CreateText(inner.transform, "Name", "Empty", 11, TextAlignmentOptions.Center);
            RectTransform nmRt = nameObj.GetComponent<RectTransform>();
            nmRt.anchorMin = new Vector2(0, 1); nmRt.anchorMax = new Vector2(1, 1);
            nmRt.pivot = new Vector2(0.5f, 1);
            nmRt.anchoredPosition = new Vector2(0, -35); nmRt.sizeDelta = new Vector2(-8, 20);
            slotData.nameText = nameObj.GetComponent<TMP_Text>();
            slotData.nameText.color = textDimColor;
            
            GameObject effectObj = CreateText(inner.transform, "Effect", "Click to equip", 9, TextAlignmentOptions.Center);
            RectTransform efRt = effectObj.GetComponent<RectTransform>();
            efRt.anchorMin = new Vector2(0, 1); efRt.anchorMax = new Vector2(1, 1);
            efRt.pivot = new Vector2(0.5f, 1);
            efRt.anchoredPosition = new Vector2(0, -55); efRt.sizeDelta = new Vector2(-8, 40);
            slotData.effectText = effectObj.GetComponent<TMP_Text>();
            slotData.effectText.color = textDimColor;
            slotData.effectText.textWrappingMode = TextWrappingModes.Normal;
            slotData.effectText.overflowMode = TextOverflowModes.Ellipsis;
            
            GameObject jewelRow = new GameObject("JewelRow");
            jewelRow.transform.SetParent(inner.transform);
            RectTransform jrRt = jewelRow.AddComponent<RectTransform>();
            jrRt.anchorMin = new Vector2(0, 0); jrRt.anchorMax = new Vector2(1, 0);
            jrRt.pivot = new Vector2(0.5f, 0);
            jrRt.anchoredPosition = new Vector2(0, -25); jrRt.sizeDelta = new Vector2(0, 20);
            
            HorizontalLayoutGroup jhlg = jewelRow.AddComponent<HorizontalLayoutGroup>();
            jhlg.spacing = 8;
            jhlg.childAlignment = TextAnchor.MiddleCenter;
            jhlg.childForceExpandWidth = false; jhlg.childForceExpandHeight = false;
            
            for (int j = 0; j < 3; j++)
            {
                GameObject jewelSlot = CreatePanel(jewelRow.transform, $"Jewel_{j}", jewelEmptyColor);
                LayoutElement jle = jewelSlot.AddComponent<LayoutElement>();
                jle.minWidth = 24; jle.minHeight = 24;
                jle.preferredWidth = 24; jle.preferredHeight = 24;
                slotData.jewelImages[j] = jewelSlot.GetComponent<Image>();
                Button jbtn = jewelSlot.AddComponent<Button>();
                jbtn.transition = Selectable.Transition.ColorTint;
                int slotIdx = index; int jewelIdx = j;
                jbtn.onClick.AddListener(() => OnJewelClicked(slotIdx, jewelIdx));
                slotData.jewelButtons[j] = jbtn;
            }
            
            relicSlots.Add(slotData);
        }
        
        private void CreateRightPanel()
        {
            GameObject rightPanel = CreatePanel(equipmentPanel.transform, "RightPanel", panelColor);
            RectTransform rt = rightPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-340, 70); rt.offsetMax = new Vector2(-20, -70);
            
            GameObject relicTitle = CreateText(rightPanel.transform, "RelicPoolTitle", "RELIC POOL", 18, TextAlignmentOptions.Center);
            RectTransform relicTitleRt = relicTitle.GetComponent<RectTransform>();
            relicTitleRt.anchorMin = new Vector2(0, 1); relicTitleRt.anchorMax = new Vector2(1, 1);
            relicTitleRt.pivot = new Vector2(0.5f, 1);
            relicTitleRt.anchoredPosition = new Vector2(0, -10); relicTitleRt.sizeDelta = new Vector2(0, 28);
            relicPoolTitleText = relicTitle.GetComponent<TMP_Text>();
            relicPoolTitleText.color = accentColor;
            
            GameObject tabContainer = new GameObject("FilterTabs");
            tabContainer.transform.SetParent(rightPanel.transform);
            RectTransform tcRt = tabContainer.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0, 1); tcRt.anchorMax = new Vector2(1, 1);
            tcRt.pivot = new Vector2(0.5f, 1);
            tcRt.anchoredPosition = new Vector2(0, -40); tcRt.sizeDelta = new Vector2(-20, 26);
            
            HorizontalLayoutGroup tabHlg = tabContainer.AddComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 2;
            tabHlg.childAlignment = TextAnchor.MiddleCenter;
            tabHlg.childForceExpandWidth = true; tabHlg.childForceExpandHeight = true;
            filterTabContainer = tabContainer.transform;
            
            CreateFilterTab("Wpn", -1, weaponTabColor);
            CreateFilterTab("Boot", 0, bootsTabColor);
            CreateFilterTab("Glv", 1, glovesTabColor);
            CreateFilterTab("Hat", 2, hatTabColor);
            CreateFilterTab("Coat", 3, coatTabColor);
            CreateFilterTab("Trnk", 4, trinketTabColor);
            CreateFilterTab("Totm", 5, totemTabColor);
            CreateFilterTab("Ult", 6, ultimateTabColor);
            CreateFilterTab("Pas", 7, passiveTabColor);
            
            GameObject relicScrollArea = CreatePanel(rightPanel.transform, "RelicScrollArea", slotEmptyColor);
            RectTransform rsaRt = relicScrollArea.GetComponent<RectTransform>();
            rsaRt.anchorMin = new Vector2(0, 0.42f); rsaRt.anchorMax = new Vector2(1, 1);
            rsaRt.offsetMin = new Vector2(10, 5); rsaRt.offsetMax = new Vector2(-10, -70);
            
            ScrollRect scrollRect = relicScrollArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            
            GameObject viewport = CreatePanel(relicScrollArea.transform, "Viewport", Color.clear);
            SetRectFill(viewport);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            
            GameObject relicContainer = new GameObject("RelicPoolContainer");
            relicContainer.transform.SetParent(viewport.transform);
            RectTransform rcRt = relicContainer.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0, 1); rcRt.anchorMax = new Vector2(1, 1);
            rcRt.pivot = new Vector2(0.5f, 1);
            rcRt.anchoredPosition = Vector2.zero; rcRt.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup rvlg = relicContainer.AddComponent<VerticalLayoutGroup>();
            rvlg.spacing = 8;
            rvlg.childForceExpandWidth = true; rvlg.childForceExpandHeight = false;
            rvlg.childControlWidth = true; rvlg.childControlHeight = false;
            rvlg.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter rcsf = relicContainer.AddComponent<ContentSizeFitter>();
            rcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = rcRt;
            relicPoolContainer = relicContainer.transform;
            
            GameObject jewelTitle = CreateText(rightPanel.transform, "JewelPoolTitle", "JEWEL POOL", 18, TextAlignmentOptions.Center);
            RectTransform jewelTitleRt = jewelTitle.GetComponent<RectTransform>();
            jewelTitleRt.anchorMin = new Vector2(0, 0.42f); jewelTitleRt.anchorMax = new Vector2(1, 0.42f);
            jewelTitleRt.pivot = new Vector2(0.5f, 1);
            jewelTitleRt.anchoredPosition = new Vector2(0, 0); jewelTitleRt.sizeDelta = new Vector2(0, 35);
            jewelTitle.GetComponent<TMP_Text>().color = accentColor;
            
            GameObject jewelPoolArea = CreatePanel(rightPanel.transform, "JewelPoolArea", slotEmptyColor);
            RectTransform jpaRt = jewelPoolArea.GetComponent<RectTransform>();
            jpaRt.anchorMin = new Vector2(0, 0); jpaRt.anchorMax = new Vector2(1, 0.42f);
            jpaRt.offsetMin = new Vector2(10, 10); jpaRt.offsetMax = new Vector2(-10, -40);
            
            GameObject jewelContainer = new GameObject("JewelPoolContainer");
            jewelContainer.transform.SetParent(jewelPoolArea.transform);
            RectTransform jcRt = jewelContainer.AddComponent<RectTransform>();
            SetRectFill(jewelContainer);
            jcRt.offsetMin = new Vector2(8, 8); jcRt.offsetMax = new Vector2(-8, -8);
            
            GridLayoutGroup glg = jewelContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(145, 42); glg.spacing = new Vector2(8, 8);
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;
            jewelPoolContainer = jewelContainer.transform;
        }
        
        private void CreateFilterTab(string label, int filterIndex, Color tabColor)
        {
            GameObject tabObj = CreatePanel(filterTabContainer, $"Tab_{label}", tabColor * 0.4f);
            Button btn = tabObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = tabColor * 0.4f;
            colors.highlightedColor = tabColor * 0.6f;
            colors.selectedColor = tabColor;
            btn.colors = colors;
            int capturedIndex = filterIndex;
            btn.onClick.AddListener(() => OnFilterTabClicked(capturedIndex));
            TMP_Text tabText = CreateText(tabObj.transform, "Text", label, 10, TextAlignmentOptions.Center).GetComponent<TMP_Text>();
            SetRectFill(tabText.gameObject);
            tabText.color = Color.white;
            tabText.fontStyle = FontStyles.Bold;
            filterTabButtons.Add(btn);
        }
        
        private void CreateButtons()
        {
            Button backButton = CreateButton(equipmentPanel.transform, "BackButton", "<- Back", new Vector2(130, 45));
            RectTransform backRt = backButton.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 0); backRt.anchorMax = new Vector2(0, 0);
            backRt.pivot = new Vector2(0, 0); backRt.anchoredPosition = new Vector2(20, 15);
            backButton.onClick.AddListener(() => onBackClicked?.Invoke());
            
            Button unequipButton = CreateButton(equipmentPanel.transform, "UnequipButton", "Unequip All", new Vector2(150, 45));
            RectTransform unequipRt = unequipButton.GetComponent<RectTransform>();
            unequipRt.anchorMin = new Vector2(0.5f, 0); unequipRt.anchorMax = new Vector2(0.5f, 0);
            unequipRt.pivot = new Vector2(0.5f, 0); unequipRt.anchoredPosition = new Vector2(-100, 15);
            unequipButton.onClick.AddListener(OnUnequipAll);
            unequipButton.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f);
            
            Button autoEquipButton = CreateButton(equipmentPanel.transform, "AutoEquipButton", "Auto Equip Enemies", new Vector2(180, 45));
            RectTransform autoEquipRt = autoEquipButton.GetComponent<RectTransform>();
            autoEquipRt.anchorMin = new Vector2(0.5f, 0); autoEquipRt.anchorMax = new Vector2(0.5f, 0);
            autoEquipRt.pivot = new Vector2(0.5f, 0); autoEquipRt.anchoredPosition = new Vector2(100, 15);
            autoEquipButton.onClick.AddListener(OnAutoEquipEnemies);
            autoEquipButton.GetComponent<Image>().color = new Color(0.5f, 0.3f, 0.2f);
            
            Button startButton = CreateButton(equipmentPanel.transform, "StartButton", "Start Battle ->", new Vector2(170, 45));
            RectTransform startRt = startButton.GetComponent<RectTransform>();
            startRt.anchorMin = new Vector2(1, 0); startRt.anchorMax = new Vector2(1, 0);
            startRt.pivot = new Vector2(1, 0); startRt.anchoredPosition = new Vector2(-20, 15);
            startButton.onClick.AddListener(OnStartBattle);
            startButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.2f);
        }
        
        #endregion
        
        #region UI Helpers
        
        private GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;
            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }
        
        private GameObject CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.localScale = Vector3.one;
            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = textColor;
            return textObj;
        }
        
        private Button CreateButton(Transform parent, string name, string text, Vector2 size)
        {
            GameObject btnObj = CreatePanel(parent, name, buttonColor);
            btnObj.GetComponent<RectTransform>().sizeDelta = size;
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonColor;
            btn.colors = colors;
            TMP_Text btnText = CreateText(btnObj.transform, "Text", text, 15, TextAlignmentOptions.Center).GetComponent<TMP_Text>();
            SetRectFill(btnText.gameObject);
            return btn;
        }
        
        private void SetRectFill(GameObject obj)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 2) + "..";
        }
        
        #endregion
        
        #region Population
        
        private void PopulateUnitList()
        {
            foreach (var item in unitListItems) if (item != null) Destroy(item);
            unitListItems.Clear();
            
            for (int i = 0; i < playerUnits.Count; i++)
                CreateUnitListItem(playerUnits[i], i, true);
            
            if (enemyUnits.Count > 0)
            {
                GameObject separator = CreatePanel(unitListContainer, "Separator", new Color(0.3f, 0.15f, 0.15f));
                separator.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
                LayoutElement sepLe = separator.AddComponent<LayoutElement>();
                sepLe.minHeight = 30;
                TMP_Text sepText = CreateText(separator.transform, "SepText", "-- ENEMY --", 12, TextAlignmentOptions.Center).GetComponent<TMP_Text>();
                SetRectFill(sepText.gameObject);
                sepText.color = new Color(1f, 0.5f, 0.5f);
                unitListItems.Add(separator);
            }
            
            for (int i = 0; i < enemyUnits.Count; i++)
                CreateUnitListItem(enemyUnits[i], playerUnits.Count + 1 + i, false);
        }
        
        private void CreateUnitListItem(UnitData unit, int index, bool isPlayer)
        {
            GameObject item = CreatePanel(unitListContainer, $"Unit_{index}", slotEmptyColor);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 68);
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.minHeight = 68; le.preferredHeight = 68;
            
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = slotEmptyColor;
            colors.highlightedColor = buttonHoverColor;
            btn.colors = colors;
            int capturedIndex = index;
            btn.onClick.AddListener(() => SelectUnit(capturedIndex));
            
            TMP_Text nameText = CreateText(item.transform, "Name", unit.unitName, 14, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1); nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0, 1);
            nameRt.anchoredPosition = new Vector2(12, -6); nameRt.sizeDelta = new Vector2(-20, 22);
            nameText.fontStyle = FontStyles.Bold;
            
            TMP_Text roleText = CreateText(item.transform, "Role", unit.GetRoleDisplayName(), 11, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform roleRt = roleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0, 1); roleRt.anchorMax = new Vector2(1, 1);
            roleRt.pivot = new Vector2(0, 1);
            roleRt.anchoredPosition = new Vector2(12, -28); roleRt.sizeDelta = new Vector2(-20, 18);
            roleText.color = textDimColor;
            
            string weaponType = unit.weaponType == WeaponType.Melee ? "[Melee]" : "[Ranged]";
            TMP_Text weaponText = CreateText(item.transform, "Weapon", $"{weaponType} {unit.GetWeaponFamilyDisplayName()}", 11, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform weaponRt = weaponText.GetComponent<RectTransform>();
            weaponRt.anchorMin = new Vector2(0, 1); weaponRt.anchorMax = new Vector2(1, 1);
            weaponRt.pivot = new Vector2(0, 1);
            weaponRt.anchoredPosition = new Vector2(12, -48); weaponRt.sizeDelta = new Vector2(-20, 18);
            weaponText.color = new Color(0.45f, 0.45f, 0.5f);
            
            unitListItems.Add(item);
        }
        
        private void PopulateRelicPool()
        {
            foreach (var item in relicPoolItems) if (item != null) Destroy(item);
            relicPoolItems.Clear();
            
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) { if (relicPoolTitleText != null) relicPoolTitleText.text = "RELIC POOL"; return; }
            
            UpdateFilterTabVisuals();
            
            if (selectedFilterIndex < 0) PopulateWeaponRelicPool(unit);
            else PopulateCategoryRelicPool(unit, filterCategories[selectedFilterIndex]);
        }
        
        private void PopulateWeaponRelicPool(UnitData unit)
        {
            if (relicPoolTitleText != null) relicPoolTitleText.text = $"RELIC POOL - {unit.GetWeaponFamilyDisplayName()}";
            var relics = WeaponRelicGenerator.GenerateRelicPoolForFamily(unit.weaponFamily, new List<WeaponRelic>());
            for (int i = 0; i < relics.Count; i++) CreateWeaponRelicPoolItem(relics[i], i, unit.role);
        }
        
        private void PopulateCategoryRelicPool(UnitData unit, RelicCategory category)
        {
            if (relicPoolTitleText != null) relicPoolTitleText.text = $"RELIC POOL - {category}";
            var relics = categoryRelicPool.Where(r => r.category == category)
                .OrderByDescending(r => r.MatchesRole(unit.role)).ThenBy(r => r.roleTag.ToString()).ToList();
            for (int i = 0; i < relics.Count; i++) CreateCategoryRelicPoolItem(relics[i], i, unit.role);
        }
        
        private void CreateWeaponRelicPoolItem(WeaponRelic relic, int index, UnitRole unitRole)
        {
            GameObject item = CreatePanel(relicPoolContainer, $"WpnRelic_{index}", slotEmptyColor);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.minHeight = 65; le.preferredHeight = 65;
            
            bool isMatch = relic.MatchesRole(unitRole);
            if (isMatch) item.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.18f);
            
            GameObject rarityBar = CreatePanel(item.transform, "RarityBar", GetRarityColor(relic.effectData.rarity));
            RectTransform barRt = rarityBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0); barRt.anchorMax = new Vector2(0, 1);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.offsetMin = new Vector2(0, 4); barRt.offsetMax = new Vector2(6, -4);
            
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = isMatch ? new Color(0.18f, 0.25f, 0.18f) : slotEmptyColor;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.38f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnRelicPoolItemClicked(relic));
            
            string weaponName = relic.baseWeaponData?.weaponName ?? relic.weaponFamily.ToString();
            TMP_Text weaponText = CreateText(item.transform, "Weapon", weaponName, 10, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform weaponRt = weaponText.GetComponent<RectTransform>();
            weaponRt.anchorMin = new Vector2(0, 1); weaponRt.anchorMax = new Vector2(0.6f, 1);
            weaponRt.pivot = new Vector2(0, 1);
            weaponRt.anchoredPosition = new Vector2(14, -4); weaponRt.sizeDelta = new Vector2(0, 16);
            weaponText.color = new Color(0.5f, 0.7f, 1f);
            
            TMP_Text nameText = CreateText(item.transform, "Name", relic.effectData.effectName, 12, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1); nameRt.anchorMax = new Vector2(0.7f, 1);
            nameRt.pivot = new Vector2(0, 1);
            nameRt.anchoredPosition = new Vector2(14, -20); nameRt.sizeDelta = new Vector2(0, 20);
            nameText.fontStyle = FontStyles.Bold;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            TMP_Text roleText = CreateText(item.transform, "Role", relic.roleTag.ToString(), 10, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform roleRt = roleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0, 1); roleRt.anchorMax = new Vector2(0.5f, 1);
            roleRt.pivot = new Vector2(0, 1);
            roleRt.anchoredPosition = new Vector2(14, -40); roleRt.sizeDelta = new Vector2(0, 16);
            roleText.color = isMatch ? accentColor : textDimColor;
            
            TMP_Text rarityText = CreateText(item.transform, "Rarity", relic.effectData.rarity.ToString(), 10, TextAlignmentOptions.Right).GetComponent<TMP_Text>();
            RectTransform rarityRt = rarityText.GetComponent<RectTransform>();
            rarityRt.anchorMin = new Vector2(0.6f, 1); rarityRt.anchorMax = new Vector2(1, 1);
            rarityRt.pivot = new Vector2(1, 1);
            rarityRt.anchoredPosition = new Vector2(-10, -4); rarityRt.sizeDelta = new Vector2(0, 16);
            rarityText.color = GetRarityColor(relic.effectData.rarity);
            
            if (isMatch)
            {
                TMP_Text matchText = CreateText(item.transform, "Match", "MATCH", 10, TextAlignmentOptions.Right).GetComponent<TMP_Text>();
                RectTransform matchRt = matchText.GetComponent<RectTransform>();
                matchRt.anchorMin = new Vector2(0.6f, 1); matchRt.anchorMax = new Vector2(1, 1);
                matchRt.pivot = new Vector2(1, 1);
                matchRt.anchoredPosition = new Vector2(-10, -20); matchRt.sizeDelta = new Vector2(0, 16);
                matchText.color = accentColor; matchText.fontStyle = FontStyles.Bold;
            }
            
            relicPoolItems.Add(item);
        }
        
        private void CreateCategoryRelicPoolItem(EquippedRelic relic, int index, UnitRole unitRole)
        {
            GameObject item = CreatePanel(relicPoolContainer, $"CatRelic_{index}", slotEmptyColor);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.minHeight = 65; le.preferredHeight = 65;
            
            bool isMatch = relic.MatchesRole(unitRole);
            if (isMatch) item.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.18f);
            
            Color catColor = GetCategoryColor(relic.category);
            GameObject catBar = CreatePanel(item.transform, "CategoryBar", catColor);
            RectTransform barRt = catBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0); barRt.anchorMax = new Vector2(0, 1);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.offsetMin = new Vector2(0, 4); barRt.offsetMax = new Vector2(6, -4);
            
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = isMatch ? new Color(0.18f, 0.25f, 0.18f) : slotEmptyColor;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.38f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnCategoryRelicPoolItemClicked(relic));
            
            TMP_Text catText = CreateText(item.transform, "Category", relic.category.ToString(), 10, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform catRt = catText.GetComponent<RectTransform>();
            catRt.anchorMin = new Vector2(0, 1); catRt.anchorMax = new Vector2(0.6f, 1);
            catRt.pivot = new Vector2(0, 1);
            catRt.anchoredPosition = new Vector2(14, -4); catRt.sizeDelta = new Vector2(0, 16);
            catText.color = catColor;
            
            TMP_Text nameText = CreateText(item.transform, "Name", relic.relicName, 12, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1); nameRt.anchorMax = new Vector2(0.7f, 1);
            nameRt.pivot = new Vector2(0, 1);
            nameRt.anchoredPosition = new Vector2(14, -20); nameRt.sizeDelta = new Vector2(0, 20);
            nameText.fontStyle = FontStyles.Bold;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            TMP_Text roleText = CreateText(item.transform, "Role", relic.roleTag.ToString(), 10, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform roleRt = roleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0, 1); roleRt.anchorMax = new Vector2(0.5f, 1);
            roleRt.pivot = new Vector2(0, 1);
            roleRt.anchoredPosition = new Vector2(14, -40); roleRt.sizeDelta = new Vector2(0, 16);
            roleText.color = isMatch ? accentColor : textDimColor;
            
            bool isPassive = relic.IsPassive();
            string infoStr = isPassive ? "Passive" : $"{relic.GetCopies()} copies";
            TMP_Text infoTextLabel = CreateText(item.transform, "Info", infoStr, 10, TextAlignmentOptions.Right).GetComponent<TMP_Text>();
            RectTransform infoRt = infoTextLabel.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0.6f, 1); infoRt.anchorMax = new Vector2(1, 1);
            infoRt.pivot = new Vector2(1, 1);
            infoRt.anchoredPosition = new Vector2(-10, -4); infoRt.sizeDelta = new Vector2(0, 16);
            infoTextLabel.color = isPassive ? trinketTabColor : textDimColor;
            
            if (isMatch)
            {
                TMP_Text matchText = CreateText(item.transform, "Match", "MATCH", 10, TextAlignmentOptions.Right).GetComponent<TMP_Text>();
                RectTransform matchRt = matchText.GetComponent<RectTransform>();
                matchRt.anchorMin = new Vector2(0.6f, 1); matchRt.anchorMax = new Vector2(1, 1);
                matchRt.pivot = new Vector2(1, 1);
                matchRt.anchoredPosition = new Vector2(-10, -20); matchRt.sizeDelta = new Vector2(0, 16);
                matchText.color = accentColor; matchText.fontStyle = FontStyles.Bold;
            }
            
            relicPoolItems.Add(item);
        }
        
        private void PopulateJewelPool()
        {
            foreach (var item in jewelPoolItems) if (item != null) Destroy(item);
            jewelPoolItems.Clear();
            
            string[] jewelNames = { "Ruby", "Sapphire", "Emerald", "Topaz", "Amethyst", "Diamond" };
            Color[] jewelColors = { new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 1f), new Color(0.3f, 1f, 0.3f),
                new Color(1f, 1f, 0.3f), new Color(0.8f, 0.3f, 0.8f), new Color(0.6f, 0.9f, 1f) };
            for (int i = 0; i < jewelNames.Length; i++) CreateJewelPoolItem(jewelNames[i], jewelColors[i], i);
        }
        
        private void CreateJewelPoolItem(string name, Color color, int index)
        {
            GameObject item = CreatePanel(jewelPoolContainer, $"Jewel_{index}", slotEmptyColor);
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.35f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnJewelPoolItemClicked(index));
            
            GameObject icon = CreatePanel(item.transform, "Icon", color);
            RectTransform iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(10, 0); iconRt.sizeDelta = new Vector2(24, 24);
            
            TMP_Text nameText = CreateText(item.transform, "Name", name, 13, TextAlignmentOptions.Left).GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0); nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0, 0.5f);
            nameRt.offsetMin = new Vector2(42, 0); nameRt.offsetMax = new Vector2(-8, 0);
            
            jewelPoolItems.Add(item);
        }
        
        private Color GetRarityColor(RelicRarity rarity) => rarity switch {
            RelicRarity.Common => commonColor, RelicRarity.Uncommon => uncommonColor, RelicRarity.Rare => rareColor, _ => commonColor };
        
        private Color GetCategoryColor(RelicCategory category) => category switch {
            RelicCategory.Boots => bootsTabColor, RelicCategory.Gloves => glovesTabColor, RelicCategory.Hat => hatTabColor,
            RelicCategory.Coat => coatTabColor, RelicCategory.Trinket => trinketTabColor, RelicCategory.Totem => totemTabColor,
            RelicCategory.Ultimate => ultimateTabColor, RelicCategory.PassiveUnique => passiveTabColor, _ => weaponTabColor };
        
        #endregion
        
        #region Selection & Interaction
        
        private void SelectUnit(int index)
        {
            selectedUnitIndex = index; selectedSlotIndex = -1; selectedJewelIndex = -1;
            UnitData unit = GetUnitByIndex(index);
            if (unit != null)
            {
                unitInfoText.text = $"| {unit.GetRoleDisplayName()} | [{unit.weaponType}] {unit.GetWeaponFamilyDisplayName()}";
                RefreshSlots(unit); PopulateRelicPool(); PopulateJewelPool(); UpdateJewelBudget(unit); UpdateInfoPanel();
            }
        }
        
        private void RefreshSlots(UnitData unit)
        {
            for (int i = 0; i < relicSlots.Count; i++)
            {
                RelicSlotData slot = relicSlots[i];
                WeaponRelic weaponRelic = unit.GetWeaponRelic(i);
                EquippedRelic categoryRelic = GetCategoryRelic(unit, i);
                
                slot.selectionOutline.gameObject.SetActive(i == selectedSlotIndex);
                
                if (weaponRelic != null)
                {
                    slot.nameText.text = weaponRelic.effectData.effectName;
                    slot.nameText.color = weaponRelic.MatchesRole(unit.role) ? accentColor : textColor;
                    slot.effectText.text = TruncateText(weaponRelic.effectData.description, 50);
                    slot.effectText.color = textDimColor;
                    slot.background.color = slotFilledColor;
                }
                else if (categoryRelic != null)
                {
                    slot.nameText.text = categoryRelic.relicName;
                    slot.nameText.color = categoryRelic.MatchesRole(unit.role) ? accentColor : textColor;
                    slot.effectText.text = TruncateText(categoryRelic.effectData?.description ?? "", 50);
                    slot.effectText.color = textDimColor;
                    slot.background.color = GetCategoryColor(categoryRelic.category) * 0.4f;
                }
                else
                {
                    slot.nameText.text = "Empty"; slot.nameText.color = textDimColor;
                    slot.effectText.text = "Click to equip"; slot.effectText.color = textDimColor;
                    slot.background.color = slotEmptyColor;
                }
                
                // Jewels - simplified for now
                for (int j = 0; j < 3; j++)
                {
                    slot.jewelImages[j].color = (i == selectedSlotIndex && j == selectedJewelIndex) 
                        ? new Color(0.3f, 0.3f, 0.4f) : jewelEmptyColor;
                }
            }
        }
        
        private UnitData GetUnitByIndex(int index)
        {
            if (index < 0) return null;
            if (index < playerUnits.Count) return playerUnits[index];
            int enemyIndex = index - playerUnits.Count - 1;
            return (enemyIndex >= 0 && enemyIndex < enemyUnits.Count) ? enemyUnits[enemyIndex] : null;
        }
        
        private bool IsSelectedUnitPlayer() => selectedUnitIndex >= 0 && selectedUnitIndex < playerUnits.Count;
        
        private void UpdateJewelBudget(UnitData unit)
        {
            // Simplified - jewels not fully implemented yet
            jewelBudgetText.text = "Jewel Budget: 0 / 0";
        }
        
        private void OnSlotClicked(int slotIndex)
        {
            selectedSlotIndex = (selectedSlotIndex == slotIndex) ? -1 : slotIndex;
            selectedJewelIndex = -1;
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit != null) { RefreshSlots(unit); UpdateInfoPanel(); }
        }
        
        private void OnJewelClicked(int slotIndex, int jewelIndex)
        {
            selectedSlotIndex = slotIndex; selectedJewelIndex = jewelIndex;
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit != null) { RefreshSlots(unit); UpdateInfoPanel(); }
        }
        
        private void OnFilterTabClicked(int filterIndex) { selectedFilterIndex = filterIndex; PopulateRelicPool(); }
        
        private void UpdateFilterTabVisuals()
        {
            Color[] tabColors = { weaponTabColor, bootsTabColor, glovesTabColor, hatTabColor, coatTabColor, trinketTabColor, totemTabColor, ultimateTabColor, passiveTabColor };
            for (int i = 0; i < filterTabButtons.Count && i < tabColors.Length; i++)
            {
                int tabFilterIndex = i - 1;
                bool isSelected = (selectedFilterIndex == tabFilterIndex);
                Image img = filterTabButtons[i].GetComponent<Image>();
                if (img != null) img.color = isSelected ? tabColors[i] : tabColors[i] * 0.4f;
            }
        }
        
        private void OnRelicPoolItemClicked(WeaponRelic relic)
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null || selectedSlotIndex < 0) return;
            
            // Clear category relic first (mutual exclusivity)
            ClearCategoryRelic(unit, selectedSlotIndex);
            
            // Store in UnitData
            unit.EquipWeaponRelic(selectedSlotIndex, relic);
            
            RefreshSlots(unit); UpdateJewelBudget(unit); UpdateInfoPanel();
        }
        
        private void OnCategoryRelicPoolItemClicked(EquippedRelic relic)
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null || selectedSlotIndex < 0) return;
            
            // Clear weapon relic first (mutual exclusivity)
            unit.EquipWeaponRelic(selectedSlotIndex, null);
            
            // Store in UnitData
            SetCategoryRelic(unit, selectedSlotIndex, relic);
            
            RefreshSlots(unit); UpdateJewelBudget(unit); UpdateInfoPanel();
        }
        
        private void OnJewelPoolItemClicked(int poolIndex)
        {
            // Jewel system not fully integrated yet
            Debug.Log("Jewel system coming soon!");
        }
        
        private void UpdateInfoPanel()
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) { infoText.text = "Select a unit"; return; }
            if (selectedSlotIndex < 0) { infoText.text = "Select a slot to view details"; return; }
            
            WeaponRelic weaponRelic = unit.GetWeaponRelic(selectedSlotIndex);
            EquippedRelic categoryRelic = GetCategoryRelic(unit, selectedSlotIndex);
            
            if (weaponRelic != null)
            {
                string matchText = weaponRelic.MatchesRole(unit.role) ? " [ROLE MATCH!]" : "";
                infoText.text = $"<b>{weaponRelic.effectData.effectName}</b>{matchText}\n" +
                    $"Role: {weaponRelic.roleTag}  |  Rarity: {weaponRelic.effectData.rarity}\n{weaponRelic.effectData.description}";
            }
            else if (categoryRelic != null)
            {
                string matchText = categoryRelic.MatchesRole(unit.role) ? " [ROLE MATCH!]" : "";
                string passive = categoryRelic.IsPassive() ? " (Passive)" : "";
                infoText.text = $"<b>{categoryRelic.relicName}</b>{matchText}{passive}\n" +
                    $"Category: {categoryRelic.category}  |  Role: {categoryRelic.roleTag}\n{categoryRelic.effectData?.description ?? ""}";
            }
            else infoText.text = "Empty slot\n\nSelect a slot, then click a relic from the pool.\nUse tabs to filter by Weapon or Category.";
        }
        
        private void OnUnequipAll()
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) return;
            
            // Clear all slots
            unit.ClearAllEquipment();
            
            // Re-equip default weapon relic if exists
            if (unit.defaultWeaponRelic != null)
                unit.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            
            RefreshSlots(unit); UpdateJewelBudget(unit); UpdateInfoPanel();
        }
        
        private void OnAutoEquipEnemies()
        {
            foreach (UnitData enemy in enemyUnits)
            {
                // Initialize arrays if needed
                if (enemy.weaponRelics == null) enemy.weaponRelics = new WeaponRelic[7];
                if (enemy.categoryRelics == null) enemy.categoryRelics = new EquippedRelic[7];
                
                // Equip default weapon
                if (enemy.defaultWeaponRelic != null)
                    enemy.EquipWeaponRelic(0, enemy.defaultWeaponRelic);
                
                // Random additional weapon relics
                for (int slot = 1; slot <= 4; slot++)
                {
                    if (UnityEngine.Random.value > 0.7f) continue;
                    
                    WeaponRelic randomRelic = GenerateRandomRelicForUnit(enemy);
                    if (randomRelic != null)
                        enemy.EquipWeaponRelic(slot, randomRelic);
                }
            }
            
            if (selectedUnitIndex >= 0 && !IsSelectedUnitPlayer())
            {
                UnitData enemy = GetUnitByIndex(selectedUnitIndex);
                if (enemy != null) { RefreshSlots(enemy); UpdateJewelBudget(enemy); }
            }
            Debug.Log($"<color=orange>Auto-equipped {enemyUnits.Count} enemies!</color>");
        }
        
        private WeaponRelic GenerateRandomRelicForUnit(UnitData unit)
        {
            UnitRole[] allRoles = (UnitRole[])System.Enum.GetValues(typeof(UnitRole));
            UnitRole randomRole = allRoles[UnityEngine.Random.Range(0, allRoles.Length)];
            float roll = UnityEngine.Random.value;
            int tier = roll < 0.50f ? 1 : (roll < 0.85f ? 2 : 3);
            return WeaponRelicGenerator.GenerateWeaponRelic(unit.weaponFamily, randomRole, tier);
        }
        
        private void OnStartBattle()
        {
            // Relics are already stored in UnitData, just log and start
            Debug.Log("<color=green>[EquipmentUI] Starting battle with equipped relics!</color>");
            foreach (var unit in playerUnits)
            {
                int weaponCount = unit.GetAllWeaponRelics().Count;
                int categoryCount = unit.GetAllCategoryRelics().Count;
                Debug.Log($"<color=cyan>{unit.unitName}: {weaponCount} weapon relics, {categoryCount} category relics</color>");
            }
            onStartBattle?.Invoke(playerUnits, enemyUnits);
        }
        
        #endregion
    }
}