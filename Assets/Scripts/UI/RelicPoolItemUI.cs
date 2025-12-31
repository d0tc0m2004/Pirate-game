using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI item for a relic in the pool (right panel).
    /// </summary>
    public class RelicPoolItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Text")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text rarityText;

        [Header("Visual")]
        [SerializeField] private Image background;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image roleMatchIcon;

        [Header("Button")]
        [SerializeField] private Button selectButton;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.18f);
        [SerializeField] private Color hoverColor = new Color(0.22f, 0.22f, 0.28f);
        [SerializeField] private Color roleMatchBgColor = new Color(0.2f, 0.18f, 0.1f);

        private object relicData;
        private bool isRoleMatch;
        private Action<object> onClicked;
        private Action<object, bool> onHover;

        public void SetupWeaponRelic(WeaponRelic relic, bool roleMatch, Action<object> clickCallback, Action<object, bool> hoverCallback)
        {
            relicData = relic;
            isRoleMatch = roleMatch;
            onClicked = clickCallback;
            onHover = hoverCallback;

            if (nameText != null) nameText.text = $"{relic.roleTag} {relic.weaponFamily}";
            if (effectText != null) effectText.text = relic.effectData.effectName;
            if (rarityText != null) rarityText.text = $"({relic.effectData.GetRarityName()})";

            if (background != null) background.color = roleMatch ? roleMatchBgColor : normalColor;
            if (rarityBorder != null) rarityBorder.color = GetRarityColor(relic.effectData.rarity);
            if (roleMatchIcon != null)
            {
                roleMatchIcon.enabled = roleMatch;
                roleMatchIcon.color = new Color(1f, 0.84f, 0f);
            }

            if (selectButton != null)
                selectButton.onClick.AddListener(OnClicked);
        }

        public void SetupRelic(RelicData relic, bool roleMatch, Action<object> clickCallback, Action<object, bool> hoverCallback)
        {
            relicData = relic;
            isRoleMatch = roleMatch;
            onClicked = clickCallback;
            onHover = hoverCallback;

            if (nameText != null) nameText.text = relic.relicName;
            if (effectText != null) effectText.text = relic.category.ToString();
            if (rarityText != null) rarityText.text = $"({relic.rarity})";

            if (background != null) background.color = roleMatch ? roleMatchBgColor : normalColor;
            if (rarityBorder != null) rarityBorder.color = GetRarityColor(relic.rarity);
            if (roleMatchIcon != null) roleMatchIcon.enabled = roleMatch;

            if (selectButton != null)
                selectButton.onClick.AddListener(OnClicked);
        }

        private Color GetRarityColor(RelicRarity rarity)
        {
            return rarity switch
            {
                RelicRarity.Common => new Color(0.5f, 0.5f, 0.5f),
                RelicRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                RelicRarity.Rare => new Color(0.4f, 0.4f, 1f),
                RelicRarity.Unique => new Color(0.8f, 0.4f, 0.8f),
                _ => Color.gray
            };
        }

        private void OnClicked()
        {
            onClicked?.Invoke(relicData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null) background.color = hoverColor;
            onHover?.Invoke(relicData, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background != null) background.color = isRoleMatch ? roleMatchBgColor : normalColor;
            onHover?.Invoke(relicData, false);
        }
    }
}