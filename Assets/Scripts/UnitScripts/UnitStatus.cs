using UnityEngine;

public class UnitStatus : MonoBehaviour
{
    [Header("Base Stats")]
    public string unitName;
    public string role;
    public string weaponType; 
    
    public int maxHP = 100;
    public int currentHP;
    public int maxMorale = 100;
    public int currentMorale;
    
    public int grit;
    public int buzz;
    public int power;
    public int aim;
    public int proficiency;
    public int skill;
    public int tactics;
    public int speed;
    public int hull;

    [Header("Combat Stats")]
    public int currentBuzz = 0;
    public int maxBuzz = 100;
    public int buzzDecayPerTurn = 15;
    public int buzzDecayOnAttack = 25; 
    public bool isTooDrunk => currentBuzz >= maxBuzz;

    public bool isExposed = false; 
    public int swapCooldown = 0;
    public int surrenderThreshold = 20; 

    [Header("Ammo")]
    public int maxArrows = 10;
    public int currentArrows;

    [Header("Flags")]
    public bool isStunned = false;  
    public bool isTrapped = false;  
    public bool hasSurrendered = false;

    public bool isCursed => curseCharges > 0; 
    public int curseCharges = 0; 
    private float curseMultiplier = 1.5f;

    [Header("Visuals")]
    public GameObject whiteFlagVisual; 
    
    [HideInInspector] public GameObject lastAttacker;
    [HideInInspector] public int mvStacks = 0; 
    
    private readonly float[] mvMultipliers = { 0f, 0f, 0.10f, 0.25f, 0.45f, 0.65f };
    private int stunDuration = 0; 

    private GridManager gridManager;

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        
        if (currentHP == 0) currentHP = maxHP;
        if (currentMorale == 0) currentMorale = maxMorale;
        
        currentArrows = maxArrows;
        if (whiteFlagVisual != null) whiteFlagVisual.SetActive(false);
    }

    public void Initialize(UnitData data)
    {
        unitName = data.unitName;
        role = data.role;
        weaponType = data.weaponType; 

        maxHP = data.health;
        currentHP = maxHP;
        maxMorale = data.morale;
        currentMorale = maxMorale;
        grit = data.grit;
        buzz = data.buzz; 
        power = data.power;
        aim = data.aim;
        proficiency = data.proficiency;
        skill = data.skill;
        tactics = data.tactics;
        speed = data.speed;
        hull = data.hull;
    }

    public void DrinkRum(string type)
    {
        currentBuzz += 30; 
        if (currentBuzz > maxBuzz) currentBuzz = maxBuzz;

        if (type == "Health") currentHP = Mathf.Min(maxHP, currentHP + 20);
        else if (type == "Morale") currentMorale = Mathf.Min(maxMorale, currentMorale + 20);
    }

    public void ReduceBuzz(int amount)
    {
        if (currentBuzz > 0)
        {
            currentBuzz -= amount;
            if (currentBuzz < 0) currentBuzz = 0;
        }
    }

    public void TakeDamage(int rawDamage, GameObject source, bool isMelee, int flatBonusHP = 0, int flatBonusMorale = 0, bool applyCurse = false)
    {
        if (hasSurrendered) return; 

        UpdateFocusFireStacks(source);
        if (applyCurse) SetCurse(true, 1.5f);

        string logHP = $"{rawDamage} Base";
        float damageMod = 1.0f;
        if (CheckAdjacencyCover()) { damageMod -= 0.10f; logHP += " -10%(Cover)"; }

        float hpMultiplier = (!isMelee) ? 1.1f : 1.0f;
        if (!isMelee) logHP += " +10%(Ranged)";

        float currentCurseMod = (curseCharges > 0) ? curseMultiplier : 1.0f;
        if (curseCharges > 0) logHP += " x1.5(Curse)";

        float swapPenaltyMod = isExposed ? 1.2f : 1.0f;
        if (isExposed) logHP += " +20%(Exposed)";

        int calculatedDamage = Mathf.RoundToInt(rawDamage * damageMod * hpMultiplier * currentCurseMod * swapPenaltyMod);
        int finalHP = calculatedDamage + flatBonusHP;
        if (flatBonusHP > 0) logHP += $" +{flatBonusHP}(HazardBonus)";
        
        currentHP -= finalHP;

        string logMorale = "";
        float moraleMultiplier = (isMelee) ? 1.1f : 1.0f; 
        int index = Mathf.Clamp(mvStacks, 0, mvMultipliers.Length - 1);
        float focusFireBonus = mvMultipliers[index];
        
        int finalMorale = Mathf.RoundToInt(rawDamage * damageMod * moraleMultiplier * (1.0f + focusFireBonus) * swapPenaltyMod) + flatBonusMorale;
        logMorale += $"{rawDamage} Base";
        if (isMelee) logMorale += " +10%(Melee)";
        if (focusFireBonus > 0) logMorale += $" +{Mathf.RoundToInt(focusFireBonus*100)}%(FocusFire x{mvStacks})";
        if (isExposed) logMorale += " +20%(Exposed)";
        if (flatBonusMorale > 0) logMorale += $" +{flatBonusMorale}(HazardBonus)";
        
        TakeMoraleDamage(finalMorale);

        string attackerName = source != null ? source.name : "Unknown Source";
        Debug.Log($"<color=red><b>DAMAGE REPORT: {name}</b></color>\n" +
                  $"<b>Attacker:</b> {attackerName}\n" +
                  $"<b>HP Lost: {finalHP}</b>  [{logHP}]\n" +
                  $"<b>Morale Lost: {finalMorale}</b>  [{logMorale}]");
        
        if (curseCharges > 0) curseCharges--;
        if (currentHP <= 0) Die();
    }

    public void ApplySwapPenalty()
    {
        int penalty = Mathf.RoundToInt(currentMorale * 0.15f);
        currentMorale -= penalty;
        isExposed = true;
    }

    void UpdateFocusFireStacks(GameObject source) 
    {
        if (lastAttacker != source) { mvStacks = 1; lastAttacker = source; } 
        else { mvStacks++; if (mvStacks >= mvMultipliers.Length) mvStacks = mvMultipliers.Length - 1; }
    }
    
    public void OnTurnStart()
    {
        if (isTrapped) isTrapped = false;
        ReduceBuzz(buzzDecayPerTurn);
        mvStacks = 0;
        lastAttacker = null;
        if (isExposed) isExposed = false;
        if (swapCooldown > 0) swapCooldown--;
    }
    
    public void ApplyStun(int duration) { isStunned = true; stunDuration = duration; }
    public void ApplyTrap() { isTrapped = true; }
    public void OnTurnEnd() { if (isStunned) { stunDuration--; if (stunDuration <= 0) isStunned = false; } }
    public void SetCurse(bool state, float multiplier) { if (state) { curseCharges = 2; curseMultiplier = multiplier; } else curseCharges = 0; }

    bool CheckAdjacencyCover()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null) return false;
        Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
        Vector2Int[] neighbors = { new Vector2Int(myPos.x + 1, myPos.y), new Vector2Int(myPos.x - 1, myPos.y), new Vector2Int(myPos.x, myPos.y + 1), new Vector2Int(myPos.x, myPos.y - 1) };
        foreach (Vector2Int n in neighbors) {
            GridCell cell = gridManager.GetCell(n.x, n.y);
            if (cell != null && cell.hasHazard) return true;
        }
        return false;
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
    }
    void Die() { Destroy(gameObject); }
    public void OnTeamEvent(string eventType) {}
}