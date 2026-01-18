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
    /// Card UI that works with FlexibleUnitEquipment.
    /// Shows whatever relics are equipped in each slot - no hardcoded categories!
    /// 
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
        [SerializeField] private float cardSpacing = 15f;
        [SerializeField] private float bottomOffset = 30f;
        [SerializeField] private float hoverLift = 30f;
        [SerializeField] private float hoverScale = 1.15f;

        [Header("Colors")]
        [SerializeField] private Color weaponColor = new Color(0.7f, 0.25f, 0.2f);
        [SerializeField] private Color bootsColor = new Color(0.2f, 0.5f, 0.3f);
        [SerializeField] private Color glovesColor = new Color(0.5f, 0.35f, 0.2f);
        [SerializeField] private Color hatColor = new Color(0.3f, 0.3f, 0.6f);
        [SerializeField] private Color coatColor = new Color(0.4f, 0.25f, 0.5f);
        [SerializeField] private Color totemColor = new Color(0.5f, 0.2f, 0.4f);
        [SerializeField] private Color trinketColor = new Color(0.3f, 0.45f, 0.45f);
        [SerializeField] private Color ultimateColor = new Color(0.6f, 0.4f, 0.1f);
        [SerializeField] private Color passiveColor = new Color(0.2f, 0.4f, 0.2f);
        [SerializeField] private Color emptyColor = new Color(0.12f, 0.12f, 0.15f);
        [SerializeField] private Color hoverTint = new Color(1.3f, 1.3f, 1.3f);
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

        private EnergyManager energyManager;
        #endregion

        #region Card Visual Class
        private class CardVisual
        {
            public GameObject root;
            public RectTransform rect;
            public Image background;
            public Image border;
            public TMP_Text typeLabel;
            public TMP_Text nameLabel;
            public TMP_Text descLabel;
            public TMP_Text costLabel;
            public TMP_Text roleLabel;
            public TMP_Text matchLabel;
            public GameObject disabledOverlay;
            public Button button;

            public int slotIndex;
            public Color baseColor;
            public Vector2 basePosition;

            // Reference to the slot data
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
            GameEvents.OnEnergyChanged += _ => UpdateCardStates();
            GameEvents.OnPlayerTurnEnd += Hide;
            GameEvents.OnEnemyTurnStart += Hide;
        }

        private void OnDestroy()
        {
            GameEvents.OnUnitSelected -= OnUnitSelected;
            GameEvents.OnUnitDeselected -= OnUnitDeselected;
            GameEvents.OnEnergyChanged -= _ => UpdateCardStates();
            GameEvents.OnPlayerTurnEnd -= Hide;
            GameEvents.OnEnemyTurnStart -= Hide;

            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X) && selectedUnit != null)
            {
                if (isVisible) Hide();
                else Show();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && isVisible)
                Hide();

            // Smooth animations
            if (isVisible)
                AnimateCards();
        }
        #endregion

        #region UI Creation
        private void CreateUI()
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("FlexibleCardCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            cardContainer = new GameObject("FlexibleCardContainer");
            cardContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, bottomOffset);

            // Create 7 cards
            float totalWidth = 7 * cardWidth + 6 * cardSpacing;
            float startX = -totalWidth / 2 + cardWidth / 2;

            for (int i = 0; i < 7; i++)
            {
                CardVisual card = CreateCard(i);
                card.basePosition = new Vector2(startX + i * (cardWidth + cardSpacing), 0);
                card.rect.anchoredPosition = card.basePosition;
                cards.Add(card);
            }
        }

        private CardVisual CreateCard(int index)
        {
            CardVisual card = new CardVisual();
            card.slotIndex = index;

            // Root
            card.root = new GameObject($"Card_{index}");
            card.root.transform.SetParent(cardContainer.transform, false);

            card.rect = card.root.AddComponent<RectTransform>();
            card.rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            card.rect.pivot = new Vector2(0.5f, 0);

            // Background
            card.background = card.root.AddComponent<Image>();
            card.background.color = emptyColor;
            card.baseColor = emptyColor;

            // Border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(card.root.transform, false);
            card.border = borderObj.AddComponent<Image>();
            card.border.color = new Color(0.4f, 0.4f, 0.4f);
            card.border.raycastTarget = false;
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(4, 4);

            // Type label (top)
            card.typeLabel = CreateText(card.root.transform, "Type", "", 11, FontStyles.Bold,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10));

            // Name label (middle-top)
            card.nameLabel = CreateText(card.root.transform, "Name", "", 13, FontStyles.Bold,
                new Vector2(0, 0.55f), new Vector2(1, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero);
            card.nameLabel.enableWordWrapping = true;

            // Description (middle)
            card.descLabel = CreateText(card.root.transform, "Desc", "", 9, FontStyles.Normal,
                new Vector2(0, 0.25f), new Vector2(1, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero);
            card.descLabel.enableWordWrapping = true;
            card.descLabel.color = new Color(0.8f, 0.8f, 0.8f);

            // Role label (bottom area)
            card.roleLabel = CreateText(card.root.transform, "Role", "", 10, FontStyles.Italic,
                new Vector2(0, 0.12f), new Vector2(1, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero);
            card.roleLabel.color = new Color(0.7f, 0.7f, 0.7f);

            // Match indicator
            card.matchLabel = CreateText(card.root.transform, "Match", "", 10, FontStyles.Bold,
                new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-5, 8));
            card.matchLabel.alignment = TextAlignmentOptions.Right;
            card.matchLabel.color = new Color(1f, 0.85f, 0.2f);

            // Cost (bottom left)
            GameObject costBg = new GameObject("CostBg");
            costBg.transform.SetParent(card.root.transform, false);
            Image costBgImg = costBg.AddComponent<Image>();
            costBgImg.color = new Color(0.15f, 0.35f, 0.6f);
            costBgImg.raycastTarget = false;
            RectTransform costBgRect = costBg.GetComponent<RectTransform>();
            costBgRect.anchorMin = costBgRect.anchorMax = Vector2.zero;
            costBgRect.pivot = Vector2.zero;
            costBgRect.anchoredPosition = new Vector2(5, 5);
            costBgRect.sizeDelta = new Vector2(26, 26);

            card.costLabel = CreateText(card.root.transform, "Cost", "1", 14, FontStyles.Bold,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 18));

            // Disabled overlay
            card.disabledOverlay = new GameObject("Disabled");
            card.disabledOverlay.transform.SetParent(card.root.transform, false);
            Image overlayImg = card.disabledOverlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.65f);
            overlayImg.raycastTarget = false;
            RectTransform overlayRect = card.disabledOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            card.disabledOverlay.SetActive(false);

            // Button + events
            card.button = card.root.AddComponent<Button>();
            card.button.targetGraphic = card.background;

            int idx = index;
            card.button.onClick.AddListener(() => OnCardClicked(idx));

            EventTrigger trigger = card.root.AddComponent<EventTrigger>();
            
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => OnHover(idx, true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => OnHover(idx, false));
            trigger.triggers.Add(exitEntry);

            return card;
        }

        private TMP_Text CreateText(Transform parent, string name, string text, int size, FontStyles style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position)
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

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(-8, 0);

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
                ClearSelection();
                return;
            }

            selectedUnit = unit;
            selectedStatus = status;
            selectedEquipment = unit.GetComponent<FlexibleUnitEquipment>();

            // Fallback: try old component and log warning
            if (selectedEquipment == null)
            {
                Debug.LogWarning($"[FlexibleCardUI] No FlexibleUnitEquipment on {unit.name}! Make sure DeploymentManager uses the new component.");
            }

            if (isVisible)
            {
                PopulateCards();
                UpdateCardStates();
            }
        }

        private void OnUnitDeselected()
        {
            ClearSelection();
        }

        private void ClearSelection()
        {
            selectedUnit = null;
            selectedStatus = null;
            selectedEquipment = null;
            Hide();
        }
        #endregion

        #region Show/Hide
        public void Show()
        {
            if (selectedUnit == null || selectedEquipment == null)
            {
                Debug.LogWarning("[FlexibleCardUI] Cannot show - no unit or equipment");
                return;
            }

            isVisible = true;
            cardContainer.SetActive(true);
            PopulateCards();
            UpdateCardStates();
        }

        public void Hide()
        {
            isVisible = false;
            hoveredIndex = -1;
            if (cardContainer != null)
                cardContainer.SetActive(false);
        }
        #endregion

        #region Card Population
        private void PopulateCards()
        {
            Debug.Log("<color=lime>[FlexibleCardUI] Populating cards...</color>");

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
                {
                    SetWeaponCard(card, slot.weaponRelic, unitRole);
                }
                else if (slot.categoryRelic != null)
                {
                    SetCategoryCard(card, slot.categoryRelic, unitRole, i);
                }
                else
                {
                    SetEmptyCard(card, i);
                }
            }

            selectedEquipment.LogEquipmentState();
        }

        private void SetWeaponCard(CardVisual card, WeaponRelic relic, UnitRole unitRole)
        {
            card.typeLabel.text = "WEAPON";
            card.typeLabel.color = new Color(1f, 0.8f, 0.3f);

            card.nameLabel.text = relic.baseWeaponData?.weaponName ?? relic.weaponFamily.ToString();
            card.descLabel.text = TruncateText(relic.effectData.description ?? "", 60);
            card.roleLabel.text = GetRoleName(relic.roleTag);
            card.costLabel.text = relic.GetEnergyCost().ToString();

            bool matches = relic.MatchesRole(unitRole);
            card.matchLabel.text = matches ? "★ MATCH" : "";
            card.matchLabel.gameObject.SetActive(matches);

            card.baseColor = weaponColor;
            card.background.color = weaponColor;
            card.border.color = matches ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.3f, 0.3f);
        }

        private void SetCategoryCard(CardVisual card, EquippedRelic relic, UnitRole unitRole, int slotIndex)
        {
            bool isUltimate = slotIndex == FlexibleUnitEquipment.ULTIMATE_SLOT;
            bool isPassiveSlot = slotIndex == FlexibleUnitEquipment.PASSIVE_SLOT;
            bool isPassive = relic.IsPassive();

            card.typeLabel.text = relic.category.ToString().ToUpper();
            card.nameLabel.text = relic.effectData?.effectName ?? relic.relicName;
            card.descLabel.text = TruncateText(relic.effectData?.description ?? "", 60);
            card.roleLabel.text = GetRoleName(relic.roleTag);
            card.costLabel.text = isPassive ? "-" : relic.GetEnergyCost().ToString();

            bool matches = relic.MatchesRole(unitRole);
            card.matchLabel.text = matches ? "★ MATCH" : "";
            card.matchLabel.gameObject.SetActive(matches);

            // Color based on category
            Color baseCol = GetCategoryColor(relic.category);
            if (isUltimate) baseCol = ultimateColor;
            if (isPassive || isPassiveSlot) baseCol = passiveColor;

            card.baseColor = baseCol;
            card.background.color = baseCol;
            card.border.color = matches ? new Color(1f, 0.85f, 0.2f) : new Color(0.35f, 0.35f, 0.4f);

            // Type label color
            if (isPassive)
                card.typeLabel.color = new Color(0.6f, 0.9f, 0.6f);
            else if (isUltimate)
                card.typeLabel.color = new Color(1f, 0.7f, 0.3f);
            else
                card.typeLabel.color = Color.white;
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
            card.descLabel.text = "No relic equipped";
            card.roleLabel.text = "";
            card.costLabel.text = "-";
            card.matchLabel.text = "";
            card.matchLabel.gameObject.SetActive(false);

            card.baseColor = emptyColor;
            card.background.color = emptyColor;
            card.border.color = new Color(0.25f, 0.25f, 0.25f);
            card.slotData = null;
        }

        private Color GetCategoryColor(RelicCategory category)
        {
            return category switch
            {
                RelicCategory.Boots => bootsColor,
                RelicCategory.Gloves => glovesColor,
                RelicCategory.Hat => hatColor,
                RelicCategory.Coat => coatColor,
                RelicCategory.Totem => totemColor,
                RelicCategory.Trinket => trinketColor,
                RelicCategory.Ultimate => ultimateColor,
                RelicCategory.PassiveUnique => passiveColor,
                _ => emptyColor
            };
        }

        private void UpdateCardStates()
        {
            if (energyManager == null)
                energyManager = ServiceLocator.Get<EnergyManager>();

            int currentEnergy = energyManager?.CurrentEnergy ?? 0;

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
                bool canAfford = currentEnergy >= cost;
                bool canPlay = canAfford && !isPassive;

                card.disabledOverlay.SetActive(!canPlay);
                card.button.interactable = canPlay;
            }
        }
        #endregion

        #region Card Interaction
        private void OnHover(int index, bool enter)
        {
            if (index < 0 || index >= cards.Count) return;

            if (enter)
            {
                hoveredIndex = index;
                cards[index].root.transform.SetAsLastSibling();
            }
            else
            {
                hoveredIndex = -1;
                cards[index].root.transform.SetSiblingIndex(index);
            }
        }

        private void AnimateCards()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool isHovered = (i == hoveredIndex) && card.button.interactable;

                Vector2 targetPos = isHovered ? card.basePosition + new Vector2(0, hoverLift) : card.basePosition;
                float targetScale = isHovered ? hoverScale : 1f;
                Color targetColor = isHovered ? card.baseColor * hoverTint : card.baseColor;

                card.rect.anchoredPosition = Vector2.Lerp(card.rect.anchoredPosition, targetPos, Time.deltaTime * 10f);
                card.rect.localScale = Vector3.Lerp(card.rect.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);
                card.background.color = Color.Lerp(card.background.color, targetColor, Time.deltaTime * 10f);
            }
        }

        private void OnCardClicked(int index)
        {
            if (index < 0 || index >= cards.Count) return;

            var card = cards[index];
            var slot = card.slotData;

            if (slot == null || slot.IsEmpty || slot.IsPassive()) return;

            int cost = slot.GetEnergyCost();
            if (!energyManager.TrySpendEnergy(cost))
            {
                Debug.Log("Not enough energy!");
                return;
            }

            if (slot.hasWeapon && slot.weaponRelic != null)
            {
                PlayWeaponCard(slot.weaponRelic);
            }
            else if (slot.categoryRelic != null)
            {
                PlayCategoryCard(slot.categoryRelic);
            }

            UpdateCardStates();
        }

        private void PlayWeaponCard(WeaponRelic relic)
        {
            Debug.Log($"<color=yellow>Playing weapon: {relic.relicName}</color>");

            UnitAttack attack = selectedUnit?.GetComponent<UnitAttack>();
            if (attack != null)
            {
                attack.ExecuteCardAttack(relic, energyAlreadySpent: true);
            }
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
                if (d < minDist)
                {
                    minDist = d;
                    closest = u;
                }
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

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }
        #endregion
    }
}