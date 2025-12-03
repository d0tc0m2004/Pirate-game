using UnityEngine;

public class HazardInstance : MonoBehaviour
{
    private HazardData data;
    private int hazardDurability; 
    private GridCell currentCell;

    public void Initialize(HazardData hazardData, GridCell cell)
    {
        data = hazardData;
        currentCell = cell;
        hazardDurability = data.maxHealth; 
    }

    public void OnTurnEnd(GameObject unit)
    {
        if (unit == null) return;
        UnitStatus status = unit.GetComponent<UnitStatus>();
        if (status == null) return;

        switch (data.effectType)
        {
            case HazardData.HazardEffectType.Fire:
                status.TakeDamage(data.damageHP);
                break;
            case HazardData.HazardEffectType.Plague:
                status.TakeMoraleDamage(data.damageMorale);
                break;
            case HazardData.HazardEffectType.ShiftingSand:
                status.TakeMoraleDamage(data.damageMorale);
                break;
            case HazardData.HazardEffectType.Lightning:
                if (Random.value > 0.5f) 
                {
                    status.ApplyStun(data.effectDuration); 
                    Debug.Log("Lightning STRIKE!");
                }
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

    
    public void TakeDamage(int amount)
    {
        if (!data.isDestructible) return;

        hazardDurability -= amount; 
        if (hazardDurability <= 0) DestroyHazard();
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

    private void OnMouseDown() { TakeDamage(1); }
}