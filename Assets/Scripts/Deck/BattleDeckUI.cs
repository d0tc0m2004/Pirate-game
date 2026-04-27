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
        [SerializeField] private Transform handContainer;       
        [SerializeField] private Transform deckPileContainer;   
        [SerializeField] private Transform discardPileContainer;
        [SerializeField] private Transform passivesButton;      
        [SerializeField] private GameObject passivesPanel;      
        
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
        [SerializeField] private float fanAngle = 5f;           
        [SerializeField] private float fanArcHeight = 20f;      
        [SerializeField] private float selectedLift = 50f;      
        [SerializeField] private float hoverLift = 30f;         
        
        [Header("Card Colors")]
        [SerializeField] private Color playableColor = Color.white;
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

        private int remainingMoveSteps = 0;
        private List<GridCell> highlightedMoveCells = new List<GridCell>();
        private Dictionary<GridCell, Color> originalCellColors = new Dictionary<GridCell, Color>();

        private UnitStatus hoveredCardOwner;
        private MeshRenderer hoveredOwnerRenderer;
        private Color hoveredOwnerOriginalColor;
        private bool isOwnerHighlighted = false;

        private UnitStatus hoveredEnemyTarget;
        private MeshRenderer hoveredEnemyRenderer;
        private Color hoveredEnemyOriginalColor;
        
        #endregion
        
        public bool IsTargeting => isTargeting;

        #region Unity Lifecycle
        
        private void Awake()
        {
            _instance = this;

            if (handContainer == null || deckPileContainer == null)
            {
                AutoGenerateUI();
            }

            HideUI();
        }

        public void HideUI()
        {
            if (handContainer != null) handContainer.gameObject.SetActive(false);
            if (deckPileContainer != null) deckPileContainer.gameObject.SetActive(false);
            if (discardPileContainer != null) discardPileContainer.gameObject.SetActive(false);
            if (passivesButton != null) passivesButton.gameObject.SetActive(false);
            if (targetingOverlay != null) targetingOverlay.SetActive(false);
        }

        public void ShowUI()
        {
            if (handContainer != null) handContainer.gameObject.SetActive(true);
            if (deckPileContainer != null) deckPileContainer.gameObject.SetActive(true);
            if (discardPileContainer != null) discardPileContainer.gameObject.SetActive(true);
            if (passivesButton != null) passivesButton.gameObject.SetActive(true);
        }
        
        private void AutoGenerateUI()
        {
            Debug.Log("<color=yellow>BattleDeckUI: Auto-generating UI (assign references to disable)</color>");
            
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
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
                
                var deckBG = deckGO.AddComponent<Image>();
                deckBG.color = new Color(0.2f, 0.3f, 0.4f, 0.9f);
                deckPileIcon = deckBG;
                
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
                
                var deckBtn = deckGO.AddComponent<Button>();
                deckBtn.onClick.AddListener(OnDeckPileClicked);
                
                deckPileContainer = deckRT;
            }
            
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
                
                var discardBG = discardGO.AddComponent<Image>();
                discardBG.color = new Color(0.4f, 0.25f, 0.2f, 0.9f);
                discardPileIcon = discardBG;
                
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
                
                var discardBtn = discardGO.AddComponent<Button>();
                discardBtn.onClick.AddListener(OnDiscardPileClicked);
                
                discardPileContainer = discardRT;
            }
            
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

                panelGO.AddComponent<PassiveRelicsPanel>();

                panelGO.SetActive(false);
                passivesPanel = panelGO;
            }
            
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
            GameEvents.OnEnergyChanged += OnEnergyChanged;
            RelicTargetSelector.OnTargetingCancelled += ForceDeselectCard;
        }

        private void OnDisable()
        {
            BattleDeckManager.OnDeckBuilt -= RefreshAll;
            BattleDeckManager.OnHandChanged -= OnHandChanged;
            BattleDeckManager.OnCardPlayed -= OnCardPlayed;
            BattleDeckManager.OnCardStowed -= OnCardStowed;
            GameEvents.OnEnergyChanged -= OnEnergyChanged;
            RelicTargetSelector.OnTargetingCancelled -= ForceDeselectCard;
        }

        private void OnEnergyChanged(int newValue)
        {
            UpdateCardVisuals();
        }
        
        private void Update()
        {
            if (isTargeting && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelTargeting();
            }

            UpdateCardOwnerPulse();
            UpdateEnemyTargetPulse();
        }
        
        #endregion
        
        #region Refresh UI
        
        private void RefreshAll()
        {
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
            hoveredCard = null;
            selectedCardUI = null;
            ClearCardOwnerHighlight();
            ClearTileHighlights();
            if (isTargeting) CancelTargeting();

            foreach (var cardUI in cardUIInstances)
            {
                if (cardUI != null)
                {
                    Destroy(cardUI.gameObject);
                }
            }
            cardUIInstances.Clear();
            
            if (hand == null || hand.Count == 0) return;
            
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
                cardGO = CardUIGenerator.CreateCard(card, handContainer);
                cardUI = cardGO.GetComponent<CardUI>();
            }
            
            cardUI.Initialize(card, this);
            cardUIInstances.Add(cardUI);
            
            PositionCardInFan(cardUI, index, totalCards);
        }
        
        private void PositionCardInFan(CardUI cardUI, int index, int totalCards)
        {
            float centerOffset = (totalCards - 1) / 2f;
            float xOffset = (index - centerOffset) * cardSpacing;
            
            float normalizedPos = (index - centerOffset) / Mathf.Max(1, centerOffset);
            float yOffset = -Mathf.Abs(normalizedPos) * fanArcHeight;
            
            float rotation = -(index - centerOffset) * fanAngle;
            
            var rt = cardUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(xOffset, yOffset);
                rt.localRotation = Quaternion.Euler(0, 0, rotation);
            }
            
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

                CardPlayabilityChecker.Result playability =
                    CardPlayabilityChecker.Check(cardUI.Card, cardUI.Card.ownerUnit);

                bool isPlayable = playability.isPlayable;

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
                cardUI.SetPlayability(playability, belongsToSelected);
                cardUI.SetInteractable(belongsToSelected);
                cardUI.SetStowedIndicator(isStowed);
            }
        }
        
        #endregion
        
        #region Card Interactions
        
        public void OnCardHoverEnter(CardUI cardUI)
        {
            if (isTargeting) return;

            hoveredCard = cardUI;

            var rt = cardUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                var pos = rt.anchoredPosition;
                pos.y += hoverLift;
                rt.anchoredPosition = pos;
            }

            cardUI.transform.SetAsLastSibling();

            if (cardUI.Card.BelongsTo(BattleDeckManager.Instance.SelectedUnit))
            {
                cardUI.ShowStowButton(true);
                cardUI.ShowDiscardButton(true);
            }

            HighlightCardOwner(cardUI.Card.ownerUnit);
            PreviewCardTargets(cardUI.Card);
        }

        public void OnCardHoverExit(CardUI cardUI)
        {
            if (cardUI == selectedCardUI) return;

            if (cardUI != hoveredCard) return;

            hoveredCard = null;

            int index = cardUIInstances.IndexOf(cardUI);
            if (index >= 0)
            {
                PositionCardInFan(cardUI, index, cardUIInstances.Count);
            }

            cardUI.ShowStowButton(false);
            cardUI.ShowDiscardButton(false);

            ClearCardOwnerHighlight();

            if (!isTargeting)
            {
                ClearTileHighlights();
            }
        }

        /// <summary>
        /// Paint the cells a card would target as a hover-time preview.
        /// Upgraded to highlight Shipwright Row buffs and Fortress buffs!
        /// </summary>
        private void PreviewCardTargets(BattleCard card)
        {
            if (card == null) return;
            ClearTileHighlights();

            var targetType = card.GetTargetType();

            // === AUTO-TARGETING CARDS (No click required) ===
            // === AUTO-TARGETING CARDS (No click required) ===
            if (targetType == CardTargetType.None)
            {
                // 1. Weapons & Gloves -> Highlight Nearest Enemy in Yellow
                if ((card.IsWeaponCard || card.category == RelicCategory.Gloves) && card.ownerUnit != null)
                {
                    UnitStatus closest = TacticalGame.Combat.TargetFinder.FindNearestEnemy(card.ownerUnit);
                    if (closest != null) HighlightTargetUnit(closest, new Color(1f, 1f, 0f, 1f));
                }
                // 2. Boots V1: Highlight Lowest Morale Ally AND ALL empty tiles!
                else if (card.effectType == RelicEffectType.Boots_AllyFreeMoveLowestMorale)
                {
                    UnitStatus lowestMorale = GetLowestMoraleAlly(card.ownerUnit);
                    if (lowestMorale != null) 
                    {
                        HighlightTargetUnit(lowestMorale, new Color(0.2f, 1f, 0.2f, 1f));
                        HighlightAllEmptyTiles(); // Draws green tiles across the whole board
                    }
                }
                // 3. Hat V1: Highlight Lowest Morale Ally only
                else if (card.effectType == RelicEffectType.Hat_RestoreMoraleLowest)
                {
                    UnitStatus lowestMorale = GetLowestMoraleAlly(card.ownerUnit);
                    if (lowestMorale != null) 
                    {
                        HighlightTargetUnit(lowestMorale, new Color(0.2f, 1f, 0.2f, 1f));
                    }
                }
                // 4. Radial AoE -> Highlight aura in Transparent Green (SHIPWRIGHT COAT V2)
                else if (card.effectType == RelicEffectType.Hat_RestoreMoraleNearby ||
                         card.effectType == RelicEffectType.Totem_RallyNoMoraleDamage ||
                         card.effectType == RelicEffectType.Coat_V2_WellFed) 
                {
                    int radius = card.sourceRelic?.effectData != null ? card.sourceRelic.effectData.tileRange : 1;
                    HighlightAoE(card.ownerUnit, radius, new Color(0.2f, 1f, 0.2f, 0.4f));
                }
                // 5. Global Buffs -> Highlight ALL Allies in Transparent Green (SHIPWRIGHT ULT V2)
                else if (card.effectType == RelicEffectType.Coat_ReduceMoraleDamage ||
                         card.effectType == RelicEffectType.Ultimate_ReflectMoraleDamage ||
                         card.effectType == RelicEffectType.Ultimate_V2_Fortress) 
                {
                    HighlightAllAllies(card.ownerUnit, new Color(0.2f, 1f, 0.2f, 0.4f));
                }
                // 6. ROW Buffs -> Highlight the entire Player-side Row (SHIPWRIGHT COAT V1)
                else if (card.effectType == RelicEffectType.Coat_RowCantBeTargeted ||
                         card.effectType == RelicEffectType.Coat_RowRangedProtection)
                {
                    HighlightRow(card.ownerUnit, new Color(0.2f, 1f, 0.2f, 0.4f));
                }
                // 7. COLUMN Buffs -> Highlight the entire Column (SHIPWRIGHT COAT V2)
                else if (card.effectType == RelicEffectType.Coat_ColumnDamageBoost)
                {
                    HighlightColumn(card.ownerUnit, new Color(0.2f, 1f, 0.2f, 0.4f));
                }
                // 8. Enemy Grit Swap -> Highlight highest and lowest Grit enemies (SHIPWRIGHT HAT V2)
                else if (card.effectType == RelicEffectType.Hat_SwapEnemyByGrit)
                {
                    var enemies = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None)
                        .Where(u => u != null && u.Team != card.ownerUnit.Team && !u.HasSurrendered)
                        .ToList();
                        
                    if (enemies.Count >= 2)
                    {
                        var highestGrit = enemies.OrderByDescending(e => e.Grit).First();
                        var lowestGrit = enemies.OrderBy(e => e.Grit).Last();
                        
                        if (highestGrit != lowestGrit)
                        {
                            HighlightTargetUnit(highestGrit, new Color(1f, 0.5f, 0f, 1f)); 
                            HighlightTargetUnit(lowestGrit, new Color(1f, 0.5f, 0f, 1f));
                        }
                    }
                }
                return;
            }

            // === TARGETED CARDS (Click required) ===
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

        private void HighlightColumn(UnitStatus owner, Color color)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || owner == null) return;
            
            var pos = gridManager.WorldToGridPosition(owner.transform.position);
            
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                var cell = gridManager.GetCell(pos.x, y);
                if (cell != null && !cell.IsMiddleColumn) PaintCell(cell, color);
            }
        }

        // --- NEW PREVIEW HELPER FOR ROWS ---
        private void HighlightRow(UnitStatus owner, Color color)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || owner == null) return;
            
            var pos = gridManager.WorldToGridPosition(owner.transform.position);
            int middleCol = gridManager.GetMiddleColumnIndex();
            
            // Loop through all tiles in the player's half of the specific row
            for (int x = 0; x < middleCol; x++)
            {
                var cell = gridManager.GetCell(x, pos.y);
                if (cell != null && !cell.IsMiddleColumn) PaintCell(cell, color);
            }
        }

        // --- PREVIEW HELPERS ---

        // --- NEW PREVIEW HELPER FOR ALL EMPTY TILES ---
        private void HighlightAllEmptyTiles()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return;
            
            int middleCol = gridManager.GetMiddleColumnIndex();
            for (int x = 0; x < middleCol; x++) // Highlights standard player-side placement tiles
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    var cell = gridManager.GetCell(x, y);
                    if (cell != null && cell.CanPlaceUnit() && !cell.IsMiddleColumn)
                    {
                        highlightedMoveCells.Add(cell);
                        PaintCell(cell, new Color(0.3f, 0.8f, 1f, 1f)); // Use your blue targeting tint
                    }
                }
            }
        }

        
        private void HighlightTargetUnit(UnitStatus target, Color color)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || target == null) return;
            
            var pos = gridManager.WorldToGridPosition(target.transform.position);
            var cell = gridManager.GetCell(pos.x, pos.y);
            if (cell != null) PaintCell(cell, color);

            hoveredEnemyTarget = target; 
            hoveredEnemyRenderer = target.GetComponent<MeshRenderer>();
            if (hoveredEnemyRenderer != null) hoveredEnemyOriginalColor = hoveredEnemyRenderer.material.color;
        }

        private void HighlightAoE(UnitStatus centerUnit, int radius, Color color)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || centerUnit == null) return;
            
            Vector2Int center = gridManager.WorldToGridPosition(centerUnit.transform.position);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // FIXED: Now uses Chebyshev distance to include all 8 diagonal directions perfectly!
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > radius) continue;
                    
                    var cell = gridManager.GetCell(center.x + dx, center.y + dy);
                    if (cell != null) PaintCell(cell, color);
                }
            }
        }

        private void HighlightAllAllies(UnitStatus owner, Color color)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || owner == null) return;
            
            var units = Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach(var u in units)
            {
                if (u != null && u.Team == owner.Team && !u.HasSurrendered)
                {
                    var pos = gridManager.WorldToGridPosition(u.transform.position);
                    var cell = gridManager.GetCell(pos.x, pos.y);
                    if (cell != null) PaintCell(cell, color);
                }
            }
        }

        private UnitStatus GetLowestMoraleAlly(UnitStatus owner)
        {
            if (owner == null) return null;
            return Object.FindObjectsByType<UnitStatus>(FindObjectsSortMode.None)
                .Where(u => u != null && u.Team == owner.Team && u != owner && !u.HasSurrendered)
                .OrderBy(u => u.MoralePercent)
                .FirstOrDefault();
        }
        
        public void OnCardClicked(CardUI cardUI)
        {
            var manager = BattleDeckManager.Instance;
            var card = cardUI.Card;

            if (!card.BelongsTo(manager.SelectedUnit))
            {
                Debug.Log($"Select {card.GetOwnerName()} first!");
                return;
            }

            if (selectedCardUI == cardUI)
            {
                if (RelicTargetSelector.Instance != null && RelicTargetSelector.Instance.IsSelecting)
                {
                     RelicTargetSelector.Instance.CancelSelection();
                }
                CancelTargeting();
                return;
            }

            var playability = CardPlayabilityChecker.Check(card, manager.SelectedUnit);
            if (!playability.isPlayable)
            {
                Debug.Log($"<color=orange>Action Blocked: {playability.reason}</color>");
                return; 
            }

            SelectCard(cardUI);

            if (card.RequiresTarget())
            {
                StartTargeting(card);
            }
            else
            {
                manager.PlayCard(card);
                ClearTileHighlights();
                DeselectCard();
            }
        }
        
        public void OnCardRightClicked(CardUI cardUI)
        {
            ShowCardContextMenu(cardUI);
        }
        
        private void SelectCard(CardUI cardUI)
        {
            if (selectedCardUI != null)
            {
                ResetCardPosition(selectedCardUI);
            }
            
            selectedCardUI = cardUI;
            BattleDeckManager.Instance.SelectCard(cardUI.Card);
            
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
            if (RelicTargetSelector.Instance != null && RelicTargetSelector.Instance.IsSelecting) return;
            ForceDeselectCard();
        }
        
        private void ForceDeselectCard()
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
        }

        private void UpdateEnemyTargetPulse()
        {
            if (hoveredEnemyTarget != null && hoveredEnemyRenderer != null)
            {
                float pulse = (Mathf.Sin(Time.time * ownerPulseSpeed * 1.8f) + 1f) / 2f;
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

            ForceDeselectCard();
        }

        private void UpdateTargetingPrompt(string text)
        {
            if (targetingPrompt != null)
                targetingPrompt.text = text;
        }

        private string GetTargetingPrompt(BattleCard card)
        {
            var targetType = card.GetTargetType();
            if (card.category == RelicCategory.Totem && targetType == CardTargetType.Tile)
            {
                return "Select a tile to place your totem/hazard";
            }
            
            switch (targetType)
            {
                case CardTargetType.Tile: return "Select an adjacent tile to move to";
                case CardTargetType.Ally: return "Select an ally";
                case CardTargetType.Enemy:
                case CardTargetType.AdjacentEnemy:
                case CardTargetType.RangedEnemy: return "Select an enemy to target";
                case CardTargetType.AnyUnit: return "Select a unit";
                default: return "Select a target";
            }
        }

        private int GetCardMoveRange(BattleCard card)
        {
            // NEW: Boatswain V2 Dynamic Move Range
            if (card.effectType == RelicEffectType.Boots_MoveAnyIfHighestHP && card.ownerUnit != null)
            {
                var allies = GameObject.FindGameObjectsWithTag("Unit")
                    .Select(go => go.GetComponent<UnitStatus>())
                    .Where(u => u != null && u.Team == card.ownerUnit.Team && !u.HasSurrendered)
                    .ToList();
                
                bool isHighest = true;
                foreach (var a in allies) {
                    if (a != card.ownerUnit && a.CurrentHP > card.ownerUnit.CurrentHP) {
                        isHighest = false; break;
                    }
                }
                if (isHighest) return 99; // Move anywhere!
            }

            // Normal Range Fetching
            if (card.sourceRelic?.effectData != null)
            {
                int range = (int)card.sourceRelic.effectData.value1;
                if (range > 0) return range;
            }
            var movement = card.ownerUnit?.GetComponent<UnitMovement>();
            return movement != null ? movement.GetEffectiveMoveRange() : 2;
        }

        private void HighlightAdjacentTiles(UnitStatus unit)
        {
            ClearTileHighlights();

            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || unit == null) return;

            Vector2Int pos = gridManager.WorldToGridPosition(unit.transform.position);
            
            // FIXED: Now includes all 8 directions (Up, Down, Left, Right, and Diagonals)
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                var cell = gridManager.GetCell(pos.x + dir.x, pos.y + dir.y);
                if (cell != null && cell.CanPlaceUnit() && !cell.IsMiddleColumn)
                {
                    highlightedMoveCells.Add(cell);
                    var renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (!originalCellColors.ContainsKey(cell))
                            originalCellColors[cell] = renderer.material.color;
                        renderer.material.color = new Color(0.3f, 0.8f, 1f, 1f); 
                    }
                }
            }
        }

        private void HighlightValidTargets(BattleCard card)
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || card == null) return;

            var targetType = card.GetTargetType();
            var validCells = CollectValidTargetCells(card, targetType, gridManager);

            bool isTileCard = (targetType == CardTargetType.Tile);

            Color targetTint = new Color(0.3f, 0.8f, 1f, 1f);
            foreach (var cell in validCells)
            {
                if (cell == null) continue;
                if (isTileCard) highlightedMoveCells.Add(cell);
                PaintCell(cell, targetTint);
            }

            int aoeRadius = GetCardAoeRadius(card);
            if (aoeRadius > 0)
            {
                Color aoeTint = new Color(0.65f, 0.4f, 0.9f, 0.8f);
                var seen = new HashSet<GridCell>();
                foreach (var center in validCells)
                {
                    if (center == null) continue;
                    for (int dx = -aoeRadius; dx <= aoeRadius; dx++)
                    {
                        for (int dy = -aoeRadius; dy <= aoeRadius; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            // FIXED: Now uses Chebyshev distance to include the diagonals in the purple highlight
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > aoeRadius) continue;

                            var aoeCell = gridManager.GetCell(center.XPosition + dx, center.YPosition + dy);
                            if (aoeCell == null) continue;
                            if (originalCellColors.ContainsKey(aoeCell)) continue; 
                            if (!seen.Add(aoeCell)) continue;
                            PaintCell(aoeCell, aoeTint);
                        }
                    }
                }
            }
        }

        private List<GridCell> CollectValidTargetCells(BattleCard card, CardTargetType targetType, GridManager gridManager)
        {
            var result = new List<GridCell>();

            switch (targetType)
            {
                case CardTargetType.Tile:
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
                    Vector2Int pos = gridManager.WorldToGridPosition(forcedTarget.transform.position);
                    var cell = gridManager.GetCell(pos.x, pos.y);
                    if (cell != null) outList.Add(cell);
                    return; 
                }
            }

            foreach (var u in units)
            {
                if (u == null) continue;
                if (!includeSelf && u == self) continue;
                
                if (targetOnlyDead)
                {
                    if (!u.HasSurrendered && u.CurrentHP > 0) continue; 
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

        private int GetCardAoeRadius(BattleCard card)
        {
            if (card == null) return 0;
            return 0;
        }

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

            if (hoveredEnemyTarget != null && hoveredEnemyRenderer != null)
            {
                hoveredEnemyRenderer.material.color = hoveredEnemyOriginalColor;
            }
            hoveredEnemyTarget = null;
            hoveredEnemyRenderer = null;
        }

        public void OnTargetSelected(UnitStatus target = null, GridCell cell = null)
        {
            if (!isTargeting || cardAwaitingTarget == null) return;

            if (target == null && cell == null) return;

            var targetType = cardAwaitingTarget.GetTargetType();

            if (targetType == CardTargetType.Tile && remainingMoveSteps > 0)
            {
                HandleMovementStep(cell);
                return;
            }

            bool valid = ValidateTarget(targetType, target, cell);

            if (valid)
            {
                var playedCard = cardAwaitingTarget;
                
                isTargeting = false;
                cardAwaitingTarget = null;
                if (targetingOverlay != null) targetingOverlay.SetActive(false);
                ClearTileHighlights();

                BattleDeckManager.Instance.PlayCard(playedCard, target, cell);
                
                if (RelicTargetSelector.Instance != null && RelicTargetSelector.Instance.IsSelecting)
                {
                    // Target selector running. Do not force deselect.
                }
                else
                {
                    ForceDeselectCard();
                }
            }
            else
            {
                Debug.Log("Invalid target — try again (right-click or Escape to cancel)");
            }
        }

        private void HandleMovementStep(GridCell cell)
        {
            if (cell == null || !highlightedMoveCells.Contains(cell))
            {
                Debug.Log("Click a highlighted adjacent tile to move");
                return;
            }

            var unit = cardAwaitingTarget.ownerUnit;
            if (unit == null) return;

            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                Vector2Int oldPos = gridManager.WorldToGridPosition(unit.transform.position);
                GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
                if (oldCell != null) oldCell.RemoveUnit();
                cell.PlaceUnit(unit.gameObject);
            }

            unit.transform.position = cell.GetWorldPosition();
            GameEvents.TriggerUnitMoved(unit.gameObject, null, cell);

            remainingMoveSteps--;

            if (remainingMoveSteps <= 0)
            {
                var manager = BattleDeckManager.Instance;
                var energyManager = ServiceLocator.Get<EnergyManager>();
                var cachedCard = cardAwaitingTarget;

                if (energyManager != null)
                {
                    bool isFree = false;
                    if (cachedCard != null && cachedCard.effectType == RelicEffectType.Boots_FreeIfGrog)
                    {
                        if (energyManager.GrogTokens > 0)
                        {
                            isFree = true;
                            Debug.Log("Grog is available! Helmsman move is free (no grog spent)!");
                        }
                    }

                    if (!isFree && cachedCard != null)
                    {
                        energyManager.TrySpendEnergy(cachedCard.energyCost);
                    }
                }

                if (cachedCard != null && cachedCard.sourceRelic != null)
                {
                    ExecutePostMoveEffects(cachedCard, unit);
                }

                if (cachedCard != null)
                {
                    manager.FinishCardAfterMove(cachedCard);
                }

                CancelTargeting();
            }
            else
            {
                HighlightAdjacentTiles(unit);
                UpdateTargetingPrompt($"Move {unit.UnitName} ({remainingMoveSteps} steps left) — click adjacent tile");
            }
        }

        private void ExecutePostMoveEffects(BattleCard card, UnitStatus unit)
        {
            if (card.sourceRelic?.effectData == null) return;

            var effect = card.sourceRelic.effectData;

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
            if (isTargeting)
            {
                CancelTargeting();
                return;
            }
            
            var card = cardUI.Card;

            if (!card.BelongsTo(BattleDeckManager.Instance.SelectedUnit))
            {
                Debug.Log($"Select {card.GetOwnerName()} first!");
                return;
            }

            BattleDeckManager.Instance.DiscardAndDraw(card);
        }
        
        public void StowCard()
        {
            var cardUI = selectedCardUI ?? hoveredCard;
            if (cardUI == null) return;
            
            BattleDeckManager.Instance.StowCard(cardUI.Card);
        }
        
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
            Debug.Log($"Card played: {card.GetDisplayName()}");
        }
        
        private void OnCardStowed(BattleCard card)
        {
            UpdateCardVisuals();
        }
        
        #endregion
        
        #region Passives Panel
        
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
            var passives = BattleDeckManager.Instance.PassiveRelics;
            Debug.Log($"Showing {passives.Count} passive relics");
        }
        
        #endregion
        
        #region Deck/Discard Click Handlers
        
        public void OnDeckPileClicked()
        {
            Debug.Log($"Deck: {BattleDeckManager.Instance.DeckCount} cards");
        }
        
        public void OnDiscardPileClicked()
        {
            Debug.Log($"Discard: {BattleDeckManager.Instance.DiscardCount} cards");
        }

        
        
        #endregion
    }
}