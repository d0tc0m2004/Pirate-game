using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TacticalGame.Enums;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// AUTO-GENERATES CARD UI AT RUNTIME.
    /// 
    /// ============================================
    /// TEMPORARY - REMOVE WHEN ADDING CUSTOM UI
    /// ============================================
    /// 
    /// To replace with your own card UI:
    /// 1. Create your own card prefab
    /// 2. Assign it to BattleDeckUI.cardUIPrefab
    /// 3. Delete this script
    /// 4. Make sure your prefab has CardUI component
    /// </summary>
    public static class CardUIGenerator
    {
        #region Card Dimensions
        
        private const float CARD_WIDTH = 120f;
        private const float CARD_HEIGHT = 180f;
        private const float CORNER_RADIUS = 8f;
        
        #endregion
        
        #region Colors
        
        private static readonly Color BOOTS_COLOR = new Color(0.6f, 0.4f, 0.2f);
        private static readonly Color GLOVES_COLOR = new Color(0.8f, 0.3f, 0.3f);
        private static readonly Color HAT_COLOR = new Color(0.3f, 0.5f, 0.8f);
        private static readonly Color COAT_COLOR = new Color(0.3f, 0.7f, 0.4f);
        private static readonly Color TOTEM_COLOR = new Color(0.7f, 0.5f, 0.8f);
        private static readonly Color ULTIMATE_COLOR = new Color(1f, 0.8f, 0.2f);
        private static readonly Color WEAPON_COLOR = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color BORDER_COLOR = new Color(0.9f, 0.85f, 0.7f);
        private static readonly Color TEXT_COLOR = Color.white;
        private static readonly Color COST_BG_COLOR = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        
        #endregion
        
        /// <summary>
        /// Create a complete card UI GameObject.
        /// </summary>
        public static GameObject CreateCard(BattleCard card, Transform parent)
        {
            // Root object
            var cardGO = new GameObject($"Card_{card.GetDisplayName()}");
            cardGO.transform.SetParent(parent, false);
            
            // Add RectTransform
            var rt = cardGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
            
            // Add CanvasGroup for fading
            cardGO.AddComponent<CanvasGroup>();
            
            // Add CardUI component
            var cardUI = cardGO.AddComponent<CardUI>();
            
            // Build visual hierarchy
            CreateCardVisuals(cardGO, card, cardUI);
            
            return cardGO;
        }
        
        private static void CreateCardVisuals(GameObject cardGO, BattleCard card, CardUI cardUI)
        {
            // === BACKGROUND ===
            var bgGO = CreateChild(cardGO, "Background");
            var bgRT = SetFullStretch(bgGO);
            var bgImage = bgGO.AddComponent<Image>();
            bgImage.color = GetCategoryColor(card.category);
            
            // Assign to CardUI
            SetPrivateField(cardUI, "cardBackground", bgImage);
            
            // === BORDER ===
            var borderGO = CreateChild(cardGO, "Border");
            SetFullStretch(borderGO);
            var borderImage = borderGO.AddComponent<Image>();
            borderImage.color = BORDER_COLOR;
            // Make it outline only
            borderImage.type = Image.Type.Sliced;
            borderImage.fillCenter = false;
            
            SetPrivateField(cardUI, "cardBorder", borderImage);
            
            // === INNER PANEL (slightly smaller) ===
            var innerGO = CreateChild(cardGO, "Inner");
            var innerRT = SetFullStretch(innerGO);
            innerRT.offsetMin = new Vector2(4, 4);
            innerRT.offsetMax = new Vector2(-4, -4);
            var innerImage = innerGO.AddComponent<Image>();
            innerImage.color = GetCategoryColor(card.category) * 0.8f;
            
            // === ENERGY COST (top left circle) ===
            var costGO = CreateChild(cardGO, "EnergyCost");
            var costRT = costGO.AddComponent<RectTransform>();
            costRT.anchorMin = new Vector2(0, 1);
            costRT.anchorMax = new Vector2(0, 1);
            costRT.pivot = new Vector2(0, 1);
            costRT.anchoredPosition = new Vector2(5, -5);
            costRT.sizeDelta = new Vector2(28, 28);
            
            var costBG = costGO.AddComponent<Image>();
            costBG.color = COST_BG_COLOR;
            
            var costTextGO = CreateChild(costGO, "CostText");
            SetFullStretch(costTextGO);
            var costText = costTextGO.AddComponent<TextMeshProUGUI>();
            costText.text = card.energyCost.ToString();
            costText.fontSize = 18;
            costText.fontStyle = FontStyles.Bold;
            costText.color = Color.yellow;
            costText.alignment = TextAlignmentOptions.Center;
            
            SetPrivateField(cardUI, "energyCostText", costText);
            
            // === CARD NAME (top) ===
            var nameGO = CreateChild(cardGO, "CardName");
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1);
            nameRT.anchorMax = new Vector2(1, 1);
            nameRT.pivot = new Vector2(0.5f, 1);
            nameRT.anchoredPosition = new Vector2(0, -8);
            nameRT.sizeDelta = new Vector2(-40, 36);
            
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = card.GetDisplayName();
            nameText.fontSize = 11;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = TEXT_COLOR;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.textWrappingMode = TextWrappingModes.Normal;
            nameText.overflowMode = TextOverflowModes.Truncate;
            
            SetPrivateField(cardUI, "cardNameText", nameText);
            
            // === CATEGORY ICON (center top) ===
            var iconGO = CreateChild(cardGO, "CategoryIcon");
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(0, 20);
            iconRT.sizeDelta = new Vector2(50, 50);
            
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white * 0.1f; // Subtle watermark
            
            // Category letter as placeholder
            var iconTextGO = CreateChild(iconGO, "IconText");
            SetFullStretch(iconTextGO);
            var iconText = iconTextGO.AddComponent<TextMeshProUGUI>();
            iconText.text = GetCategoryLetter(card.category);
            iconText.fontSize = 28;
            iconText.fontStyle = FontStyles.Bold;
            iconText.color = Color.white;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // === DESCRIPTION PANEL (middle with background) ===
            var descPanelGO = CreateChild(cardGO, "DescriptionPanel");
            var descPanelRT = descPanelGO.AddComponent<RectTransform>();
            descPanelRT.anchorMin = new Vector2(0.05f, 0.18f);
            descPanelRT.anchorMax = new Vector2(0.95f, 0.65f);
            descPanelRT.offsetMin = Vector2.zero;
            descPanelRT.offsetMax = Vector2.zero;

            // Semi-transparent background for better readability
            var descPanelBG = descPanelGO.AddComponent<Image>();
            descPanelBG.color = new Color(0f, 0f, 0f, 0.35f);

            // === DESCRIPTION TEXT ===
            var descGO = CreateChild(descPanelGO, "Description");
            var descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = Vector2.zero;
            descRT.anchorMax = Vector2.one;
            descRT.offsetMin = new Vector2(4, 4);
            descRT.offsetMax = new Vector2(-4, -4);

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = card.description;
            descText.fontSize = 11;
            descText.color = TEXT_COLOR;
            descText.alignment = TextAlignmentOptions.Center;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.overflowMode = TextOverflowModes.Ellipsis;

            SetPrivateField(cardUI, "descriptionText", descText);
            
            // === OWNER NAME (bottom) ===
            var ownerGO = CreateChild(cardGO, "OwnerName");
            var ownerRT = ownerGO.AddComponent<RectTransform>();
            ownerRT.anchorMin = new Vector2(0, 0);
            ownerRT.anchorMax = new Vector2(1, 0);
            ownerRT.pivot = new Vector2(0.5f, 0);
            ownerRT.anchoredPosition = new Vector2(0, 5);
            ownerRT.sizeDelta = new Vector2(-10, 20);
            
            var ownerText = ownerGO.AddComponent<TextMeshProUGUI>();
            ownerText.text = card.GetOwnerName();
            ownerText.fontSize = 10;
            ownerText.color = new Color(0.8f, 1f, 0.8f);
            ownerText.alignment = TextAlignmentOptions.Center;
            
            SetPrivateField(cardUI, "ownerNameText", ownerText);
            
            // === STOWED INDICATOR (corner ribbon) ===
            var stowedGO = CreateChild(cardGO, "StowedIndicator");
            var stowedRT = stowedGO.AddComponent<RectTransform>();
            stowedRT.anchorMin = new Vector2(1, 1);
            stowedRT.anchorMax = new Vector2(1, 1);
            stowedRT.pivot = new Vector2(1, 1);
            stowedRT.anchoredPosition = new Vector2(-2, -2);
            stowedRT.sizeDelta = new Vector2(24, 24);
            
            var stowedBG = stowedGO.AddComponent<Image>();
            stowedBG.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            
            var stowedTextGO = CreateChild(stowedGO, "StowText");
            SetFullStretch(stowedTextGO);
            var stowedText = stowedTextGO.AddComponent<TextMeshProUGUI>();
            stowedText.text = "S";
            stowedText.fontSize = 14;
            stowedText.fontStyle = FontStyles.Bold;
            stowedText.color = Color.white;
            stowedText.alignment = TextAlignmentOptions.Center;
            
            stowedGO.SetActive(false);
            SetPrivateField(cardUI, "stowedIndicator", stowedGO);

            // === STOW BUTTON (bottom right, hidden by default) ===
            var stowBtnGO = CreateChild(cardGO, "StowButton");
            var stowBtnRT = stowBtnGO.AddComponent<RectTransform>();
            stowBtnRT.anchorMin = new Vector2(1, 0);
            stowBtnRT.anchorMax = new Vector2(1, 0);
            stowBtnRT.pivot = new Vector2(1, 0);
            stowBtnRT.anchoredPosition = new Vector2(-5, 25);
            stowBtnRT.sizeDelta = new Vector2(28, 22);

            var stowBtnBG = stowBtnGO.AddComponent<Image>();
            stowBtnBG.color = new Color(0.2f, 0.5f, 0.8f, 0.95f);

            var stowBtn = stowBtnGO.AddComponent<Button>();
            stowBtn.targetGraphic = stowBtnBG;

            // Button hover colors
            var colors = stowBtn.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.8f, 0.95f);
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f, 1f);
            stowBtn.colors = colors;

            var stowBtnTextGO = CreateChild(stowBtnGO, "Text");
            SetFullStretch(stowBtnTextGO);
            var stowBtnText = stowBtnTextGO.AddComponent<TextMeshProUGUI>();
            stowBtnText.text = "STOW";
            stowBtnText.fontSize = 10;
            stowBtnText.fontStyle = FontStyles.Bold;
            stowBtnText.color = Color.white;
            stowBtnText.alignment = TextAlignmentOptions.Center;

            // Wire up the button click to CardUI
            stowBtn.onClick.AddListener(() => cardUI.OnStowButtonClicked());

            stowBtnGO.SetActive(false);
            SetPrivateField(cardUI, "stowButton", stowBtnGO);

            // === DISCARD BUTTON (bottom right, below stow, hidden by default) ===
            var discardBtnGO = CreateChild(cardGO, "DiscardButton");
            var discardBtnRT = discardBtnGO.AddComponent<RectTransform>();
            discardBtnRT.anchorMin = new Vector2(1, 0);
            discardBtnRT.anchorMax = new Vector2(1, 0);
            discardBtnRT.pivot = new Vector2(1, 0);
            discardBtnRT.anchoredPosition = new Vector2(-38, 25);
            discardBtnRT.sizeDelta = new Vector2(32, 22);

            var discardBtnBG = discardBtnGO.AddComponent<Image>();
            discardBtnBG.color = new Color(0.7f, 0.25f, 0.2f, 0.95f);

            var discardBtn = discardBtnGO.AddComponent<Button>();
            discardBtn.targetGraphic = discardBtnBG;

            var dColors = discardBtn.colors;
            dColors.normalColor = new Color(0.7f, 0.25f, 0.2f, 0.95f);
            dColors.highlightedColor = new Color(0.85f, 0.35f, 0.3f, 1f);
            dColors.pressedColor = new Color(0.55f, 0.2f, 0.15f, 1f);
            discardBtn.colors = dColors;

            var discardBtnTextGO = CreateChild(discardBtnGO, "Text");
            SetFullStretch(discardBtnTextGO);
            var discardBtnText = discardBtnTextGO.AddComponent<TextMeshProUGUI>();
            discardBtnText.text = "DISC";
            discardBtnText.fontSize = 9;
            discardBtnText.fontStyle = FontStyles.Bold;
            discardBtnText.color = Color.white;
            discardBtnText.alignment = TextAlignmentOptions.Center;

            discardBtn.onClick.AddListener(() => cardUI.OnDiscardButtonClicked());

            discardBtnGO.SetActive(false);
            SetPrivateField(cardUI, "discardButton", discardBtnGO);
        }
        
        #region Helpers
        
        private static GameObject CreateChild(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }
        
        private static RectTransform SetFullStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            return rt;
        }
        
        private static Color GetCategoryColor(RelicCategory category)
        {
            switch (category)
            {
                case RelicCategory.Boots: return BOOTS_COLOR;
                case RelicCategory.Gloves: return GLOVES_COLOR;
                case RelicCategory.Hat: return HAT_COLOR;
                case RelicCategory.Coat: return COAT_COLOR;
                case RelicCategory.Totem: return TOTEM_COLOR;
                case RelicCategory.Ultimate: return ULTIMATE_COLOR;
                case RelicCategory.Weapon: return WEAPON_COLOR;
                default: return Color.gray;
            }
        }
        
        private static string GetCategoryLetter(RelicCategory category)
        {
            switch (category)
            {
                case RelicCategory.Boots: return "B";
                case RelicCategory.Gloves: return "G";
                case RelicCategory.Hat: return "H";
                case RelicCategory.Coat: return "C";
                case RelicCategory.Totem: return "T";
                case RelicCategory.Ultimate: return "U";
                case RelicCategory.Weapon: return "W";
                default: return "?";
            }
        }
        

        
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
        
        #endregion
    }
}