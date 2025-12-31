using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using TacticalGame.Equipment;
using TacticalGame.Enums;
using TacticalGame.Managers;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI component for a single relic slot with 3 jewel sockets.
    /// </summary>
    public class RelicSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields

        [Header("Slot Identity")]
        [SerializeField] private TMP_Text slotLabelText;

        [Header("Relic Display")]
        [SerializeField] private Image relicBackground;
        [SerializeField] private Image relicIcon;
        [SerializeField] private TMP_Text relicNameText;
        [SerializeField] private TMP_Text relicEffectText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image roleMatchIndicator;

        [Header("Jewel Sockets")]
        [SerializeField] private Image jewelSocket1;
        [SerializeField] private Image jewelSocket2;
        [SerializeField] private Image jewelSocket3;
        [SerializeField] private Button jewelButton1;
        [SerializeField] private Button jewelButton2;
        [SerializeField] private Button jewelButton3;

        [Header("Selection Highlight")]
        [SerializeField] private Image selectionOutline;

        [Header("Colors")]
        [SerializeField] private Color emptySlotColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color filledSlotColor = new Color(0.2f, 0.2f, 0.3f);
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.25f, 0.35f);
        [SerializeField] private Color emptyJewelColor = new Color(0.1f, 0.1f, 0.12f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color roleMatchColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color jewelHighlightColor = new Color(0.5f, 1f, 0.5f);

        [Header("Rarity Colors")]
        [SerializeField] private Color commonColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color uncommonColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color rareColor = new Color(0.4f, 0.4f, 1f);

        #endregion

        #region Private State

        private string slotLabel;
        private RelicSlotType slotType;
        private int slotIndex;
        private bool isEmpty = true;
        private bool isHighlighted = false;
        private int highlightedJewelIndex = -1;

        private Action<RelicSlotUI> onSlotClicked;
        private Action<RelicSlotUI, int> onJewelClicked;

        // Store original jewel colors for highlight reset
        private Color[] originalJewelColors = new Color[3];

        #endregion

        #region Public Properties

        public string SlotLabel => slotLabel;
        public RelicSlotType SlotType => slotType;
        public int SlotIndex { get => slotIndex; set => slotIndex = value; }
        public bool IsEmpty => isEmpty;

        #endregion

        #region Setup

        public void Setup(string label, RelicSlotType type, int index,
                         Action<RelicSlotUI> slotClickCallback,
                         Action<RelicSlotUI, int> jewelClickCallback)
        {
            slotLabel = label;
            slotType = type;
            slotIndex = index;
            onSlotClicked = slotClickCallback;
            onJewelClicked = jewelClickCallback;

            if (slotLabelText != null)
                slotLabelText.text = label;

            if (jewelButton1 != null)
                jewelButton1.onClick.AddListener(() => OnJewelSocketClicked(0));
            if (jewelButton2 != null)
                jewelButton2.onClick.AddListener(() => OnJewelSocketClicked(1));
            if (jewelButton3 != null)
                jewelButton3.onClick.AddListener(() => OnJewelSocketClicked(2));

            SetEmpty();
            SetHighlight(false);
        }

        #endregion

        #region Display Methods

        public void SetEmpty()
        {
            isEmpty = true;

            if (relicBackground != null)
                relicBackground.color = emptySlotColor;

            if (relicIcon != null)
                relicIcon.enabled = false;

            if (relicNameText != null)
                relicNameText.text = "Empty";

            if (relicEffectText != null)
                relicEffectText.text = "Click to equip";

            if (rarityBorder != null)
                rarityBorder.color = emptySlotColor;

            if (roleMatchIndicator != null)
                roleMatchIndicator.enabled = false;

            SetJewelSocket(jewelSocket1, null, 0);
            SetJewelSocket(jewelSocket2, null, 1);
            SetJewelSocket(jewelSocket3, null, 2);
        }

        public void DisplayWeaponRelic(WeaponRelic relic, bool isRoleMatch)
        {
            if (relic == null)
            {
                SetEmpty();
                return;
            }

            isEmpty = false;

            if (relicBackground != null)
                relicBackground.color = filledSlotColor;

            if (relicIcon != null)
            {
                if (relic.baseWeaponData != null && relic.baseWeaponData.weaponIcon != null)
                {
                    relicIcon.sprite = relic.baseWeaponData.weaponIcon;
                    relicIcon.enabled = true;
                }
                else
                {
                    relicIcon.enabled = false;
                }
            }

            if (relicNameText != null)
            {
                string name = relic.relicName;
                if (name.Length > 18) name = name.Substring(0, 15) + "...";
                relicNameText.text = name;
            }

            if (relicEffectText != null)
                relicEffectText.text = relic.effectData.effectName;

            if (rarityBorder != null)
                rarityBorder.color = GetRarityColor(relic.effectData.rarity);

            if (roleMatchIndicator != null)
            {
                roleMatchIndicator.enabled = isRoleMatch;
                if (isRoleMatch) roleMatchIndicator.color = roleMatchColor;
            }
        }

        public void DisplayRelic(RelicData relic, bool isRoleMatch)
        {
            if (relic == null)
            {
                SetEmpty();
                return;
            }

            isEmpty = false;

            if (relicBackground != null)
                relicBackground.color = filledSlotColor;

            if (relicIcon != null && relic.relicIcon != null)
            {
                relicIcon.sprite = relic.relicIcon;
                relicIcon.enabled = true;
            }
            else if (relicIcon != null)
            {
                relicIcon.enabled = false;
            }

            if (relicNameText != null)
                relicNameText.text = relic.relicName;

            if (relicEffectText != null)
            {
                string effect = relic.effectDescription;
                if (effect.Length > 25) effect = effect.Substring(0, 22) + "...";
                relicEffectText.text = effect;
            }

            if (rarityBorder != null)
                rarityBorder.color = GetRarityColor(relic.rarity);

            if (roleMatchIndicator != null)
            {
                roleMatchIndicator.enabled = isRoleMatch;
                if (isRoleMatch) roleMatchIndicator.color = roleMatchColor;
            }
        }

        public void UpdateJewels(JewelData[] jewels)
        {
            if (jewels == null || jewels.Length < 3)
            {
                SetJewelSocket(jewelSocket1, null, 0);
                SetJewelSocket(jewelSocket2, null, 1);
                SetJewelSocket(jewelSocket3, null, 2);
                return;
            }

            SetJewelSocket(jewelSocket1, jewels[0], 0);
            SetJewelSocket(jewelSocket2, jewels[1], 1);
            SetJewelSocket(jewelSocket3, jewels[2], 2);
        }

        private void SetJewelSocket(Image socket, JewelData jewel, int index)
        {
            if (socket == null) return;

            Color color;
            if (jewel != null)
            {
                color = jewel.jewelColor != Color.white ? jewel.jewelColor : GetJewelTypeColor(jewel.type);
            }
            else
            {
                color = emptyJewelColor;
            }

            socket.color = color;
            originalJewelColors[index] = color;
        }

        private Color GetJewelTypeColor(JewelType type)
        {
            return type switch
            {
                JewelType.Damage => new Color(1f, 0.3f, 0.3f),
                JewelType.Defense => new Color(0.3f, 0.5f, 1f),
                JewelType.Utility => new Color(0.3f, 1f, 0.3f),
                JewelType.Duration => new Color(1f, 1f, 0.3f),
                JewelType.Cost => new Color(0.3f, 1f, 1f),
                JewelType.Target => new Color(1f, 0.5f, 0f),
                JewelType.Special => new Color(0.8f, 0.3f, 0.8f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
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

        #region Highlight Methods

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;

            if (selectionOutline != null)
            {
                selectionOutline.enabled = highlight;
                if (highlight) selectionOutline.color = highlightColor;
            }

            // Reset jewel highlights when slot highlight changes
            if (!highlight)
            {
                highlightedJewelIndex = -1;
                ResetJewelHighlights();
            }
        }

        public void HighlightJewelSlot(int jewelIndex)
        {
            ResetJewelHighlights();
            highlightedJewelIndex = jewelIndex;

            Image targetSocket = jewelIndex switch
            {
                0 => jewelSocket1,
                1 => jewelSocket2,
                2 => jewelSocket3,
                _ => null
            };

            if (targetSocket != null)
            {
                targetSocket.color = jewelHighlightColor;
            }
        }

        private void ResetJewelHighlights()
        {
            if (jewelSocket1 != null) jewelSocket1.color = originalJewelColors[0];
            if (jewelSocket2 != null) jewelSocket2.color = originalJewelColors[1];
            if (jewelSocket3 != null) jewelSocket3.color = originalJewelColors[2];
        }

        #endregion

        #region Event Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            onSlotClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isHighlighted && relicBackground != null)
            {
                relicBackground.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHighlighted && relicBackground != null)
            {
                relicBackground.color = isEmpty ? emptySlotColor : filledSlotColor;
            }
        }

        private void OnJewelSocketClicked(int jewelIndex)
        {
            onJewelClicked?.Invoke(this, jewelIndex);
        }

        #endregion
    }
}