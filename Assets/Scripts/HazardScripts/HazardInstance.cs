using UnityEngine;

public class HazardInstance : MonoBehaviour
{
    public HazardData data;
    private GridCell currentCell;
    
    [Header("Obstacle Stats")]
    public bool isSoftObstacle = false; 
    public bool isHardObstacle = false; 
    public int obstacleHP = 2; 

    public void Initialize(HazardData hazardData, GridCell cell)
    {
        data = hazardData;
        currentCell = cell;
        
        if (data.effectType == HazardData.HazardEffectType.Box) isSoftObstacle = true;
        if (data.effectType == HazardData.HazardEffectType.Boulder) isHardObstacle = true;
        
        if (isSoftObstacle || isHardObstacle) obstacleHP = data.maxHealth;
    }

    public void TakeObstacleDamage(int amount)
    {
        if (isHardObstacle) 
        {
            Debug.Log("Clang! Hard obstacle took no damage.");
            return;
        }

        if (isSoftObstacle)
        {
            obstacleHP -= amount;
            Debug.Log($"Obstacle hit! HP left: {obstacleHP}");
            
            if (obstacleHP <= 0)
            {
                Debug.Log("Obstacle Destroyed!");
                DestroyHazard();
            }
        }
    }

    public void OnTurnEnd(GameObject unit)
    {
        if (unit == null) return;
        UnitStatus status = unit.GetComponent<UnitStatus>();
        if (status == null) return;

        switch (data.effectType)
        {
            case HazardData.HazardEffectType.Fire:
                status.TakeDamage(data.damageHP, this.gameObject, false);
                break;

            case HazardData.HazardEffectType.Plague:
                status.TakeMoraleDamage(data.damageMorale);
                break;

            case HazardData.HazardEffectType.ShiftingSand:
                status.TakeMoraleDamage(data.damageMorale);
                break;

            case HazardData.HazardEffectType.Lightning:
                if (Random.value > 0.5f) 
                    status.ApplyStun(data.effectDuration); 
                break;

            case HazardData.HazardEffectType.Cursed:
                status.SetCurse(true, data.curseMultiplier);
                break;
        }
    }

    public void OnUnitEnter(GameObject unit)
    {
        UnitStatus status = unit.GetComponent<UnitStatus>();
        if (status == null) return;

        switch (data.effectType)
        {
            case HazardData.HazardEffectType.Trap:
                status.ApplyTrap();
                DestroyHazard(); 
                break;

            case HazardData.HazardEffectType.Cursed:
                status.SetCurse(true, data.curseMultiplier);
                break;
        }
    }

    void DestroyHazard()
    {
        if (data.effectType == HazardData.HazardEffectType.Box && data.dropItem != null)
        {
            Instantiate(data.dropItem, transform.position, Quaternion.identity);
        }

        if (currentCell != null)
        {
            currentCell.hasHazard = false;
            currentCell.isBlocked = false;
            currentCell.hazardVisualObject = null;
        }
        Destroy(gameObject);
    }
    
    private void OnMouseDown() { TakeObstacleDamage(1); }
}