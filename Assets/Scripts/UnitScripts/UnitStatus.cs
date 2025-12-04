using UnityEngine;

public class UnitStatus : MonoBehaviour
{
    [Header("HP Stats")]
    public int maxHP = 100;
    public int currentHP;

    [Header("Morale Stats")]
    public int maxMorale = 100;
    public int currentMorale;

    [Header("Status Flags")]
    public bool isStunned = false;  
    public bool isTrapped = false;  
    public bool isCursed = false;   
    
    // This variable was missing in your screenshot, causing the error!
    private int stunDuration = 0; 

    private float damageMultiplier = 1.0f; 

    private void Start()
    {
        currentHP = maxHP;
        currentMorale = maxMorale;
    }

    public void ApplyTrap()
    {
        isTrapped = true;
        Debug.Log($"<color=orange>TRAPPED:</color> {name} caught in a Trap! Cannot Move.");
    }

    // Accepts duration to fix CS1501 error
    public void ApplyStun(int duration)
    {
        isStunned = true;
        stunDuration = duration;
        Debug.Log($"<color=cyan>STUNNED:</color> {name} struck by Lightning! Cannot Act for {duration} turn(s).");
    }

    public void SetCurse(bool state, float multiplier)
    {
        isCursed = state;
        damageMultiplier = state ? multiplier : 1.0f;
        if(state) Debug.Log($"<color=purple>CURSED:</color> {name} feels vulnerable.");
    }

    public void TakeDamage(int amount)
    {
        float finalDamage = amount * damageMultiplier;
        int damageInt = Mathf.RoundToInt(finalDamage);

        currentHP -= damageInt;
        Debug.Log($"{name} took {damageInt} HP Damage. (HP: {currentHP})");

        if (currentHP <= 0) Die();
    }

    public void TakeMoraleDamage(int amount)
    {
        currentMorale -= amount;
        Debug.Log($"{name} took {amount} Morale Damage. (Morale: {currentMorale})");
    }

    // This function fixes the "UnitStatus does not contain definition for OnTurnEnd" error
    public void OnTurnEnd()
    {
        // Handle Stun Timer
        if (isStunned)
        {
            stunDuration--;
            if (stunDuration <= 0)
            {
                isStunned = false;
                Debug.Log($"{name} is no longer stunned.");
            }
        }
    }

    // This function fixes the "OnTurnStart" error
    public void OnTurnStart()
    {
        if (isTrapped) isTrapped = false; 
        isCursed = false;
        damageMultiplier = 1.0f; 
    }

    void Die()
    {
        Debug.Log($"{name} has been defeated!");
        Destroy(gameObject);
    }
}