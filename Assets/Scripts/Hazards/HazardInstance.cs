using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Units;
using TacticalGame.Grid;

namespace TacticalGame.Hazards
{
    /// <summary>
    /// Instance of a hazard placed on the grid.
    /// </summary>
    public class HazardInstance : MonoBehaviour
    {
        #region Private State

        private HazardData data;
        private GridCell currentCell;
        private bool isSoftObstacle = false;
        private bool isHardObstacle = false;
        private int obstacleHP = 2;

        #endregion

        #region Public Properties

        public HazardData Data => data;
        public bool IsSoftObstacle => isSoftObstacle;
        public bool IsHardObstacle => isHardObstacle;
        public int ObstacleHP => obstacleHP;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the hazard instance with data and cell reference.
        /// </summary>
        public void Initialize(HazardData hazardData, GridCell cell)
        {
            data = hazardData;
            currentCell = cell;

            // Determine obstacle type based on effect
            if (data.effectType == HazardEffectType.Box)
            {
                isSoftObstacle = true;
            }
            if (data.effectType == HazardEffectType.Boulder)
            {
                isHardObstacle = true;
            }

            // Set HP for destructible obstacles
            if (isSoftObstacle || isHardObstacle)
            {
                obstacleHP = data.maxHealth;
            }
        }

        #endregion

        #region Obstacle Damage

        /// <summary>
        /// Apply damage to this obstacle (if applicable).
        /// </summary>
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

        #endregion

        #region Effect Triggers

        /// <summary>
        /// Called when a unit ends their turn on this hazard.
        /// </summary>
        public void OnTurnEnd(GameObject unit)
        {
            if (unit == null) return;
            
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) return;

            switch (data.effectType)
            {
                case HazardEffectType.Fire:
                    status.TakeDamage(data.damageHP, gameObject, false);
                    break;

                case HazardEffectType.Plague:
                    status.ApplyMoraleDamage(data.damageMorale);
                    break;

                case HazardEffectType.ShiftingSand:
                    status.ApplyMoraleDamage(data.damageMorale);
                    break;

                case HazardEffectType.Lightning:
                    if (Random.value > 0.5f)
                    {
                        status.ApplyStun(data.effectDuration);
                    }
                    break;

                case HazardEffectType.Cursed:
                    status.ApplyCurse(data.curseMultiplier);
                    break;
            }
        }

        /// <summary>
        /// Called when a unit enters this hazard's cell.
        /// </summary>
        public void OnUnitEnter(GameObject unit)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) return;

            switch (data.effectType)
            {
                case HazardEffectType.Trap:
                    status.ApplyTrap();
                    DestroyHazard();
                    break;

                case HazardEffectType.Cursed:
                    status.ApplyCurse(data.curseMultiplier);
                    break;
            }
        }

        #endregion

        #region Destruction

        private void DestroyHazard()
        {
            // Drop loot if applicable
            if (data.effectType == HazardEffectType.Box && data.dropItem != null)
            {
                Instantiate(data.dropItem, transform.position, Quaternion.identity);
            }

            // Clear cell state
            if (currentCell != null)
            {
                currentCell.hasHazardState = false;
                currentCell.isBlockedState = false;
                currentCell.hazardVisualObjectRef = null;
            }

            GameEvents.TriggerHazardDestroyed(this);
            Destroy(gameObject);
        }

        #endregion

        #region Input Handling

        private void OnMouseDown()
        {
            TakeObstacleDamage(1);
        }

        #endregion
    }
}