using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// UI component for a single card in hand.
    /// Handles display, hover, click, and visual states.
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region References
        
        [Header("Card Display")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardBorder;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private Image ownerPortrait;
        
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI energyCostText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI ownerNameText;
        
        [Header("Indicators")]
        [SerializeField] private GameObject stowedIndicator;
        [SerializeField] private GameObject weaponIndicator;
        
        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        
        [Header("Category Colors")]
        [SerializeField] private Color bootsColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color glovesColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color hatColor = new Color(0.3f, 0.5f, 0.8f);
        [SerializeField] private Color coatColor = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color totemColor = new Color(0.7f, 0.5f, 0.8f);
        [SerializeField] private Color ultimateColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color weaponColor = new Color(0.8f, 0.8f, 0.8f);
        
        #endregion
        
        #region State
        
        private BattleCard card;
        private BattleDeckUI deckUI;
        private bool isInteractable = true;
        private CanvasGroup canvasGroup;
        
        #endregion
        
        #region Properties
        
        public BattleCard Card => card;
        
        #endregion
        
        #region Initialization
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
            
            if (stowedIndicator != null)
            {
                stowedIndicator.SetActive(false);
            }
        }
        
        /// <summary>
        /// Initialize the card UI with data.
        /// </summary>
        public void Initialize(BattleCard cardData, BattleDeckUI ui)
        {
            card = cardData;
            deckUI = ui;
            
            RefreshDisplay();
        }
        
        /// <summary>
        /// Refresh all visual elements.
        /// </summary>
        public void RefreshDisplay()
        {
            if (card == null) return;
            
            // Card name
            if (cardNameText != null)
            {
                cardNameText.text = card.GetDisplayName();
            }
            
            // Energy cost
            if (energyCostText != null)
            {
                energyCostText.text = card.energyCost.ToString();
            }
            
            // Description
            if (descriptionText != null)
            {
                descriptionText.text = card.description;
            }
            
            // Owner name
            if (ownerNameText != null)
            {
                ownerNameText.text = card.GetOwnerName();
            }
            
            // Category color
            if (cardBackground != null)
            {
                cardBackground.color = GetCategoryColor(card.category);
            }
            
            // Weapon indicator
            if (weaponIndicator != null)
            {
                weaponIndicator.SetActive(card.IsWeaponCard);
            }
            
            // Stowed indicator
            if (stowedIndicator != null)
            {
                stowedIndicator.SetActive(card.isStowed);
            }
        }
        
        #endregion
        
        #region Visual State
        
        /// <summary>
        /// Set the card's tint color.
        /// </summary>
        public void SetColor(Color color)
        {
            if (cardBackground != null)
            {
                // Blend with category color
                var categoryColor = GetCategoryColor(card?.category ?? RelicCategory.Boots);
                cardBackground.color = Color.Lerp(categoryColor, color, 0.5f);
            }
            
            if (cardBorder != null)
            {
                cardBorder.color = color;
            }
        }
        
        /// <summary>
        /// Set whether the card is interactable.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = interactable ? 1f : 0.6f;
            }
        }
        
        /// <summary>
        /// Show/hide the stowed indicator.
        /// </summary>
        public void SetStowedIndicator(bool stowed)
        {
            if (stowedIndicator != null)
            {
                stowedIndicator.SetActive(stowed);
            }
        }
        
        private Color GetCategoryColor(RelicCategory category)
        {
            switch (category)
            {
                case RelicCategory.Boots: return bootsColor;
                case RelicCategory.Gloves: return glovesColor;
                case RelicCategory.Hat: return hatColor;
                case RelicCategory.Coat: return coatColor;
                case RelicCategory.Totem: return totemColor;
                case RelicCategory.Ultimate: return ultimateColor;
                case RelicCategory.Weapon: return weaponColor;
                default: return Color.white;
            }
        }
        
        #endregion
        
        #region Tooltip
        
        /// <summary>
        /// Show the card tooltip.
        /// </summary>
        public void ShowTooltip()
        {
            if (tooltipPanel == null || card == null) return;
            
            tooltipPanel.SetActive(true);
            
            if (tooltipText != null)
            {
                string tooltip = BuildTooltip();
                tooltipText.text = tooltip;
            }
        }
        
        /// <summary>
        /// Hide the card tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        private string BuildTooltip()
        {
            if (card == null) return "";
            
            var sb = new System.Text.StringBuilder();
            
            // Header
            sb.AppendLine($"<b>{card.GetDisplayName()}</b>");
            sb.AppendLine($"<size=80%>{card.category} - {card.roleTag}</size>");
            sb.AppendLine();
            
            // Cost
            sb.AppendLine($"<color=yellow>Cost: {card.energyCost} Energy</color>");
            
            // Owner
            sb.AppendLine($"<color=#88ff88>Owner: {card.GetOwnerName()}</color>");
            sb.AppendLine();
            
            // Description
            if (!string.IsNullOrEmpty(card.description))
            {
                sb.AppendLine(card.description);
            }
            
            // Target type
            if (card.RequiresTarget())
            {
                sb.AppendLine();
                sb.AppendLine($"<color=#ffaa88>Target: {card.GetTargetType()}</color>");
            }
            
            // Stowed status
            if (card.isStowed)
            {
                sb.AppendLine();
                sb.AppendLine("<color=#88ddff>[STOWED - Won't discard]</color>");
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region Pointer Events
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            
            deckUI?.OnCardHoverEnter(this);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            deckUI?.OnCardHoverExit(this);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                deckUI?.OnCardClicked(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                deckUI?.OnCardRightClicked(this);
            }
        }
        
        #endregion
        
        #region Context Menu Actions
        
        /// <summary>
        /// Button handler for stow action.
        /// </summary>
        public void OnStowButtonClicked()
        {
            if (card == null) return;
            BattleDeckManager.Instance.StowCard(card);
        }
        
        /// <summary>
        /// Button handler for discard-draw action.
        /// </summary>
        public void OnDiscardButtonClicked()
        {
            if (card == null) return;
            BattleDeckManager.Instance.DiscardAndDraw(card);
        }
        
        #endregion
    }
}