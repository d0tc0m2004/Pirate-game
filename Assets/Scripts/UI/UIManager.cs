using UnityEngine;
using System.Collections.Generic;
using TMPro;
using TacticalGame.Units;

namespace TacticalGame.UI
{
    /// <summary>
    /// Manages per-unit UI blocks in a panel (legacy panel-based UI).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject uiTemplatePrefab;
        [SerializeField] private Transform uiContainerPanel;

        private class UnitUIBlock
        {
            public UnitStatus TrackedUnit;
            public TMP_Text NameText;
            public TMP_Text HPText;
            public TMP_Text MoraleText;
            public TMP_Text StunText;
        }

        private readonly List<UnitUIBlock> activeUIBlocks = new List<UnitUIBlock>();
        private bool isUIActive = false;

        public void SetupBattleUI()
        {
            if (uiTemplatePrefab == null || uiContainerPanel == null)
            {
                Debug.LogWarning("UI Manager: Prefab or Panel not assigned.");
                return;
            }

            foreach (Transform child in uiContainerPanel)
            {
                if (child.gameObject != uiTemplatePrefab)
                    Destroy(child.gameObject);
            }
            activeUIBlocks.Clear();

            foreach (var unit in FindObjectsByType<UnitStatus>(FindObjectsSortMode.None))
            {
                CreateUIForUnit(unit);
            }
            isUIActive = true;
        }

        private void CreateUIForUnit(UnitStatus unit)
        {
            GameObject newUI = Instantiate(uiTemplatePrefab, uiContainerPanel);
            newUI.SetActive(true);

            var block = new UnitUIBlock { TrackedUnit = unit };
            TMP_Text[] texts = newUI.GetComponentsInChildren<TMP_Text>();
            
            if (texts.Length >= 4)
            {
                block.NameText = texts[0];
                block.HPText = texts[1];
                block.MoraleText = texts[2];
                block.StunText = texts[3];
                block.NameText.text = unit.gameObject.name;
            }
            activeUIBlocks.Add(block);
        }

        private void Update()
        {
            if (!isUIActive) return;

            foreach (var block in activeUIBlocks)
            {
                if (block.TrackedUnit == null) continue;
                if (block.HPText) block.HPText.text = $"HP: {block.TrackedUnit.CurrentHP}";
                if (block.MoraleText) block.MoraleText.text = $"Morale: {block.TrackedUnit.CurrentMorale}";
                if (block.StunText) block.StunText.text = $"IsStunned: {(block.TrackedUnit.IsStunned ? "<color=red>YES</color>" : "No")}";
            }
        }
    }
}