using UnityEngine;
using System.Collections;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Hazards;
using TacticalGame.Units;
using TacticalGame.Combat;
using TacticalGame.Enums;

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
        private bool playerWentFirst = true; // Track who went first this round
        private EnergyManager energyManager;
        private GridManager gridManager;

        #endregion

        #region Public Properties

        public bool IsPlayerTurn => isPlayerTurn;
        public int CurrentRound => currentRound;
        public int SwapsUsedThisRound => swapsUsedThisRound;
        
        /// <summary>
        /// Returns true if the current acting team went first this round.
        /// Used for first-action damage bonus.
        /// </summary>
        public bool IsFirstActionTeam => (isPlayerTurn && playerWentFirst) || (!isPlayerTurn && !playerWentFirst);

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
            swapsUsedThisRound = 0;

            // Calculate initiative to determine who goes first
            var initiative = InitiativeSystem.CalculateInitiative();
            playerWentFirst = (initiative.FirstTeam == Team.Player);
            isPlayerTurn = playerWentFirst;

            if (energyManager != null)
            {
                energyManager.StartTurn();
            }

            ResetUnitsForNewTurn();
            
            GameEvents.TriggerBattleStart();
            GameEvents.TriggerRoundStart(currentRound);
            
            if (isPlayerTurn)
            {
                GameEvents.TriggerPlayerTurnStart();
            }
            else
            {
                GameEvents.TriggerEnemyTurnStart();
                StartCoroutine(AutoSkipEnemyTurn());
            }
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

            // Check if we've completed a full round (both teams have acted)
            bool roundComplete = (isPlayerTurn == playerWentFirst);

            if (roundComplete)
            {
                // New round begins
                currentRound++;
                swapsUsedThisRound = 0;
                
                // Recalculate initiative for the new round
                var initiative = InitiativeSystem.CalculateInitiative();
                playerWentFirst = (initiative.FirstTeam == Team.Player);
                isPlayerTurn = playerWentFirst;
                
                Debug.Log($"Round {currentRound} Start! Swap limit reset.");

                if (energyManager != null && isPlayerTurn)
                {
                    energyManager.StartTurn();
                }

                ResetUnitsForNewTurn();
                
                GameEvents.TriggerRoundStart(currentRound);
            }

            if (isPlayerTurn)
            {
                if (!roundComplete && energyManager != null)
                {
                    energyManager.StartTurn();
                }
                ResetUnitsForNewTurn();
                GameEvents.TriggerPlayerTurnStart();
            }
            else
            {
                ResetUnitsForNewTurn();
                GameEvents.TriggerEnemyTurnStart();
                Debug.Log($"Enemy Turn. Skipping in {GameConfig.Instance.enemyTurnDelay}s...");
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

                // Reset combo counter for Skill system
                UnitAttack attack = unit.GetComponent<UnitAttack>();
                if (attack != null)
                {
                    attack.ResetCombo();
                }
            }
        }

        #endregion
    }
}