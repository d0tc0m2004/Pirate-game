using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TacticalGame.Units;
using TacticalGame.Grid;
using TacticalGame.Core;
using TacticalGame.Combat;
using TacticalGame.Managers;
using TacticalGame.Enums;
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
        // Dim — but stay readable. The card stack's CanvasGroup alpha ALSO drops
        // to 0.9 for non-interactable cards, so don't push this too low or text
        // becomes unreadable on top of the tint.
        [SerializeField] private Color unplayableColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color stowedColor = new Color(0.7f, 0.9f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 0.7f, 1f);
        
        [Header("Card Hover Unit Highlight")]
        [SerializeField] private Color cardOwnerHighlightColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private float ownerPulseSpeed = 3f;

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

        // Multi-step movement
        private int remainingMoveSteps = 0;
        private List<GridCell> highlightedMoveCells = new List<GridCell>();
        private Dictionary<GridCell, Color> originalCellColors = new Dictionary<GridCell, Color>();

        // Card hover unit highlighting
        private UnitStatus hoveredCardOwner;
        private MeshRenderer hoveredOwnerRenderer;
        private Color hoveredOwnerOriginalColor;
        private bool isOwnerHighlighted = false;

        // Enemy targeting highlight
        private UnitStatus hoveredEnemyTarget;
        private MeshRenderer hoveredEnemyRenderer;
        private Color hoveredEnemyOriginalColor;
        
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

            // Hide UI initially - will show when battle starts (OnDeckBuilt)
            HideUI();
        }

        /// <summary>
        /// Hide all deck UI elements. Called initially and when leaving battle.
        /// </summary>
        public void HideUI()
        {
            if (handContainer != null) handContainer.gameObject.SetActive(false);
            if (deckPileContainer != null) deckPileContainer.gameObject.SetActive(false);
            if (discardPileContainer != null) discardPileContainer.gameObject.SetActive(false);
            if (passivesButton != null) passivesButton.gameObject.SetActive(false);
            if (targetingOverlay != null) targetingOverlay.SetActive(false);
        }

        /// <summary>
        /// Show all deck UI elements. Called when battle starts.
        /// </summary>
        public void ShowUI()
        {
            if (handContainer != null) handContainer.gameObject.SetActive(true);
            if (deckPileContainer != null) deckPileContainer.gameObject.SetActive(true);
            if (discardPileContainer != null) discardPileContainer.gameObject.SetActive(true);
            if (passivesButton != null) passivesButton.gameObject.SetActive(true);
            // Note: targetingOverlay stays hidden until targeting mode
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

                // Create all children FIRST before adding PassiveRelicsPanel component
                // (so Awake can find them)

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
                contentRT.offsetMin = new Vector2(10, 50);
                contentRT.offsetMax = new Vector2(-10, -10);

                var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.childControlHeight = false;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.padding = new RectOffset(5, 5, 5, 5);

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

                // Add PassiveRelicsPanel component LAST (after all children exist)
                panelGO.AddComponent<PassiveRelicsPanel>();

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
            // Refresh card visuals when energy changes — this is what flips cards
            // to/from the red "can't afford" state in real time, without waiting
            // for the player to click anything.
            GameEvents.OnEnergyChanged += OnEnergyChanged;
        }

        private void OnDisable()
        {
            BattleDeckManager.OnDeckBuilt -= RefreshAll;
            BattleDeckManager.OnHandChanged -= OnHandChanged;
            BattleDeckManager.OnCardPlayed -= OnCardPlayed;
            BattleDeckManager.OnCardStowed -= OnCardStowed;
            GameEvents.OnEnergyChanged -= OnEnergyChanged;
        }

        private void OnEnergyChanged(int newValue)
        {
            UpdateCardVisuals();
        }
        
        private void Update()
        {
            // Cancel targeting with right click or escape
            if (isTargeting && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelTargeting();
            }

            // Pulse the hovered card's owner unit and targeted enemy
            UpdateCardOwnerPulse();
            UpdateEnemyTargetPulse();
        }
        
        #endregion
        
        #region Refresh UI
        
        private void RefreshAll()
        {
            // Show UI when deck is built (battle starts)
            ShowUI();

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
            // Clear stale hover/selection/highlight state from previous turn
            // (cards are being destroyed, so we can't rely on OnPointerExit firing)
            hoveredCard = null;
            selectedCardUI = null;
            ClearCardOwnerHighlight();
            ClearTileHighlights();
            if (isTargeting) CancelTargeting();

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
                bool belongsToSelected = cardUI.Card.BelongsTo(manager.SelectedUnit);
                bool isStowed = cardUI.Card.isStowed;
                bool isSelected = cardUI == selectedCardUI;

                // Always show the real playability state for EVERY card, not just
                // ones owned by the currently selected unit. We pass the card's
                // own owner as the "selected unit" so the "Not your card" guard
                // doesn't trip — the player shouldn't have to click a unit just
                // to find out which of its cards are red.
                CardPlayabilityChecker.Result playability =
                    CardPlayabilityChecker.Check(cardUI.Card, cardUI.Card.ownerUnit);

                bool isPlayable = playability.isPlayable;

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
                // SetPlayability must run AFTER SetColor — it overrides the border
                // with red when unplayable, tints the energy cost text red when
                // the player can't afford the cost, and applies the blue highlight border.
                cardUI.SetPlayability(playability, belongsToSelected);
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

            // Show stow and discard buttons if card belongs to selected unit
            if (cardUI.Card.BelongsTo(BattleDeckManager.Instance.SelectedUnit))
            {
                cardUI.ShowStowButton(true);
                cardUI.ShowDiscardButton(true);
            }

            // Highlight the card's owner unit
            HighlightCardOwner(cardUI.Card.ownerUnit);

            // Show what cells the card would hit. Auto-target cards (CardTargetType.None)
            // produce an empty preview, so this is a no-op for them.
            PreviewCardTargets(cardUI.Card);
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

            // Hide stow and discard buttons
            cardUI.ShowStowButton(false);
            cardUI.ShowDiscardButton(false);

            // Clear owner unit highlight
            ClearCardOwnerHighlight();

            // Clear the target preview — but ONLY if we haven't already committed
            // to playing this card (in which case targeting mode owns the highlights
            // and they'll be cleared when the card resolves or is cancelled).
            if (!isTargeting)
            {
                ClearTileHighlights();
            }
        }

        /// <summary>
        /// Paint the cells a card would target as a hover-time preview.
        /// Reuses the same highlight system as targeting mode, so when the player
        /// clicks the card the highlights are already in place — no flicker.
        /// </summary>
        private void PreviewCardTargets(BattleCard card)
        {
            if (card == null) return;

            // Start fresh — clears any leftover preview from a previously hovered card.
            ClearTileHighlights();

            var targetType = card.GetTargetType();

            // Special case for Coat AOE feedback (Highlight caster location and radius 1)
            if (card.category == RelicCategory.Coat && card.ownerUnit != null)
            {
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager != null)
                {
                    Vector2Int pos = gridManager.WorldToGridPosition(card.ownerUnit.transform.position);
                    var centerCell = gridManager.GetCell(pos.x, pos.y);
                    if (centerCell != null)
                    {
                        Color aoeTint = new Color(1f, 1f, 0f, 0.4f); // Transparent yellow

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                var cell = gridManager.GetCell(pos.x + dx, pos.y + dy);
                                if (cell != null) PaintCell(cell, aoeTint);
                            }
                        }
                    }
                }
                return;
            }
            
            // If the card strictly auto-targets (Weapons and Gloves)
            if (targetType == CardTargetType.None)
            {
                // Weapons and Gloves dynamically seek out the nearest enemy and fire
                if ((card.IsWeaponCard || card.category == RelicCategory.Gloves) && card.ownerUnit != null)
                {
                    UnitStatus closest = TacticalGame.Combat.TargetFinder.FindNearestEnemy(card.ownerUnit);
                    
                    if (closest != null)
                    {
                        var gridManager = ServiceLocator.Get<GridManager>();
                        if (gridManager != null)
                        {
                            var pos = gridManager.WorldToGridPosition(closest.transform.position);
                            var cell = gridManager.GetCell(pos.x, pos.y);
                            Debug.Log($"[PreviewCardTargets] Nearest Enemy is {closest.UnitName} via TargetFinder!");

                            if (cell != null)
                            {
                                // Attach material highlighting to the actual unit
                                hoveredEnemyTarget = closest;
                                hoveredEnemyRenderer = closest.GetComponent<MeshRenderer>();
                                if (hoveredEnemyRenderer != null)
                                {
                                    hoveredEnemyOriginalColor = hoveredEnemyRenderer.material.color;
                                    Debug.Log($"[PreviewCardTargets] Attached MeshRenderer to {closest.UnitName}");
                                }
                                else
                                {
                                    Debug.Log($"[PreviewCardTargets] CRITICAL: No MeshRenderer found on {closest.UnitName}!");
                                }

                                // Highlight nearest enemy in pure, bright solid yellow for absolute visibility against red ships
                                PaintCell(cell, new Color(1f, 1f, 0f, 1f));
                            }
                        }
                    }
                }
                return;
            }

            bool isMovementCard = (targetType == CardTargetType.Tile && card.category == RelicCategory.Boots);

            if (isMovementCard)
            {
                HighlightAdjacentTiles(card.ownerUnit);
            }
            else
            {
                HighlightValidTargets(card);
            }
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

            // Log if card is fundamentally unplayable
            var playability = CardPlayabilityChecker.Check(card, manager.SelectedUnit);
            if (!playability.isPlayable)
            {
                Debug.Log($"<color=orange>Playability Block: {playability.reason}</color>");
                // We do not return here immediately since selecting unplayable cards is permitted 
                // in some designs for UI inspection, but play-execution will ultimately prevent it.
            }

            // If card needs target, enter targeting mode (highlights are already
            // up from the hover preview — StartTargeting will just re-paint them
            // and flip the input mode to "waiting for a target click").
            if (card.RequiresTarget())
            {
                StartTargeting(card);
            }
            else
            {
                // Auto-target card: play immediately on the first click. Clear any
                // leftover preview highlights so the board returns to its base state.
                manager.PlayCard(card);
                ClearTileHighlights();
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
        
        #region Card Owner Highlighting

        private void HighlightCardOwner(UnitStatus owner)
        {
            if (owner == null) return;

            // Clear previous highlight if different owner
            if (isOwnerHighlighted && hoveredCardOwner != owner)
            {
                ClearCardOwnerHighlight();
            }

            hoveredCardOwner = owner;
            hoveredOwnerRenderer = owner.GetComponent<MeshRenderer>();
            if (hoveredOwnerRenderer != null)
            {
                hoveredOwnerOriginalColor = hoveredOwnerRenderer.material.color;
                isOwnerHighlighted = true;
            }
        }

        private UnitStatus activeHighlightedUnit;
        private MeshRenderer activeHighlightedRenderer;
        private Color activeHighlightedOriginalColor;

        private void UpdateCardOwnerPulse()
        {
            var manager = BattleDeckManager.Instance;
            UnitStatus targetUnit = hoveredCardOwner != null ? hoveredCardOwner : (manager != null ? manager.SelectedUnit : null);

            // Shift visual highlight state to the new appropriate unit
            if (activeHighlightedUnit != targetUnit)
            {
                if (activeHighlightedRenderer != null)
                {
                    activeHighlightedRenderer.material.color = activeHighlightedOriginalColor;
                }
                
                activeHighlightedUnit = targetUnit;
                if (targetUnit != null)
                {
                    activeHighlightedRenderer = targetUnit.GetComponent<MeshRenderer>();
                    if (activeHighlightedRenderer != null)
                    {
                        activeHighlightedOriginalColor = activeHighlightedRenderer.material.color;
                    }
                }
            }

            if (activeHighlightedUnit != null && activeHighlightedRenderer != null)
            {
                // Hyper-visible pulse intensities
                float speed = (hoveredCardOwner != null) ? ownerPulseSpeed * 1.5f : ownerPulseSpeed * 0.8f;
                float intensity = (hoveredCardOwner != null) ? 1.0f : 0.85f;
                float pulse = (Mathf.Sin(Time.time * speed) + 1f) / 2f * intensity;
                activeHighlightedRenderer.material.color = Color.Lerp(activeHighlightedOriginalColor, cardOwnerHighlightColor, pulse);
            }
        }

        private void ClearCardOwnerHighlight()
        {
            hoveredCardOwner = null;
            hoveredOwnerRenderer = null;
            isOwnerHighlighted = false;
            // The Update loop will dynamically revert to the selected unit
        }

        private void UpdateEnemyTargetPulse()
        {
            if (hoveredEnemyTarget != null && hoveredEnemyRenderer != null)
            {
                float pulse = (Mathf.Sin(Time.time * ownerPulseSpeed * 1.8f) + 1f) / 2f;
                // Bright intense yellow pulse overlaying the target
                hoveredEnemyRenderer.material.color = Color.Lerp(hoveredEnemyOriginalColor, new Color(1f, 1f, 0f, 1f), pulse);
            }
        }

        #endregion

        #region Targeting
        
        private void StartTargeting(BattleCard card)
        {
            isTargeting = true;
            cardAwaitingTarget = card;
            remainingMoveSteps = 0;

            if (targetingOverlay != null)
                targetingOverlay.SetActive(true);

            var targetType = card.GetTargetType();

            // Boots tile cards are MOVEMENT — they walk the unit step by step.
            // Other tile cards (Totem placement, Coat tile effects) are SINGLE-CLICK
            // placements anywhere on the player side.
            bool isMovementCard = (targetType == CardTargetType.Tile && card.category == RelicCategory.Boots);

            if (isMovementCard)
            {
                int moveRange = GetCardMoveRange(card);
                remainingMoveSteps = moveRange;
                HighlightAdjacentTiles(card.ownerUnit);
                UpdateTargetingPrompt($"Move {card.ownerUnit.UnitName} ({remainingMoveSteps} steps left) — click adjacent tile");
            }
            else
            {
                UpdateTargetingPrompt(GetTargetingPrompt(card));
                HighlightValidTargets(card);
            }
        }

        private void CancelTargeting()
        {
            isTargeting = false;
            cardAwaitingTarget = null;
            remainingMoveSteps = 0;

            ClearTileHighlights();

            if (targetingOverlay != null)
                targetingOverlay.SetActive(false);

            DeselectCard();
        }

        private void UpdateTargetingPrompt(string text)
        {
            if (targetingPrompt != null)
                targetingPrompt.text = text;
        }

        private string GetTargetingPrompt(BattleCard card)
        {
            var targetType = card.GetTargetType();
            switch (targetType)
            {
                case CardTargetType.Tile:
                    return "Select an adjacent tile to move to";
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

        /// <summary>
        /// Get the movement range from a card's effect data.
        /// </summary>
        private int GetCardMoveRange(BattleCard card)
        {
            if (card.sourceRelic?.effectData != null)
            {
                int range = (int)card.sourceRelic.effectData.value1;
                if (range > 0) return range;
            }
            // Fallback to unit's default move range
            var movement = card.ownerUnit?.GetComponent<UnitMovement>();
            return movement != null ? movement.GetEffectiveMoveRange() : 2;
        }

        /// <summary>
        /// Highlight only adjacent (manhattan distance 1) empty tiles around a unit.
        /// </summary>
        private void HighlightAdjacentTiles(UnitStatus unit)
        {
            ClearTileHighlights();

            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || unit == null) return;

            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);

            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            foreach (var dir in directions)
            {
                var cell = gridManager.GetCell(pos.x + dir.x, pos.y + dir.y);
                if (cell != null && cell.CanPlaceUnit() && !cell.IsMiddleColumn)
                {
                    highlightedMoveCells.Add(cell);

                    // Tint the cell to show it's a valid move target
                    var renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (!originalCellColors.ContainsKey(cell))
                            originalCellColors[cell] = renderer.material.color;
                        renderer.material.color = new Color(0.3f, 0.8f, 1f, 1f); // Cyan highlight
                    }
                }
            }
        }

        /// <summary>
        /// Highlight all valid target cells for a card (units or tiles), and any
        /// area-of-effect cells the card would hit around each target.
        ///
        /// Cyan tint  = valid target cell (player can click here)
        /// Purple tint = AoE preview (would be hit, but not directly clickable)
        /// </summary>
        private void HighlightValidTargets(BattleCard card)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || card == null) return;

            var targetType = card.GetTargetType();
            var validCells = CollectValidTargetCells(card, targetType, gridManager);

            // Tile-target cards click on the cell directly, so the cell list also
            // doubles as the validation list for OnTargetSelected.
            bool isTileCard = (targetType == CardTargetType.Tile);

            Color targetTint = new Color(0.3f, 0.8f, 1f, 1f); // cyan
            foreach (var cell in validCells)
            {
                if (cell == null) continue;
                if (isTileCard) highlightedMoveCells.Add(cell);
                PaintCell(cell, targetTint);
            }

            // AoE preview: for effects that splash around a clicked target, paint
            // every cell that would be hit in a softer color. Returns 0 for cards
            // that don't splash (skips the preview entirely).
            int aoeRadius = GetCardAoeRadius(card);
            if (aoeRadius > 0)
            {
                Color aoeTint = new Color(0.65f, 0.4f, 0.9f, 0.8f); // pale purple
                var seen = new HashSet<GridCell>();
                foreach (var center in validCells)
                {
                    if (center == null) continue;
                    for (int dx = -aoeRadius; dx <= aoeRadius; dx++)
                    {
                        for (int dy = -aoeRadius; dy <= aoeRadius; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) > aoeRadius) continue;

                            var aoeCell = gridManager.GetCell(center.XPosition + dx, center.YPosition + dy);
                            if (aoeCell == null) continue;
                            if (originalCellColors.ContainsKey(aoeCell)) continue; // don't overwrite a primary
                            if (!seen.Add(aoeCell)) continue;
                            PaintCell(aoeCell, aoeTint);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Build the list of grid cells that constitute valid targets for a card.
        /// </summary>
        private List<GridCell> CollectValidTargetCells(BattleCard card, CardTargetType targetType, GridManager gridManager)
        {
            var result = new List<GridCell>();

            switch (targetType)
            {
                case CardTargetType.Tile:
                    // Empty, non-blocked tiles on the player side. (For movement cards
                    // we use the existing HighlightAdjacentTiles path instead — this
                    // branch handles cards like Totem_SummonCannon.)
                    int middleCol = gridManager.GetMiddleColumnIndex();
                    for (int x = 0; x < middleCol; x++)
                    {
                        for (int y = 0; y < gridManager.GridHeight; y++)
                        {
                            var c = gridManager.GetCell(x, y);
                            if (c != null && c.CanPlaceUnit() && !c.IsMiddleColumn)
                                result.Add(c);
                        }
                    }
                    break;

                case CardTargetType.Ally:
                    bool onlyDead = (card.effectType == RelicEffectType.Ultimate_ReviveAlly || card.effectType == RelicEffectType.Ultimate_V2_MassRevive);
                    AddCellsForUnits(result, gridManager, card.ownerUnit, wantSameTeam: true, includeSelf: false, targetOnlyDead: onlyDead);
                    break;

                case CardTargetType.Enemy:
                case CardTargetType.AdjacentEnemy:
                case CardTargetType.RangedEnemy:
                    AddCellsForUnits(result, gridManager, card.ownerUnit, wantSameTeam: false, includeSelf: false, targetOnlyDead: false);
                    break;

                case CardTargetType.AnyUnit:
                    AddCellsForUnits(result, gridManager, card.ownerUnit, wantSameTeam: true, includeSelf: false, targetOnlyDead: false);
                    AddCellsForUnits(result, gridManager, card.ownerUnit, wantSameTeam: false, includeSelf: false, targetOnlyDead: false);
                    break;
            }

            return result;
        }

        private void AddCellsForUnits(List<GridCell> outList, GridManager gridManager, UnitStatus self, bool wantSameTeam, bool includeSelf, bool targetOnlyDead = false)
        {
            if (self == null) return;
            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);

            // If we are looking for enemies (wantSameTeam == false), check if any enemy has OnlyTargetThisTurn
            if (!wantSameTeam)
            {
                var forcedTarget = units.FirstOrDefault(u => 
                    u != null && 
                    !u.HasSurrendered && 
                    u.CurrentHP > 0 && 
                    u.Team != self.Team && 
                    u.GetComponent<StatusEffectManager>()?.HasEffect(StatusEffectType.OnlyTargetThisTurn) == true
                );

                if (forcedTarget != null)
                {
                    // Restrict targeting strictly to this unit
                    Vector2Int pos = gridManager.WorldToGridPosition(forcedTarget.transform.position);
                    var cell = gridManager.GetCell(pos.x, pos.y);
                    if (cell != null) outList.Add(cell);
                    return; // Skip adding any other units
                }
            }

            foreach (var u in units)
            {
                if (u == null) continue;
                if (!includeSelf && u == self) continue;
                
                if (targetOnlyDead)
                {
                    if (!u.HasSurrendered && u.CurrentHP > 0) continue; // Only target surrendered/dead
                }
                else
                {
                    if (u.HasSurrendered) continue;
                    if (u.CurrentHP <= 0) continue;
                }
                bool sameTeam = (u.Team == self.Team);
                if (wantSameTeam != sameTeam) continue;

                Vector2Int pos = gridManager.WorldToGridPosition(u.transform.position);
                var cell = gridManager.GetCell(pos.x, pos.y);
                if (cell != null) outList.Add(cell);
            }
        }

        private void PaintCell(GridCell cell, Color color)
        {
            var renderer = cell.GetComponent<Renderer>();
            if (renderer == null) return;
            if (!originalCellColors.ContainsKey(cell))
                originalCellColors[cell] = renderer.material.color;
            renderer.material.color = color;
        }

        /// <summary>
        /// AoE radius (in manhattan distance) of cells that splash around a clicked
        /// target. Returns 0 for single-target / self-targeted / non-AoE cards.
        ///
        /// Add cases here as new role batches introduce splash effects.
        /// Captain has no around-target AoE cards, so this returns 0 for all of them.
        /// </summary>
        private int GetCardAoeRadius(BattleCard card)
        {
            if (card == null) return 0;
            // Future: switch on card.effectType for splash cards (e.g. cannon AoE).
            return 0;
        }

        /// <summary>
        /// Clear all tile highlights and restore original colors.
        /// </summary>
        private void ClearTileHighlights()
        {
            foreach (var kvp in originalCellColors)
            {
                if (kvp.Key != null)
                {
                    var renderer = kvp.Key.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material.color = kvp.Value;
                }
            }
            highlightedMoveCells.Clear();
            originalCellColors.Clear();

            // Clear the 3D unit target highlight as well
            if (hoveredEnemyTarget != null && hoveredEnemyRenderer != null)
            {
                hoveredEnemyRenderer.material.color = hoveredEnemyOriginalColor;
            }
            hoveredEnemyTarget = null;
            hoveredEnemyRenderer = null;
        }

        /// <summary>
        /// Called when a target is selected (unit or tile).
        /// </summary>
        public void OnTargetSelected(UnitStatus target = null, GridCell cell = null)
        {
            if (!isTargeting || cardAwaitingTarget == null) return;

            // Ignore clicks that didn't hit anything useful
            if (target == null && cell == null) return;

            var targetType = cardAwaitingTarget.GetTargetType();

            // Multi-step tile movement
            if (targetType == CardTargetType.Tile && remainingMoveSteps > 0)
            {
                HandleMovementStep(cell);
                return;
            }

            // Standard targeting (enemy, ally, etc.)
            bool valid = ValidateTarget(targetType, target, cell);

            if (valid)
            {
                BattleDeckManager.Instance.PlayCard(cardAwaitingTarget, target, cell);
                CancelTargeting();
            }
            else
            {
                Debug.Log("Invalid target — try again (right-click or Escape to cancel)");
            }
        }

        /// <summary>
        /// Handle one step of multi-step tile movement.
        /// </summary>
        private void HandleMovementStep(GridCell cell)
        {
            if (cell == null || !highlightedMoveCells.Contains(cell))
            {
                Debug.Log("Click a highlighted adjacent tile to move");
                return;
            }

            var unit = cardAwaitingTarget.ownerUnit;
            if (unit == null) return;

            // Move the unit's grid cell registration
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                Vector2Int oldPos = gridManager.WorldToGridPosition(unit.transform.position);
                GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
                if (oldCell != null) oldCell.RemoveUnit();
                cell.PlaceUnit(unit.gameObject);
            }

            // Move the unit visually
            unit.transform.position = cell.GetWorldPosition();
            GameEvents.TriggerUnitMoved(unit.gameObject, null, cell);

            remainingMoveSteps--;

            if (remainingMoveSteps <= 0)
            {
                // All steps used — spend energy and finish
                var manager = BattleDeckManager.Instance;
                // Spend energy and discard the card manually (don't call PlayCard since we already moved)
                var energyManager = ServiceLocator.Get<EnergyManager>();
                if (energyManager != null)
                    energyManager.TrySpendEnergy(cardAwaitingTarget.energyCost);

                // IMPORTANT: Cache the card. If ExecutePostMoveEffects draws a new card,
                // it instantly triggers OnHandChanged, which forces BattleDeckUI to refresh,
                // calling CancelTargeting() and setting cardAwaitingTarget to NULL.
                var cachedCard = cardAwaitingTarget;

                // Execute any additional effects (morale restore, buff, etc.) via the relic effect
                if (cachedCard != null && cachedCard.sourceRelic != null)
                {
                    // Execute non-movement parts of the effect (buffs, heals, etc.)
                    ExecutePostMoveEffects(cachedCard, unit);
                }

                // Discard the card from hand using the cached reference
                if (cachedCard != null)
                {
                    manager.FinishCardAfterMove(cachedCard);
                }

                CancelTargeting();
            }
            else
            {
                // More steps remaining — re-highlight from new position
                HighlightAdjacentTiles(unit);
                UpdateTargetingPrompt($"Move {unit.UnitName} ({remainingMoveSteps} steps left) — click adjacent tile");
            }
        }

        /// <summary>
        /// Execute post-movement effects (buffs, morale, etc.) from boots cards.
        /// </summary>
        private void ExecutePostMoveEffects(BattleCard card, UnitStatus unit)
        {
            if (card.sourceRelic?.effectData == null) return;

            var effect = card.sourceRelic.effectData;

            // Apply secondary effects based on effect type
            // These are the buff/heal parts of boots effects that happen after movement
            switch (effect.effectType)
            {
                case RelicEffectType.Boots_MoveRestoreMorale:
                    unit.RestoreMorale(Mathf.RoundToInt(unit.MaxMorale * effect.value2));
                    Debug.Log($"{unit.UnitName} restored morale after moving");
                    break;
                case RelicEffectType.Boots_MoveClearBuzz:
                    unit.ReduceBuzz(unit.CurrentBuzz);
                    Debug.Log($"{unit.UnitName} cleared buzz after moving");
                    break;
                case RelicEffectType.Boots_MoveGainGrit:
                    StatusEffectManager sem = unit.GetComponent<StatusEffectManager>();
                    if (sem != null) sem.ApplyEffect(StatusEffect.CreateGritBoost(effect.duration, effect.value2, null));
                    break;
                case RelicEffectType.Boots_MoveGainAim:
                    StatusEffectManager sem2 = unit.GetComponent<StatusEffectManager>();
                    if (sem2 != null) sem2.ApplyEffect(StatusEffect.CreateAimBoost(effect.duration, effect.value2, null));
                    break;
                case RelicEffectType.Boots_V2_MoveGainGrog:
                    var em = ServiceLocator.Get<EnergyManager>();
                    if (em != null) em.AddGrog((int)effect.value2);
                    break;
                case RelicEffectType.Boots_V2_MoveGainArmor:
                    unit.RestoreHull((int)effect.value2);
                    break;
                // === COOK BOOTS ===
                case RelicEffectType.Boots_MoveDrawCard:
                    var deckManager = BattleDeckManager.Instance;
                    if (deckManager != null && deckManager.DrawOneCard())
                    {
                        var drawnCard = deckManager.Hand.LastOrDefault();
                        if (drawnCard != null && drawnCard.roleTag == UnitRole.Cook)
                        {
                            drawnCard.energyCost = Mathf.Max(0, drawnCard.energyCost - 1);
                            Debug.Log($"Cook relic cost reduced by 1 for {drawnCard.GetDisplayName()}");
                        }
                    }
                    Debug.Log($"{unit.UnitName} drew a card after moving");
                    break;
                case RelicEffectType.Boots_V2_MoveBoostProficiency:
                    StatusEffectManager sem3 = unit.GetComponent<StatusEffectManager>();
                    if (sem3 != null) sem3.ApplyEffect(StatusEffect.CreateDamageBoost(effect.duration, effect.value2, null));
                    Debug.Log($"{unit.UnitName} gained {effect.value2 * 100}% proficiency after moving");
                    break;
            }
        }

        private bool ValidateTarget(CardTargetType type, UnitStatus target, GridCell cell)
        {
            var owner = cardAwaitingTarget.ownerUnit;

            switch (type)
            {
                case CardTargetType.Tile:
                    return cell != null && highlightedMoveCells.Contains(cell);

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
            var card = cardUI.Card;

            if (!card.BelongsTo(BattleDeckManager.Instance.SelectedUnit))
            {
                Debug.Log($"Select {card.GetOwnerName()} first!");
                return;
            }

            // Right-click discards the card and draws a new one
            BattleDeckManager.Instance.DiscardAndDraw(card);
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