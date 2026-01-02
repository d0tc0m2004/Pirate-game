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

namespace TacticalGame.UI
{
    /// <summary>
    /// Manages the relic card hand UI during combat.
    /// When a player unit is selected, 6 relic cards fan out at the bottom.
    /// Clicking a card executes an attack using that relic.
    /// </summary>
    public class RelicCardUI : MonoBehaviour
    {
        #region Singleton
        
        private static RelicCardUI _instance;
        public static RelicCardUI Instance => _instance;
        
        #endregion
        
        #region Events
        
        public event Action<int> OnCardPlayed; // Card index (0-5)
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Card Settings")]
        [SerializeField] private float cardWidth = 140f;
        [SerializeField] private float cardHeight = 200f;
        [SerializeField] private float cardSpacing = 10f;
        [SerializeField] private float fanAngle = 3f; // Degrees of rotation per card from center
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
            public TMP_Text weaponText;
            public TMP_Text effectText;
            public TMP_Text roleText;
            public TMP_Text costText;
            public TMP_Text matchText;
            public GameObject disabledOverlay;
            public Button button;
            
            public WeaponRelic relic;
            public int slotIndex;
            public bool isEmpty;
            public bool isDisabled;
            
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
                        // If cards are showing, hide them
                        Hide();
                    }
                    else
                    {
                        // Show cards for the selected unit
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
                
                // Smooth position
                Vector2 currentPos = card.rectTransform.anchoredPosition;
                Vector2 newPos = Vector2.Lerp(currentPos, card.targetPosition, Time.deltaTime * animationSpeed);
                card.rectTransform.anchoredPosition = newPos;
                
                // Smooth rotation
                float currentRot = card.rectTransform.localEulerAngles.z;
                if (currentRot > 180) currentRot -= 360;
                float newRot = Mathf.Lerp(currentRot, card.targetRotation, Time.deltaTime * animationSpeed);
                card.rectTransform.localEulerAngles = new Vector3(0, 0, newRot);
                
                // Smooth scale
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
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                canvasRoot = existingCanvas.gameObject;
            }
            else
            {
                canvasRoot = new GameObject("RelicCardCanvas");
                Canvas canvas = canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // On top
                canvasRoot.AddComponent<CanvasScaler>();
                canvasRoot.AddComponent<GraphicRaycaster>();
            }
            
            // Create card container
            cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(canvasRoot.transform, false);
            RectTransform containerRt = cardContainer.AddComponent<RectTransform>();
            
            // Anchor to bottom center
            containerRt.anchorMin = new Vector2(0.5f, 0);
            containerRt.anchorMax = new Vector2(0.5f, 0);
            containerRt.pivot = new Vector2(0.5f, 0);
            containerRt.anchoredPosition = new Vector2(0, bottomOffset);
            containerRt.sizeDelta = new Vector2(1000, cardHeight + 50);
            
            // Create 6 cards
            for (int i = 0; i < 6; i++)
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
            
