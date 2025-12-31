using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Equipment UI that builds itself entirely through code.
    /// Just attach this to an empty GameObject and it creates everything!
    /// 
    /// FIXES:
    /// - Relic pool items now 75px height (was 52px) with proper spacing
    /// - Added jewel slots (3 sockets) to each relic pool item
    /// - Proper ScrollRect for relic pool
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
        private Transform relicPoolContainer;
        private Transform jewelPoolContainer;
        private TMP_Text jewelBudgetText;
        private TMP_Text relicPoolTitleText; // Shows "RELIC POOL - [WeaponName]"
        private List<GameObject> relicPoolItems = new List<GameObject>();
        private List<GameObject> jewelPoolItems = new List<GameObject>();
        
        #endregion
        
        #region State
        
        private List<UnitData> playerUnits = new List<UnitData>();
        private List<UnitData> enemyUnits = new List<UnitData>();
        private int selectedUnitIndex = -1;
        private int selectedSlotIndex = -1;
        private int selectedJewelIndex = -1;
        
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
        
        #region Public Methods
        
        public void Open(List<UnitData> players, List<UnitData> enemies)
        {
            playerUnits = players;
            enemyUnits = enemies;
            
            foreach (var unit in playerUnits)
            {
                if (unit.equipment == null)
                    unit.equipment = new UnitEquipmentData();
                if (unit.defaultWeaponRelic != null && unit.equipment.IsSlotEmpty(0))
                    unit.equipment.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
            
            foreach (var unit in enemyUnits)
            {
                if (unit.equipment == null)
                    unit.equipment = new UnitEquipmentData();
                if (unit.defaultWeaponRelic != null && unit.equipment.IsSlotEmpty(0))
                    unit.equipment.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            }
            
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
            vlg.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            unitListContainer = container.transform;
            
            GameObject budgetPanel = CreatePanel(leftPanel.transform, "BudgetPanel", slotEmptyColor);
            RectTransform budgetRt = budgetPanel.GetComponent<RectTransform>();
            budgetRt.anchorMin = new Vector2(0, 0);
            budgetRt.anchorMax = new Vector2(1, 0);
            budgetRt.pivot = new Vector2(0.5f, 0);
            budgetRt.anchoredPosition = new Vector2(0, 10);
            budgetRt.sizeDelta = new Vector2(-20, 50);
            
            jewelBudgetText = CreateText(budgetPanel.transform, "BudgetText", "Jewel Budget: 0 / 0", 14, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            SetRectFill(jewelBudgetText.gameObject);
        }
        
        private void CreateCenterPanel()
        {
            GameObject centerPanel = CreatePanel(equipmentPanel.transform, "CenterPanel", panelColor);
            RectTransform rt = centerPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(290, 70);
            rt.offsetMax = new Vector2(-360, -70);
            
            GameObject infoArea = CreatePanel(centerPanel.transform, "UnitInfo", slotEmptyColor);
            RectTransform infoRt = infoArea.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0, 1);
            infoRt.anchorMax = new Vector2(1, 1);
            infoRt.pivot = new Vector2(0.5f, 1);
            infoRt.anchoredPosition = new Vector2(0, -10);
            infoRt.sizeDelta = new Vector2(-20, 55);
            
            unitInfoText = CreateText(infoArea.transform, "UnitInfoText", "Select a unit", 16, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            SetRectFill(unitInfoText.gameObject);
            
            GameObject slotsArea = new GameObject("RelicSlotsContainer");
            slotsArea.transform.SetParent(centerPanel.transform);
            RectTransform slotsRt = slotsArea.AddComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0, 0.5f);
            slotsRt.anchorMax = new Vector2(1, 0.5f);
            slotsRt.pivot = new Vector2(0.5f, 0.5f);
            slotsRt.anchoredPosition = new Vector2(0, 20);
            slotsRt.sizeDelta = new Vector2(-20, 200);
            
            HorizontalLayoutGroup hlg = slotsArea.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            
            relicSlotContainer = slotsArea.transform;
            
            string[] slotLabels = { "R1", "R2", "R3", "R4", "ULT", "PAS" };
            for (int i = 0; i < 6; i++)
            {
                CreateRelicSlot(slotsArea.transform, slotLabels[i], i);
            }
            
            GameObject infoPanel = CreatePanel(centerPanel.transform, "InfoPanel", slotEmptyColor);
            RectTransform infoPanelRt = infoPanel.GetComponent<RectTransform>();
            infoPanelRt.anchorMin = new Vector2(0, 0);
            infoPanelRt.anchorMax = new Vector2(1, 0);
            infoPanelRt.pivot = new Vector2(0.5f, 0);
            infoPanelRt.anchoredPosition = new Vector2(0, 10);
            infoPanelRt.sizeDelta = new Vector2(-20, 100);
            
            infoText = CreateText(infoPanel.transform, "InfoText", "Select a slot to view details", 13, TextAlignmentOptions.TopLeft)
                .GetComponent<TMP_Text>();
            RectTransform infoTextRt = infoText.GetComponent<RectTransform>();
            SetRectFill(infoText.gameObject);
            infoTextRt.offsetMin = new Vector2(15, 10);
            infoTextRt.offsetMax = new Vector2(-15, -10);
        }
        
        private void CreateRelicSlot(Transform parent, string label, int index)
        {
            RelicSlotData slotData = new RelicSlotData();
            slotData.slotIndex = index;
            
            GameObject slot = CreatePanel(parent, $"Slot_{label}", slotEmptyColor);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130, 180);
            
            Button slotButton = slot.AddComponent<Button>();
            slotButton.transition = Selectable.Transition.None;
            int capturedIndex = index;
            slotButton.onClick.AddListener(() => OnSlotClicked(capturedIndex));
            
            slotData.root = slot;
            slotData.background = slot.GetComponent<Image>();
            
            GameObject outline = CreatePanel(slot.transform, "SelectionOutline", accentColor);
            RectTransform outlineRt = outline.GetComponent<RectTransform>();
            SetRectFill(outline);
            outlineRt.offsetMin = new Vector2(-3, -3);
            outlineRt.offsetMax = new Vector2(3, 3);
            outline.GetComponent<Image>().raycastTarget = false;
            outline.SetActive(false);
            slotData.selectionOutline = outline.GetComponent<Image>();
            
            slotData.labelText = CreateText(slot.transform, "Label", label, 16, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            RectTransform labelRt = slotData.labelText.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 1);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.pivot = new Vector2(0.5f, 1);
            labelRt.anchoredPosition = new Vector2(0, -8);
            labelRt.sizeDelta = new Vector2(0, 28);
            slotData.labelText.fontStyle = FontStyles.Bold;
            
            slotData.nameText = CreateText(slot.transform, "Name", "Empty", 12, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            RectTransform nameRt = slotData.nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0.5f, 1);
            nameRt.anchoredPosition = new Vector2(0, -80);
            nameRt.sizeDelta = new Vector2(-10, 24);
            
            slotData.effectText = CreateText(slot.transform, "Effect", "Click to equip", 10, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            RectTransform effectRt = slotData.effectText.GetComponent<RectTransform>();
            effectRt.anchorMin = new Vector2(0, 1);
            effectRt.anchorMax = new Vector2(1, 1);
            effectRt.pivot = new Vector2(0.5f, 1);
            effectRt.anchoredPosition = new Vector2(0, -105);
            effectRt.sizeDelta = new Vector2(-10, 22);
            slotData.effectText.color = textDimColor;
            
            GameObject jewelCont = new GameObject("JewelContainer");
            jewelCont.transform.SetParent(slot.transform);
            RectTransform jcRt = jewelCont.AddComponent<RectTransform>();
            jcRt.anchorMin = new Vector2(0.5f, 0);
            jcRt.anchorMax = new Vector2(0.5f, 0);
            jcRt.pivot = new Vector2(0.5f, 0);
            jcRt.anchoredPosition = new Vector2(0, 15);
            jcRt.sizeDelta = new Vector2(110, 35);
            
            HorizontalLayoutGroup jhlg = jewelCont.AddComponent<HorizontalLayoutGroup>();
            jhlg.spacing = 8;
            jhlg.childAlignment = TextAnchor.MiddleCenter;
            jhlg.childForceExpandWidth = false;
            jhlg.childForceExpandHeight = false;
            jhlg.childControlWidth = false;
            jhlg.childControlHeight = false;
            
            // Brighter socket color for visibility
            Color socketColor = new Color(0.25f, 0.25f, 0.3f);
            
            for (int j = 0; j < 3; j++)
            {
                GameObject jewel = new GameObject($"Jewel{j}");
                jewel.transform.SetParent(jewelCont.transform);
                
                RectTransform jrt = jewel.AddComponent<RectTransform>();
                jrt.sizeDelta = new Vector2(28, 28);
                
                Image jewelImg = jewel.AddComponent<Image>();
                jewelImg.color = socketColor;
                
                // Add LayoutElement to enforce size
                LayoutElement jle = jewel.AddComponent<LayoutElement>();
                jle.minWidth = 28;
                jle.minHeight = 28;
                jle.preferredWidth = 28;
                jle.preferredHeight = 28;
                
                Button jBtn = jewel.AddComponent<Button>();
                jBtn.transition = Selectable.Transition.ColorTint;
                ColorBlock jColors = jBtn.colors;
                jColors.normalColor = socketColor;
                jColors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
                jBtn.colors = jColors;
                
                int capturedJ = j;
                int capturedSlot = index;
                jBtn.onClick.AddListener(() => OnJewelClicked(capturedSlot, capturedJ));
                
                slotData.jewelImages[j] = jewelImg;
                slotData.jewelButtons[j] = jBtn;
            }
            
            relicSlots.Add(slotData);
        }
        
        private void CreateRightPanel()
        {
            GameObject rightPanel = CreatePanel(equipmentPanel.transform, "RightPanel", panelColor);
            RectTransform rt = rightPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.offsetMin = new Vector2(-340, 70);
            rt.offsetMax = new Vector2(-20, -70);
            
            // === RELIC POOL SECTION (Top 60%) ===
            GameObject relicTitle = CreateText(rightPanel.transform, "RelicPoolTitle", "RELIC POOL", 18, TextAlignmentOptions.Center);
            RectTransform relicTitleRt = relicTitle.GetComponent<RectTransform>();
            relicTitleRt.anchorMin = new Vector2(0, 1);
            relicTitleRt.anchorMax = new Vector2(1, 1);
            relicTitleRt.pivot = new Vector2(0.5f, 1);
            relicTitleRt.anchoredPosition = new Vector2(0, -10);
            relicTitleRt.sizeDelta = new Vector2(0, 35);
            relicPoolTitleText = relicTitle.GetComponent<TMP_Text>();
            relicPoolTitleText.color = accentColor;
            
            // Relic pool scroll area
            GameObject relicScrollArea = CreatePanel(rightPanel.transform, "RelicScrollArea", slotEmptyColor);
            RectTransform rsaRt = relicScrollArea.GetComponent<RectTransform>();
            rsaRt.anchorMin = new Vector2(0, 0.42f);
            rsaRt.anchorMax = new Vector2(1, 1);
            rsaRt.offsetMin = new Vector2(10, 5);
            rsaRt.offsetMax = new Vector2(-10, -50);
            
            // ScrollRect for scrolling
            ScrollRect scrollRect = relicScrollArea.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            
            // Viewport with mask
            GameObject viewport = CreatePanel(relicScrollArea.transform, "Viewport", Color.clear);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            SetRectFill(viewport);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            scrollRect.viewport = vpRt;
            
            // Content container
            GameObject relicContainer = new GameObject("RelicPoolContainer");
            relicContainer.transform.SetParent(viewport.transform);
            RectTransform rcRt = relicContainer.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0, 1);
            rcRt.anchorMax = new Vector2(1, 1);
            rcRt.pivot = new Vector2(0.5f, 1);
            rcRt.anchoredPosition = Vector2.zero;
            rcRt.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup rvlg = relicContainer.AddComponent<VerticalLayoutGroup>();
            rvlg.spacing = 8;
            rvlg.childForceExpandWidth = true;
            rvlg.childForceExpandHeight = false;
            rvlg.childControlWidth = true;
            rvlg.childControlHeight = false;
            rvlg.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter rcsf = relicContainer.AddComponent<ContentSizeFitter>();
            rcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = rcRt;
            relicPoolContainer = relicContainer.transform;
            
            // === JEWEL POOL SECTION (Bottom 40%) ===
            GameObject jewelTitle = CreateText(rightPanel.transform, "JewelPoolTitle", "JEWEL POOL", 18, TextAlignmentOptions.Center);
            RectTransform jewelTitleRt = jewelTitle.GetComponent<RectTransform>();
            jewelTitleRt.anchorMin = new Vector2(0, 0.42f);
            jewelTitleRt.anchorMax = new Vector2(1, 0.42f);
            jewelTitleRt.pivot = new Vector2(0.5f, 1);
            jewelTitleRt.anchoredPosition = new Vector2(0, 0);
            jewelTitleRt.sizeDelta = new Vector2(0, 35);
            jewelTitle.GetComponent<TMP_Text>().color = accentColor;
            
            GameObject jewelPoolArea = CreatePanel(rightPanel.transform, "JewelPoolArea", slotEmptyColor);
            RectTransform jpaRt = jewelPoolArea.GetComponent<RectTransform>();
            jpaRt.anchorMin = new Vector2(0, 0);
            jpaRt.anchorMax = new Vector2(1, 0.42f);
            jpaRt.offsetMin = new Vector2(10, 10);
            jpaRt.offsetMax = new Vector2(-10, -40);
            
            GameObject jewelContainer = new GameObject("JewelPoolContainer");
            jewelContainer.transform.SetParent(jewelPoolArea.transform);
            RectTransform jcRt = jewelContainer.AddComponent<RectTransform>();
            SetRectFill(jewelContainer);
            jcRt.offsetMin = new Vector2(8, 8);
            jcRt.offsetMax = new Vector2(-8, -8);
            
            GridLayoutGroup glg = jewelContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(145, 42);
            glg.spacing = new Vector2(8, 8);
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;
            
            jewelPoolContainer = jewelContainer.transform;
        }
        
        private void CreateButtons()
        {
            Button backButton = CreateButton(equipmentPanel.transform, "BackButton", "<- Back", new Vector2(130, 45));
            RectTransform backRt = backButton.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 0);
            backRt.anchorMax = new Vector2(0, 0);
            backRt.pivot = new Vector2(0, 0);
            backRt.anchoredPosition = new Vector2(20, 15);
            backButton.onClick.AddListener(() => onBackClicked?.Invoke());
            
            Button unequipButton = CreateButton(equipmentPanel.transform, "UnequipButton", "Unequip All", new Vector2(150, 45));
            RectTransform unequipRt = unequipButton.GetComponent<RectTransform>();
            unequipRt.anchorMin = new Vector2(0.5f, 0);
            unequipRt.anchorMax = new Vector2(0.5f, 0);
            unequipRt.pivot = new Vector2(0.5f, 0);
            unequipRt.anchoredPosition = new Vector2(0, 15);
            unequipButton.onClick.AddListener(OnUnequipAll);
            unequipButton.GetComponent<Image>().color = new Color(0.5f, 0.2f, 0.2f);
            
            Button startButton = CreateButton(equipmentPanel.transform, "StartButton", "Start Battle ->", new Vector2(170, 45));
            RectTransform startRt = startButton.GetComponent<RectTransform>();
            startRt.anchorMin = new Vector2(1, 0);
            startRt.anchorMax = new Vector2(1, 0);
            startRt.pivot = new Vector2(1, 0);
            startRt.anchoredPosition = new Vector2(-20, 15);
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
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonColor;
            btn.colors = colors;
            TMP_Text btnText = CreateText(btnObj.transform, "Text", text, 15, TextAlignmentOptions.Center)
                .GetComponent<TMP_Text>();
            SetRectFill(btnText.gameObject);
            return btn;
        }
        
        private void SetRectFill(GameObject obj)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 2) + "..";
        }
        
        #endregion
        
        #region Population
        
        private void PopulateUnitList()
        {
            foreach (var item in unitListItems)
                if (item != null) Destroy(item);
            unitListItems.Clear();
            
            for (int i = 0; i < playerUnits.Count; i++)
            {
                CreateUnitListItem(playerUnits[i], i, true);
            }
            
            if (enemyUnits.Count > 0)
            {
                GameObject separator = CreatePanel(unitListContainer, "Separator", new Color(0.3f, 0.15f, 0.15f));
                RectTransform sepRt = separator.GetComponent<RectTransform>();
                sepRt.sizeDelta = new Vector2(0, 30);
                TMP_Text sepText = CreateText(separator.transform, "SepText", "-- ENEMY --", 12, TextAlignmentOptions.Center)
                    .GetComponent<TMP_Text>();
                SetRectFill(sepText.gameObject);
                sepText.color = new Color(1f, 0.5f, 0.5f);
                unitListItems.Add(separator);
            }
            
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                // +1 to account for separator
                CreateUnitListItem(enemyUnits[i], playerUnits.Count + 1 + i, false);
            }
        }
        
        private void CreateUnitListItem(UnitData unit, int index, bool isPlayer)
        {
            GameObject item = CreatePanel(unitListContainer, $"Unit_{index}", slotEmptyColor);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 68);
            
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = slotEmptyColor;
            colors.highlightedColor = buttonHoverColor;
            colors.selectedColor = new Color(0.3f, 0.4f, 0.5f);
            btn.colors = colors;
            
            int capturedIndex = index;
            btn.onClick.AddListener(() => SelectUnit(capturedIndex));
            
            TMP_Text nameText = CreateText(item.transform, "Name", unit.unitName, 14, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0, 1);
            nameRt.anchoredPosition = new Vector2(12, -6);
            nameRt.sizeDelta = new Vector2(-20, 22);
            nameText.fontStyle = FontStyles.Bold;
            
            TMP_Text roleText = CreateText(item.transform, "Role", unit.GetRoleDisplayName(), 11, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform roleRt = roleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0, 1);
            roleRt.anchorMax = new Vector2(1, 1);
            roleRt.pivot = new Vector2(0, 1);
            roleRt.anchoredPosition = new Vector2(12, -28);
            roleRt.sizeDelta = new Vector2(-20, 18);
            roleText.color = textDimColor;
            
            string weaponType = unit.weaponType == WeaponType.Melee ? "[Melee]" : "[Ranged]";
            TMP_Text weaponText = CreateText(item.transform, "Weapon", $"{weaponType} {unit.GetWeaponFamilyDisplayName()}", 11, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform weaponRt = weaponText.GetComponent<RectTransform>();
            weaponRt.anchorMin = new Vector2(0, 1);
            weaponRt.anchorMax = new Vector2(1, 1);
            weaponRt.pivot = new Vector2(0, 1);
            weaponRt.anchoredPosition = new Vector2(12, -48);
            weaponRt.sizeDelta = new Vector2(-20, 18);
            weaponText.color = new Color(0.45f, 0.45f, 0.5f);
            
            unitListItems.Add(item);
        }
        
        private void PopulateRelicPool()
        {
            foreach (var item in relicPoolItems)
                if (item != null) Destroy(item);
            relicPoolItems.Clear();
            
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null)
            {
                if (relicPoolTitleText != null)
                    relicPoolTitleText.text = "RELIC POOL";
                return;
            }
            
            // Update title to show current weapon family
            string weaponFamilyName = unit.GetWeaponFamilyDisplayName();
            if (relicPoolTitleText != null)
            {
                relicPoolTitleText.text = $"RELIC POOL - {weaponFamilyName}";
            }
            
            Debug.Log($"<color=cyan>[EquipmentUI] PopulateRelicPool for {unit.unitName}, WeaponFamily: {unit.weaponFamily}</color>");
            
            List<WeaponRelic> availableRelics = WeaponRelicGenerator.GenerateRelicPoolForFamily(
                unit.weaponFamily, 
                new List<WeaponRelic>()
            );
            
            Debug.Log($"<color=cyan>[EquipmentUI] Generated {availableRelics.Count} relics for {unit.weaponFamily}</color>");
            
            if (availableRelics.Count > 0)
            {
                Debug.Log($"<color=cyan>[EquipmentUI] First relic: {availableRelics[0].relicName}, Family: {availableRelics[0].weaponFamily}</color>");
            }
            
            for (int i = 0; i < availableRelics.Count; i++)
            {
                CreateRelicPoolItem(availableRelics[i], i, unit.role);
            }
        }
        
        private void CreateRelicPoolItem(WeaponRelic relic, int index, UnitRole unitRole)
        {
            // 65px height for weapon + effect + role (no sockets)
            GameObject item = CreatePanel(relicPoolContainer, $"Relic_{index}", slotEmptyColor);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 65);
            
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.minHeight = 65;
            le.preferredHeight = 65;
            
            bool isMatch = relic.MatchesRole(unitRole);
            
            Image bg = item.GetComponent<Image>();
            if (isMatch)
                bg.color = new Color(0.18f, 0.25f, 0.18f);
            
            // Left rarity bar
            GameObject rarityBar = CreatePanel(item.transform, "RarityBar", GetRarityColor(relic.effectData.rarity));
            RectTransform barRt = rarityBar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(0, 1);
            barRt.pivot = new Vector2(0, 0.5f);
            barRt.offsetMin = new Vector2(0, 4);
            barRt.offsetMax = new Vector2(6, -4);
            
            Button btn = item.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = btn.colors;
            colors.normalColor = isMatch ? new Color(0.18f, 0.25f, 0.18f) : slotEmptyColor;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.38f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnRelicPoolItemClicked(relic));
            
            // Weapon name (top line) - Shows which weapon this relic is for
            string weaponName = relic.baseWeaponData != null ? relic.baseWeaponData.weaponName : relic.weaponFamily.ToString();
            TMP_Text weaponText = CreateText(item.transform, "Weapon", weaponName, 10, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform weaponRt = weaponText.GetComponent<RectTransform>();
            weaponRt.anchorMin = new Vector2(0, 1);
            weaponRt.anchorMax = new Vector2(0.6f, 1);
            weaponRt.pivot = new Vector2(0, 1);
            weaponRt.anchoredPosition = new Vector2(14, -4);
            weaponRt.sizeDelta = new Vector2(0, 16);
            weaponText.color = new Color(0.5f, 0.7f, 1f); // Light blue for weapon name
            
            // Effect name (second line)
            TMP_Text nameText = CreateText(item.transform, "Name", relic.effectData.effectName, 12, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 1);
            nameRt.anchorMax = new Vector2(0.7f, 1);
            nameRt.pivot = new Vector2(0, 1);
            nameRt.anchoredPosition = new Vector2(14, -20);
            nameRt.sizeDelta = new Vector2(0, 20);
            nameText.fontStyle = FontStyles.Bold;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            // Role name (third line)
            TMP_Text roleText = CreateText(item.transform, "Role", relic.roleTag.ToString(), 10, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform roleRt = roleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0, 1);
            roleRt.anchorMax = new Vector2(0.5f, 1);
            roleRt.pivot = new Vector2(0, 1);
            roleRt.anchoredPosition = new Vector2(14, -40);
            roleRt.sizeDelta = new Vector2(0, 16);
            roleText.color = isMatch ? accentColor : textDimColor;
            
            // Rarity text (top right)
            TMP_Text rarityText = CreateText(item.transform, "Rarity", relic.effectData.rarity.ToString(), 10, TextAlignmentOptions.Right)
                .GetComponent<TMP_Text>();
            RectTransform rarityRt = rarityText.GetComponent<RectTransform>();
            rarityRt.anchorMin = new Vector2(0.6f, 1);
            rarityRt.anchorMax = new Vector2(1, 1);
            rarityRt.pivot = new Vector2(1, 1);
            rarityRt.anchoredPosition = new Vector2(-10, -4);
            rarityRt.sizeDelta = new Vector2(0, 16);
            rarityText.color = GetRarityColor(relic.effectData.rarity);
            
            // Match indicator (right side, below rarity)
            if (isMatch)
            {
                TMP_Text matchText = CreateText(item.transform, "Match", "MATCH", 10, TextAlignmentOptions.Right)
                    .GetComponent<TMP_Text>();
                RectTransform matchRt = matchText.GetComponent<RectTransform>();
                matchRt.anchorMin = new Vector2(0.6f, 1);
                matchRt.anchorMax = new Vector2(1, 1);
                matchRt.pivot = new Vector2(1, 1);
                matchRt.anchoredPosition = new Vector2(-10, -20);
                matchRt.sizeDelta = new Vector2(0, 16);
                matchText.color = accentColor;
                matchText.fontStyle = FontStyles.Bold;
            }
            
            relicPoolItems.Add(item);
        }
        
        private void PopulateJewelPool()
        {
            foreach (var item in jewelPoolItems)
                if (item != null) Destroy(item);
            jewelPoolItems.Clear();
            
            string[] jewelNames = { "Ruby", "Sapphire", "Emerald", "Topaz", "Amethyst", "Diamond" };
            Color[] jewelColors = {
                new Color(1f, 0.3f, 0.3f),
                new Color(0.3f, 0.5f, 1f),
                new Color(0.3f, 1f, 0.3f),
                new Color(1f, 1f, 0.3f),
                new Color(0.8f, 0.3f, 0.8f),
                new Color(0.6f, 0.9f, 1f)
            };
            
            for (int i = 0; i < jewelNames.Length; i++)
            {
                CreateJewelPoolItem(jewelNames[i], jewelColors[i], i);
            }
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
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(10, 0);
            iconRt.sizeDelta = new Vector2(24, 24);
            
            TMP_Text nameText = CreateText(item.transform, "Name", name, 13, TextAlignmentOptions.Left)
                .GetComponent<TMP_Text>();
            RectTransform nameRt = nameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0, 0.5f);
            nameRt.offsetMin = new Vector2(42, 0);
            nameRt.offsetMax = new Vector2(-8, 0);
            
            jewelPoolItems.Add(item);
        }
        
        private Color GetRarityColor(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => commonColor,
                RelicRarity.Uncommon => uncommonColor,
                RelicRarity.Rare => rareColor,
                _ => commonColor
            };
        }
        
        #endregion
        
        #region Selection & Interaction
        
        private void SelectUnit(int index)
        {
            selectedUnitIndex = index;
            selectedSlotIndex = -1;
            selectedJewelIndex = -1;
            
            UnitData unit = GetUnitByIndex(index);
            if (unit == null) return;
            
            string weaponType = unit.weaponType == WeaponType.Melee ? "[Melee]" : "[Ranged]";
            unitInfoText.text = $"{unit.unitName}  |  {unit.GetRoleDisplayName()}  |  {weaponType} {unit.GetWeaponFamilyDisplayName()}";
            
            RefreshSlots(unit);
            UpdateJewelBudget(unit);
            
            // Both player AND enemy units can equip relics/jewels
            PopulateRelicPool();
            PopulateJewelPool();
            
            // Update selection highlight in unit list
            for (int i = 0; i < unitListItems.Count; i++)
            {
                Image bg = unitListItems[i].GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = (i == index) ? new Color(0.25f, 0.35f, 0.45f) : slotEmptyColor;
                }
            }
        }
        
        /// <summary>
        /// Get unit by combined index (players first, then separator, then enemies)
        /// Index layout: [Player0, Player1, ..., Separator, Enemy0, Enemy1, ...]
        /// </summary>
        private UnitData GetUnitByIndex(int index)
        {
            if (index < 0) return null;
            
            // Player units are at indices 0 to playerUnits.Count-1
            if (index < playerUnits.Count)
                return playerUnits[index];
            
            // Separator is at index playerUnits.Count (skip it)
            // Enemy units start at index playerUnits.Count + 1
            int enemyIndex = index - playerUnits.Count - 1; // -1 for separator
            
            if (enemyIndex >= 0 && enemyIndex < enemyUnits.Count)
                return enemyUnits[enemyIndex];
            
            return null;
        }
        
        /// <summary>
        /// Check if the selected unit is a player unit
        /// </summary>
        private bool IsSelectedUnitPlayer()
        {
            return selectedUnitIndex >= 0 && selectedUnitIndex < playerUnits.Count;
        }
        
        private void RefreshSlots(UnitData unit)
        {
            for (int i = 0; i < 6; i++)
            {
                RefreshSlot(i, unit);
            }
        }
        
        private void RefreshSlot(int slotIndex, UnitData unit)
        {
            RelicSlotData slot = relicSlots[slotIndex];
            WeaponRelic relic = unit.equipment?.GetWeaponRelic(slotIndex);
            
            if (relic != null)
            {
                slot.nameText.text = TruncateText(relic.effectData.effectName, 14);
                slot.effectText.text = relic.roleTag.ToString();
                slot.nameText.color = GetRarityColor(relic.effectData.rarity);
                slot.background.color = slotFilledColor;
            }
            else
            {
                slot.nameText.text = "Empty";
                slot.effectText.text = "Click to equip";
                slot.nameText.color = textColor;
                slot.background.color = slotEmptyColor;
            }
            
            slot.selectionOutline.gameObject.SetActive(slotIndex == selectedSlotIndex);
            
            for (int j = 0; j < 3; j++)
            {
                JewelData jewel = unit.equipment?.GetJewel(slotIndex, j);
                slot.jewelImages[j].color = jewel != null ? jewel.jewelColor : jewelEmptyColor;
            }
        }
        
        private void UpdateJewelBudget(UnitData unit)
        {
            if (unit.equipment != null)
            {
                int used = unit.equipment.GetTotalEquippedJewelCount();
                int budget = unit.equipment.GetJewelBudget(unit.role);
                jewelBudgetText.text = $"Jewel Budget: {used} / {budget}";
                jewelBudgetText.color = used > budget ? new Color(1f, 0.4f, 0.4f) : textColor;
            }
            else
            {
                jewelBudgetText.text = "Jewel Budget: 0 / 0";
            }
        }
        
        private void OnSlotClicked(int slotIndex)
        {
            if (selectedSlotIndex == slotIndex)
            {
                selectedSlotIndex = -1;
                selectedJewelIndex = -1;
            }
            else
            {
                selectedSlotIndex = slotIndex;
                selectedJewelIndex = -1;
            }
            
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit != null)
            {
                RefreshSlots(unit);
                UpdateInfoPanel();
            }
        }
        
        private void OnJewelClicked(int slotIndex, int jewelIndex)
        {
            selectedSlotIndex = slotIndex;
            selectedJewelIndex = jewelIndex;
            
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit != null)
            {
                RefreshSlots(unit);
                UpdateInfoPanel();
            }
        }
        
        private void OnRelicPoolItemClicked(WeaponRelic relic)
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) return;
            if (selectedSlotIndex < 0) return;
            
            unit.equipment.EquipWeaponRelic(selectedSlotIndex, relic);
            
            RefreshSlots(unit);
            UpdateJewelBudget(unit);
            UpdateInfoPanel();
        }
        
        private void OnJewelPoolItemClicked(int poolIndex)
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) return;
            if (selectedSlotIndex < 0 || selectedJewelIndex < 0) return;
            
            int used = unit.equipment.GetTotalEquippedJewelCount();
            int budget = unit.equipment.GetJewelBudget(unit.role);
            
            if (used >= budget)
            {
                Debug.Log("Jewel budget exceeded!");
                return;
            }
            
            JewelData jewel = ScriptableObject.CreateInstance<JewelData>();
            string[] names = { "Ruby", "Sapphire", "Emerald", "Topaz", "Amethyst", "Diamond" };
            Color[] colors = {
                new Color(1f, 0.3f, 0.3f),
                new Color(0.3f, 0.5f, 1f),
                new Color(0.3f, 1f, 0.3f),
                new Color(1f, 1f, 0.3f),
                new Color(0.8f, 0.3f, 0.8f),
                new Color(0.6f, 0.9f, 1f)
            };
            jewel.jewelName = names[poolIndex];
            jewel.jewelColor = colors[poolIndex];
            
            unit.equipment.EquipJewel(selectedSlotIndex, selectedJewelIndex, jewel);
            
            RefreshSlots(unit);
            UpdateJewelBudget(unit);
        }
        
        private void UpdateInfoPanel()
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null)
            {
                infoText.text = "Select a unit";
                return;
            }
            
            if (selectedSlotIndex < 0)
            {
                infoText.text = "Select a slot to view details";
                return;
            }
            
            WeaponRelic relic = unit.equipment?.GetWeaponRelic(selectedSlotIndex);
            
            if (relic != null)
            {
                string matchText = relic.MatchesRole(unit.role) ? " [ROLE MATCH!]" : "";
                infoText.text = $"<b>{relic.effectData.effectName}</b>{matchText}\n" +
                               $"Role: {relic.roleTag}  |  Rarity: {relic.effectData.rarity}\n" +
                               $"{relic.effectData.description}";
            }
            else
            {
                infoText.text = "Empty slot\n\nSelect a slot, then click a relic from the pool to equip it.";
            }
        }
        
        private void OnUnequipAll()
        {
            UnitData unit = GetUnitByIndex(selectedUnitIndex);
            if (unit == null) return;
            
            unit.equipment?.UnequipAll();
            
            if (unit.defaultWeaponRelic != null)
                unit.equipment.EquipWeaponRelic(0, unit.defaultWeaponRelic);
            
            RefreshSlots(unit);
            UpdateJewelBudget(unit);
            UpdateInfoPanel();
        }
        
        private void OnStartBattle()
        {
            onStartBattle?.Invoke(playerUnits, enemyUnits);
        }
        
        #endregion
    }
}