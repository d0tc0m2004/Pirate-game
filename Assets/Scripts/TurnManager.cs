using UnityEngine;
using System.Collections; 

public class TurnManager : MonoBehaviour
{
    public bool isPlayerTurn = true;
    public int currentRound = 1;
    
    [Header("References")]
    public EnergyManager energyManager; // Link in Inspector

    public void StartGameLoop()
    {
        currentRound = 1;
        isPlayerTurn = true;
        
        // Refill Energy at start of combat
        if (energyManager != null) energyManager.StartTurn();
        
        ResetUnitsForNewTurn();
    }

    public void EndTurn()
    {
        // 1. IF ENDING PLAYER TURN: Convert leftover Energy to Grog
        if (isPlayerTurn && energyManager != null)
        {
            energyManager.EndTurn(); 
        }

        // Switch Sides
        isPlayerTurn = !isPlayerTurn;

        if (isPlayerTurn)
        {
            // --- PLAYER TURN STARTS AGAIN ---
            currentRound++; 
            Debug.Log("Round " + currentRound + " Start!");
            
            // 2. REFILL ENERGY TO 3
            if (energyManager != null) energyManager.StartTurn();

            ResetUnitsForNewTurn();
        }
        else
        {
            // --- ENEMY TURN START ---
            Debug.Log("Enemy Turn Started (No AI). Skipping back to player in 1.5 seconds...");
            ResetUnitsForNewTurn();
            
            // AUTOMATICALLY SKIP ENEMY TURN (So you can keep playing)
            StartCoroutine(AutoSkipEnemyTurn());
        }
    }

    // A temporary helper to loop the game until you add AI later
    IEnumerator AutoSkipEnemyTurn()
    {
        yield return new WaitForSeconds(1.5f); // Wait so you can see the UI change
        EndTurn(); // Loop back to Player
    }

    void ResetUnitsForNewTurn()
    {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach (GameObject unit in units)
        {
            // Reset Movement
            UnitMovement move = unit.GetComponent<UnitMovement>();
            if (move != null) move.BeginTurn();

            // Reset Status Effects (Stun duration, Trap checks)
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status != null) status.OnTurnStart();
        }
    }
}