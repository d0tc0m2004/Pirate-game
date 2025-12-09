using UnityEngine;

public class UnitStatus : MonoBehaviour
{
    [Header("HP Stats")]
    public int maxHP = 100;
    public int currentHP;

    [Header("Morale Stats")]
    public int maxMorale = 100;
    public int currentMorale;
    public int surrenderThreshold = 20; 

    [Header("Buzz Stats")]
    public int currentBuzz = 0;
    public int maxBuzz = 100;
    public int buzzDecayPerTurn = 15;
    public int buzzDecayOnAttack = 25; 
    public bool isTooDrunk => currentBuzz >= maxBuzz;

    [Header("Ammo")]
    public int maxArrows = 10;
    public int currentArrows;

    [Header("Status Flags")]
    public bool isStunned = false;  
    public bool isTrapped = false;  
    public bool hasSurrendered = false;

    // Compatibility for Hazards
    public bool isCursed => curseCharges > 0; 
    public int curseCharges = 0; 
    private float curseMultiplier = 1.5f;

    [Header("Visuals")]
    public GameObject whiteFlagVisual; 
    
    // Focus Fire Logic
    [HideInInspector] public GameObject lastAttacker;
    [HideInInspector] public int mvStacks = 0; 
    private readonly float[] mvMultipliers = { 0f, 0.10f, 0.25f, 0.45f, 0.65f };
    private int stunDuration = 0; 

    private void Start()
    {
        currentHP = maxHP;
        currentMorale = maxMorale;
        currentArrows = maxArrows;
        if (whiteFlagVisual != null) whiteFlagVisual.SetActive(false);
    }

    // --- RUM SYSTEM ---
    public void DrinkRum(string type)
    {
        currentBuzz += 30; 
        if (currentBuzz > maxBuzz) currentBuzz = maxBuzz;

        if (type == "Health")
        {
            currentHP = Mathf.Min(maxHP, currentHP + 20);
            Debug.Log($"{name} drank Health Rum.");
        }
        else if (type == "Morale")
        {
            currentMorale = Mathf.Min(maxMorale, currentMorale + 20);
            Debug.Log($"{name} drank Morale Rum.");
        }
    }

    public void ReduceBuzz(int amount)
    {
        if (currentBuzz > 0)
        {
            currentBuzz -= amount;
            if (currentBuzz < 0) currentBuzz = 0;
        }
    }

    // --- DAMAGE LOGIC ---
    public void TakeDamage(int rawDamage, GameObject source, bool isMelee, int flatBonusHP = 0, int flatBonusMorale = 0, bool applyCurse = false)
    {
        if (hasSurrendered) return; 

        if (applyCurse) SetCurse(true, 1.5f);
        UpdateFocusFireStacks(source);

        // 1. Check Cover (REAL LOGIC RESTORED)
        float damageMod = 1.0f;
        if (CheckCover(source)) 
        {
            damageMod -= 0.10f;
            Debug.Log("Cover Reduction Applied!");
        }

        // 2. HP Math
        float hpMultiplier = (!isMelee) ? 1.1f : 1.0f;
        float currentCurseMod = (curseCharges > 0) ? curseMultiplier : 1.0f;

        int finalHP = Mathf.RoundToInt(rawDamage * damageMod * hpMultiplier * currentCurseMod) + flatBonusHP;
        currentHP -= finalHP;
        Debug.Log($"{name} took {finalHP} HP Dmg.");

        // 3. Morale Math
        float moraleMultiplier = (isMelee) ? 1.1f : 1.0f; 
        float focusFireBonus = (mvStacks < mvMultipliers.Length) ? mvMultipliers[mvStacks] : 0.65f;
        int finalMorale = Mathf.RoundToInt(rawDamage * damageMod * moraleMultiplier * (1.0f + focusFireBonus)) + flatBonusMorale;
        
        TakeMoraleDamage(finalMorale);

        if (curseCharges > 0) curseCharges--;
        if (currentHP <= 0) Die();
    }

    // --- REAL HELPER FUNCTIONS (No Placeholders) ---
    bool CheckCover(GameObject attacker)
    {
        if (attacker == null) return false;
        
        float dist = Vector3.Distance(transform.position, attacker.transform.position);
        Vector3 dir = (attacker.transform.position - transform.position).normalized; // Direction TO attacker
        
        // Raycast logic: Check if obstacle exists between me and attacker
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up, dir, dist);
        
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject || hit.collider.gameObject == attacker) continue;
            
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null && cell.hasHazard) return true; // Found Cover!
        }
        return false;
    }

    public void SetCurse(bool state, float multiplier) 
    {
        if (state) { curseCharges = 2; curseMultiplier = multiplier; } else curseCharges = 0;
    }

    public void ApplyStun(int duration)
    {
        isStunned = true;
        stunDuration = duration;
        Debug.Log($"{name} is Stunned!");
    }

    public void ApplyTrap()
    {
        isTrapped = true;
        Debug.Log($"{name} is Trapped!");
    }

    public void OnTurnEnd()
    {
        if (isStunned)
        {
            stunDuration--;
            if (stunDuration <= 0) isStunned = false;
        }
    }
    
    public void OnTurnStart()
    {
        if (isTrapped) isTrapped = false;
        ReduceBuzz(buzzDecayPerTurn);
    }

    void UpdateFocusFireStacks(GameObject source) {
        if (lastAttacker != source) { mvStacks = 1; lastAttacker = source; } else { mvStacks++; if (mvStacks > 4) mvStacks = 4; }
    }
    public void TakeMoraleDamage(int amount) {
        currentMorale -= amount;
        if (currentMorale < 0) currentMorale = 0;
        if (currentMorale < surrenderThreshold && !hasSurrendered) Surrender();
    }
    void Surrender() {
        hasSurrendered = true;
        GetComponent<MeshRenderer>().material.color = Color.grey;
        if (whiteFlagVisual != null) whiteFlagVisual.SetActive(true);
        tag = "Untagged"; 
        BroadcastMessageToAllies("AllySurrender"); 
        BroadcastMessageToEnemies("EnemySurrender"); 
    }
    void Die() {
        BroadcastMessageToAllies("AllyDeath"); 
        BroadcastMessageToEnemies("EnemyDeath"); 
        Destroy(gameObject); 
    }

    void BroadcastMessageToAllies(string eventType) {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach(var u in units) { if (IsAlly(u)) u.GetComponent<UnitStatus>().OnTeamEvent(eventType); }
    }
    void BroadcastMessageToEnemies(string eventType) {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach(var u in units) { if (!IsAlly(u)) u.GetComponent<UnitStatus>().OnTeamEvent(eventType); }
    }
    bool IsAlly(GameObject other) {
        if (name.Contains("Enemy") && other.name.Contains("Enemy")) return true;
        if (!name.Contains("Enemy") && !other.name.Contains("Enemy")) return true;
        return false;
    }
    public void OnTeamEvent(string eventType) {
        if (hasSurrendered) return;
        switch(eventType) {
            case "AllyDeath": TakeMoraleDamage(10); break; 
            case "AllySurrender": TakeMoraleDamage(5); break; 
            case "EnemyDeath": currentMorale = Mathf.Min(maxMorale, currentMorale + 15); break; 
            case "EnemySurrender": currentMorale = Mathf.Min(maxMorale, currentMorale + 10); break; 
        }
    }
}