using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlobalUIManager : MonoBehaviour
{
    [Header("Team Bars")]
    public Slider teamHPSlider;
    public TMP_Text teamHPText;
    
    public Slider teamMoraleSlider;
    public TMP_Text teamMoraleText;
    
    [Header("Info")]
    public TMP_Text roundText;
    public TurnManager turnManager;

    void Update()
    {
        CalculateTeamStats();
    }

    void CalculateTeamStats()
    {
        int totalCurrentHP = 0;
        int totalMaxHP = 0;
        int totalCurrentMorale = 0;
        int totalMaxMorale = 0;

        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");

        foreach (GameObject u in units)
        {
            if (u.name.Contains("Player") || u.name.Contains("Captain"))
            {
                UnitStatus status = u.GetComponent<UnitStatus>();
                if (status != null)
                {
                    totalCurrentHP += status.currentHP;
                    totalMaxHP += status.maxHP;
                    
                    totalCurrentMorale += status.currentMorale;
                    totalMaxMorale += status.maxMorale;
                }
            }
        }
        if (teamHPSlider != null)
        {
            teamHPSlider.maxValue = totalMaxHP;
            teamHPSlider.value = totalCurrentHP;
            if (teamHPText) teamHPText.text = $"{totalCurrentHP} / {totalMaxHP}";
        }
        if (teamMoraleSlider != null)
        {
            teamMoraleSlider.maxValue = totalMaxMorale;
            teamMoraleSlider.value = totalCurrentMorale;
            if (teamMoraleText) teamMoraleText.text = $"{totalCurrentMorale} / {totalMaxMorale}";
        }
        if (roundText && turnManager)
        {
            roundText.text = turnManager.currentRound.ToString();
        }
    }
}