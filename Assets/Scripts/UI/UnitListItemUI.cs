using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using TacticalGame.Units;
using TacticalGame.Managers;

namespace TacticalGame.UI
{
    /// <summary>
    /// UI item for a single unit in the left panel list.
    /// </summary>
    public class UnitListItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Text")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text weaponText;

        [Header("Visual")]
        [SerializeField] private Image background;
        [SerializeField] private Image selectionIndicator;
        [SerializeField] private Image relicCountIndicator;

        [Header("Button")]
        [SerializeField] private Button selectButton;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.18f, 0.18f, 0.22f);
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.45f, 0.6f);

        private UnitData unitData;
        private int unitIndex;
        private EquipmentUIManager equipmentManager;
        private bool isSelected = false;

        public void Setup(UnitData data, int index, EquipmentUIManager manager)
        {
            unitData = data;
            unitIndex = index;
            equipmentManager = manager;

            if (nameText != null) nameText.text = data.unitName;
            if (roleText != null) roleText.text = data.GetRoleDisplayName();
            if (weaponText != null) weaponText.text = data.GetWeaponFamilyDisplayName();

            if (selectButton != null)
                selectButton.onClick.AddListener(OnClicked);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
            if (selectionIndicator != null)
                selectionIndicator.enabled = selected;
        }

        private void OnClicked()
        {
            equipmentManager?.SelectUnit(unitIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected && background != null)
                background.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected && background != null)
                background.color = normalColor;
        }
    }
}