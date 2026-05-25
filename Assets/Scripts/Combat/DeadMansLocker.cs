using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Individual Dead Man's Locker (cursed chest) placed on the player side.
    /// Uses Hit Pips (not HP). Any hit = 1 pip lost. Stores tribute from enemy loot.
    /// </summary>
    public class DeadMansLocker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Locker State")]
        [SerializeField] private int basePips = 3;
        [SerializeField] private int currentPips;
        [SerializeField] private int fortifyPips = 0; // Bonus pips from depositing tribute (max +2)
        [SerializeField] private float storedTribute = 0f;

        [Header("Grid Position")]
        [SerializeField] private int gridX;
        [SerializeField] private int gridY;

        #endregion

        #region Private State

        private GridCell occupiedCell;
        private bool isDestroyed = false;
        private MeshRenderer meshRenderer;

        #endregion

        #region Public Properties

        public int BasePips => basePips;
        public int CurrentPips => currentPips;
        public int MaxPips => basePips + fortifyPips;
        public int FortifyPips => fortifyPips;
        public float StoredTribute => storedTribute;
        public bool IsDestroyed => isDestroyed;
        public int GridX => gridX;
        public int GridY => gridY;
        public GridCell OccupiedCell => occupiedCell;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the locker on a specific grid cell.
        /// </summary>
        public void Initialize(GridCell cell, int pips)
        {
            occupiedCell = cell;
            gridX = cell.XPosition;
            gridY = cell.YPosition;
            basePips = pips;
            currentPips = basePips;
            fortifyPips = 0;
            storedTribute = 0f;
            isDestroyed = false;

            meshRenderer = GetComponent<MeshRenderer>();

            // Mark cell as occupied/blocked so units can't walk through
            cell.isBlockedState = true;

            Debug.Log($"<color=yellow>[Locker] Spawned at ({gridX},{gridY}) with {currentPips} pips</color>");
        }

        #endregion

        #region Pip Damage

        /// <summary>
        /// Take a hit. Any hit = 1 pip lost regardless of damage amount.
        /// Returns true if the locker was destroyed by this hit.
        /// </summary>
        public bool TakeHit(GameObject attacker)
        {
            if (isDestroyed) return false;

            currentPips--;

            Debug.Log($"<color=orange>[Locker] Hit at ({gridX},{gridY})! Pips: {currentPips}/{MaxPips}</color>");

            // Leak tribute on hit: 10% of stored tribute spills to adjacent tiles
            if (storedTribute > 0)
            {
                float leakAmount = storedTribute * 0.10f;
                storedTribute -= leakAmount;
                SpillLooseTribute(leakAmount);
                Debug.Log($"<color=orange>[Locker] Leaked {leakAmount:F1} tribute from hit</color>");
            }

            // Apply Morale Shock to all living player units
            ApplyMoraleShock();

            // Fire event
            GameEvents.TriggerLockerHit(gameObject, currentPips);

            if (currentPips <= 0)
            {
                DestroyLocker();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply Morale Shock when a pip is lost.
        /// ShockTeam = TotalMorale / TotalPipsRemaining
        /// ShockPerUnit = ShockTeam / U split among living units.
        /// </summary>
        private void ApplyMoraleShock()
        {
            var lockerManager = ServiceLocator.Get<DeadMansLockerManager>();
            if (lockerManager == null) return;

            var survivingLockersList = lockerManager.GetSurvivingLockers();
            if (survivingLockersList.Count == 0) return;

            int totalPipsRemaining = 0;
            foreach (var locker in survivingLockersList)
            {
                totalPipsRemaining += locker.CurrentPips;
            }
            if (totalPipsRemaining <= 0) return;

            // Gather all living player units
            var playerUnits = GetLivingPlayerUnits();
            if (playerUnits.Count == 0) return;

            // Calculate total morale across all living player units
            int totalMorale = 0;
            foreach (var unit in playerUnits)
            {
                totalMorale += unit.CurrentMorale;
            }

            // ShockTeam = TotalMorale / TotalPipsRemaining
            float shockTeam = (float)totalMorale / totalPipsRemaining;

            // Split among living units
            int shockPerUnit = Mathf.RoundToInt(shockTeam / playerUnits.Count);

            if (shockPerUnit <= 0) return;

            foreach (var unit in playerUnits)
            {
                unit.ApplyMoraleDamage(shockPerUnit);
                Debug.Log($"<color=magenta>[Morale Shock] {unit.UnitName} lost {shockPerUnit} morale from locker hit</color>");
            }
        }

        #endregion

        #region Tribute

        /// <summary>
        /// Deposit tribute into this locker (from siphoned enemy loot).
        /// </summary>
        public void DepositTribute(float amount)
        {
            if (isDestroyed) return;

            storedTribute += amount;

            // Check for fortification: every 25% of quota deposited = +1 pip (max +2)
            var lockerManager = ServiceLocator.Get<DeadMansLockerManager>();
            if (lockerManager != null)
            {
                float quota = lockerManager.GetTributeQuota();
                if (quota > 0 && fortifyPips < 2)
                {
                    int fortifyThreshold = Mathf.FloorToInt(storedTribute / (quota * 0.25f));
                    int newFortifyPips = Mathf.Min(fortifyThreshold, 2);
                    if (newFortifyPips > fortifyPips)
                    {
                        int gained = newFortifyPips - fortifyPips;
                        fortifyPips = newFortifyPips;
                        currentPips += gained;
                        Debug.Log($"<color=green>[Locker] Fortified! +{gained} pip(s), now {currentPips}/{MaxPips}</color>");
                    }
                }
            }

            GameEvents.TriggerLockerTributeChanged(gameObject, storedTribute);
        }

        /// <summary>
        /// Get the Manhattan distance from this locker to a world position.
        /// </summary>
        public int DistanceTo(int x, int y)
        {
            return Mathf.Abs(gridX - x) + Mathf.Abs(gridY - y);
        }

        #endregion

        #region Destruction

        private void DestroyLocker()
        {
            isDestroyed = true;

            // 50% tribute lost permanently, 50% spills as Loose Tribute
            float spillAmount = storedTribute * 0.50f;
            float lostAmount = storedTribute - spillAmount;
            storedTribute = 0f;

            Debug.Log($"<color=red>[Locker] DESTROYED at ({gridX},{gridY})! Lost {lostAmount:F1} tribute, spilled {spillAmount:F1}</color>");

            SpillLooseTribute(spillAmount);

            // Unblock the cell
            if (occupiedCell != null)
            {
                occupiedCell.isBlockedState = false;
            }

            GameEvents.TriggerLockerDestroyed(gameObject);

            // Visual feedback - disable renderer, keep object for reference
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Spill loose tribute onto adjacent tiles.
        /// Other units/lockers on those tiles can pick it up.
        /// </summary>
        private void SpillLooseTribute(float amount)
        {
            if (amount <= 0) return;

            // For now, the spilled tribute is tracked by the manager
            // Future: create visual loose tribute pickups on adjacent cells
            var lockerManager = ServiceLocator.Get<DeadMansLockerManager>();
            if (lockerManager != null)
            {
                lockerManager.AddLooseTribute(gridX, gridY, amount);
            }
        }

        #endregion

        #region Helpers

        private List<UnitStatus> GetLivingPlayerUnits()
        {
            var units = new List<UnitStatus>();
            var allUnits = FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit.Team == Team.Player && !unit.HasSurrendered && unit.CurrentHP > 0)
                {
                    units.Add(unit);
                }
            }
            return units;
        }

        #endregion
    }
}