            // Border (slightly larger background)
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(card.root.transform, false);
            RectTransform borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-3, -3);
            borderRt.offsetMax = new Vector2(3, 3);
            card.border = borderObj.AddComponent<Image>();
            card.border.color = cardBorderColor;
            
            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(card.root.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            card.background = bgObj.AddComponent<Image>();
            card.background.color = cardBackgroundColor;
            
            // Rarity bar (top)
            GameObject rarityObj = new GameObject("RarityBar");
            rarityObj.transform.SetParent(card.root.transform, false);
            RectTransform rarityRt = rarityObj.AddComponent<RectTransform>();
            rarityRt.anchorMin = new Vector2(0, 1);
            rarityRt.anchorMax = new Vector2(1, 1);
            rarityRt.pivot = new Vector2(0.5f, 1);
            rarityRt.anchoredPosition = Vector2.zero;
            rarityRt.sizeDelta = new Vector2(0, 6);
            card.rarityBar = rarityObj.AddComponent<Image>();
            card.rarityBar.color = commonColor;
            
            // Weapon name (top)
            card.weaponText = CreateCardText(card.root.transform, "WeaponText", 
                new Vector2(0, -14), new Vector2(-10, 22), 11, FontStyles.Normal);
            card.weaponText.color = new Color(0.6f, 0.8f, 1f);
            
            // Effect name (center, larger)
            card.effectText = CreateCardText(card.root.transform, "EffectText",
                new Vector2(0, -45), new Vector2(-10, 50), 13, FontStyles.Bold);
            card.effectText.alignment = TextAlignmentOptions.Center;
            
            // Role text (below effect)
            card.roleText = CreateCardText(card.root.transform, "RoleText",
                new Vector2(0, -100), new Vector2(-10, 22), 10, FontStyles.Italic);
            card.roleText.color = new Color(0.7f, 0.7f, 0.7f);
            
            // Match indicator
            card.matchText = CreateCardText(card.root.transform, "MatchText",
                new Vector2(0, -125), new Vector2(-10, 22), 11, FontStyles.Bold);
            card.matchText.color = matchColor;
            card.matchText.text = "";
            
            // Cost (bottom left)
            GameObject costContainer = new GameObject("CostContainer");
            costContainer.transform.SetParent(card.root.transform, false);
            RectTransform costContRt = costContainer.AddComponent<RectTransform>();
            costContRt.anchorMin = new Vector2(0, 0);
            costContRt.anchorMax = new Vector2(0, 0);
            costContRt.pivot = new Vector2(0, 0);
            costContRt.anchoredPosition = new Vector2(8, 8);
            costContRt.sizeDelta = new Vector2(35, 35);
            
            Image costBg = costContainer.AddComponent<Image>();
            costBg.color = new Color(0.2f, 0.3f, 0.5f);
            
            card.costText = CreateCardText(costContainer.transform, "CostText",
                Vector2.zero, Vector2.zero, 16, FontStyles.Bold);
            RectTransform costTextRt = card.costText.GetComponent<RectTransform>();
            costTextRt.anchorMin = Vector2.zero;
            costTextRt.anchorMax = Vector2.one;
            costTextRt.offsetMin = Vector2.zero;
            costTextRt.offsetMax = Vector2.zero;
            card.costText.alignment = TextAlignmentOptions.Center;
            card.costText.text = "1";
            
            // Disabled overlay
            card.disabledOverlay = new GameObject("DisabledOverlay");
            card.disabledOverlay.transform.SetParent(card.root.transform, false);
            RectTransform disabledRt = card.disabledOverlay.AddComponent<RectTransform>();
            disabledRt.anchorMin = Vector2.zero;
            disabledRt.anchorMax = Vector2.one;
            disabledRt.offsetMin = Vector2.zero;
            disabledRt.offsetMax = Vector2.zero;
            Image disabledImg = card.disabledOverlay.AddComponent<Image>();
            disabledImg.color = cardDisabledColor;
            disabledImg.raycastTarget = false;
            card.disabledOverlay.SetActive(false);
            
            // Button
            card.button = card.root.AddComponent<Button>();
            card.button.transition = Selectable.Transition.None;
            
            int capturedIndex = index;
            card.button.onClick.AddListener(() => OnCardClicked(capturedIndex));
            
            // Hover events
            EventTrigger trigger = card.root.AddComponent<EventTrigger>();
            
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnCardHoverEnter(capturedIndex));
            trigger.triggers.Add(enterEntry);
            
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnCardHoverExit(capturedIndex));
            trigger.triggers.Add(exitEntry);
            
            cards.Add(card);
        }
        
        private TMP_Text CreateCardText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, FontStyles style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            
            return text;
        }
        
        #endregion
        
        #region Show/Hide
        
        /// <summary>
        /// Show cards for a unit
        /// </summary>
        public void Show(GameObject unitObject, UnitData unit)
        {
            if (unit == null || unitObject == null) return;
            
            // Only show for player units
            if (unit.team != Team.Player) return;
            
            currentUnit = unit;
            currentUnitObject = unitObject;
            isVisible = true;
            cardContainer.SetActive(true);
            
            // Populate cards from unit's equipment
            PopulateCards();
            
            // Position cards in fan formation
            PositionCards();
            
            // Check energy availability
            UpdateCardStates();
        }
        
        /// <summary>
        /// Hide the card UI
        /// </summary>
        public void Hide()
        {
            isVisible = false;
            currentUnit = null;
            currentUnitObject = null;
            hoveredCardIndex = -1;
            
            if (cardContainer != null)
                cardContainer.SetActive(false);
        }
        
        /// <summary>
        /// Fully clear everything (called on turn end, etc.)
        /// </summary>
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
            
            // Store as pending unit - cards will show when X is pressed
            UnitData data = GetUnitDataFromObject(unit);
            if (data != null)
            {
                pendingUnit = unit;
                pendingUnitData = data;
                
                // If cards were already visible for a different unit, hide them
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
        
        /// <summary>
        /// Actually show the cards for the pending unit
        /// </summary>
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
        
        /// <summary>
        /// Try to get UnitData from a unit GameObject.
        /// This looks for matching unit in the deployment manager's lists.
        /// </summary>
        private UnitData GetUnitDataFromObject(GameObject unitObj)
        {
            // Try to get from a component that stores the reference
            UnitDataHolder holder = unitObj.GetComponent<UnitDataHolder>();
            if (holder != null && holder.unitData != null)
            {
                return holder.unitData;
            }
            
            // Fallback: create temporary data from UnitStatus
            UnitStatus status = unitObj.GetComponent<UnitStatus>();
            if (status != null)
            {
                // Check if unit has UnitAttack with weapon relic
                UnitAttack attack = unitObj.GetComponent<UnitAttack>();
                WeaponRelic defaultRelic = attack?.GetWeaponRelic();
                
                // Create a temporary UnitData to display cards
                UnitData tempData = new UnitData();
                tempData.unitName = status.UnitName;
                tempData.role = status.Role;
                tempData.team = status.Team;
                tempData.weaponType = status.WeaponType;
                tempData.defaultWeaponRelic = defaultRelic;
                
                // Initialize equipment if needed
                if (tempData.equipment == null)
                {
                    tempData.equipment = new UnitEquipmentData();
                    if (defaultRelic != null)
                    {
                        tempData.equipment.EquipWeaponRelic(0, defaultRelic);
                    }
                }
                
                return tempData;
            }
            
            return null;
        }
        
        #endregion
        
        #region Card Population
        
        private void PopulateCards()
        {
            for (int i = 0; i < 6; i++)
            {
                RelicCard card = cards[i];
                WeaponRelic relic = currentUnit.equipment?.GetWeaponRelic(i);
                
                card.relic = relic;
                card.isEmpty = (relic == null);
                
                if (relic != null)
                {
                    // Weapon name
                    card.weaponText.text = relic.baseWeaponData?.weaponName ?? relic.weaponFamily.ToString();
                    
                    // Effect name
                    card.effectText.text = relic.effectData.effectName;
                    
                    // Role
                    card.roleText.text = GetRoleDisplayName(relic.roleTag);
                    
                    // Match indicator
                    bool isMatch = relic.MatchesRole(currentUnit.role);
                    card.matchText.text = isMatch ? "★ MATCH" : "";
                    card.matchText.gameObject.SetActive(isMatch);
                    
                    // Rarity color
                    card.rarityBar.color = GetRarityColor(relic.effectData.rarity);
                    
                    // Border color based on match
                    card.border.color = isMatch ? matchColor : cardBorderColor;
                    
                    // Cost (always 1 for now)
                    int cost = relic.baseWeaponData?.energyCost ?? 1;
                    card.costText.text = cost.ToString();
                    
                    // Background
                    card.background.color = cardBackgroundColor;
                }
                else
                {
                    // Empty slot
                    card.weaponText.text = "";
                    card.effectText.text = "Empty Slot";
                    card.roleText.text = GetSlotLabel(i);
                    card.matchText.text = "";
                    card.matchText.gameObject.SetActive(false);
                    card.rarityBar.color = emptySlotColor;
                    card.border.color = emptySlotColor;
                    card.costText.text = "-";
                    card.background.color = emptySlotColor;
                }
            }
        }
        
        private void PositionCards()
        {
            int cardCount = 6;
            float totalWidth = (cardCount - 1) * (cardWidth + cardSpacing);
            float startX = -totalWidth / 2f;
            
            for (int i = 0; i < cardCount; i++)
            {
                RelicCard card = cards[i];
                
                // Calculate base position
                float x = startX + i * (cardWidth + cardSpacing);
                float y = 0;
                
                // Calculate rotation (fan out from center)
                float centerIndex = (cardCount - 1) / 2f;
                float offsetFromCenter = i - centerIndex;
                float rotation = -offsetFromCenter * fanAngle;
                
                // Slight Y offset for fan effect
                float yOffset = -Mathf.Abs(offsetFromCenter) * 5f;
                
                card.basePosition = new Vector2(x, y + yOffset);
                card.baseRotation = rotation;
                
                // Set initial target
                card.targetPosition = card.basePosition;
                card.targetRotation = card.baseRotation;
                card.targetScale = 1f;
                
                // Set initial position instantly
                card.rectTransform.anchoredPosition = card.basePosition;
                card.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
                card.rectTransform.localScale = Vector3.one;
                
                // Set sibling index for layering (hovered card should be on top)
                card.root.transform.SetSiblingIndex(i);
            }
        }
        
        private void UpdateCardStates()
        {
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();
            
            int currentEnergy = energyManager?.CurrentEnergy ?? 0;
            
            // Check if unit has already attacked
            bool hasAttacked = false;
            if (currentUnitObject != null)
            {
                UnitMovement movement = currentUnitObject.GetComponent<UnitMovement>();
                if (movement != null)
                    hasAttacked = movement.HasAttacked;
            }
            
            for (int i = 0; i < 6; i++)
            {
                RelicCard card = cards[i];
                
                if (card.isEmpty)
                {
                    card.isDisabled = true;
                }
                else
                {
                    int cost = card.relic?.baseWeaponData?.energyCost ?? 1;
                    card.isDisabled = (currentEnergy < cost) || hasAttacked;
                }
                
                card.disabledOverlay.SetActive(card.isDisabled);
                card.button.interactable = !card.isDisabled && !card.isEmpty;
            }
        }
        
        #endregion
        
        #region Card Interaction
        
        private void OnCardHoverEnter(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            if (card.isEmpty || card.isDisabled) return;
            
            hoveredCardIndex = index;
            
            // Lift and scale the hovered card
            card.targetPosition = card.basePosition + new Vector2(0, hoverLift);
            card.targetRotation = 0; // Straighten on hover
            card.targetScale = hoverScale;
            
            // Change background color
            card.background.color = cardHoverColor;
            
            // Bring to front
            card.root.transform.SetAsLastSibling();
        }
        
        private void OnCardHoverExit(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            
            hoveredCardIndex = -1;
            
            // Return to base position
            card.targetPosition = card.basePosition;
            card.targetRotation = card.baseRotation;
            card.targetScale = 1f;
            
            // Reset background color
            card.background.color = card.isEmpty ? emptySlotColor : cardBackgroundColor;
            
            // Reset sibling order
            card.root.transform.SetSiblingIndex(index);
        }
        
        private void OnCardClicked(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            
            RelicCard card = cards[index];
            if (card.isEmpty || card.isDisabled) return;
            if (card.relic == null) return;
            
            Debug.Log($"<color=yellow>Playing card {index}: {card.relic.effectData.effectName}</color>");
            
            // Execute attack with this relic
            ExecuteRelicAttack(index, card.relic);
            
            // Fire event
            OnCardPlayed?.Invoke(index);
        }
        
        private void ExecuteRelicAttack(int slotIndex, WeaponRelic relic)
        {
            if (currentUnitObject == null) return;
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();
            
            // Get cost from relic's weapon data
            int cost = relic.baseWeaponData?.energyCost ?? 1;
            
            // Spend energy
            if (!energyManager.TrySpendEnergy(cost))
            {
                Debug.Log("Not enough energy!");
                return;
            }
            
            // Get attack component
            UnitAttack attack = currentUnitObject.GetComponent<UnitAttack>();
            if (attack == null)
            {
                Debug.LogError("No UnitAttack component on selected unit!");
                return;
            }
            
            // Use the new ExecuteCardAttack method - energy already spent above
            attack.ExecuteCardAttack(relic, energyAlreadySpent: true);
            
            // Update card states after attack
            UpdateCardStates();
            
            // Play card animation
            StartCoroutine(CardPlayedAnimation(slotIndex));
        }
        
        private IEnumerator CardPlayedAnimation(int index)
        {
            RelicCard card = cards[index];
            
            // Quick flash
            Color originalColor = card.background.color;
            card.background.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            card.background.color = originalColor;
            
            // Update states
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
                _ => commonColor
            };
        }
        
        private string GetRoleDisplayName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
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
                4 => "ULT",
                5 => "PAS",
                _ => $"Slot {index}"
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Component to store UnitData reference on spawned unit GameObjects.
    /// Attach this when spawning units so we can retrieve the data later.
    /// </summary>
    public class UnitDataHolder : MonoBehaviour
    {
        public UnitData unitData;
    }
}