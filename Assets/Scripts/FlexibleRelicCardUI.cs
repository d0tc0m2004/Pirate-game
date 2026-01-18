using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using TacticalGame.Units;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Core;
using TacticalGame.Managers;

namespace TacticalGame.UI
{
    /// <summary>
    /// Card UI with fan layout and smooth animations.
    /// Works with FlexibleUnitEquipment (slot-based system).
    /// Press X with a unit selected to show their cards.
    /// </summary>
    public class FlexibleRelicCardUI : MonoBehaviour
    {
        #region Singleton
        public static FlexibleRelicCardUI Instance { get; private set; }
        #endregion

        #region Settings
        [Header("Card Dimensions")]
        [SerializeField] private float cardWidth = 140f;
        [SerializeField] private float cardHeight = 200f;
        [SerializeField] private float cardSpacing = 10f;
        [SerializeField] private float fanAngle = 3f;
        [SerializeField] private float hoverLift = 40f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float animationSpeed = 8f;

        [Header("Position")]
        [SerializeField] private float bottomOffset = 20f;
        #endregion

        #region Runtime State
        private Canvas canvas;
        private GameObject cardContainer;
        private List<CardVisual> cards = new List<CardVisual>();

        private GameObject selectedUnit;
        private FlexibleUnitEquipment selectedEquipment;
        private UnitStatus selectedStatus;

        private bool isVisible = false;
        private int hoveredIndex = -1;
        
        private GameObject pendingUnit;
        private EnergyManager energyManager;
        
        // Colors - all dark/neutral
        private Color cardBgColor = new Color(0.15f, 0.15f, 0.18f);
        private Color cardBorderColor = new Color(0.3f, 0.3f, 0.35f);
        private Color cardHoverColor = new Color(0.22f, 0.22f, 0.26f);
        private Color disabledOverlayColor = new Color(0f, 0f, 0f, 0.5f);
        private Color emptyBgColor = new Color(0.1f, 0.1f, 0.12f);
        private Color costBgColor = new Color(0.2f, 0.35f, 0.6f);
        private Color matchGoldColor = new Color(1f, 0.85f, 0.4f);
        #endregion

        #region Card Visual Class
        private class CardVisual
        {
            public GameObject root;
            public RectTransform rect;
            public Image background;
            public Image border;
            public Image topBar;
            public TMP_Text typeLabel;
            public TMP_Text nameLabel;
            public TMP_Text costLabel;
            public TMP_Text roleLabel;
            public TMP_Text matchLabel;
            public TMP_Text copiesLabel;
            public GameObject disabledOverlay;
            public Button button;

            public int slotIndex;
            public Color baseColor;
            
            public Vector2 basePosition;
            public float baseRotation;
            public Vector2 targetPosition;
            public float targetRotation;
            public float targetScale;

            public FlexibleUnitEquipment.RelicSlot slotData;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            energyManager = ServiceLocator.Get<EnergyManager>();
            CreateUI();
            Hide();

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

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X) && pendingUnit != null)
            {
                if (isVisible)
                    Hide();
                else
                    ShowCards();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && isVisible)
                Hide();

            if (!isVisible) return;

