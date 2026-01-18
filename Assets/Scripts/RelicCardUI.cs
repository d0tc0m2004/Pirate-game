using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Core;
using TacticalGame.Managers;
using TacticalGame.Grid;

namespace TacticalGame.UI
{
    /// <summary>
    /// Manages the relic card hand UI during combat.
    /// When a player unit is selected and X is pressed, 7 relic cards fan out at the bottom.
    /// Cards display the unit's equipped relics (can be WeaponRelics or CategoryRelics).
    /// Clicking a card executes the relic's effect.
    /// </summary>
    public class RelicCardUI : MonoBehaviour
    {
        #region Singleton
        
        private static RelicCardUI _instance;
        public static RelicCardUI Instance => _instance;
        
        #endregion
        
        #region Events
        
        public event Action<int> OnCardPlayed; // Card index (0-6)
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Card Settings")]
        [SerializeField] private float cardWidth = 140f;
        [SerializeField] private float cardHeight = 200f;
        [SerializeField] private float cardSpacing = 10f;
        [SerializeField] private float fanAngle = 3f;
        [SerializeField] private float hoverLift = 40f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float animationSpeed = 8f;
        
        [Header("Position")]
        [SerializeField] private float bottomOffset = 20f;
        
        [Header("Colors")]
        [SerializeField] private Color cardBackgroundColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color cardBorderColor = new Color(0.3f, 0.3f, 0.4f);
        [SerializeField] private Color cardHoverColor = new Color(0.25f, 0.25f, 0.35f);
        [SerializeField] private Color cardDisabledColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        [SerializeField] private Color emptySlotColor = new Color(0.1f, 0.1f, 0.12f);
        [SerializeField] private Color matchColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color commonColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color uncommonColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color rareColor = new Color(0.4f, 0.6f, 1f);
        [SerializeField] private Color uniqueColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] private Color weaponColor = new Color(0.8f, 0.3f, 0.3f); // Red tint for weapons
        
        #endregion
        
        #region Private State
        
        private GameObject canvasRoot;
        private GameObject cardContainer;
        private List<RelicCard> cards = new List<RelicCard>();
        private UnitData currentUnit;
        private GameObject currentUnitObject;
        private bool isVisible = false;
        private int hoveredCardIndex = -1;
        
        // Pending unit (selected but cards not shown yet)
        private GameObject pendingUnit;
        private UnitData pendingUnitData;
        
        // References
        private EnergyManager energyManager;
        private BattleManager battleManager;
        
        #endregion
        
        #region Nested Class - RelicCard
        
        private class RelicCard
        {
            public GameObject root;
            public RectTransform rectTransform;
            public Image background;
            public Image border;
            public Image rarityBar;
            public TMP_Text typeText;       // "Weapon" or category name
            public TMP_Text effectText;     // Effect/weapon name
            public TMP_Text roleText;       // Role tag
            public TMP_Text costText;       // Energy cost
            public TMP_Text matchText;      // Match indicator
            public TMP_Text copiesText;     // Number of copies
            public GameObject disabledOverlay;
            public Button button;
            
            // Can be either type
            public WeaponRelic weaponRelic;
            public EquippedRelic categoryRelic;
            
            public int slotIndex;
            public bool isEmpty;
            public bool isDisabled;
            public bool isPassive;
            public bool isWeapon;           // True if this slot has a weapon relic
            
            // Animation state
            public Vector2 basePosition;
            public float baseRotation;
            public Vector2 targetPosition;
            public float targetRotation;
            public float targetScale;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Start()
        {
            energyManager = ServiceLocator.Get<EnergyManager>();
            battleManager = ServiceLocator.Get<BattleManager>();
            
            CreateUI();
            Hide();
            
            // Subscribe to events
            GameEvents.OnUnitSelected += OnUnitSelected;
            GameEvents.OnUnitDeselected += OnUnitDeselected;
            GameEvents.OnEnergyChanged += OnEnergyChanged;
            GameEvents.OnPlayerTurnEnd += FullHide;
            GameEvents.OnEnemyTurnStart += FullHide;
        }
        
        private void OnDestroy()
        {
            GameEvents.OnUnitSelected -= OnUnitSelected;
            GameEvents.OnUnitDeselected -= OnUnitDeselected;
            GameEvents.OnEnergyChanged -= OnEnergyChanged;
            GameEvents.OnPlayerTurnEnd -= FullHide;
            GameEvents.OnEnemyTurnStart -= FullHide;
            
            if (_instance == this)
                _instance = null;
        }
        
