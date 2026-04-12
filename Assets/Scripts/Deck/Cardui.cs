using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalGame.Enums;
using TacticalGame.Core;

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
        [SerializeField] private GameObject stowButton;
        [SerializeField] private GameObject discardButton;
        

        
        [Header("Category Colors")]
        [SerializeField] private Color bootsColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color glovesColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color hatColor = new Color(0.3f, 0.5f, 0.8f);
        [SerializeField] private Color coatColor = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color totemColor = new Color(0.7f, 0.5f, 0.8f);
        [SerializeField] private Color ultimateColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color weaponColor = new Color(0.8f, 0.8f, 0.8f);

        [Header("Playability Colors")]
        [SerializeField] private Color energyOkColor = Color.white;
        [SerializeField] private Color energyShortColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color borderPlayableColor = Color.white;
        [SerializeField] private Color borderUnplayableColor = new Color(1f, 0.2f, 0.2f);

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
            

            
            if (stowedIndicator != null)
            {
                stowedIndicator.SetActive(false);
            }

            if (stowButton != null)
            {
                stowButton.SetActive(false);
            }

            if (discardButton != null)
            {
                discardButton.SetActive(false);
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

            // Hide stow button if card is stowed
            if (stowButton != null && card.isStowed)
            {
                stowButton.SetActive(false);
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
        /// Non-interactable cards stay almost fully opaque so the player can still
        /// READ them — the dimming is a hint, not a "this card is invisible" signal.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = interactable ? 1f : 0.9f;
            }
        }

        /// <summary>
        /// Apply playability visuals:
        ///   - Energy cost text turns red when the player can't afford the cost.
        ///   - Card border turns red when the card is unplayable for ANY reason
        ///     (energy, missing target, no allies, etc).
        ///
        /// Must be called every visual refresh — it always writes both the energy
        /// text color and the border color so the visuals reset cleanly when a
        /// blocker clears (e.g. you gain energy, an ally walks into range, etc).
        /// </summary>
        public void SetPlayability(CardPlayabilityChecker.Result result)
        {
            if (energyCostText != null)
            {
                energyCostText.color = result.hasEnergy ? energyOkColor : energyShortColor;
            }

            if (cardBorder != null)
            {
                cardBorder.color = result.isPlayable ? borderPlayableColor : borderUnplayableColor;
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

        /// <summary>
        /// Show/hide the stow button. Won't show if card is already stowed.
        /// </summary>
        public void ShowStowButton(bool show)
        {
            if (stowButton != null)
            {
                // Don't show stow button if card is already stowed
                bool shouldShow = show && card != null && !card.isStowed;
                stowButton.SetActive(shouldShow);
            }
        }

        /// <summary>
        /// Show/hide the discard button.
        /// </summary>
        public void ShowDiscardButton(bool show)
        {
            if (discardButton != null)
            {
                discardButton.SetActive(show && card != null);
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
        

        
        #region Pointer Events
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Allow hover even on non-interactable cards so players see they can click to select owner
            deckUI?.OnCardHoverEnter(this);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            deckUI?.OnCardHoverExit(this);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!isInteractable)
                {
                    // Auto-select the card's owner unit so cards become interactable
                    if (card?.ownerUnit != null)
                    {
                        BattleDeckManager.Instance.SetSelectedUnit(card.ownerUnit);
                        // Trigger unit selection event so BattleManager updates too
                        GameEvents.TriggerUnitSelected(card.ownerUnit.gameObject);
                    }
                    return;
                }
                deckUI?.OnCardClicked(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!isInteractable) return;
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
            if (BattleDeckManager.Instance.StowCard(card))
            {
                // Hide the stow button after successful stow
                ShowStowButton(false);
            }
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