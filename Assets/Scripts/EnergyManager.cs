using UnityEngine;
using TMPro;

public class EnergyManager : MonoBehaviour
{
    [Header("Stats")]
    public int maxEnergy = 3;
    public int currentEnergy;
    public int grogTokens = 0;

    [Header("UI References")]
    public TMP_Text energyText;
    public TMP_Text grogText;

    public void StartTurn()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
    }

    public void EndTurn()
    {
        if (currentEnergy > 0)
        {
            grogTokens += currentEnergy;
            Debug.Log($"Converted {currentEnergy} Energy into Grog! Total Grog: {grogTokens}");
            currentEnergy = 0;
        }
        UpdateUI();
    }

    public bool TrySpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            UpdateUI();
            return true;
        }
        Debug.Log("Not enough Energy!");
        return false;
    }

    public bool TrySpendGrog(int amount)
    {
        if (grogTokens >= amount)
        {
            grogTokens -= amount;
            UpdateUI();
            return true;
        }
        Debug.Log("Not enough Grog!");
        return false;
    }

    void UpdateUI()
    {
        if (energyText) energyText.text = $"Energy: {currentEnergy}/{maxEnergy}";
        if (grogText) grogText.text = $"Grog: {grogTokens}";
    }
}