            // Animate cards
            foreach (var card in cards)
            {
                if (card.root == null) continue;

                Vector2 currentPos = card.rect.anchoredPosition;
                card.rect.anchoredPosition = Vector2.Lerp(currentPos, card.targetPosition, Time.deltaTime * animationSpeed);

                float currentRot = card.rect.localEulerAngles.z;
                if (currentRot > 180) currentRot -= 360;
                float newRot = Mathf.Lerp(currentRot, card.targetRotation, Time.deltaTime * animationSpeed);
                card.rect.localEulerAngles = new Vector3(0, 0, newRot);

                float currentScale = card.rect.localScale.x;
                float newScale = Mathf.Lerp(currentScale, card.targetScale, Time.deltaTime * animationSpeed);
                card.rect.localScale = Vector3.one * newScale;
            }
        }
        #endregion

        #region UI Creation
        private void CreateUI()
        {
            // Create dedicated canvas
            GameObject canvasObj = new GameObject("RelicCardCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // Container
            cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, bottomOffset);
            containerRect.sizeDelta = new Vector2(1000, cardHeight + 50);

            // Create 7 cards
            for (int i = 0; i < 7; i++)
            {
                CreateCard(i);
            }
        }

        private void CreateCard(int index)
        {
            CardVisual card = new CardVisual();
            card.slotIndex = index;

            // Root
            card.root = new GameObject($"Card_{index}");
            card.root.transform.SetParent(cardContainer.transform, false);

            card.rect = card.root.AddComponent<RectTransform>();
            card.rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            card.rect.pivot = new Vector2(0.5f, 0);

            // Background - DARK
            card.background = card.root.AddComponent<Image>();
            card.background.color = cardBgColor;
            card.baseColor = cardBgColor;

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

            // Top color bar (thin accent line)
            GameObject topBarObj = new GameObject("TopBar");
            topBarObj.transform.SetParent(card.root.transform, false);
            card.topBar = topBarObj.AddComponent<Image>();
            card.topBar.color = cardBorderColor;
            card.topBar.raycastTarget = false;
            RectTransform topBarRect = topBarObj.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 4);
            topBarRect.anchoredPosition = Vector2.zero;

            // Type label
            card.typeLabel = CreateText(card.root.transform, "Type", "", 11, FontStyles.Bold);
            RectTransform typeRect = card.typeLabel.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0, 1);
            typeRect.anchorMax = new Vector2(1, 1);
            typeRect.pivot = new Vector2(0.5f, 1);
            typeRect.anchoredPosition = new Vector2(0, -10);
            typeRect.sizeDelta = new Vector2(-10, 20);
            card.typeLabel.color = new Color(0.7f, 0.7f, 0.7f);

            // Name label
            card.nameLabel = CreateText(card.root.transform, "Name", "", 12, FontStyles.Bold);
            RectTransform nameRect = card.nameLabel.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.4f);
            nameRect.anchorMax = new Vector2(1, 0.75f);
            nameRect.sizeDelta = new Vector2(-10, 0);
            nameRect.anchoredPosition = Vector2.zero;
            card.nameLabel.enableWordWrapping = true;
            card.nameLabel.color = Color.white;

            // Role label
            card.roleLabel = CreateText(card.root.transform, "Role", "", 10, FontStyles.Italic);
            RectTransform roleRect = card.roleLabel.GetComponent<RectTransform>();
            roleRect.anchorMin = new Vector2(0, 0.25f);
            roleRect.anchorMax = new Vector2(1, 0.4f);
            roleRect.sizeDelta = new Vector2(-10, 0);
            roleRect.anchoredPosition = Vector2.zero;
            card.roleLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // Match label
            card.matchLabel = CreateText(card.root.transform, "Match", "", 10, FontStyles.Bold);
            RectTransform matchRect = card.matchLabel.GetComponent<RectTransform>();
            matchRect.anchorMin = new Vector2(0, 0.12f);
            matchRect.anchorMax = new Vector2(1, 0.25f);
            matchRect.sizeDelta = new Vector2(-10, 0);
            matchRect.anchoredPosition = Vector2.zero;
            card.matchLabel.color = matchGoldColor;

            // Cost background
            GameObject costBg = new GameObject("CostBg");
            costBg.transform.SetParent(card.root.transform, false);
            Image costBgImg = costBg.AddComponent<Image>();
            costBgImg.color = costBgColor;
            costBgImg.raycastTarget = false;
            RectTransform costBgRect = costBg.GetComponent<RectTransform>();
            costBgRect.anchorMin = costBgRect.anchorMax = Vector2.zero;
            costBgRect.pivot = Vector2.zero;
            costBgRect.anchoredPosition = new Vector2(6, 6);
            costBgRect.sizeDelta = new Vector2(26, 26);

            // Cost label
            card.costLabel = CreateText(card.root.transform, "Cost", "1", 14, FontStyles.Bold);
            RectTransform costRect = card.costLabel.GetComponent<RectTransform>();
            costRect.anchorMin = costRect.anchorMax = Vector2.zero;
            costRect.pivot = Vector2.zero;
            costRect.anchoredPosition = new Vector2(6, 6);
            costRect.sizeDelta = new Vector2(26, 26);

            // Copies label
            card.copiesLabel = CreateText(card.root.transform, "Copies", "", 10, FontStyles.Normal);
            RectTransform copiesRect = card.copiesLabel.GetComponent<RectTransform>();
            copiesRect.anchorMin = copiesRect.anchorMax = new Vector2(1, 0);
            copiesRect.pivot = new Vector2(1, 0);
            copiesRect.anchoredPosition = new Vector2(-8, 8);
            copiesRect.sizeDelta = new Vector2(30, 20);
            card.copiesLabel.color = new Color(0.5f, 0.5f, 0.5f);

            // Disabled overlay
            card.disabledOverlay = new GameObject("Disabled");
            card.disabledOverlay.transform.SetParent(card.root.transform, false);
            Image overlayImg = card.disabledOverlay.AddComponent<Image>();
            overlayImg.color = disabledOverlayColor;
            overlayImg.raycastTarget = false;
            RectTransform overlayRect = card.disabledOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            card.disabledOverlay.SetActive(false);

            // Button
            card.button = card.root.AddComponent<Button>();
            card.button.targetGraphic = card.background;
            
            // Remove color transition (keep it manual)
            var colors = card.button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            card.button.colors = colors;

            int idx = index;
            card.button.onClick.AddListener(() => OnCardClicked(idx));

            EventTrigger trigger = card.root.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => OnCardHoverEnter(idx));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => OnCardHoverExit(idx));
            trigger.triggers.Add(exitEntry);

            cards.Add(card);
        }

        private TMP_Text CreateText(Transform parent, string name, string text, int size, FontStyles style)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return tmp;
        }
        #endregion

        #region Event Handlers
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

            pendingUnit = unit;

            if (isVisible && selectedUnit != unit)
                Hide();
        }

        private void OnUnitDeselected()
        {
            ClearPendingUnit();
            Hide();
        }

        private void ClearPendingUnit() => pendingUnit = null;

        private void OnEnergyChanged(int e)
        {
            if (isVisible) UpdateCardStates();
        }

        private void FullHide()
        {
            ClearPendingUnit();
            Hide();
        }
        #endregion

        #region Show/Hide
        private void ShowCards()
        {
            if (pendingUnit == null) return;

            selectedUnit = pendingUnit;
            selectedStatus = selectedUnit.GetComponent<UnitStatus>();
            selectedEquipment = selectedUnit.GetComponent<FlexibleUnitEquipment>();

            if (selectedEquipment == null)
            {
                Debug.LogWarning($"[FlexibleCardUI] No FlexibleUnitEquipment on {selectedUnit.name}");
                return;
            }

            isVisible = true;
            cardContainer.SetActive(true);
            
            PopulateCards();
            PositionCards();
            UpdateCardStates();
        }

        public void Hide()
        {
            isVisible = false;
            selectedUnit = null;
            selectedEquipment = null;
            selectedStatus = null;
            hoveredIndex = -1;

            if (cardContainer != null)
                cardContainer.SetActive(false);
        }

        private void PositionCards()
        {
            int count = cards.Count;
            float totalWidth = count * cardWidth + (count - 1) * cardSpacing;
            float startX = -totalWidth / 2f + cardWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                CardVisual card = cards[i];
                
                float x = startX + i * (cardWidth + cardSpacing);
                float centerOffset = (i - (count - 1) / 2f);
                float rotation = -centerOffset * fanAngle;
                float yOffset = -Mathf.Abs(centerOffset) * 5f;

                card.basePosition = new Vector2(x, yOffset);
                card.baseRotation = rotation;
                card.targetPosition = card.basePosition;
                card.targetRotation = card.baseRotation;
                card.targetScale = 1f;

                card.rect.anchoredPosition = card.basePosition;
                card.rect.localEulerAngles = new Vector3(0, 0, rotation);
                card.rect.localScale = Vector3.one;
            }
        }
        #endregion

        #region Card Population
        private void PopulateCards()
        {
            UnitRole unitRole = selectedStatus?.Role ?? UnitRole.Deckhand;

            for (int i = 0; i < 7; i++)
            {
                var card = cards[i];
                var slot = selectedEquipment.GetSlot(i);
                card.slotData = slot;

                if (slot == null || slot.IsEmpty)
                {
                    SetEmptyCard(card, i);
                    continue;
                }

                if (slot.hasWeapon && slot.weaponRelic != null)
                    SetWeaponCard(card, slot.weaponRelic, unitRole);
                else if (slot.categoryRelic != null)
                    SetCategoryCard(card, slot.categoryRelic, unitRole, i);
                else
                    SetEmptyCard(card, i);
            }
        }

        private void SetWeaponCard(CardVisual card, WeaponRelic relic, UnitRole unitRole)
        {
            card.typeLabel.text = "WEAPON";
            card.typeLabel.color = new Color(0.9f, 0.75f, 0.4f);

            string name = relic.baseWeaponData != null ? relic.baseWeaponData.weaponName : relic.weaponFamily.ToString();
            card.nameLabel.text = name;
            card.nameLabel.color = Color.white;
            
            card.roleLabel.text = GetRoleName(relic.roleTag);
            card.costLabel.text = relic.GetEnergyCost().ToString();

            bool matches = relic.MatchesRole(unitRole);
            card.matchLabel.text = matches ? "★ MATCH" : "";
            card.matchLabel.gameObject.SetActive(matches);

            int copies = relic.baseWeaponData != null ? relic.baseWeaponData.cardCopies : 1;
            card.copiesLabel.text = copies > 1 ? $"x{copies}" : "";

            // DARK background
            card.baseColor = cardBgColor;
            card.background.color = cardBgColor;
            card.border.color = matches ? matchGoldColor : cardBorderColor;
            card.topBar.color = new Color(0.9f, 0.75f, 0.4f);
        }

        private void SetCategoryCard(CardVisual card, EquippedRelic relic, UnitRole unitRole, int slotIndex)
        {
            bool isUltimate = slotIndex == FlexibleUnitEquipment.ULTIMATE_SLOT;
            bool isPassive = relic.IsPassive();

            card.typeLabel.text = relic.category.ToString().ToUpper();
            
            string effectName = relic.effectData != null ? relic.effectData.effectName : "";
            if (string.IsNullOrEmpty(effectName)) effectName = relic.relicName;
            card.nameLabel.text = effectName;
            card.nameLabel.color = Color.white;
            
            card.roleLabel.text = GetRoleName(relic.roleTag);
            card.costLabel.text = isPassive ? "-" : relic.GetEnergyCost().ToString();

            bool matches = relic.MatchesRole(unitRole);
            card.matchLabel.text = matches ? "★ MATCH" : "";
            card.matchLabel.gameObject.SetActive(matches);

            int copies = relic.GetCopies();
            card.copiesLabel.text = (copies > 1 && !isPassive) ? $"x{copies}" : "";

            // Type label color based on category
            Color typeColor = relic.category switch
            {
                RelicCategory.Boots => new Color(0.4f, 0.8f, 0.5f),
                RelicCategory.Gloves => new Color(0.8f, 0.6f, 0.4f),
                RelicCategory.Hat => new Color(0.5f, 0.6f, 0.9f),
                RelicCategory.Coat => new Color(0.7f, 0.5f, 0.8f),
                RelicCategory.Totem => new Color(0.8f, 0.4f, 0.6f),
                RelicCategory.Trinket => new Color(0.4f, 0.7f, 0.7f),
                RelicCategory.Ultimate => new Color(1f, 0.7f, 0.3f),
                RelicCategory.PassiveUnique => new Color(0.5f, 0.8f, 0.5f),
                _ => new Color(0.7f, 0.7f, 0.7f)
            };

            card.typeLabel.color = typeColor;
            card.topBar.color = typeColor;

            // DARK background always
            card.baseColor = cardBgColor;
            card.background.color = cardBgColor;
            card.border.color = matches ? matchGoldColor : cardBorderColor;
        }

        private void SetEmptyCard(CardVisual card, int slotIndex)
        {
            string label = slotIndex switch
            {
                5 => "ULTIMATE",
                6 => "PASSIVE",
                _ => $"SLOT {slotIndex + 1}"
            };

            card.typeLabel.text = label;
            card.typeLabel.color = new Color(0.4f, 0.4f, 0.4f);
            card.nameLabel.text = "Empty";
            card.nameLabel.color = new Color(0.4f, 0.4f, 0.4f);
            card.roleLabel.text = "";
            card.costLabel.text = "-";
            card.matchLabel.text = "";
            card.matchLabel.gameObject.SetActive(false);
            card.copiesLabel.text = "";

            card.baseColor = emptyBgColor;
            card.background.color = emptyBgColor;
            card.border.color = new Color(0.2f, 0.2f, 0.22f);
            card.topBar.color = new Color(0.2f, 0.2f, 0.22f);
            card.slotData = null;
        }

        private void UpdateCardStates()
        {
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();

            int energy = energyManager?.CurrentEnergy ?? 0;

            foreach (var card in cards)
            {
                var slot = card.slotData;

                if (slot == null || slot.IsEmpty)
                {
                    card.disabledOverlay.SetActive(true);
                    card.button.interactable = false;
                    continue;
                }

                bool isPassive = slot.IsPassive();
                int cost = slot.GetEnergyCost();
                bool canPlay = energy >= cost && !isPassive;

                card.disabledOverlay.SetActive(!canPlay);
                card.button.interactable = canPlay;
            }
        }
        #endregion

        #region Card Interaction
        private void OnCardHoverEnter(int index)
        {
            if (index < 0 || index >= cards.Count) return;

            CardVisual card = cards[index];
            if (card.slotData == null || card.slotData.IsEmpty) return;

            hoveredIndex = index;

            card.targetPosition = card.basePosition + new Vector2(0, hoverLift);
            card.targetRotation = 0;
            card.targetScale = hoverScale;
            card.background.color = cardHoverColor;
            card.root.transform.SetAsLastSibling();
        }

        private void OnCardHoverExit(int index)
        {
            if (index < 0 || index >= cards.Count) return;

            CardVisual card = cards[index];
            hoveredIndex = -1;

            card.targetPosition = card.basePosition;
            card.targetRotation = card.baseRotation;
            card.targetScale = 1f;
            card.background.color = card.baseColor;
            card.root.transform.SetSiblingIndex(index);
        }

        private void OnCardClicked(int index)
        {
            if (index < 0 || index >= cards.Count) return;

            CardVisual card = cards[index];
            var slot = card.slotData;

            if (slot == null || slot.IsEmpty || slot.IsPassive()) return;

            int cost = slot.GetEnergyCost();
            if (!energyManager.TrySpendEnergy(cost))
            {
                Debug.Log("Not enough energy!");
                return;
            }

            if (slot.hasWeapon && slot.weaponRelic != null)
                PlayWeaponCard(slot.weaponRelic);
            else if (slot.categoryRelic != null)
                PlayCategoryCard(slot.categoryRelic);

            UpdateCardStates();
        }

        private void PlayWeaponCard(WeaponRelic relic)
        {
            Debug.Log($"<color=yellow>Playing weapon: {relic.relicName}</color>");
            UnitAttack attack = selectedUnit?.GetComponent<UnitAttack>();
            if (attack != null)
                attack.ExecuteCardAttack(relic, energyAlreadySpent: true);
        }

        private void PlayCategoryCard(EquippedRelic relic)
        {
            Debug.Log($"<color=cyan>Playing relic: {relic.relicName} ({relic.category})</color>");
            UnitStatus target = FindClosestEnemy();
            RelicEffectExecutor.Execute(relic, selectedStatus, target, null);
        }

        private UnitStatus FindClosestEnemy()
        {
            if (selectedStatus == null) return null;

            UnitStatus[] units = FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            UnitStatus closest = null;
            float minDist = float.MaxValue;

            foreach (var u in units)
            {
                if (u == null || u.Team == selectedStatus.Team || u.HasSurrendered) continue;
                float d = Vector3.Distance(selectedStatus.transform.position, u.transform.position);
                if (d < minDist) { minDist = d; closest = u; }
            }

            return closest;
        }
        #endregion

        #region Helpers
        private string GetRoleName(UnitRole role)
        {
            return role switch
            {
                UnitRole.MasterGunner => "Master Gunner",
                UnitRole.MasterAtArms => "Master-at-Arms",
                _ => role.ToString()
            };
        }
        #endregion
    }
}