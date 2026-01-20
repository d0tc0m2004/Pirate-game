using UnityEngine;
using System.Collections;
using System.Linq;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Hazards;
using TacticalGame.Combat;
using TacticalGame.Equipment;
using TacticalGame.Enums;

namespace TacticalGame.Units
{
    /// <summary>
    /// Handles unit movement and animation.
    /// Integrates with StatusEffectManager for movement modifiers.
    /// </summary>
    public class UnitMovement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private int moveRange = 3;

        #endregion

        #region Private State

        private bool isMoving = false;
        private bool hasAttacked = false;
        private Color baseColor = Color.clear;
        private MeshRenderer meshRenderer;
        private UnitStatus status;
        private StatusEffectManager statusEffects;
        private PassiveRelicManager passiveManager;

        #endregion

        #region Public Properties

        public int MoveRange => moveRange;
        public bool IsMoving => isMoving;
        public bool HasAttacked => hasAttacked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            status = GetComponent<UnitStatus>();
            statusEffects = GetComponent<StatusEffectManager>();
            passiveManager = GetComponent<PassiveRelicManager>();
            CaptureBaseColor();
        }

        #endregion

        #region Color Management

        private void CaptureBaseColor()
        {
            if (meshRenderer != null && baseColor == Color.clear)
            {
                baseColor = meshRenderer.material.color;
            }
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Reset unit state for a new turn.
        /// </summary>
        public void BeginTurn()
        {
            hasAttacked = false;

            if (meshRenderer != null)
            {
                if (baseColor == Color.clear || baseColor.a == 0)
                {
                    baseColor = meshRenderer.material.color;
                }
                meshRenderer.material.color = baseColor;
            }
        }

        /// <summary>
        /// Mark that this unit has attacked.
        /// </summary>
        public void MarkAsAttacked()
        {
            hasAttacked = true;
        }

        #endregion

        #region Movement Range

        /// <summary>
        /// Get effective move range after status effects and passives.
        /// </summary>
        public int GetEffectiveMoveRange()
        {
            int range = moveRange;
            
            // Apply movement reduction from status effects (Slowed)
            if (statusEffects != null)
            {
                int reduction = statusEffects.GetMovementReduction();
                if (reduction > 0)
                {
                    range -= reduction;
                    Debug.Log($"<color=yellow>Movement reduced by {reduction} (Slowed)</color>");
                }
            }
            
            // Apply ally extra movement from passives (Navigator V2 - AllAlliesExtraMove)
            if (passiveManager != null)
            {
                // Check if any ally has the movement bonus passive
                var allies = GameObject.FindGameObjectsWithTag("Unit");
                foreach (var allyObj in allies)
                {
                    if (allyObj == gameObject) continue;
                    
                    var allyStatus = allyObj.GetComponent<UnitStatus>();
                    var allyPassive = allyObj.GetComponent<PassiveRelicManager>();
                    
                    if (allyStatus != null && allyStatus.Team == status.Team && allyPassive != null)
                    {
                        int extraMove = allyPassive.GetAllyExtraMovement();
                        if (extraMove > 0)
                        {
                            range += extraMove;
                            Debug.Log($"<color=green>+{extraMove} movement from ally passive</color>");
                            break; // Only apply once
                        }
                    }
                }
            }
            
            // Check if enemies have movement limit passive (Swashbuckler V2 - EnemyBootsLimited)
            if (status != null && status.Team == Team.Enemy)
            {
                var playerUnits = GameObject.FindGameObjectsWithTag("Unit");
                foreach (var playerObj in playerUnits)
                {
                    var playerStatus = playerObj.GetComponent<UnitStatus>();
                    var playerPassive = playerObj.GetComponent<PassiveRelicManager>();
                    
                    if (playerStatus != null && playerStatus.Team == Team.Player && playerPassive != null)
                    {
                        int limit = playerPassive.GetEnemyMovementLimit();
                        if (limit > 0)
                        {
                            range = Mathf.Min(range, limit);
                            Debug.Log($"<color=red>Enemy movement limited to {limit} tiles!</color>");
                            break;
                        }
                    }
                }
            }
            
            return Mathf.Max(0, range);
        }

        /// <summary>
        /// Check if this movement should cost energy (or if free move is active).
        /// </summary>
        public bool ShouldCostEnergy()
        {
            if (statusEffects != null && statusEffects.HasFreeMove())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Consume free move buff if available.
        /// </summary>
        public void ConsumeFreeMove()
        {
            if (statusEffects != null && statusEffects.HasFreeMove())
            {
                statusEffects.ConsumeFreeMove();
                Debug.Log("<color=green>Free move consumed!</color>");
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// Move to a target cell with animation.
        /// </summary>
        public void MoveToCell(GridCell targetCell)
        {
            if (isMoving || hasAttacked) return;

            StartCoroutine(AnimateMovement(targetCell));
        }

        private IEnumerator AnimateMovement(GridCell targetCell)
        {
            isMoving = true;
            
            Vector3 targetPos = targetCell.GetWorldPosition();
            float moveSpeed = GameConfig.Instance.moveAnimationSpeed;

            // Store starting cell for event
            GridManager gridManager = ServiceLocator.Get<GridManager>();
            GridCell startCell = null;
            if (gridManager != null)
            {
                Vector2Int startPos = gridManager.WorldToGridPosition(transform.position);
                startCell = gridManager.GetCell(startPos.x, startPos.y);
            }

            // Animate movement
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, 
                    targetPos, 
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Snap to final position
            transform.position = targetPos;
            isMoving = false;

            // Fire movement event
            GameEvents.TriggerUnitMoved(gameObject, startCell, targetCell);

            // Notify StatusEffectManager of movement (for bleed, traps, etc.)
            if (statusEffects != null)
            {
                statusEffects.OnUnitMoved();
            }

            // Check for hazards on new tile
            CheckHazardOnTile();
        }

        private void CheckHazardOnTile()
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up, Vector3.down, 2.0f);
            
            foreach (RaycastHit hit in hits)
            {
                GridCell cell = hit.collider.GetComponent<GridCell>();
                if (cell != null && cell.HasHazard && cell.HazardVisualObject != null)
                {
                    HazardInstance hazard = cell.HazardVisualObject.GetComponent<HazardInstance>();
                    if (hazard != null)
                    {
                        hazard.OnUnitEnter(gameObject);
                        GameEvents.TriggerUnitEnteredHazard(gameObject, hazard);
                        return;
                    }
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Check if unit can move (not moving, hasn't attacked, not stunned/stasis/trapped).
        /// </summary>
        public bool CanMove()
        {
            if (isMoving) return false;
            if (hasAttacked) return false;
            if (status != null && !status.CanAct()) return false;
            if (status != null && status.IsTrapped) return false;
            
            // Check for stun from StatusEffectManager
            if (statusEffects != null)
            {
                if (statusEffects.IsStunned())
                {
                    Debug.Log($"<color=red>{gameObject.name} is stunned and cannot move!</color>");
                    return false;
                }
                
                if (statusEffects.IsInStasis())
                {
                    Debug.Log($"<color=red>{gameObject.name} is in stasis and cannot move!</color>");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Check if unit can move to a specific cell (range check with modifiers).
        /// </summary>
        public bool CanMoveToCell(GridCell targetCell)
        {
            if (!CanMove()) return false;
            if (targetCell == null) return false;
            if (!targetCell.CanPlaceUnit()) return false;
            
            GridManager gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null) return false;
            
            Vector2Int currentPos = gridManager.WorldToGridPosition(transform.position);
            Vector2Int targetPos = new Vector2Int(targetCell.XPosition, targetCell.YPosition);
            
            int distance = Mathf.Abs(currentPos.x - targetPos.x) + Mathf.Abs(currentPos.y - targetPos.y);
            int effectiveRange = GetEffectiveMoveRange();
            
            return distance <= effectiveRange;
        }

        /// <summary>
        /// Check if unit can ignore obstacles (Trinket passive).
        /// </summary>
        public bool CanIgnoreObstacles()
        {
            // Check if self has the passive
            if (passiveManager != null && passiveManager.NearbyAlliesIgnoreObstacles())
            {
                return true;
            }
            
            // Check if any nearby ally has the passive that grants it to us
            var allies = GameObject.FindGameObjectsWithTag("Unit");
            foreach (var allyObj in allies)
            {
                if (allyObj == gameObject) continue;
                
                var allyStatus = allyObj.GetComponent<UnitStatus>();
                var allyPassive = allyObj.GetComponent<PassiveRelicManager>();
                
                if (allyStatus != null && allyStatus.Team == status.Team && allyPassive != null)
                {
                    if (allyPassive.NearbyAlliesIgnoreObstacles())
                    {
                        // Check if we're nearby (within 1 tile, or global if that passive is active)
                        bool isGlobal = allyPassive.IsNearbyRadiusGlobal();
                        if (isGlobal)
                        {
                            return true;
                        }
                        
                        GridManager gridManager = ServiceLocator.Get<GridManager>();
                        if (gridManager != null)
                        {
                            Vector2Int myPos = gridManager.WorldToGridPosition(transform.position);
                            Vector2Int allyPos = gridManager.WorldToGridPosition(allyObj.transform.position);
                            int dist = Mathf.Abs(myPos.x - allyPos.x) + Mathf.Abs(myPos.y - allyPos.y);
                            if (dist <= 1)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Check if unit can be knocked back / displaced.
        /// </summary>
        public bool CanBeDisplaced()
        {
            if (statusEffects != null)
            {
                return statusEffects.CanBeKnockedBack();
            }
            return true;
        }

        /// <summary>
        /// Force move unit to a cell (for knockback/push effects).
        /// </summary>
        public void ForceMoveTo(GridCell targetCell)
        {
            if (targetCell == null) return;
            if (!CanBeDisplaced())
            {
                Debug.Log($"<color=yellow>{gameObject.name} cannot be displaced!</color>");
                return;
            }
            
            GridManager gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager != null)
            {
                Vector2Int currentPos = gridManager.WorldToGridPosition(transform.position);
                GridCell currentCell = gridManager.GetCell(currentPos.x, currentPos.y);
                
                if (currentCell != null)
                {
                    currentCell.RemoveUnit();
                }
            }
            
            transform.position = targetCell.GetWorldPosition();
            targetCell.PlaceUnit(gameObject);
            
            // Notify of knockback
            if (statusEffects != null)
            {
                statusEffects.OnKnockedBack(null);
            }
            
            Debug.Log($"<color=cyan>{gameObject.name} was displaced!</color>");
        }

        #endregion
    }
}