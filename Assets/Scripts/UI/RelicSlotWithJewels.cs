using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalGame.Equipment;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI component for a relic slot with 3 jewel slots underneath.
    /// Structure:
    /// - RelicSlot (Image) - the main relic box
    /// - SlotLabel (Text) - R1, R2, ULT, PAS, etc.
    /// - JewelSlot1, JewelSlot2, JewelSlot3 (Images)
    /// </summary>
    public class RelicSlotWithJewels : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields

        [Header("Relic Slot")]
        [SerializeField] private Image relicSlotImage;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private Image relicIconImage; // Optional: icon inside the slot

        [Header("Jewel Slots")]
        [SerializeField] private Image jewelSlot1;
        [SerializeField] private Image jewelSlot2;
        [SerializeField] private Image jewelSlot3;

        [Header("Colors")]
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color filledSlotColor = new Color(0.4f, 0.4f, 0.6f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.5f, 0.7f, 1f);
        [SerializeField] private Color emptyJewelColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color filledJewelColor = new Color(0.9f, 0.7f, 0.2f, 1f);

        #endregion

        #region Private State

        private string slotLabel = "";
        private RelicData equippedRelic;
        private JewelData[] equippedJewels = new JewelData[3];
        private bool isInteractable = true;

        #endregion

        #region Public Properties

        public string SlotLabel => slotLabel;
        public bool IsEmpty => equippedRelic == null;
        public RelicData EquippedRelic => equippedRelic;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set initial colors
            SetEmpty();
        }

        #endregion

        #region Public Methods - Setup

        /// <summary>
        /// Initialize the slot with a label (R1, R2, R3, R4, ULT, PAS).
        /// </summary>
        public void Initialize(string label, bool interactable = true)
        {
            slotLabel = label;
            isInteractable = interactable;

            if (slotLabelText != null)
            {
                slotLabelText.text = label;
            }

            SetEmpty();
        }

        /// <summary>
        /// Set this slot as empty.
        /// </summary>
        public void SetEmpty()
        {
            equippedRelic = null;
            equippedJewels = new JewelData[3];

            if (relicSlotImage != null)
            {
                relicSlotImage.color = emptySlotColor;
            }

            if (relicIconImage != null)
            {
                relicIconImage.enabled = false;
            }

            UpdateJewelVisuals();
        }

        /// <summary>
        /// Equip a relic to this slot.
        /// </summary>
        public void EquipRelic(RelicData relic)
        {
            equippedRelic = relic;

            if (relicSlotImage != null)
            {
                relicSlotImage.color = relic != null ? filledSlotColor : emptySlotColor;
            }

            if (relicIconImage != null && relic != null && relic.relicIcon != null)
            {
                relicIconImage.sprite = relic.relicIcon;
                relicIconImage.enabled = true;
            }
            else if (relicIconImage != null)
            {
                relicIconImage.enabled = false;
            }

            UpdateJewelVisuals();
        }

        /// <summary>
        /// Equip a jewel to a specific slot (0, 1, or 2).
        /// </summary>
        public bool EquipJewel(int slotIndex, JewelData jewel)
        {
            if (slotIndex < 0 || slotIndex >= 3) return false;

            equippedJewels[slotIndex] = jewel;
            UpdateJewelVisuals();
            return true;
        }

        /// <summary>
        /// Remove jewel from a slot.
        /// </summary>
        public JewelData RemoveJewel(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3) return null;

            JewelData removed = equippedJewels[slotIndex];
            equippedJewels[slotIndex] = null;
            UpdateJewelVisuals();
            return removed;
        }

        /// <summary>
        /// Set interactable state.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        #endregion

        #region Private Methods

        private void UpdateJewelVisuals()
        {
            UpdateSingleJewelVisual(jewelSlot1, 0);
            UpdateSingleJewelVisual(jewelSlot2, 1);
            UpdateSingleJewelVisual(jewelSlot3, 2);
        }

        private void UpdateSingleJewelVisual(Image jewelImage, int index)
        {
            if (jewelImage == null) return;

            bool hasjewel = equippedJewels[index] != null;
            jewelImage.color = hasjewel ? filledJewelColor : emptyJewelColor;

            // If jewel has custom color, use it
            if (hasjewel && equippedJewels[index].jewelColor != Color.white)
            {
                jewelImage.color = equippedJewels[index].jewelColor;
            }
        }

        #endregion

        #region Pointer Events

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;

            // TODO: Open relic/jewel selection popup
            Debug.Log($"Clicked slot: {slotLabel}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;

            if (relicSlotImage != null)
            {
                relicSlotImage.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (relicSlotImage != null)
            {
                relicSlotImage.color = IsEmpty ? emptySlotColor : filledSlotColor;
            }
        }

        #endregion
    }
}