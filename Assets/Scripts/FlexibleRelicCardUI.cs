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
    public class FlexibleRelicCardUI : MonoBehaviour
    {
        public static FlexibleRelicCardUI Instance { get; private set; }

        [Header("Card Dimensions")]
        [SerializeField] private float cardWidth = 140f;
        [SerializeField] private float cardHeight = 200f;
        [SerializeField] private float cardSpacing = 10f;
        [SerializeField] private float fanAngle = 3f;
        [SerializeField] private float hoverLift = 40f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float animationSpeed = 8f;
        [SerializeField] private float bottomOffset = 20f;

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

        private class CardVisual
        {
            public GameObject root;
            public RectTransform rect;
            public Image background;
            public TMP_Text typeLabel;
            public TMP_Text nameLabel;
            public TMP_Text costLabel;
            public TMP_Text roleLabel;
            public TMP_Text matchLabel;
            public TMP_Text copiesLabel;
            public GameObject disabledOverlay;

            public int slotIndex;
            public Vector2 basePosition;
            public float baseRotation;
            public Vector2 targetPosition;
            public float targetRotation;
            public float targetScale;
            public FlexibleUnitEquipment.RelicSlot slotData;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            energyManager = ServiceLocator.Get<EnergyManager>();
            CreateUI();
            Hide();

            GameEvents.OnUnitSelected += OnUnitSelected;
            GameEvents.OnUnitDeselected += OnUnitDeselected;
            GameEvents.OnEnergyChanged += e => { if (isVisible) UpdateCardStates(); };
            GameEvents.OnPlayerTurnEnd += FullHide;
            GameEvents.OnEnemyTurnStart += FullHide;
        }

        private void OnDestroy()
        {
            GameEvents.OnUnitSelected -= OnUnitSelected;
            GameEvents.OnUnitDeselected -= OnUnitDeselected;
            GameEvents.OnPlayerTurnEnd -= FullHide;
            GameEvents.OnEnemyTurnStart -= FullHide;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.X) && pendingUnit != null)
            {
                if (isVisible) Hide(); else ShowCards();
            }
            if (Input.GetKeyDown(KeyCode.Escape) && isVisible) Hide();

            if (!isVisible) return;

            foreach (var card in cards)
            {
                if (card.root == null) continue;
                card.rect.anchoredPosition = Vector2.Lerp(card.rect.anchoredPosition, card.targetPosition, Time.deltaTime * animationSpeed);
                
                float rot = card.rect.localEulerAngles.z;
                if (rot > 180) rot -= 360;
                card.rect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rot, card.targetRotation, Time.deltaTime * animationSpeed));
                
                float scale = Mathf.Lerp(card.rect.localScale.x, card.targetScale, Time.deltaTime * animationSpeed);
                card.rect.localScale = Vector3.one * scale;
            }
        }

        private void CreateUI()
        {
            GameObject canvasObj = new GameObject("RelicCardCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(canvas.transform, false);
            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, bottomOffset);
            containerRect.sizeDelta = new Vector2(1000, cardHeight + 50);

            for (int i = 0; i < 7; i++) CreateCard(i);
        }

        private void CreateCard(int index)
        {
            CardVisual card = new CardVisual();
            card.slotIndex = index;

            card.root = new GameObject($"Card_{index}");
            card.root.transform.SetParent(cardContainer.transform, false);
            card.rect = card.root.AddComponent<RectTransform>();
            card.rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            card.rect.pivot = new Vector2(0.5f, 0);

            // Background - plain dark gray
            card.background = card.root.AddComponent<Image>();
            card.background.color = new Color(0.18f, 0.18f, 0.2f);

            // Type label
            card.typeLabel = CreateText(card.root.transform, "Type", "");
            RectTransform typeRect = card.typeLabel.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0, 1);
            typeRect.anchorMax = new Vector2(1, 1);
            typeRect.pivot = new Vector2(0.5f, 1);
            typeRect.anchoredPosition = new Vector2(0, -10);
            typeRect.sizeDelta = new Vector2(-10, 20);
            card.typeLabel.fontSize = 11;
            card.typeLabel.fontStyle = FontStyles.Bold;
            card.typeLabel.color = new Color(0.7f, 0.7f, 0.7f);

            // Name label
            card.nameLabel = CreateText(card.root.transform, "Name", "");
            RectTransform nameRect = card.nameLabel.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.4f);
            nameRect.anchorMax = new Vector2(1, 0.75f);
            nameRect.sizeDelta = new Vector2(-10, 0);
            card.nameLabel.fontSize = 12;
            card.nameLabel.fontStyle = FontStyles.Bold;
            card.nameLabel.enableWordWrapping = true;

            // Role label
            card.roleLabel = CreateText(card.root.transform, "Role", "");
            RectTransform roleRect = card.roleLabel.GetComponent<RectTransform>();
            roleRect.anchorMin = new Vector2(0, 0.25f);
            roleRect.anchorMax = new Vector2(1, 0.4f);
            roleRect.sizeDelta = new Vector2(-10, 0);
            card.roleLabel.fontSize = 10;
            card.roleLabel.fontStyle = FontStyles.Italic;
            card.roleLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // Match label
            card.matchLabel = CreateText(card.root.transform, "Match", "");
            RectTransform matchRect = card.matchLabel.GetComponent<RectTransform>();
            matchRect.anchorMin = new Vector2(0, 0.12f);
            matchRect.anchorMax = new Vector2(1, 0.25f);
            matchRect.sizeDelta = new Vector2(-10, 0);
            card.matchLabel.fontSize = 10;
            card.matchLabel.fontStyle = FontStyles.Bold;
            card.matchLabel.color = new Color(1f, 0.85f, 0.4f);

            // Cost background
            GameObject costBg = new GameObject("CostBg");
            costBg.transform.SetParent(card.root.transform, false);
            Image costBgImg = costBg.AddComponent<Image>();
            costBgImg.color = new Color(0.25f, 0.4f, 0.6f);
            costBgImg.raycastTarget = false;
            RectTransform costBgRect = costBg.GetComponent<RectTransform>();
            costBgRect.anchorMin = costBgRect.anchorMax = Vector2.zero;
            costBgRect.pivot = Vector2.zero;
            costBgRect.anchoredPosition = new Vector2(6, 6);
            costBgRect.sizeDelta = new Vector2(26, 26);

            // Cost label
            card.costLabel = CreateText(card.root.transform, "Cost", "1");
            RectTransform costRect = card.costLabel.GetComponent<RectTransform>();
            costRect.anchorMin = costRect.anchorMax = Vector2.zero;
            costRect.pivot = Vector2.zero;
            costRect.anchoredPosition = new Vector2(6, 6);
            costRect.sizeDelta = new Vector2(26, 26);
            card.costLabel.fontSize = 14;
            card.costLabel.fontStyle = FontStyles.Bold;

            // Copies label
            card.copiesLabel = CreateText(card.root.transform, "Copies", "");
            RectTransform copiesRect = card.copiesLabel.GetComponent<RectTransform>();
            copiesRect.anchorMin = copiesRect.anchorMax = new Vector2(1, 0);
            copiesRect.pivot = new Vector2(1, 0);
            copiesRect.anchoredPosition = new Vector2(-8, 8);
            copiesRect.sizeDelta = new Vector2(30, 20);
            card.copiesLabel.fontSize = 10;
            card.copiesLabel.color = new Color(0.6f, 0.6f, 0.6f);

            // Disabled overlay
            card.disabledOverlay = new GameObject("Disabled");
            card.disabledOverlay.transform.SetParent(card.root.transform, false);
            Image overlayImg = card.disabledOverlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.5f);
            overlayImg.raycastTarget = false;
            RectTransform overlayRect = card.disabledOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            card.disabledOverlay.SetActive(false);

            // Event trigger for hover and click
            EventTrigger trigger = card.root.AddComponent<EventTrigger>();
            int idx = index;

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => OnHover(idx, true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => OnHover(idx, false));
            trigger.triggers.Add(exitEntry);

            var clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => OnClick(idx));
            trigger.triggers.Add(clickEntry);

            cards.Add(card);
        }

        private TMP_Text CreateText(Transform parent, string name, string text)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void OnUnitSelected(GameObject unit)
        {
            if (unit == null) return;
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null || status.Team != Team.Player) { pendingUnit = null; Hide(); return; }
            pendingUnit = unit;
            if (isVisible && selectedUnit != unit) Hide();
        }

        private void OnUnitDeselected() { pendingUnit = null; Hide(); }
        private void FullHide() { pendingUnit = null; Hide(); }

        private void ShowCards()
        {
            if (pendingUnit == null) return;
            selectedUnit = pendingUnit;
            selectedStatus = selectedUnit.GetComponent<UnitStatus>();
            selectedEquipment = selectedUnit.GetComponent<FlexibleUnitEquipment>();
            if (selectedEquipment == null) { Debug.LogWarning($"No FlexibleUnitEquipment on {selectedUnit.name}"); return; }

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
            if (cardContainer != null) cardContainer.SetActive(false);
        }

        private void PositionCards()
        {
            int count = cards.Count;
            float totalWidth = count * cardWidth + (count - 1) * cardSpacing;
            float startX = -totalWidth / 2f + cardWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                var card = cards[i];
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

        private void PopulateCards()
        {
            UnitRole unitRole = selectedStatus?.Role ?? UnitRole.Deckhand;

            for (int i = 0; i < 7; i++)
            {
                var card = cards[i];
                var slot = selectedEquipment.GetSlot(i);
                card.slotData = slot;

                // Reset to dark background
                card.background.color = new Color(0.18f, 0.18f, 0.2f);

                if (slot == null || slot.IsEmpty)
                {
                    card.typeLabel.text = i == 5 ? "ULTIMATE" : i == 6 ? "PASSIVE" : $"SLOT {i + 1}";
                    card.nameLabel.text = "Empty";
                    card.roleLabel.text = "";
                    card.costLabel.text = "-";
                    card.matchLabel.text = "";
                    card.copiesLabel.text = "";
                    card.background.color = new Color(0.12f, 0.12f, 0.14f);
                    continue;
                }

                if (slot.hasWeapon && slot.weaponRelic != null)
                {
                    var relic = slot.weaponRelic;
                    card.typeLabel.text = "WEAPON";
                    card.nameLabel.text = relic.baseWeaponData != null ? relic.baseWeaponData.weaponName : relic.weaponFamily.ToString();
                    card.roleLabel.text = relic.roleTag.ToString();
                    card.costLabel.text = relic.GetEnergyCost().ToString();
                    bool match = relic.MatchesRole(unitRole);
                    card.matchLabel.text = match ? "★ MATCH" : "";
                    int copies = relic.baseWeaponData != null ? relic.baseWeaponData.cardCopies : 1;
                    card.copiesLabel.text = copies > 1 ? $"x{copies}" : "";
                }
                else if (slot.categoryRelic != null)
                {
                    var relic = slot.categoryRelic;
                    bool isPassive = relic.IsPassive();
                    card.typeLabel.text = relic.category.ToString().ToUpper();
                    string name = relic.effectData != null ? relic.effectData.effectName : relic.relicName;
                    card.nameLabel.text = string.IsNullOrEmpty(name) ? relic.relicName : name;
                    card.roleLabel.text = relic.roleTag.ToString();
                    card.costLabel.text = isPassive ? "-" : relic.GetEnergyCost().ToString();
                    bool match = relic.MatchesRole(unitRole);
                    card.matchLabel.text = match ? "★ MATCH" : "";
                    int copies = relic.GetCopies();
                    card.copiesLabel.text = (copies > 1 && !isPassive) ? $"x{copies}" : "";
                }
            }
        }

        private void UpdateCardStates()
        {
            if (energyManager == null) energyManager = ServiceLocator.Get<EnergyManager>();
            int energy = energyManager?.CurrentEnergy ?? 0;

            foreach (var card in cards)
            {
                var slot = card.slotData;
                if (slot == null || slot.IsEmpty)
                {
                    card.disabledOverlay.SetActive(true);
                    continue;
                }
                bool isPassive = slot.IsPassive();
                int cost = slot.GetEnergyCost();
                bool canPlay = energy >= cost && !isPassive;
                card.disabledOverlay.SetActive(!canPlay);
            }
        }

        private void OnHover(int index, bool enter)
        {
            if (index < 0 || index >= cards.Count) return;
            var card = cards[index];
            
            if (enter && card.slotData != null && !card.slotData.IsEmpty)
            {
                hoveredIndex = index;
                card.targetPosition = card.basePosition + new Vector2(0, hoverLift);
                card.targetRotation = 0;
                card.targetScale = hoverScale;
                card.background.color = new Color(0.25f, 0.25f, 0.28f);
                card.root.transform.SetAsLastSibling();
            }
            else
            {
                hoveredIndex = -1;
                card.targetPosition = card.basePosition;
                card.targetRotation = card.baseRotation;
                card.targetScale = 1f;
                card.background.color = (card.slotData == null || card.slotData.IsEmpty) 
                    ? new Color(0.12f, 0.12f, 0.14f) 
                    : new Color(0.18f, 0.18f, 0.2f);
                card.root.transform.SetSiblingIndex(index);
            }
        }

        private void OnClick(int index)
        {
            if (index < 0 || index >= cards.Count) return;
            var card = cards[index];
            var slot = card.slotData;

            if (slot == null || slot.IsEmpty || slot.IsPassive()) return;

            int cost = slot.GetEnergyCost();
            if (!energyManager.TrySpendEnergy(cost)) { Debug.Log("Not enough energy!"); return; }

            if (slot.hasWeapon && slot.weaponRelic != null)
            {
                Debug.Log($"Playing weapon: {slot.weaponRelic.relicName}");
                var attack = selectedUnit?.GetComponent<UnitAttack>();
                if (attack != null) attack.ExecuteCardAttack(slot.weaponRelic, energyAlreadySpent: true);
            }
            else if (slot.categoryRelic != null)
            {
                Debug.Log($"Playing relic: {slot.categoryRelic.relicName}");
                UnitStatus target = FindClosestEnemy();
                RelicEffectExecutor.Execute(slot.categoryRelic, selectedStatus, target, null);
            }

            UpdateCardStates();
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
    }
}