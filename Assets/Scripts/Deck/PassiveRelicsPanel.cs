using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace TacticalGame.Equipment
{
    /// <summary>
    /// UI panel showing all passive relics currently active.
    /// </summary>
    public class PassiveRelicsPanel : MonoBehaviour
    {
        #region References
        
        [Header("UI References")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private GameObject passiveItemPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI headerText;
        
        #endregion
        
        #region State
        
        private List<GameObject> itemInstances = new List<GameObject>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }
        
        private void OnEnable()
        {
            Refresh();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Refresh the panel with current passive relics.
        /// </summary>
        public void Refresh()
        {
            // Auto-find content container if not assigned
            if (contentContainer == null)
            {
                var content = transform.Find("Content");
                if (content != null)
                    contentContainer = content;
                else
                    contentContainer = transform; // Fallback to self
            }
            
            // Clear old items
            foreach (var item in itemInstances)
            {
                if (item != null)
                    Destroy(item);
            }
            itemInstances.Clear();
            
            // Get passive relics
            var manager = BattleDeckManager.Instance;
            if (manager == null) return;
            
            var passives = manager.PassiveRelics;
            
            // Update header
            if (headerText != null)
            {
                headerText.text = $"Passive Relics ({passives.Count})";
            }
            
            // Create items
            foreach (var relic in passives)
            {
                CreatePassiveItem(relic);
            }
        }
        
        /// <summary>
        /// Close the panel.
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Toggle panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (gameObject.activeSelf)
                Close();
            else
            {
                gameObject.SetActive(true);
                Refresh();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreatePassiveItem(EquippedRelic relic)
        {
            if (contentContainer == null) return;
            
            GameObject item;
            
            if (passiveItemPrefab != null)
            {
                item = Instantiate(passiveItemPrefab, contentContainer);
            }
            else
            {
                // Create simple fallback
                item = CreateSimplePassiveItem(relic);
            }
            
            itemInstances.Add(item);
            
            // Try to set data
            var itemUI = item.GetComponent<PassiveRelicItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(relic);
            }
            else
            {
                // Set text directly if no custom component
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = FormatPassiveText(relic);
                }
            }
        }
        
        private GameObject CreateSimplePassiveItem(EquippedRelic relic)
        {
            var item = new GameObject("PassiveItem");
            item.transform.SetParent(contentContainer, false);
            
            // Add layout element
            var layout = item.AddComponent<LayoutElement>();
            layout.minHeight = 60;
            layout.preferredHeight = 60;
            
            // Add background
            var bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            
            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(item.transform, false);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = FormatPassiveText(relic);
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(10, 5, 10, 5);
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return item;
        }
        
        private string FormatPassiveText(EquippedRelic relic)
        {
            if (relic == null) return "";
            
            string name = relic.relicName ?? $"{relic.roleTag} {relic.category}";
            string desc = relic.effectData?.description ?? "";
            
            return $"<b>{name}</b>\n<size=80%>{desc}</size>";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Individual passive relic item in the panel.
    /// </summary>
    public class PassiveRelicItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private Image background;
        
        [Header("Category Colors")]
        [SerializeField] private Color trinketColor = new Color(0.4f, 0.6f, 0.5f);
        [SerializeField] private Color passiveUniqueColor = new Color(0.6f, 0.4f, 0.6f);
        
        private EquippedRelic relic;
        
        public void Initialize(EquippedRelic relicData)
        {
            relic = relicData;
            Refresh();
        }
        
        public void Refresh()
        {
            if (relic == null) return;
            
            if (nameText != null)
            {
                nameText.text = relic.relicName ?? $"{relic.roleTag} {relic.category}";
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = relic.effectData?.description ?? "";
            }
            
            if (background != null)
            {
                background.color = relic.category == RelicCategory.Trinket 
                    ? trinketColor 
                    : passiveUniqueColor;
            }
        }
    }
}