using UnityEngine;

public class UnitAttack : MonoBehaviour
{
    [Header("Stats")]
    public int meleeDamage = 20;
    public int rangedDamage = 15;
    public int attackEnergyCost = 1;

    private UnitStatus myStatus;
    private UnitMovement myMovement;
    private EnergyManager energyManager;
    private GridManager gridManager; 

    private void Start()
    {
        myStatus = GetComponent<UnitStatus>();
        myMovement = GetComponent<UnitMovement>();
        
        energyManager = FindFirstObjectByType<EnergyManager>();
        gridManager = FindFirstObjectByType<GridManager>();
    }
    
    public void SetupManagers(GridManager grid, EnergyManager energy)
    {
        this.gridManager = grid;
        this.energyManager = energy;
    }

    public void TryMeleeAttack()
    {
        if (!CanAct()) return;
        
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (!energyManager.TrySpendEnergy(attackEnergyCost)) return;

        UnitStatus target = FindNearestEnemy();
        if (target != null)
        {
            if (IsBlockedByRow(target)) 
            {
                Debug.Log("Attack Blocked by Obstacle in Row!");
                return;
            }

            var bonuses = GetStandingBonuses();
            
            float drunkMod = myStatus.isTooDrunk ? 0.8f : 1.0f;
            int finalDmg = Mathf.RoundToInt(meleeDamage * drunkMod);
            
            target.TakeDamage(finalDmg, this.gameObject, true, bonuses.hp, bonuses.morale, bonuses.applyCurse);
            
            myStatus.ReduceBuzz(myStatus.buzzDecayOnAttack);
            myMovement.hasAttacked = true; 
            GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    public void TryRangedAttack()
    {
        if (!CanAct()) return;
        if (myStatus.currentArrows <= 0) return;
        
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (!energyManager.TrySpendEnergy(attackEnergyCost)) return;

        UnitStatus target = FindNearestEnemy();
        if (target != null)
        {
            if (IsBlockedByRow(target)) 
            {
                 Debug.Log("Attack Blocked by Obstacle in Row!");
                 myStatus.currentArrows--; 
                 return;
            }

            var bonuses = GetStandingBonuses();

            float drunkMod = myStatus.isTooDrunk ? 0.8f : 1.0f;
            int finalDmg = Mathf.RoundToInt(rangedDamage * drunkMod);

            myStatus.currentArrows--;
            target.TakeDamage(finalDmg, this.gameObject, false, bonuses.hp, bonuses.morale, bonuses.applyCurse);
            
            myStatus.ReduceBuzz(myStatus.buzzDecayOnAttack);
            myMovement.hasAttacked = true;
            GetComponent<MeshRenderer>().material.color = Color.gray;
        }
    }

    (int hp, int morale, bool applyCurse) GetStandingBonuses()
    {
        int totalHP = 0;
        int totalMorale = 0;
        bool applyCurse = false;

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            Vector2Int pos = gridManager.WorldToGridPosition(transform.position);
            GridCell cell = gridManager.GetCell(pos.x, pos.y);
            
            if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
            {
                HazardInstance hazardInst = cell.hazardVisualObject.GetComponent<HazardInstance>();
                if (hazardInst != null && hazardInst.data != null)
                {
                    totalHP += hazardInst.data.standingBonusHP;
                    totalMorale += hazardInst.data.standingBonusMorale;
                    if (hazardInst.data.standingAppliesCurse) applyCurse = true;
                }
            }
        }
        return (totalHP, totalMorale, applyCurse);
    }

    bool IsBlockedByRow(UnitStatus target)
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null) return false; 

        Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
        Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);

        if (myPos.y != targetPos.y) return false; 

        int startX = Mathf.Min(myPos.x, targetPos.x) + 1;
        int endX = Mathf.Max(myPos.x, targetPos.x);

        for (int x = startX; x < endX; x++)
        {
            GridCell cell = gridManager.GetCell(x, myPos.y);
            if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
            {
                HazardInstance hazard = cell.hazardVisualObject.GetComponent<HazardInstance>();
                if (hazard != null && (hazard.isHardObstacle || hazard.isSoftObstacle))
                {
                    hazard.TakeObstacleDamage(100); 
                    return true; 
                }
            }
        }
        return false;
    }
    
    UnitStatus FindNearestEnemy()
    {
        GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
        UnitStatus nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject unitObj in allUnits)
        {
            if (unitObj == this.gameObject) continue;
            UnitStatus status = unitObj.GetComponent<UnitStatus>();
            if (status.hasSurrendered) continue;

            bool isMyEnemy = false;
            if (name.Contains("Player") || name.Contains("Captain")) { if (unitObj.name.Contains("Enemy")) isMyEnemy = true; }
            else if (name.Contains("Enemy")) { if (!unitObj.name.Contains("Enemy")) isMyEnemy = true; }

            if (isMyEnemy)
            {
                float distX = Mathf.Abs(transform.position.x - unitObj.transform.position.x);
                float distZ = Mathf.Abs(transform.position.z - unitObj.transform.position.z);
                float dist = distX + distZ;
                if (dist < minDistance) { minDistance = dist; nearest = status; }
            }
        }
        return nearest;
    }

    bool CanAct()
    {
        if (myStatus.hasSurrendered) return false;
        if (myStatus.isStunned) return false;
        if (myMovement.hasAttacked) return false; 
        return true;
    }
}