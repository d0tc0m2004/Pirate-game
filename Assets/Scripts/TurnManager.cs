using UnityEngine;
using System.Collections; 

public class TurnManager : MonoBehaviour
{
    public bool isPlayerTurn = true;
    public int currentRound = 1;
    public int swapsUsedThisRound = 0;

    [Header("References")]
    public EnergyManager energyManager; 

    public void StartGameLoop()
    {
        currentRound = 1;
        isPlayerTurn = true;
        swapsUsedThisRound = 0;
        
        if (energyManager != null) energyManager.StartTurn();
        ResetUnitsForNewTurn();
    }

    public void EndTurn()
    {
        ApplyHazardEffects();

        if (isPlayerTurn && energyManager != null)
        {
            energyManager.EndTurn(); 
        }

        isPlayerTurn = !isPlayerTurn;

        if (isPlayerTurn)
        {
            currentRound++; 
            swapsUsedThisRound = 0;
            Debug.Log($"Round {currentRound} Start! Swap limit reset.");
            
            if (energyManager != null) energyManager.StartTurn();
            ResetUnitsForNewTurn();
        }
        else
        {
            Debug.Log("Enemy Turn. Skipping in 1.5s...");
            ResetUnitsForNewTurn();
            StartCoroutine(AutoSkipEnemyTurn());
        }
    }

    IEnumerator AutoSkipEnemyTurn()
    {
        yield return new WaitForSeconds(1.5f); 
        EndTurn(); 
    }

    void ApplyHazardEffects()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach (GameObject unit in units)
        {
            RaycastHit[] hits = Physics.RaycastAll(unit.transform.position + Vector3.up, Vector3.down, 2.0f);
            foreach (var hit in hits)
            {
                 GridCell cell = hit.collider.GetComponent<GridCell>();
                 if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
                 {
                     HazardInstance hazard = cell.hazardVisualObject.GetComponent<HazardInstance>();
                     if (hazard != null) hazard.OnTurnEnd(unit);
                 }
            }
        }
    }

    void ResetUnitsForNewTurn()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach (GameObject unit in units)
        {
            UnitMovement move = unit.GetComponent<UnitMovement>();
            if (move != null) move.BeginTurn();

            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status != null) status.OnTurnStart();
        }
    }
}