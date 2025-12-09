using UnityEngine;
using UnityEngine.UI;

public class UnitIcon : MonoBehaviour
{
    public UnitStatus targetUnit;
    public Image iconImage;
    public Color teamColor;

    public void Setup(UnitStatus unit, Color color)
    {
        targetUnit = unit;
        teamColor = color;
        if (iconImage == null) iconImage = GetComponent<Image>();
        iconImage.color = teamColor;
    }

    void Update()
    {
        if (targetUnit == null)
        {
            iconImage.color = Color.black;
            return;
        }

        if (targetUnit.hasSurrendered)
        {
            iconImage.color = Color.grey;
            return;
        }

        iconImage.color = teamColor;
    }
}