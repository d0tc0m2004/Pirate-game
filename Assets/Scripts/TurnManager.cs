using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [Header("State")]
    public int currentRound = 1;
    public bool isPlayerTurn = false;

    [Header("References")]
    public BattleManager battleManager;
    public void StartGameLoop()
    {
        currentRound = 1;
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        isPlayerTurn = true;
        battleManager.isBattleActive = true;
        Debug.Log($"<color=white>--- ROUND {currentRound} START ---</color>");
        UnitMovement[] allUnits = FindObjectsByType<UnitMovement>(FindObjectsSortMode.None);
        foreach (UnitMovement unit in allUnits)
        {
            unit.BeginTurn(); 
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status != null) status.OnTurnStart();
        }
    }
    public void EndPlayerTurnButton()
    {
        if (!isPlayerTurn) return;

        StartCoroutine(ProcessEndTurn());
    }

    IEnumerator ProcessEndTurn()
    {
        isPlayerTurn = false;
        battleManager.isBattleActive = false;
        
        Debug.Log("Processing Turn End Effects...");

        ApplyAllTurnEndEffects();

        yield return new WaitForSeconds(1.5f);

        Debug.Log("<color=red>Enemy Turn...</color>");
        yield return new WaitForSeconds(1f); 
        currentRound++;
        StartPlayerTurn();
    }

    void ApplyAllTurnEndEffects()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");

        foreach (GameObject unit in units)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) continue;
            status.OnTurnEnd();
            CheckFloorHazards(unit);
        }
    }

    void CheckFloorHazards(GameObject unit)
    {
        RaycastHit[] hits = Physics.RaycastAll(unit.transform.position + Vector3.up, Vector3.down, 2.0f);

        foreach (RaycastHit hit in hits)
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
            {
                HazardInstance hazard = cell.hazardVisualObject.GetComponent<HazardInstance>();
                if (hazard != null)
                {
                    Debug.Log($"Hazard found under {unit.name}: {hazard.name}");
                    hazard.OnTurnEnd(unit);
                    return;
                }
            }
        }
    }
}