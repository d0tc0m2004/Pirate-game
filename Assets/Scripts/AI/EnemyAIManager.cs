using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TacticalGame.Core;
using TacticalGame.Managers;

namespace TacticalGame.AI
{
    /// <summary>
    /// Coordinates the AI logic across all enemies.
    /// Replaces the AutoSkipEnemyTurn functionality.
    /// </summary>
    public class EnemyAIManager : MonoBehaviour
    {
        private TurnManager turnManager;

        private bool isRecalculating = false;

        private void Awake()
        {
            ServiceLocator.Register(this);
            
            if (GetComponent<TacticalGame.Managers.EnemyEnergyManager>() == null) gameObject.AddComponent<TacticalGame.Managers.EnemyEnergyManager>();
            if (GetComponent<EnemyDeckManager>() == null) gameObject.AddComponent<EnemyDeckManager>();
            
            GameEvents.OnPlayerTurnStart += InitializeTurn;
            GameEvents.OnBoardStateChanged += RequestRecalculation;
            GameEvents.OnBattleStart += BuildDeckForBattle;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EnemyAIManager>();
            GameEvents.OnPlayerTurnStart -= InitializeTurn;
            GameEvents.OnBoardStateChanged -= RequestRecalculation;
            GameEvents.OnBattleStart -= BuildDeckForBattle;
        }

        private void Start()
        {
            turnManager = ServiceLocator.Get<TurnManager>();
        }

        private void BuildDeckForBattle()
        {
            var deckManager = ServiceLocator.Get<EnemyDeckManager>();
            if (deckManager != null)
            {
                deckManager.BuildDeckFromScene();
            }
        }

        /// <summary>
        /// Called at the start of the Player's turn to initialize enemy resources and calculate initial intents.
        /// </summary>
        private void InitializeTurn()
        {
            // 1. Refresh Energy
            var energyManager = ServiceLocator.Get<TacticalGame.Managers.EnemyEnergyManager>();
            if (energyManager != null) energyManager.StartTurn();

            // 2. Refresh Hand
            var deckManager = ServiceLocator.Get<EnemyDeckManager>();
            if (deckManager != null) deckManager.DrawToFillHand();

            CalculateAllIntents();
        }

        private void RequestRecalculation()
        {
            // Only dynamically recalculate during the player's turn!
            if (turnManager != null && !turnManager.IsPlayerTurn) return;

            if (!isRecalculating)
            {
                isRecalculating = true;
                StartCoroutine(RecalculateRoutine());
            }
        }

        private IEnumerator RecalculateRoutine()
        {
            // Wait for end of frame so we don't recalculate 10 times instantly if multiple events fire
            yield return new WaitForEndOfFrame();
            
            CalculateAllIntents();
            isRecalculating = false;
        }

        /// <summary>
        /// Evaluates current hand and locks intents for all enemies dynamically.
        /// </summary>
        private void CalculateAllIntents()
        {
            var deckManager = ServiceLocator.Get<EnemyDeckManager>();
            var energyManager = ServiceLocator.Get<TacticalGame.Managers.EnemyEnergyManager>();

            var enemies = FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            
            // 3. Clear existing intents
            foreach (var enemy in enemies)
            {
                enemy.ClearIntent();
            }

            // 4. Commander Logic
            if (deckManager != null && energyManager != null)
            {
                // We track available energy during this planning phase without actually deducting it yet
                int planningEnergy = energyManager.CurrentEnergy;
                Debug.Log($"<color=yellow>[EnemyAIManager] Planning Intents. Starting Energy: {planningEnergy}. Hand size: {deckManager.Hand.Count}</color>");
                
                // Sort hand to play highest energy / most impactful cards first
                var sortedHand = new List<TacticalGame.Equipment.BattleCard>(deckManager.Hand);
                sortedHand.Sort((a, b) => b.energyCost.CompareTo(a.energyCost));

                foreach (var card in sortedHand)
                {
                    if (planningEnergy >= card.energyCost && card.ownerUnit != null && !card.ownerUnit.HasSurrendered)
                    {
                        var ownerBrain = card.ownerUnit.GetComponent<EnemyBrain>();
                        // Only assign if the unit hasn't been assigned an action yet
                        if (ownerBrain != null && ownerBrain.CurrentIntent == null)
                        {
                            bool success = ownerBrain.EvaluateAndLockCardIntent(card);
                            Debug.Log($"<color=yellow>[EnemyAIManager] Attempted to lock {card.GetDisplayName()} for {card.ownerUnit.UnitName}. Success: {success}. Cost: {card.energyCost}</color>");
                            if (success)
                            {
                                planningEnergy -= card.energyCost;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"<color=grey>[EnemyAIManager] Skipped {card.GetDisplayName()} (Cost: {card.energyCost}, PlanningEnergy: {planningEnergy}, Owner: {card.ownerUnit?.UnitName})</color>");
                    }
                }
            }

            // 5. Fallback for units that didn't get a card
            // REMOVED: Enemies must use cards to act in the new card-driven system
            /*
            foreach (var enemy in enemies)
            {
                if (enemy.CurrentIntent == null)
                {
                    enemy.EvaluateAndLockIntent();
                }
            }
            */
        }

        /// <summary>
        /// Called by TurnManager when it's the enemy's turn.
        /// </summary>
        public void StartEnemyTurn()
        {
            StartCoroutine(ExecuteEnemyTurnCoroutine());
        }

        private IEnumerator ExecuteEnemyTurnCoroutine()
        {
            Debug.Log("<color=magenta>--- ENEMY TURN START ---</color>");
            yield return new WaitForSeconds(0.5f); // Small pause for UX

            var enemies = FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            
            // Execute each enemy one by one
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue; // In case an enemy died during the turn

                // Tell the enemy to execute its locked intent
                yield return enemy.ExecuteLockedIntent();
                
                // Small delay between enemy actions
                yield return new WaitForSeconds(0.3f);
            }

            // Discard the remaining cards and reset energy
            var deckManager = ServiceLocator.Get<EnemyDeckManager>();
            if (deckManager != null) deckManager.DiscardAllCards();
            
            var energyManager = ServiceLocator.Get<TacticalGame.Managers.EnemyEnergyManager>();
            if (energyManager != null) energyManager.EndTurn();

            Debug.Log("<color=magenta>--- ENEMY TURN END ---</color>");
            yield return new WaitForSeconds(0.5f);

            // Pass the turn back to the player
            if (turnManager != null)
            {
                turnManager.EndTurn();
            }
        }
    }
}
