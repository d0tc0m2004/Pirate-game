using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameObject uiTemplatePrefab; 
    public Transform uiContainerPanel;  
    private class UnitUIBlock
    {
        public UnitStatus trackedUnit;
        public TMP_Text nameText;
        public TMP_Text hpText;
        public TMP_Text moraleText;
        public TMP_Text stunText;
    }

    private List<UnitUIBlock> activeUIBlocks = new List<UnitUIBlock>();
    private bool isUIActive = false;

    public void SetupBattleUI()
    {
        if (uiTemplatePrefab == null || uiContainerPanel == null) 
        {
            Debug.LogWarning("UI Manager is running, but you haven't assigned the Prefab or Panel yet. UI will be skipped.");
            return;
        }

        foreach (Transform child in uiContainerPanel)
        {
            if (child.gameObject != uiTemplatePrefab) Destroy(child.gameObject);
        }
        activeUIBlocks.Clear();

        UnitStatus[] allUnits = FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
        foreach (UnitStatus unit in allUnits)
        {
            CreateUIForUnit(unit);
        }

        isUIActive = true;
    }

    void CreateUIForUnit(UnitStatus unit)
    {
        GameObject newUI = Instantiate(uiTemplatePrefab, uiContainerPanel);
        newUI.SetActive(true);

        UnitUIBlock block = new UnitUIBlock();
        block.trackedUnit = unit;

        TMP_Text[] texts = newUI.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 4)
        {
            block.nameText = texts[0];
            block.hpText = texts[1];
            block.moraleText = texts[2];
            block.stunText = texts[3];
            block.nameText.text = unit.gameObject.name;
        }

        activeUIBlocks.Add(block);
    }

    private void Update()
    {
        if (!isUIActive) return;

        foreach (UnitUIBlock block in activeUIBlocks)
        {
            if (block.trackedUnit != null)
            {
                if(block.hpText) block.hpText.text = $"HP: {block.trackedUnit.currentHP}";
                if(block.moraleText) block.moraleText.text = $"Morale: {block.trackedUnit.currentMorale}";
                
                string stunStatus = block.trackedUnit.isStunned ? "<color=red>YES</color>" : "No";
                if(block.stunText) block.stunText.text = $"IsStunned: {stunStatus}";
            }
        }
    }
}