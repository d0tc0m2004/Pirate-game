using UnityEngine;
using System.Collections;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Hazards;
using TacticalGame.Units;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages turn flow, round progression, and turn-based effects.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Private State

        private bool isPlayerTurn = true;
        private int currentRound = 1;
        private int swapsUsedThisRound = 0;
        private EnergyManager energyManager;
        private GridManager gridManager;

        #endregion

        #region Public Properties

        public bool IsPlayerTurn => isPlayerTurn;
        public int CurrentRound => currentRound;
        public int SwapsUsedThisRound => swapsUsedThisRound;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TurnManager>();
        }

        private void Start()
        {
            energyManager = ServiceLocator.Get<EnergyManager>();
            gridManager = ServiceLocator.Get<GridManager>();
        }

        #endregion

        #region Game Loop

        /// <summary>
        /// Start the game loop (call after deployment).
        /// </summary>
        public void StartGameLoop()
        {
            currentRound = 1;
            isPlayerTurn = true;
            swapsUsedThisRound = 0;

            if (energyManager != null)
            {
                energyManager.StartTurn();
            }

            ResetUnitsForNewTurn();
            
            GameEvents.TriggerBattleStart();
            GameEvents.TriggerRoundStart(currentRound);
            GameEvents.TriggerPlayerTurnStart();
        }

        /// <summary>
        /// End the current turn.
        /// </summary>
        public void EndTurn()
        {
            // Apply end-of-turn hazard effects
            ApplyHazardEffects();

            // Handle player turn end
            if (isPlayerTurn)
            {
                if (energyManager != null)
                {
                    energyManager.EndTurn();
                }
                
                GameEvents.TriggerPlayerTurnEnd();
            }
            else
            {
                GameEvents.TriggerEnemyTurnEnd();
            }

            // Toggle turn
            isPlayerTurn = !isPlayerTurn;

            if (isPlayerTurn)
            {
                // New round begins
                currentRound++;
                swapsUsedThisRound = 0;
                
                Debug.Log($"Round {currentRound} Start! Swap limit reset.");

                if (energyManager != null)
                {
                    energyManager.StartTurn();
                }

                ResetUnitsForNewTurn();
                
                GameEvents.TriggerRoundStart(currentRound);
                GameEvents.TriggerPlayerTurnStart();
            }
            else
            {
                // Enemy turn
                Debug.Log($"Enemy Turn. Skipping in {GameConfig.Instance.enemyTurnDelay}s...");
                ResetUnitsForNewTurn();
                
                GameEvents.TriggerEnemyTurnStart();
                
                StartCoroutine(AutoSkipEnemyTurn());
            }
        }

        /// <summary>
        /// Record that a swap was used this round.
        /// </summary>
        public void UseSwap()
        {
            swapsUsedThisRound++;
        }

        /// <summary>
        /// Check if more swaps are allowed this round.
        /// </summary>
        public bool CanSwap()
        {
            return swapsUsedThisRound < GameConfig.Instance.maxSwapsPerRound;
        }

        #endregion

        #region Private Methods

        private IEnumerator AutoSkipEnemyTurn()
        {
            yield return new WaitForSeconds(GameConfig.Instance.enemyTurnDelay);
            EndTurn();
        }

        private void ApplyHazardEffects()
        {
            GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unit in units)
            {
                if (unit == null) continue;

                // Find hazard under unit via raycast
                RaycastHit[] hits = Physics.RaycastAll(
                    unit.transform.position + Vector3.up, 
                    Vector3.down, 
                    2.0f
                );

                foreach (var hit in hits)
                {
                    GridCell cell = hit.collider.GetComponent<GridCell>();
                    if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
                    {
                        HazardInstance hazard = cell.HazardVisualObject.GetComponent<HazardInstance>();
                        if (hazard != null)
                        {
                            hazard.OnTurnEnd(unit);
                        }
                    }
                }
            }
        }

        private void ResetUnitsForNewTurn()
        {
            GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unit in units)
            {
                if (unit == null) continue;

                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.BeginTurn();
                }

                UnitStatus status = unit.GetComponent<UnitStatus>();
                if (status != null)
                {
                    status.OnTurnStart();
                }
            }
        }

        #endregion
    }
}