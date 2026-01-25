using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Grid;
using TMPro;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// Main UI controller for the battle deck system.
    /// Manages deck pile, discard pile, hand display, and card interactions.
    /// </summary>
    public class BattleDeckUI : MonoBehaviour
    {
        #region Singleton
        
        private static BattleDeckUI _instance;
        public static BattleDeckUI Instance => _instance;
        
        #endregion
        
        #region References
        
        [Header("UI Containers")]
        [SerializeField] private Transform handContainer;       // Bottom center - card fan
        [SerializeField] private Transform deckPileContainer;   // Bottom left - deck
        [SerializeField] private Transform discardPileContainer;// Bottom left - discard
        [SerializeField] private Transform passivesButton;      // Button to show passives
        [SerializeField] private GameObject passivesPanel;      // Panel showing passive relics
        
        [Header("Prefabs")]
        [SerializeField] private GameObject cardUIPrefab;
        
        [Header("Deck Pile Display")]
        [SerializeField] private Image deckPileIcon;
        [SerializeField] private TextMeshProUGUI deckCountText;
        
        [Header("Discard Pile Display")]
        [SerializeField] private Image discardPileIcon;
        [SerializeField] private TextMeshProUGUI discardCountText;
        
        [Header("Hand Layout Settings")]
        [SerializeField] private float cardSpacing = 80f;
        [SerializeField] private float fanAngle = 5f;           // Angle between cards
        [SerializeField] private float fanArcHeight = 20f;      // Arc height for fan
        [SerializeField] private float selectedLift = 50f;      // How much selected card lifts
        [SerializeField] private float hoverLift = 30f;         // How much hovered card lifts
        
        [Header("Card Colors")]
        [SerializeField] private Color playableColor = Color.white;
        [SerializeField] private Color unplayableColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color stowedColor = new Color(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0.7f, 1f);
        
        [Header("Targeting")]
        [SerializeField] private GameObject targetingOverlay;
        [SerializeField] private TextMeshProUGUI targetingPrompt;
        
        #endregion
        
        #region State
        
        private List<CardUI> cardUIInstances = new List<CardUI>();
        private CardUI hoveredCard;
        private CardUI selectedCardUI;
        private bool isTargeting = false;
        private BattleCard cardAwaitingTarget;
        
        #endregion
        
        public bool IsTargeting => isTargeting;

        #region Unity Lifecycle
        
        private void Awake()
        {
            _instance = this;
            
            // Auto-generate UI if not assigned
            if (handContainer == null || deckPileContainer == null)
            {
                AutoGenerateUI();
            }
        }
        
        /// <summary>
        /// AUTO-GENERATES DECK UI AT RUNTIME.
        /// 
        /// ============================================
        /// TEMPORARY - REMOVE WHEN ADDING CUSTOM UI
        /// ============================================
        /// 
        /// To replace with your own UI:
        /// 1. Create your own Canvas with hand, deck pile, discard pile
        /// 2. Assign references in inspector
        /// 3. Remove this method call from Awake()
        /// </summary>
        private void AutoGenerateUI()
        {
            Debug.Log("<color=yellow>BattleDeckUI: Auto-generating UI (assign references to disable)</color>");
            
            // Ensure we have a Canvas
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            // === HAND CONTAINER (bottom center) ===
            if (handContainer == null)
            {
                var handGO = new GameObject("HandContainer");
                handGO.transform.SetParent(transform, false);
                var handRT = handGO.AddComponent<RectTransform>();
                handRT.anchorMin = new Vector2(0.5f, 0);
                handRT.anchorMax = new Vector2(0.5f, 0);
                handRT.pivot = new Vector2(0.5f, 0);
                handRT.anchoredPosition = new Vector2(0, 20);
                handRT.sizeDelta = new Vector2(800, 200);
                handContainer = handRT;
            }
            
            // === DECK PILE (bottom left) ===
            if (deckPileContainer == null)
            {
                var deckGO = new GameObject("DeckPile");
                deckGO.transform.SetParent(transform, false);
                var deckRT = deckGO.AddComponent<RectTransform>();
                deckRT.anchorMin = new Vector2(0, 0);
                deckRT.anchorMax = new Vector2(0, 0);
                deckRT.pivot = new Vector2(0, 0);
                deckRT.anchoredPosition = new Vector2(20, 20);
                deckRT.sizeDelta = new Vector2(80, 100);
                
                // Background
                var deckBG = deckGO.AddComponent<Image>();
                deckBG.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
                deckPileIcon = deckBG;
                
                // Label
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(deckGO.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(0, 1);
                labelRT.anchorMax = new Vector2(1, 1);
                labelRT.pivot = new Vector2(0.5f, 1);
                labelRT.anchoredPosition = new Vector2(0, -5);
                labelRT.sizeDelta = new Vector2(0, 20);
                var labelText = labelGO.AddComponent<TextMeshProUGUI>();
                labelText.text = "DECK";
                labelText.fontSize = 12;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Center;
                
                // Count
                var countGO = new GameObject("Count");
                countGO.transform.SetParent(deckGO.transform, false);
                var countRT = countGO.AddComponent<RectTransform>();
                countRT.anchorMin = new Vector2(0, 0);
                countRT.anchorMax = new Vector2(1, 0.7f);
                countRT.offsetMin = Vector2.zero;
                countRT.offsetMax = Vector2.zero;
                deckCountText = countGO.AddComponent<TextMeshProUGUI>();
                deckCountText.text = "0";
                deckCountText.fontSize = 32;
                deckCountText.fontStyle = FontStyles.Bold;
                deckCountText.color = Color.white;
                deckCountText.alignment = TextAlignmentOptions.Center;
                
                // Click handler
                var deckBtn = deckGO.AddComponent<Button>();
                deckBtn.onClick.AddListener(OnDeckPileClicked);
                
                deckPileContainer = deckRT;
            }
            
            // === DISCARD PILE (next to deck) ===
            if (discardPileContainer == null)
            {
                var discardGO = new GameObject("DiscardPile");
                discardGO.transform.SetParent(transform, false);
                var discardRT = discardGO.AddComponent<RectTransform>();
                discardRT.anchorMin = new Vector2(0, 0);
                discardRT.anchorMax = new Vector2(0, 0);
                discardRT.pivot = new Vector2(0, 0);
                discardRT.anchoredPosition = new Vector2(110, 20);
                discardRT.sizeDelta = new Vector2(80, 100);
                
                // Background
                var discardBG = discardGO.AddComponent<Image>();
                discardBG.color = new Color(0.4f, 0.25f, 0.2f, 0.9f);
                discardPileIcon = discardBG;
                
                // Label
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(discardGO.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(0, 1);
                labelRT.anchorMax = new Vector2(1, 1);
                labelRT.pivot = new Vector2(0.5f, 1);
                labelRT.anchoredPosition = new Vector2(0, -5);
                labelRT.sizeDelta = new Vector2(0, 20);
                var labelText = labelGO.AddComponent<TextMeshProUGUI>();
                labelText.text = "DISCARD";
                labelText.fontSize = 10;
                labelText.color = Color.white;
                labelText.alignment = TextAlignmentOptions.Center;
                
                // Count
                var countGO = new GameObject("Count");
                countGO.transform.SetParent(discardGO.transform, false);
                var countRT = countGO.AddComponent<RectTransform>();
                countRT.anchorMin = new Vector2(0, 0);
                countRT.anchorMax = new Vector2(1, 0.7f);
                countRT.offsetMin = Vector2.zero;
                countRT.offsetMax = Vector2.zero;
                discardCountText = countGO.AddComponent<TextMeshProUGUI>();
                discardCountText.text = "0";
                discardCountText.fontSize = 32;
                discardCountText.fontStyle = FontStyles.Bold;
                discardCountText.color = new Color(1f, 0.8f, 0.8f);
                discardCountText.alignment = TextAlignmentOptions.Center;
                
                // Click handler
                var discardBtn = discardGO.AddComponent<Button>();
                discardBtn.onClick.AddListener(OnDiscardPileClicked);
                
                discardPileContainer = discardRT;
            }
            
            // === PASSIVES BUTTON (bottom right of discard) ===
            if (passivesButton == null)
            {
                var btnGO = new GameObject("PassivesButton");
                btnGO.transform.SetParent(transform, false);
                var btnRT = btnGO.AddComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0, 0);
                btnRT.anchorMax = new Vector2(0, 0);
                btnRT.pivot = new Vector2(0, 0);
                btnRT.anchoredPosition = new Vector2(200, 20);
                btnRT.sizeDelta = new Vector2(80, 40);
                
                var btnBG = btnGO.AddComponent<Image>();
                btnBG.color = new Color(0.3f, 0.4f, 0.3f, 0.9f);
                
                var btn = btnGO.AddComponent<Button>();
                btn.onClick.AddListener(TogglePassivesPanel);
                
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(btnGO.transform, false);
                var textRT = textGO.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                var text = textGO.AddComponent<TextMeshProUGUI>();
                text.text = "Passives";
                text.fontSize = 12;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                
                passivesButton = btnRT;
            }
            
            // === PASSIVES PANEL (hidden) ===
            if (passivesPanel == null)
            {
                var panelGO = new GameObject("PassivesPanel");
                panelGO.transform.SetParent(transform, false);
                var panelRT = panelGO.AddComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(0, 0.5f);
                panelRT.anchorMax = new Vector2(0, 0.5f);
                panelRT.pivot = new Vector2(0, 0.5f);
                panelRT.anchoredPosition = new Vector2(20, 0);
                panelRT.sizeDelta = new Vector2(250, 400);
                
                var panelBG = panelGO.AddComponent<Image>();
                panelBG.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
                
                // Add PassiveRelicsPanel component
                var passivePanel = panelGO.AddComponent<PassiveRelicsPanel>();
                
                // Header
                var headerGO = new GameObject("Header");
                headerGO.transform.SetParent(panelGO.transform, false);
                var headerRT = headerGO.AddComponent<RectTransform>();
                headerRT.anchorMin = new Vector2(0, 1);
                headerRT.anchorMax = new Vector2(1, 1);
                headerRT.pivot = new Vector2(0.5f, 1);
                headerRT.anchoredPosition = Vector2.zero;
                headerRT.sizeDelta = new Vector2(0, 40);
                var headerText = headerGO.AddComponent<TextMeshProUGUI>();
                headerText.text = "Passive Relics";
                headerText.fontSize = 16;
                headerText.fontStyle = FontStyles.Bold;
                headerText.color = Color.white;
                headerText.alignment = TextAlignmentOptions.Center;
                
                // Content area with scroll
                var contentGO = new GameObject("Content");
                contentGO.transform.SetParent(panelGO.transform, false);
                var contentRT = contentGO.AddComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0, 0);
                contentRT.anchorMax = new Vector2(1, 1);
                contentRT.offsetMin = new Vector2(10, 10);
                contentRT.offsetMax = new Vector2(-10, -50);
                
                var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 5;
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                
                // Close button
                var closeGO = new GameObject("CloseButton");
                closeGO.transform.SetParent(panelGO.transform, false);
                var closeRT = closeGO.AddComponent<RectTransform>();
                closeRT.anchorMin = new Vector2(1, 1);
                closeRT.anchorMax = new Vector2(1, 1);
                closeRT.pivot = new Vector2(1, 1);
                closeRT.anchoredPosition = new Vector2(-5, -5);
                closeRT.sizeDelta = new Vector2(30, 30);
                
                var closeBG = closeGO.AddComponent<Image>();
                closeBG.color = new Color(0.6f, 0.2f, 0.2f);
                
                var closeBtn = closeGO.AddComponent<Button>();
                closeBtn.onClick.AddListener(() => panelGO.SetActive(false));
                
                var closeTextGO = new GameObject("X");
                closeTextGO.transform.SetParent(closeGO.transform, false);
                var closeTextRT = closeTextGO.AddComponent<RectTransform>();
                closeTextRT.anchorMin = Vector2.zero;
                closeTextRT.anchorMax = Vector2.one;
                closeTextRT.offsetMin = Vector2.zero;
                closeTextRT.offsetMax = Vector2.zero;
                var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
                closeText.text = "X";
                closeText.fontSize = 16;
                closeText.fontStyle = FontStyles.Bold;
                closeText.color = Color.white;
                closeText.alignment = TextAlignmentOptions.Center;
                
                panelGO.SetActive(false);
                passivesPanel = panelGO;
            }
            
            // === TARGETING OVERLAY (hidden) ===
            if (targetingOverlay == null)
            {
                var overlayGO = new GameObject("TargetingOverlay");
                overlayGO.transform.SetParent(transform, false);
                var overlayRT = overlayGO.AddComponent<RectTransform>();
                overlayRT.anchorMin = new Vector2(0.5f, 1);
                overlayRT.anchorMax = new Vector2(0.5f, 1);
                overlayRT.pivot = new Vector2(0.5f, 1);
                overlayRT.anchoredPosition = new Vector2(0, -50);
                overlayRT.sizeDelta = new Vector2(400, 50);
                
                var overlayBG = overlayGO.AddComponent<Image>();
                overlayBG.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
                
                var promptGO = new GameObject("Prompt");
                promptGO.transform.SetParent(overlayGO.transform, false);
                var promptRT = promptGO.AddComponent<RectTransform>();
                promptRT.anchorMin = Vector2.zero;
                promptRT.anchorMax = Vector2.one;
                promptRT.offsetMin = new Vector2(10, 5);
                promptRT.offsetMax = new Vector2(-10, -5);
                targetingPrompt = promptGO.AddComponent<TextMeshProUGUI>();
                targetingPrompt.text = "Select a target...";
                targetingPrompt.fontSize = 16;
                targetingPrompt.color = Color.yellow;
                targetingPrompt.alignment = TextAlignmentOptions.Center;
                
                overlayGO.SetActive(false);
                targetingOverlay = overlayGO;
            }
        }
        
        private void OnEnable()
        {
            BattleDeckManager.OnDeckBuilt += RefreshAll;
            BattleDeckManager.OnHandChanged += OnHandChanged;
            BattleDeckManager.OnCardPlayed += OnCardPlayed;
            BattleDeckManager.OnCardStowed += OnCardStowed;
        }
        
        private void OnDisable()
        {
            BattleDeckManager.OnDeckBuilt -= RefreshAll;
            BattleDeckManager.OnHandChanged -= OnHandChanged;
            BattleDeckManager.OnCardPlayed -= OnCardPlayed;
            BattleDeckManager.OnCardStowed -= OnCardStowed;
        }
        
        private void Update()
        {
            // Cancel targeting with right click or escape
            if (isTargeting && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelTargeting();
            }
        }
        
        #endregion
        
        #region Refresh UI
        
        private void RefreshAll()
        {
            RefreshDeckPile();
            RefreshDiscardPile();
            RefreshHand(BattleDeckManager.Instance.Hand.ToList());
        }
        
        private void OnHandChanged(List<BattleCard> hand)
        {
            RefreshDeckPile();
            RefreshDiscardPile();
            RefreshHand(hand);
        }
        
        private void RefreshDeckPile()
        {
            if (deckCountText != null)
            {
                deckCountText.text = BattleDeckManager.Instance.DeckCount.ToString();
            }
        }
        
        private void RefreshDiscardPile()
        {
            if (discardCountText != null)
            {
                discardCountText.text = BattleDeckManager.Instance.DiscardCount.ToString();
            }
        }
        
        private void RefreshHand(List<BattleCard> hand)
        {
            // Clear old card UIs
            foreach (var cardUI in cardUIInstances)
            {
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
            }
            cardUIInstances.Clear();
            
            if (hand == null || hand.Count == 0) return;
            
            // Create new card UIs
            for (int i = 0; i < hand.Count; i++)
            {
                CreateCardUI(hand[i], i, hand.Count);
            }
            
            UpdateCardVisuals();
        }
        
        private void CreateCardUI(BattleCard card, int index, int totalCards)
        {
            if (handContainer == null) return;
            
            GameObject cardGO;
            CardUI cardUI;
            
            // Use prefab if assigned, otherwise auto-generate
            if (cardUIPrefab != null)
            {
                cardGO = Instantiate(cardUIPrefab, handContainer);
                cardUI = cardGO.GetComponent<CardUI>();
                if (cardUI == null)
                {
                    cardUI = cardGO.AddComponent<CardUI>();
                }
            }
            else
            {
                // AUTO-GENERATE CARD UI
                // To use your own prefab instead:
                // 1. Create card prefab with CardUI component
                // 2. Assign to cardUIPrefab field in inspector
                // 3. Delete CardUIGenerator.cs
                cardGO = CardUIGenerator.CreateCard(card, handContainer);
                cardUI = cardGO.GetComponent<CardUI>();
            }
            
            cardUI.Initialize(card, this);
            cardUIInstances.Add(cardUI);
            
            // Position in fan layout
            PositionCardInFan(cardUI, index, totalCards);
        }
        
        private void PositionCardInFan(CardUI cardUI, int index, int totalCards)
        {
            // Calculate fan position
            float centerOffset = (totalCards - 1) / 2f;
            float xOffset = (index - centerOffset) * cardSpacing;
            
            // Arc effect - cards in middle are higher
            float normalizedPos = (index - centerOffset) / Mathf.Max(1, centerOffset);
            float yOffset = -Mathf.Abs(normalizedPos) * fanArcHeight;
            
            // Rotation - slight angle for each card
            float rotation = -(index - centerOffset) * fanAngle;
            
            var rt = cardUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(xOffset, yOffset);
                rt.localRotation = Quaternion.Euler(0, 0, rotation);
            }
            
            // Set sibling index for proper layering
            cardUI.transform.SetSiblingIndex(index);
        }
        
        #endregion
        
        #region Card Visuals
        
        private void UpdateCardVisuals()
        {
            var manager = BattleDeckManager.Instance;
            
            foreach (var cardUI in cardUIInstances)
            {
                bool isPlayable = manager.IsCardPlayable(cardUI.Card);
                bool isStowed = cardUI.Card.isStowed;
                bool isSelected = cardUI == selectedCardUI;
                bool belongsToSelected = cardUI.Card.BelongsTo(manager.SelectedUnit);
                
                // Determine color
                Color targetColor;
                if (isSelected)
                    targetColor = selectedColor;
                else if (isStowed)
                    targetColor = stowedColor;
                else if (!belongsToSelected)
                    targetColor = unplayableColor;
                else if (isPlayable)
                    targetColor = playableColor;
                else
                    targetColor = unplayableColor;
                
                cardUI.SetColor(targetColor);
                cardUI.SetInteractable(belongsToSelected);
                cardUI.SetStowedIndicator(isStowed);
            }
        }
        
        #endregion
        
        #region Card Interactions
        
        /// <summary>
        /// Called when mouse enters a card.
        /// </summary>
        public void OnCardHoverEnter(CardUI cardUI)
        {
            if (isTargeting) return;
            
            hoveredCard = cardUI;
            
            // Lift card slightly
            var rt = cardUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                var pos = rt.anchoredPosition;
                pos.y += hoverLift;
                rt.anchoredPosition = pos;
            }
            
            // Bring to front
            cardUI.transform.SetAsLastSibling();
            
            // Show tooltip
            cardUI.ShowTooltip();
        }
        
        /// <summary>
        /// Called when mouse exits a card.
        /// </summary>
        public void OnCardHoverExit(CardUI cardUI)
        {
            if (cardUI != hoveredCard) return;
            
            hoveredCard = null;
            
            // Restore position
            int index = cardUIInstances.IndexOf(cardUI);
            if (index >= 0)
            {
                PositionCardInFan(cardUI, index, cardUIInstances.Count);
            }
            
            // Hide tooltip
            cardUI.HideTooltip();
        }
        
        /// <summary>
        /// Called when a card is clicked.
        /// </summary>
        public void OnCardClicked(CardUI cardUI)
        {
            var manager = BattleDeckManager.Instance;
            var card = cardUI.Card;
            
            // Check if card belongs to selected unit
            if (!card.BelongsTo(manager.SelectedUnit))
            {
                Debug.Log($"Select {card.GetOwnerName()} first!");
                return;
            }
            
            // Check if already selected - deselect
            if (selectedCardUI == cardUI)
            {
                DeselectCard();
                return;
            }
            
            // Select this card
            SelectCard(cardUI);
            
            // If card needs target, enter targeting mode
            if (card.RequiresTarget())
            {
                StartTargeting(card);
            }
            else
            {
                // Play immediately
                manager.PlayCard(card);
                DeselectCard();
            }
        }
        
        /// <summary>
        /// Called when right-clicking a card (for stow/discard menu).
        /// </summary>
        public void OnCardRightClicked(CardUI cardUI)
        {
            ShowCardContextMenu(cardUI);
        }
        
        private void SelectCard(CardUI cardUI)
        {
            // Deselect previous
            if (selectedCardUI != null)
            {
                ResetCardPosition(selectedCardUI);
            }
            
            selectedCardUI = cardUI;
            BattleDeckManager.Instance.SelectCard(cardUI.Card);
            
            // Lift selected card
            var rt = cardUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                var pos = rt.anchoredPosition;
                pos.y += selectedLift;
                rt.anchoredPosition = pos;
            }
            
            cardUI.transform.SetAsLastSibling();
            UpdateCardVisuals();
        }
        
        private void DeselectCard()
        {
            if (selectedCardUI != null)
            {
                ResetCardPosition(selectedCardUI);
                selectedCardUI = null;
            }
            
            BattleDeckManager.Instance.DeselectCard();
            UpdateCardVisuals();
        }
        
        private void ResetCardPosition(CardUI cardUI)
        {
            int index = cardUIInstances.IndexOf(cardUI);
            if (index >= 0)
            {
                PositionCardInFan(cardUI, index, cardUIInstances.Count);
            }
        }
        
        #endregion
        
        #region Targeting
        
        private void StartTargeting(BattleCard card)
        {
            isTargeting = true;
            cardAwaitingTarget = card;
            
            if (targetingOverlay != null)
                targetingOverlay.SetActive(true);
            
            if (targetingPrompt != null)
            {
                string prompt = GetTargetingPrompt(card);
                targetingPrompt.text = prompt;
            }
            
            // Enable tile/unit selection based on target type
            EnableTargetSelection(card.GetTargetType());
        }
        
        private void CancelTargeting()
        {
            isTargeting = false;
            cardAwaitingTarget = null;
            
            if (targetingOverlay != null)
                targetingOverlay.SetActive(false);
            
            DeselectCard();
            DisableTargetSelection();
        }
        
        private string GetTargetingPrompt(BattleCard card)
        {
            var targetType = card.GetTargetType();
            switch (targetType)
            {
                case CardTargetType.Tile:
                    return "Select a tile to move to";
                case CardTargetType.Ally:
                    return "Select an ally";
                case CardTargetType.Enemy:
                case CardTargetType.AdjacentEnemy:
                case CardTargetType.RangedEnemy:
                    return "Select an enemy to target";
                case CardTargetType.AnyUnit:
                    return "Select a unit";
                default:
                    return "Select a target";
            }
        }
        
        private void EnableTargetSelection(CardTargetType targetType)
        {
            // This would hook into your existing selection system
            // For now, we'll use a simplified approach
            Debug.Log($"Targeting enabled: {targetType}");
        }
        
        private void DisableTargetSelection()
        {
            Debug.Log("Targeting disabled");
        }
        
        /// <summary>
        /// Called when a target is selected (unit or tile).
        /// </summary>
        public void OnTargetSelected(UnitStatus target = null, GridCell cell = null)
        {
            if (!isTargeting || cardAwaitingTarget == null) return;
            
            var manager = BattleDeckManager.Instance;
            
            // Validate target
            var targetType = cardAwaitingTarget.GetTargetType();
            bool valid = ValidateTarget(targetType, target, cell);
            
            if (valid)
            {
                manager.PlayCard(cardAwaitingTarget, target, cell);
            }
            else
            {
                Debug.Log("Invalid target!");
            }
            
            CancelTargeting();
        }
        
        private bool ValidateTarget(CardTargetType type, UnitStatus target, GridCell cell)
        {
            var owner = cardAwaitingTarget.ownerUnit;
            
            switch (type)
            {
                case CardTargetType.Tile:
                    return cell != null;
                    
                case CardTargetType.Ally:
                    return target != null && target.Team == owner.Team;
                    
                case CardTargetType.Enemy:
                case CardTargetType.AdjacentEnemy:
                case CardTargetType.RangedEnemy:
                    return target != null && target.Team != owner.Team && !target.HasSurrendered;
                    
                case CardTargetType.AnyUnit:
                    return target != null;
                    
                default:
                    return true;
            }
        }
        
        #endregion
        
        #region Context Menu
        
        private void ShowCardContextMenu(CardUI cardUI)
        {
            // Simple context menu for stow/discard
            // In a full implementation, you'd use a proper UI menu
            
            var card = cardUI.Card;
            
            // For now, toggle stow on right-click
            if (card.isStowed)
            {
                // Can't un-stow, but could add that feature
                Debug.Log("Card is stowed");
            }
            else
            {
                // Show menu with options
                Debug.Log("Right-click menu: [S]tow or [D]iscard");
            }
        }
        
        /// <summary>
        /// Stow the currently hovered/selected card.
        /// </summary>
        public void StowCard()
        {
            var cardUI = selectedCardUI ?? hoveredCard;
            if (cardUI == null) return;
            
            BattleDeckManager.Instance.StowCard(cardUI.Card);
        }
        
        /// <summary>
        /// Discard and draw for the currently hovered/selected card.
        /// </summary>
        public void DiscardAndDraw()
        {
            var cardUI = selectedCardUI ?? hoveredCard;
            if (cardUI == null) return;
            
            BattleDeckManager.Instance.DiscardAndDraw(cardUI.Card);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnCardPlayed(BattleCard card)
        {
            // Visual feedback
            Debug.Log($"Card played: {card.GetDisplayName()}");
        }
        
        private void OnCardStowed(BattleCard card)
        {
            UpdateCardVisuals();
        }
        
        #endregion
        
        #region Passives Panel
        
        /// <summary>
        /// Toggle the passive relics panel.
        /// </summary>
        public void TogglePassivesPanel()
        {
            if (passivesPanel != null)
            {
                passivesPanel.SetActive(!passivesPanel.activeSelf);
                
                if (passivesPanel.activeSelf)
                {
                    RefreshPassivesPanel();
                }
            }
        }
        
        private void RefreshPassivesPanel()
        {
            // Populate passives panel with all passive relics
            // This would create UI elements for each passive
            var passives = BattleDeckManager.Instance.PassiveRelics;
            Debug.Log($"Showing {passives.Count} passive relics");
        }
        
        #endregion
        
        #region Deck/Discard Click Handlers
        
        /// <summary>
        /// Called when clicking the deck pile.
        /// </summary>
        public void OnDeckPileClicked()
        {
            // Could show deck contents or just a count
            Debug.Log($"Deck: {BattleDeckManager.Instance.DeckCount} cards");
        }
        
        /// <summary>
        /// Called when clicking the discard pile.
        /// </summary>
        public void OnDiscardPileClicked()
        {
            // Could show discard pile contents
            Debug.Log($"Discard: {BattleDeckManager.Instance.DiscardCount} cards");
        }
        
        #endregion
    }
}