        private void Update()
        {
            // Check for X key to toggle cards when unit is selected
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (pendingUnit != null && pendingUnitData != null)
                {
                    if (isVisible)
                    {
                        Hide();
                    }
                    else
                    {
                        ShowCards();
                    }
                }
            }
            
            // Escape key to hide cards
            if (Input.GetKeyDown(KeyCode.Escape) && isVisible)
            {
                Hide();
            }
            
            if (!isVisible) return;
            
            // Animate cards
            foreach (var card in cards)
            {
                if (card.root == null) continue;
                
                Vector2 currentPos = card.rectTransform.anchoredPosition;
                Vector2 newPos = Vector2.Lerp(currentPos, card.targetPosition, Time.deltaTime * animationSpeed);
                card.rectTransform.anchoredPosition = newPos;
                
                float currentRot = card.rectTransform.localEulerAngles.z;
                if (currentRot > 180) currentRot -= 360;
                float newRot = Mathf.Lerp(currentRot, card.targetRotation, Time.deltaTime * animationSpeed);
                card.rectTransform.localEulerAngles = new Vector3(0, 0, newRot);
                
                float currentScale = card.rectTransform.localScale.x;
                float newScale = Mathf.Lerp(currentScale, card.targetScale, Time.deltaTime * animationSpeed);
                card.rectTransform.localScale = Vector3.one * newScale;
            }
        }
        
        #endregion
        
        #region UI Creation
        
        private void CreateUI()
        {
            // Find or create canvas
            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null)
            {
                canvasRoot = existingCanvas.gameObject;
            }
            else
            {
                canvasRoot = new GameObject("RelicCardCanvas");
                Canvas canvas = canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasRoot.AddComponent<CanvasScaler>();
                canvasRoot.AddComponent<GraphicRaycaster>();
            }
            
