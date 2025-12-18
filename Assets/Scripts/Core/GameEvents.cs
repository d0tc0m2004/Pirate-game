using UnityEngine;
using System;
using TacticalGame.Grid;
using TacticalGame.Hazards;

namespace TacticalGame.Core
{
    /// <summary>
    /// Central event hub for game-wide events.
    /// Subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // === TURN EVENTS ===
        public static event Action OnPlayerTurnStart;
        public static event Action OnPlayerTurnEnd;
        public static event Action OnEnemyTurnStart;
        public static event Action OnEnemyTurnEnd;
        public static event Action<int> OnRoundStart; // int = round number

        // === UNIT EVENTS ===
        public static event Action<GameObject> OnUnitSelected;
        public static event Action OnUnitDeselected;
        public static event Action<GameObject> OnUnitDeath;
        public static event Action<GameObject> OnUnitSurrender;
        public static event Action<GameObject, int> OnUnitDamaged; // unit, damage amount
        public static event Action<GameObject, int> OnUnitHealed; // unit, heal amount
        public static event Action<GameObject, int> OnMoraleDamaged; // unit, damage amount
        public static event Action<GameObject, GameObject> OnUnitAttack; // attacker, target
        public static event Action<GameObject, GridCell, GridCell> OnUnitMoved; // unit, from, to

        // === STATUS EFFECT EVENTS ===
        public static event Action<GameObject> OnUnitStunned;
        public static event Action<GameObject> OnUnitTrapped;
        public static event Action<GameObject> OnUnitCursed;
        public static event Action<GameObject> OnUnitExposed;

        // === COMBAT EVENTS ===
        public static event Action<GameObject, GameObject, bool> OnAttackBlocked; // attacker, obstacle, was destroyed

        // === RESOURCE EVENTS ===
        public static event Action<int> OnEnergyChanged; // new value
        public static event Action<int> OnGrogChanged; // new value

        // === GAME STATE EVENTS ===
        public static event Action OnBattleStart;
        public static event Action OnBattleEnd;
        public static event Action<bool> OnGameEnd; // true = player won
        public static event Action OnDeploymentStart;
        public static event Action OnDeploymentEnd;

        // === HAZARD EVENTS ===
        public static event Action<GameObject, HazardInstance> OnUnitEnteredHazard;
        public static event Action<HazardInstance> OnHazardDestroyed;

        // === TRIGGER METHODS ===
        
        // Turn Events
        public static void TriggerPlayerTurnStart() => OnPlayerTurnStart?.Invoke();
        public static void TriggerPlayerTurnEnd() => OnPlayerTurnEnd?.Invoke();
        public static void TriggerEnemyTurnStart() => OnEnemyTurnStart?.Invoke();
        public static void TriggerEnemyTurnEnd() => OnEnemyTurnEnd?.Invoke();
        public static void TriggerRoundStart(int round) => OnRoundStart?.Invoke(round);

        // Unit Events
        public static void TriggerUnitSelected(GameObject unit) => OnUnitSelected?.Invoke(unit);
        public static void TriggerUnitDeselected() => OnUnitDeselected?.Invoke();
        public static void TriggerUnitDeath(GameObject unit) => OnUnitDeath?.Invoke(unit);
        public static void TriggerUnitSurrender(GameObject unit) => OnUnitSurrender?.Invoke(unit);
        public static void TriggerUnitDamaged(GameObject unit, int damage) => OnUnitDamaged?.Invoke(unit, damage);
        public static void TriggerUnitHealed(GameObject unit, int amount) => OnUnitHealed?.Invoke(unit, amount);
        public static void TriggerMoraleDamaged(GameObject unit, int damage) => OnMoraleDamaged?.Invoke(unit, damage);
        public static void TriggerUnitAttack(GameObject attacker, GameObject target) => OnUnitAttack?.Invoke(attacker, target);
        public static void TriggerUnitMoved(GameObject unit, GridCell from, GridCell to) => OnUnitMoved?.Invoke(unit, from, to);

        // Status Effect Events
        public static void TriggerUnitStunned(GameObject unit) => OnUnitStunned?.Invoke(unit);
        public static void TriggerUnitTrapped(GameObject unit) => OnUnitTrapped?.Invoke(unit);
        public static void TriggerUnitCursed(GameObject unit) => OnUnitCursed?.Invoke(unit);
        public static void TriggerUnitExposed(GameObject unit) => OnUnitExposed?.Invoke(unit);

        // Combat Events
        public static void TriggerAttackBlocked(GameObject attacker, GameObject obstacle, bool destroyed) 
            => OnAttackBlocked?.Invoke(attacker, obstacle, destroyed);

        // Resource Events
        public static void TriggerEnergyChanged(int newValue) => OnEnergyChanged?.Invoke(newValue);
        public static void TriggerGrogChanged(int newValue) => OnGrogChanged?.Invoke(newValue);

        // Game State Events
        public static void TriggerBattleStart() => OnBattleStart?.Invoke();
        public static void TriggerBattleEnd() => OnBattleEnd?.Invoke();
        public static void TriggerGameEnd(bool playerWon) => OnGameEnd?.Invoke(playerWon);
        public static void TriggerDeploymentStart() => OnDeploymentStart?.Invoke();
        public static void TriggerDeploymentEnd() => OnDeploymentEnd?.Invoke();

        // Hazard Events
        public static void TriggerUnitEnteredHazard(GameObject unit, HazardInstance hazard) 
            => OnUnitEnteredHazard?.Invoke(unit, hazard);
        public static void TriggerHazardDestroyed(HazardInstance hazard) => OnHazardDestroyed?.Invoke(hazard);

        /// <summary>
        /// Clear all event subscriptions. Call on scene unload to prevent memory leaks.
        /// </summary>
        public static void ClearAllEvents()
        {
            OnPlayerTurnStart = null;
            OnPlayerTurnEnd = null;
            OnEnemyTurnStart = null;
            OnEnemyTurnEnd = null;
            OnRoundStart = null;
            OnUnitSelected = null;
            OnUnitDeselected = null;
            OnUnitDeath = null;
            OnUnitSurrender = null;
            OnUnitDamaged = null;
            OnUnitHealed = null;
            OnMoraleDamaged = null;
            OnUnitAttack = null;
            OnUnitMoved = null;
            OnUnitStunned = null;
            OnUnitTrapped = null;
            OnUnitCursed = null;
            OnUnitExposed = null;
            OnAttackBlocked = null;
            OnEnergyChanged = null;
            OnGrogChanged = null;
            OnBattleStart = null;
            OnBattleEnd = null;
            OnGameEnd = null;
            OnDeploymentStart = null;
            OnDeploymentEnd = null;
            OnUnitEnteredHazard = null;
            OnHazardDestroyed = null;
        }
    }
}