using UnityEngine;
using TacticalGame.Enums;
using TacticalGame.Config;
using TacticalGame.Core;
using TacticalGame.Combat;
using TacticalGame.Grid;
using TacticalGame.Hazards;
using TacticalGame.Managers;

namespace TacticalGame.Units
{
    /// <summary>
    /// Handles unit attack actions (melee and ranged).
    /// </summary>
    public class UnitAttack : MonoBehaviour
    {
        #region Cached References
        
        private UnitStatus status;
        private UnitMovement movement;
        private EnergyManager energyManager;
        private GridManager gridManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            status = GetComponent<UnitStatus>();
            movement = GetComponent<UnitMovement>();
        }

        private void Start()
        {
            CacheManagerReferences();
        }

        #endregion

        #region Setup

        /// <summary>
        /// Manually set manager references (called during deployment).
        /// </summary>
        public void SetupManagers(GridManager grid, EnergyManager energy)
        {
            gridManager = grid;
            energyManager = energy;
        }

        private void CacheManagerReferences()
        {
            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }
            if (energyManager == null)
            {
                energyManager = ServiceLocator.Get<EnergyManager>();
            }
        }

        #endregion

        #region Public Attack Methods

        /// <summary>
        /// Attempt a melee attack on the nearest enemy.
        /// </summary>
        public void TryMeleeAttack()
        {
            if (!CanAttack()) return;
            
            if (status.WeaponType == WeaponType.Ranged)
            {
                Debug.Log($"<color=red>{gameObject.name} cannot Melee! (Equipped: Ranged)</color>");
                return;
            }

            if (!TrySpendAttackEnergy()) return;

            UnitStatus target = TargetFinder.FindNearestEnemy(status);
            if (target == null)
            {
                Debug.Log("No valid target found!");
                return;
            }

            if (IsBlockedByObstacle(target))
            {
                Debug.Log("Attack Blocked by Obstacle in Row!");
                return;
            }

            ExecuteAttack(target, true);
        }

        /// <summary>
        /// Attempt a ranged attack on the nearest enemy.
        /// </summary>
        public void TryRangedAttack()
        {
            if (!CanAttack()) return;
            
            if (status.WeaponType == WeaponType.Melee)
            {
                Debug.Log($"<color=red>{gameObject.name} cannot Shoot! (Equipped: Melee)</color>");
                return;
            }

            if (status.CurrentArrows <= 0)
            {
                Debug.Log("No arrows remaining!");
                return;
            }

            if (!TrySpendAttackEnergy()) return;

            UnitStatus target = TargetFinder.FindNearestEnemy(status);
            if (target == null)
            {
                Debug.Log("No valid target found!");
                return;
            }

            // Use arrow regardless of hit
            status.UseArrow();

            if (IsBlockedByObstacle(target))
            {
                Debug.Log("Shot Blocked by Obstacle in Row!");
                return;
            }

            ExecuteAttack(target, false);
        }

        #endregion

        #region Private Attack Logic

        private bool CanAttack()
        {
            if (!status.CanAct())
            {
                Debug.Log($"{gameObject.name} cannot act (surrendered or stunned)");
                return false;
            }

            if (movement != null && movement.HasAttacked)
            {
                Debug.Log($"{gameObject.name} has already attacked this turn");
                return false;
            }

            return true;
        }

        private bool TrySpendAttackEnergy()
        {
            CacheManagerReferences();
            
            if (energyManager == null)
            {
                Debug.LogError("EnergyManager not found!");
                return false;
            }

            return energyManager.TrySpendEnergy(GameConfig.Instance.attackEnergyCost);
        }

        private void ExecuteAttack(UnitStatus target, bool isMelee)
        {
            var config = GameConfig.Instance;
            
            // Get standing bonuses from current tile
            var bonuses = GetStandingBonuses();

            // Calculate base damage
            int baseDamage = isMelee 
                ? DamageCalculator.GetMeleeBaseDamage(status)
                : DamageCalculator.GetRangedBaseDamage(status);

            // Apply damage to target
            target.TakeDamage(
                baseDamage, 
                gameObject, 
                isMelee, 
                bonuses.hp, 
                bonuses.morale, 
                bonuses.applyCurse
            );

            // Post-attack effects
            status.ReduceBuzz(config.buzzDecayOnAttack);
            
            if (movement != null)
            {
                movement.MarkAsAttacked();
            }
            
            status.SetActedVisual();
            
            // Fire event
            GameEvents.TriggerUnitAttack(gameObject, target.gameObject);
        }

        #endregion

        #region Obstacle Blocking

        private bool IsBlockedByObstacle(UnitStatus target)
        {
            CacheManagerReferences();
            
            if (gridManager == null) return false;

            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
            Vector2Int targetPos = gridManager.WorldToGridPosition(target.transform.position);

            // Only check horizontal blocking (same row)
            if (myPos.y != targetPos.y) return false;

            int startX = Mathf.Min(myPos.x, targetPos.x) + 1;
            int endX = Mathf.Max(myPos.x, targetPos.x);

            for (int x = startX; x < endX; x++)
            {
                GridCell cell = gridManager.GetCell(x, myPos.y);
                if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
                {
                    HazardInstance hazard = cell.HazardVisualObject.GetComponent<HazardInstance>();
                    if (hazard != null && (hazard.IsHardObstacle || hazard.IsSoftObstacle))
                    {
                        // Damage the obstacle
                        hazard.TakeObstacleDamage(GameConfig.Instance.obstacleBlockDamage);
                        
                        GameEvents.TriggerAttackBlocked(gameObject, hazard.gameObject, hazard == null);
                        return true;
                    }
                }
            }
            
            return false;
        }

        #endregion

        #region Standing Bonuses

        private (int hp, int morale, bool applyCurse) GetStandingBonuses()
        {
            int totalHP = 0;
            int totalMorale = 0;
            bool applyCurse = false;

            CacheManagerReferences();
            
            if (gridManager == null) return (totalHP, totalMorale, applyCurse);

            Vector2Int pos = gridManager.WorldToGridPosition(transform.position);
            GridCell cell = gridManager.GetCell(pos.x, pos.y);

            if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
            {
                HazardInstance hazardInst = cell.HazardVisualObject.GetComponent<HazardInstance>();
                if (hazardInst != null && hazardInst.Data != null)
                {
                    totalHP += hazardInst.Data.standingBonusHP;
                    totalMorale += hazardInst.Data.standingBonusMorale;
                    if (hazardInst.Data.standingAppliesCurse)
                    {
                        applyCurse = true;
                    }
                }
            }

            return (totalHP, totalMorale, applyCurse);
        }

        #endregion
    }
}