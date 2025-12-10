using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitWorldUI : MonoBehaviour
{
    [Header("References")]
    public UnitStatus unitStatus;
    
    [Header("Bars")]
    public Slider hpSlider;
    public Slider moraleSlider;
    public Slider buzzSlider;

    [Header("Text Numbers (Drag objects here!)")]
    public TMP_Text hpNumberText; 
    public TMP_Text moraleNumberText; 
    public TMP_Text arrowText;

    private void Start()
    {
        if (unitStatus == null) unitStatus = GetComponentInParent<UnitStatus>();
    }

    private void Update()
    {
        if (unitStatus == null) return;
        if (hpSlider) 
        { 
            hpSlider.maxValue = unitStatus.maxHP; 
            hpSlider.value = unitStatus.currentHP; 
        }
        
        if (moraleSlider) 
        { 
            moraleSlider.maxValue = unitStatus.maxMorale; 
            moraleSlider.value = unitStatus.currentMorale; 
        }
        
        if (buzzSlider) 
        { 
            buzzSlider.maxValue = unitStatus.maxBuzz; 
            buzzSlider.value = unitStatus.currentBuzz; 
            Image fill = buzzSlider.fillRect.GetComponent<Image>();
            if (fill) fill.color = unitStatus.isTooDrunk ? Color.green : Color.yellow;
        }
        if (hpNumberText) 
            hpNumberText.text = $"{unitStatus.currentHP}";
            
        if (moraleNumberText) 
            moraleNumberText.text = $"{unitStatus.currentMorale}";
        
        if (arrowText) 
            arrowText.text = unitStatus.currentArrows.ToString();
    }
}