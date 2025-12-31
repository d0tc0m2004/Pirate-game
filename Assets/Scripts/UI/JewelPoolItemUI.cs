using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using TacticalGame.Equipment;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI item for a jewel in the pool.
    /// </summary>
    public class JewelPoolItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual")]
        [SerializeField] private Image background;
        [SerializeField] private Image jewelIcon;
        [SerializeField] private TMP_Text nameText;

        [Header("Button")]
        [SerializeField] private Button selectButton;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.12f, 0.12f, 0.15f);
        [SerializeField] private Color hoverColor = new Color(0.2f, 0.2f, 0.25f);

        private JewelData jewelData;
        private Action<JewelData> onClicked;
        private Action<JewelData, bool> onHover;

        public void Setup(JewelData jewel, Action<JewelData> clickCallback, Action<JewelData, bool> hoverCallback)
        {
            jewelData = jewel;
            onClicked = clickCallback;
            onHover = hoverCallback;

            if (nameText != null) nameText.text = jewel.jewelName;
            
            if (jewelIcon != null)
            {
                if (jewel.jewelIcon != null)
                    jewelIcon.sprite = jewel.jewelIcon;
                jewelIcon.color = GetJewelColor(jewel);
            }

            if (background != null) background.color = normalColor;

            if (selectButton != null)
                selectButton.onClick.AddListener(OnClicked);
        }

        private Color GetJewelColor(JewelData jewel)
        {
            if (jewel.jewelColor != Color.white) return jewel.jewelColor;
            return jewel.type switch
            {
                JewelType.Damage => new Color(1f, 0.3f, 0.3f),
                JewelType.Defense => new Color(0.3f, 0.5f, 1f),
                JewelType.Utility => new Color(0.3f, 1f, 0.3f),
                JewelType.Duration => new Color(1f, 1f, 0.3f),
                JewelType.Cost => new Color(0.3f, 1f, 1f),
                _ => new Color(0.8f, 0.8f, 0.8f)
            };
        }

        private void OnClicked()
        {
            onClicked?.Invoke(jewelData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null) background.color = hoverColor;
            onHover?.Invoke(jewelData, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null) background.color = normalColor;
            onHover?.Invoke(jewelData, false);
        }
    }
}