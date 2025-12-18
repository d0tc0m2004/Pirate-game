using UnityEngine;
using System.Collections;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Hazards;

namespace TacticalGame.Units
{
    /// <summary>
    /// Handles unit movement and animation.
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
        /// Check if unit can move (not moving, hasn't attacked).
        /// </summary>
        public bool CanMove()
        {
            if (isMoving) return false;
            if (hasAttacked) return false;
            if (status != null && !status.CanAct()) return false;
            if (status != null && status.IsTrapped) return false;
            return true;
        }

        #endregion
    }
}