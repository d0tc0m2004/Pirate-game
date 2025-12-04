using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitWorldUI : MonoBehaviour
{
    [Header("References")]
    public UnitStatus unitStatus;
    public Slider hpSlider;
    public Slider moraleSlider;
    public TMP_Text statusText;

    private void Start()
    {
        if (unitStatus == null) unitStatus = GetComponent<UnitStatus>();
    }

    private void Update()
    {
        if (unitStatus == null) return;

        if (hpSlider != null)
        {
            hpSlider.maxValue = unitStatus.maxHP;
            hpSlider.value = unitStatus.currentHP;
        }
        if (moraleSlider != null)
        {
            moraleSlider.maxValue = unitStatus.maxMorale;
            moraleSlider.value = unitStatus.currentMorale;
        }
        if (statusText != null)
        {
            statusText.text = "";

            if (unitStatus.isStunned) statusText.text += "<color=red>STUNNED</color>\n";
            if (unitStatus.isTrapped) statusText.text += "<color=orange>TRAPPED</color>\n";
            if (unitStatus.isCursed)  statusText.text += "<color=purple>CURSED</color>\n";
        }
    }
}