            // Create container for cards
            cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(canvasRoot.transform, false);
            
            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, bottomOffset);
            containerRect.sizeDelta = new Vector2(1000, cardHeight + 50);
            
            // Create 7 card slots
            for (int i = 0; i < 7; i++)
            {
                CreateCard(i);
            }
        }
        
        private void CreateCard(int index)
        {
            RelicCard card = new RelicCard();
            card.slotIndex = index;
            
            // Root object
            card.root = new GameObject($"Card_{index}");
            card.root.transform.SetParent(cardContainer.transform, false);
            
            card.rectTransform = card.root.AddComponent<RectTransform>();
            card.rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);
            card.rectTransform.pivot = new Vector2(0.5f, 0);
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(card.root.transform, false);
            card.background = bgObj.AddComponent<Image>();
            card.background.color = cardBackgroundColor;
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(card.root.transform, false);
            card.border = borderObj.AddComponent<Image>();
            card.border.color = cardBorderColor;
            card.border.raycastTarget = false;
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(4, 4);
            borderRect.anchoredPosition = Vector2.zero;
            
            // Rarity bar at top
            GameObject rarityObj = new GameObject("RarityBar");
            rarityObj.transform.SetParent(card.root.transform, false);
            card.rarityBar = rarityObj.AddComponent<Image>();
            card.rarityBar.color = commonColor;
            card.rarityBar.raycastTarget = false;
            RectTransform rarityRect = rarityObj.GetComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0, 1);
            rarityRect.anchorMax = new Vector2(1, 1);
            rarityRect.pivot = new Vector2(0.5f, 1);
            rarityRect.sizeDelta = new Vector2(0, 6);
            rarityRect.anchoredPosition = Vector2.zero;
            
            // Type text (top) - "Weapon" or category like "Boots"
            card.typeText = CreateText(card.root.transform, "TypeText", "", 12, TextAlignmentOptions.Center);
            RectTransform typeRect = card.typeText.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0, 1);
            typeRect.anchorMax = new Vector2(1, 1);
            typeRect.pivot = new Vector2(0.5f, 1);
            typeRect.anchoredPosition = new Vector2(0, -12);
            typeRect.sizeDelta = new Vector2(-10, 20);
            card.typeText.fontStyle = FontStyles.Bold;
            
            // Effect/weapon name (middle)
            card.effectText = CreateText(card.root.transform, "EffectText", "", 11, TextAlignmentOptions.Center);
            RectTransform effectRect = card.effectText.GetComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0, 0.4f);
            effectRect.anchorMax = new Vector2(1, 0.75f);
            effectRect.sizeDelta = new Vector2(-10, 0);
            effectRect.anchoredPosition = Vector2.zero;
            card.effectText.textWrappingMode = TextWrappingModes.Normal;
            card.effectText.overflowMode = TextOverflowModes.Ellipsis;
            
            // Role text (below effect)
            card.roleText = CreateText(card.root.transform, "RoleText", "", 10, TextAlignmentOptions.Center);
            RectTransform roleRect = card.roleText.GetComponent<RectTransform>();
            roleRect.anchorMin = new Vector2(0, 0.25f);
            roleRect.anchorMax = new Vector2(1, 0.4f);
            roleRect.sizeDelta = new Vector2(-10, 0);
            roleRect.anchoredPosition = Vector2.zero;
            card.roleText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Match indicator
            card.matchText = CreateText(card.root.transform, "MatchText", "", 10, TextAlignmentOptions.Center);
            RectTransform matchRect = card.matchText.GetComponent<RectTransform>();
            matchRect.anchorMin = new Vector2(0, 0.1f);
            matchRect.anchorMax = new Vector2(1, 0.25f);
            matchRect.sizeDelta = new Vector2(-10, 0);
            matchRect.anchoredPosition = Vector2.zero;
            card.matchText.color = matchColor;
            card.matchText.fontStyle = FontStyles.Bold;
            
            // Cost text (bottom left)
            card.costText = CreateText(card.root.transform, "CostText", "1", 16, TextAlignmentOptions.Center);
            RectTransform costRect = card.costText.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(0, 0);
            costRect.pivot = new Vector2(0, 0);
            costRect.anchoredPosition = new Vector2(8, 8);
            costRect.sizeDelta = new Vector2(24, 24);
            card.costText.fontStyle = FontStyles.Bold;
            
            // Energy icon background
            GameObject costBg = new GameObject("CostBg");
            costBg.transform.SetParent(card.root.transform, false);
            costBg.transform.SetSiblingIndex(card.costText.transform.GetSiblingIndex());
            Image costBgImg = costBg.AddComponent<Image>();
            costBgImg.color = new Color(0.2f, 0.4f, 0.8f);
            costBgImg.raycastTarget = false;
            RectTransform costBgRect = costBg.GetComponent<RectTransform>();
            costBgRect.anchorMin = new Vector2(0, 0);
            costBgRect.anchorMax = new Vector2(0, 0);
            costBgRect.pivot = new Vector2(0, 0);
            costBgRect.anchoredPosition = new Vector2(5, 5);
            costBgRect.sizeDelta = new Vector2(28, 28);
            
            // Copies text (bottom right)
            card.copiesText = CreateText(card.root.transform, "CopiesText", "", 10, TextAlignmentOptions.Center);
            RectTransform copiesRect = card.copiesText.GetComponent<RectTransform>();
            copiesRect.anchorMin = new Vector2(1, 0);
            copiesRect.anchorMax = new Vector2(1, 0);
            copiesRect.pivot = new Vector2(1, 0);
            copiesRect.anchoredPosition = new Vector2(-8, 8);
            copiesRect.sizeDelta = new Vector2(30, 20);
            card.copiesText.color = new Color(0.6f, 0.6f, 0.6f);
            
            // Disabled overlay
            card.disabledOverlay = new GameObject("DisabledOverlay");
            card.disabledOverlay.transform.SetParent(card.root.transform, false);
            Image overlayImg = card.disabledOverlay.AddComponent<Image>();
            overlayImg.color = cardDisabledColor;
            overlayImg.raycastTarget = false;
            RectTransform overlayRect = card.disabledOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            card.disabledOverlay.SetActive(false);
            
            // Button for interaction
            card.button = card.root.AddComponent<Button>();
            card.button.targetGraphic = card.background;
            
            // Event triggers for hover
            EventTrigger trigger = card.root.AddComponent<EventTrigger>();
            
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            int cardIndex = index;
            enterEntry.callback.AddListener((data) => OnCardHoverEnter(cardIndex));
            trigger.triggers.Add(enterEntry);
            
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnCardHoverExit(cardIndex));
            trigger.triggers.Add(exitEntry);
            
            // Click handler
            card.button.onClick.AddListener(() => OnCardClicked(cardIndex));
            
            cards.Add(card);
        }
        
        private TMP_Text CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            
            return tmp;
        }
        
        #endregion
        
        #region Show/Hide
        
        public void Show(GameObject unitObject, UnitData unit)
        {
            if (unitObject == null) return;
            
            // Check if player unit via UnitStatus component
            UnitStatus status = unitObject.GetComponent<UnitStatus>();
            if (status == null || status.Team != Team.Player) return;
            
            currentUnit = unit;
            currentUnitObject = unitObject;
            isVisible = true;
            cardContainer.SetActive(true);
            
            PopulateCards();
            PositionCards();
            UpdateCardStates();
        }
        
        public void Hide()
        {
            isVisible = false;
            currentUnit = null;
            currentUnitObject = null;
            hoveredCardIndex = -1;
            
            if (cardContainer != null)
                cardContainer.SetActive(false);
        }
        
        private void FullHide()
        {
            ClearPendingUnit();
            Hide();
        }
        
        private void OnUnitSelected(GameObject unit)
        {
            if (unit == null) return;
            
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null || status.Team != Team.Player) 
            {
                ClearPendingUnit();
                Hide();
                return;
            }
            
            UnitData data = GetUnitDataFromObject(unit);
            if (data != null)
            {
                pendingUnit = unit;
                pendingUnitData = data;
                
                if (isVisible && currentUnitObject != unit)
                {
                    Hide();
                }
            }
        }
        
        private void OnUnitDeselected()
        {
            ClearPendingUnit();
            Hide();
        }
        
        private void ClearPendingUnit()
        {
            pendingUnit = null;
            pendingUnitData = null;
        }
        
        private void ShowCards()
        {
            if (pendingUnit == null || pendingUnitData == null) return;
            Show(pendingUnit, pendingUnitData);
        }
        
        private void OnEnergyChanged(int newEnergy)
        {
            if (isVisible)
            {
                UpdateCardStates();
            }
        }
        
        private UnitData GetUnitDataFromObject(GameObject unitObj)
        {
            // We just need basic info - relics will be read directly from UnitEquipmentUpdated
            UnitStatus status = unitObj.GetComponent<UnitStatus>();
            if (status != null)
            {
                UnitData tempData = new UnitData();
                tempData.unitName = status.UnitName;
                // Note: UnitData.role type may vary - this creates minimal data
                // The card UI gets relics directly from UnitEquipmentUpdated component
                return tempData;
            }
            
            return new UnitData();
        }
        
        #endregion
        
        #region Card Population
        
        private void PopulateCards()
        {
            // Get equipment component directly from the unit GameObject
            UnitEquipmentUpdated equipComp = currentUnitObject?.GetComponent<UnitEquipmentUpdated>();
            UnitStatus unitStatus = currentUnitObject?.GetComponent<UnitStatus>();
            UnitRole unitRole = unitStatus?.Role ?? UnitRole.Deckhand;
            
            // Clear all cards first
            for (int i = 0; i < 7; i++)
            {
                cards[i].weaponRelic = null;
                cards[i].categoryRelic = null;
                cards[i].isWeapon = false;
                cards[i].isPassive = false;
                cards[i].isEmpty = true;
            }
            
            if (equipComp == null)
            {
                for (int i = 0; i < 7; i++)
                    PopulateEmptyCard(cards[i], i);
                return;
            }
            
            // Collect ALL equipped relics into a list
            List<WeaponRelic> weaponRelics = equipComp.GetAllWeaponRelics();
            List<EquippedRelic> categoryRelics = equipComp.GetAllEquippedRelics();
            
            // Separate active relics from Ultimate and PassiveUnique
            List<EquippedRelic> activeRelics = new List<EquippedRelic>();
            EquippedRelic ultimate = null;
            EquippedRelic passive = null;
            
            foreach (var relic in categoryRelics)
            {
                if (relic.category == RelicCategory.Ultimate)
                    ultimate = relic;
                else if (relic.category == RelicCategory.PassiveUnique)
                    passive = relic;
                else if (!relic.IsPassive()) // Skip trinket (always active, no card)
                    activeRelics.Add(relic);
            }
            
            // Fill cards: weapons first, then active category relics, then ultimate, then passive
            int cardIndex = 0;
            
            // Add weapon relics
            foreach (var weapon in weaponRelics)
            {
                if (cardIndex >= 5) break; // Leave room for ultimate and passive
                PopulateWeaponCard(cards[cardIndex], weapon, unitRole);
                cardIndex++;
            }
            
            // Add active category relics
            foreach (var relic in activeRelics)
            {
                if (cardIndex >= 5) break;
                PopulateCategoryCard(cards[cardIndex], relic, unitRole);
                cardIndex++;
            }
            
            // Fill remaining slots 0-4 with empty
            while (cardIndex < 5)
            {
                PopulateEmptyCard(cards[cardIndex], cardIndex);
                cardIndex++;
            }
            
            // Slot 5 = Ultimate
            if (ultimate != null)
                PopulateCategoryCard(cards[5], ultimate, unitRole);
            else
                PopulateEmptyCard(cards[5], 5);
            
            // Slot 6 = Passive
            if (passive != null)
                PopulateCategoryCard(cards[6], passive, unitRole);
            else
                PopulateEmptyCard(cards[6], 6);
        }
        
        private void PopulateWeaponCard(RelicCard card, WeaponRelic relic, UnitRole unitRole)
        {
            card.weaponRelic = relic;
            card.isWeapon = true;
            card.isEmpty = false;
            card.isPassive = false;
            
            // Type label
            card.typeText.text = "WEAPON";
            card.typeText.color = weaponColor;
            
            // Weapon/effect name
            string weaponName = relic.baseWeaponData?.weaponName ?? relic.weaponFamily.ToString();
            card.effectText.text = weaponName;
            
            // Role
            card.roleText.text = GetRoleDisplayName(relic.roleTag);
            
            // Match indicator
            bool isMatch = relic.MatchesRole(unitRole);
            card.matchText.text = isMatch ? "★ MATCH" : "";
            card.matchText.gameObject.SetActive(isMatch);
            
            // Rarity color (from effect data if available)
            // Note: effectData is a struct, check if it has valid data
            if (!string.IsNullOrEmpty(relic.effectData.effectName))
            {
                card.rarityBar.color = GetRarityColor(relic.effectData.rarity);
            }
            else
            {
                card.rarityBar.color = commonColor;
            }
            
            // Border color based on match
            card.border.color = isMatch ? matchColor : weaponColor;
            
            // Cost
            int cost = relic.baseWeaponData?.energyCost ?? 1;
            card.costText.text = cost.ToString();
            
            // Copies
            int copies = relic.baseWeaponData?.cardCopies ?? 2;
            card.copiesText.text = $"x{copies}";
            
            // Background with slight red tint for weapons
            card.background.color = new Color(0.18f, 0.12f, 0.12f);
        }
        
        private void PopulateCategoryCard(RelicCard card, EquippedRelic relic, UnitRole unitRole)
        {
            card.categoryRelic = relic;
            card.isWeapon = false;
            card.isEmpty = false;
            card.isPassive = relic.IsPassive();
            
            // Type label (category name)
            card.typeText.text = relic.category.ToString().ToUpper();
            card.typeText.color = Color.white;
            
            // Effect name
            string effectName = "";
            if (relic.effectData != null && !string.IsNullOrEmpty(relic.effectData.effectName))
            {
                effectName = relic.effectData.effectName;
            }
            else if (!string.IsNullOrEmpty(relic.relicName))
            {
                effectName = relic.relicName;
            }
            else
            {
                effectName = $"{relic.roleTag} {relic.category}";
            }
            card.effectText.text = effectName;
            
            // Role
            card.roleText.text = GetRoleDisplayName(relic.roleTag);
            
            // Match indicator
            bool isMatch = relic.MatchesRole(unitRole);
            card.matchText.text = isMatch ? "★ MATCH" : "";
            card.matchText.gameObject.SetActive(isMatch);
            
            // Rarity color
            if (relic.effectData != null)
            {
                card.rarityBar.color = GetRarityColor(relic.effectData.rarity);
            }
            else
            {
                card.rarityBar.color = commonColor;
            }
            
            // Border color based on match
            card.border.color = isMatch ? matchColor : cardBorderColor;
            
            // Cost
            int cost = relic.GetEnergyCost();
            card.costText.text = card.isPassive ? "P" : cost.ToString();
            
            // Copies
            int copies = relic.GetCopies();
            card.copiesText.text = card.isPassive ? "Passive" : $"x{copies}";
            
            // Background - green tint for passive, normal for active
            card.background.color = card.isPassive ? new Color(0.1f, 0.15f, 0.1f) : cardBackgroundColor;
        }
        
        private void PopulateEmptyCard(RelicCard card, int slotIndex)
        {
            card.isEmpty = true;
            card.isWeapon = false;
            card.isPassive = false;
            
            card.typeText.text = GetSlotLabel(slotIndex);
            card.typeText.color = new Color(0.5f, 0.5f, 0.5f);
            card.effectText.text = "Empty Slot";
            card.roleText.text = "";
            card.matchText.text = "";
            card.matchText.gameObject.SetActive(false);
            card.rarityBar.color = emptySlotColor;
            card.border.color = emptySlotColor;
            card.costText.text = "-";
            card.copiesText.text = "";
            card.background.color = emptySlotColor;
        }
        
        private void PositionCards()
        {
            int cardCount = 7;
            float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
            float startX = -totalWidth / 2f;
            
            for (int i = 0; i < cardCount; i++)
            {
                RelicCard card = cards[i];
                
                float x = startX + i * (cardWidth + cardSpacing);
                float y = 0;
                
                float centerIndex = (cardCount - 1) / 2f;
                float offsetFromCenter = i - centerIndex;
                float rotation = -offsetFromCenter * fanAngle;
                float yOffset = -Mathf.Abs(offsetFromCenter) * 5f;
                
                card.basePosition = new Vector2(x, y + yOffset);
                card.baseRotation = rotation;
                card.targetPosition = card.basePosition;
                card.targetRotation = card.baseRotation;
                card.targetScale = 1f;
                
                card.rectTransform.anchoredPosition = card.basePosition;
                card.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
                card.rectTransform.localScale = Vector3.one;
                card.root.transform.SetSiblingIndex(i);
            }
        }
        
        private void UpdateCardStates()
        {
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();
            
            int currentEnergy = energyManager?.CurrentEnergy ?? 0;
            
            bool hasAttacked = false;
            if (currentUnitObject != null)
            {
                UnitMovement movement = currentUnitObject.GetComponent<UnitMovement>();
                if (movement != null)
                    hasAttacked = movement.HasAttacked;
            }
            
            for (int i = 0; i < 7; i++)
            {
                RelicCard card = cards[i];
                
                if (card.isEmpty)
                {
                    card.isDisabled = true;
                }
                else if (card.isPassive)
                {
                    // Passive relics can't be played
                    card.isDisabled = true;
                }
                else
                {
                    int cost = GetCardCost(card);
                    card.isDisabled = (currentEnergy < cost) || hasAttacked;
                }
                
                card.disabledOverlay.SetActive(card.isDisabled);
                card.button.interactable = !card.isDisabled && !card.isEmpty && !card.isPassive;
            }
        }
        
        private int GetCardCost(RelicCard card)
        {
            if (card.isWeapon && card.weaponRelic != null)
            {
                return card.weaponRelic.baseWeaponData?.energyCost ?? 1;
            }
            else if (card.categoryRelic != null)
            {
                return card.categoryRelic.GetEnergyCost();
            }
            return 1;
        }
        
        #endregion
        
        #region Card Interaction
        
        private void OnCardHoverEnter(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            if (card.isEmpty) return;
            
            hoveredCardIndex = index;
            
            card.targetPosition = card.basePosition + new Vector2(0, hoverLift);
            card.targetRotation = 0;
            card.targetScale = hoverScale;
            
            if (!card.isPassive)
            {
                card.background.color = cardHoverColor;
            }
            
            card.root.transform.SetAsLastSibling();
        }
        
        private void OnCardHoverExit(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            hoveredCardIndex = -1;
            
            card.targetPosition = card.basePosition;
            card.targetRotation = card.baseRotation;
            card.targetScale = 1f;
            
            // Reset background color
            if (card.isEmpty)
            {
                card.background.color = emptySlotColor;
            }
            else if (card.isPassive)
            {
                card.background.color = new Color(0.1f, 0.15f, 0.1f);
            }
            else if (card.isWeapon)
            {
                card.background.color = new Color(0.18f, 0.12f, 0.12f);
            }
            else
            {
                card.background.color = cardBackgroundColor;
            }
            
            card.root.transform.SetSiblingIndex(index);
        }
        
        private void OnCardClicked(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            if (card.isEmpty || card.isDisabled || card.isPassive) return;
            
            if (card.isWeapon && card.weaponRelic != null)
            {
                Debug.Log($"<color=yellow>Playing weapon card {index}: {card.weaponRelic.relicName}</color>");
                ExecuteWeaponAttack(index, card.weaponRelic);
            }
            else if (card.categoryRelic != null)
            {
                string effectName = card.categoryRelic.effectData?.effectName ?? card.categoryRelic.relicName;
                Debug.Log($"<color=yellow>Playing category card {index}: {effectName}</color>");
                ExecuteCategoryEffect(index, card.categoryRelic);
            }
            
            OnCardPlayed?.Invoke(index);
        }
        
        private void ExecuteWeaponAttack(int slotIndex, WeaponRelic relic)
        {
            if (currentUnitObject == null) return;
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();
            
            int cost = relic.baseWeaponData?.energyCost ?? 1;
            
            if (!energyManager.TrySpendEnergy(cost))
            {
                Debug.Log("Not enough energy!");
                return;
            }
            
            // Get attack component and execute
            UnitAttack attack = currentUnitObject.GetComponent<UnitAttack>();
            if (attack != null)
            {
                attack.ExecuteCardAttack(relic, energyAlreadySpent: true);
            }
            
            UpdateCardStates();
            StartCoroutine(CardPlayedAnimation(slotIndex));
        }
        
        private void ExecuteCategoryEffect(int slotIndex, EquippedRelic relic)
        {
            if (currentUnitObject == null) return;
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();
            
            int cost = relic.GetEnergyCost();
            
            if (!energyManager.TrySpendEnergy(cost))
            {
                Debug.Log("Not enough energy!");
                return;
            }
            
            // Get caster status
            UnitStatus casterStatus = currentUnitObject.GetComponent<UnitStatus>();
            if (casterStatus == null)
            {
                Debug.LogWarning("No UnitStatus found on caster!");
                return;
            }
            
            // Find a target (closest enemy) for effects that need one
            UnitStatus targetStatus = FindClosestEnemy(casterStatus);
            
            // Execute through RelicEffectExecutor (static class)
            RelicEffectExecutor.Execute(relic, casterStatus, targetStatus, null);
            
            UpdateCardStates();
            StartCoroutine(CardPlayedAnimation(slotIndex));
        }
        
        /// <summary>
        /// Find the closest enemy unit to use as a target.
        /// </summary>
        private UnitStatus FindClosestEnemy(UnitStatus caster)
        {
            if (caster == null) return null;
            
            UnitStatus[] allUnits = FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            UnitStatus closest = null;
            float closestDist = float.MaxValue;
            
            foreach (var unit in allUnits)
            {
                if (unit == null) continue;
                
                // Skip allies and surrendered units
                if (unit.Team == caster.Team) continue;
                if (unit.HasSurrendered) continue;
                
                float dist = Vector3.Distance(caster.transform.position, unit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = unit;
                }
            }
            
            return closest;
        }
        
        private IEnumerator CardPlayedAnimation(int index)
        {
            RelicCard card = cards[index];
            
            Color originalColor = card.background.color;
            card.background.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            card.background.color = originalColor;
            
            UpdateCardStates();
        }
        
        #endregion
        
        #region Helpers
        
        private Color GetRarityColor(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => commonColor,
                RelicRarity.Uncommon => uncommonColor,
                RelicRarity.Rare => rareColor,
                RelicRarity.Unique => uniqueColor,
                _ => commonColor
            };
        }
        
        private string GetRoleDisplayName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                UnitRole.Helmsmaster => "Helmsman",
                _ => role.ToString()
            };
        }
        
        private string GetSlotLabel(int index)
        {
            return index switch
            {
                0 => "R1",
                1 => "R2",
                2 => "R3",
                3 => "R4",
                4 => "R5",
                5 => "ULT",
                6 => "PAS",
                _ => $"Slot {index}"
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Component to store UnitData reference on spawned unit GameObjects.
    /// </summary>
    public class UnitDataHolder : MonoBehaviour
    {
        public UnitData unitData;
    }
}