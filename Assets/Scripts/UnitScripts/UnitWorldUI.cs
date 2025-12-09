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
    public TMP_Text hpNumberText;      // Displays "80 / 100"
    public TMP_Text moraleNumberText;  // Displays "50 / 100"
    public TMP_Text arrowText;         // Displays "10"

    private void Start()
    {
        if (unitStatus == null) unitStatus = GetComponentInParent<UnitStatus>();
    }

    private void Update()
    {
        if (unitStatus == null) return;

        // 1. UPDATE SLIDERS
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
            
            // Color change for drunk state
            Image fill = buzzSlider.fillRect.GetComponent<Image>();
            if (fill) fill.color = unitStatus.isTooDrunk ? Color.green : Color.yellow;
        }

        // 2. UPDATE TEXT NUMBERS (New Logic)
        if (hpNumberText) 
            hpNumberText.text = $"{unitStatus.currentHP}";
            
        if (moraleNumberText) 
            moraleNumberText.text = $"{unitStatus.currentMorale}";
        
        if (arrowText) 
            arrowText.text = unitStatus.currentArrows.ToString();
    }
}