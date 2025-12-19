using UnityEngine;
using System.Collections.Generic;
using TMPro;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Enums;
using TacticalGame.Combat;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages battle interactions: unit selection, movement, attacks, and swapping.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TMP_Text instructionText;

        [Header("Selection Visuals")]
        [SerializeField] private Color moveRangeColor = Color.blue;
        [SerializeField] private Color attackTargetColor = Color.red;

        #endregion

        #region Private State

        private GridManager gridManager;
        private EnergyManager energyManager;
        private TurnManager turnManager;

        private bool isBattleActive = false;
        private GameObject selectedUnit;
        private bool isSwapping = false;

        private List<GridCell> validMoveTiles = new List<GridCell>();
        private Dictionary<GridCell, Material> originalMaterials = new Dictionary<GridCell, Material>();
        
        // Attack target highlighting
        private GameObject currentAttackTarget;
        private Color originalTargetColor;

        #endregion

        #region Public Properties

        public bool IsBattleActive
        {
            get => isBattleActive;
            set => isBattleActive = value;
        }

        public GameObject SelectedUnit => selectedUnit;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<BattleManager>();
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
            energyManager = ServiceLocator.Get<EnergyManager>();
            turnManager = ServiceLocator.Get<TurnManager>();

            if (instructionText != null)
            {
                instructionText.text = "";
            }
        }

        private void Update()
        {
            if (!isBattleActive) return;

            HandleInput();
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Left click - select/move
            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            // Right click - deselect
            if (Input.GetMouseButtonDown(1))
            {
                DeselectUnit();
            }

            // Attack hotkeys
            if (selectedUnit != null && !isSwapping)
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    TryMeleeAttack();
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    TryRangedAttack();
                }
            }
        }

        private void HandleLeftClick()
        {
            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            // Handle swap mode
            if (isSwapping && selectedUnit != null)
            {
                ExecuteSwap(hit);
                return;
            }

            // Check if clicked on a unit directly
            UnitMovement unitClicked = hit.collider.GetComponent<UnitMovement>();
            if (unitClicked != null)
            {
                if (selectedUnit != null) DeselectUnit();
                SelectUnit(unitClicked.gameObject);
                return;
            }

            // Check if clicked on a grid cell
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null)
            {
                HandleCellClick(cell);
            }
        }

        private void HandleCellClick(GridCell cell)
        {
            // Click on occupied cell - select that unit
            if (cell.IsOccupied && cell.OccupyingUnit != null)
            {
                if (selectedUnit != null) DeselectUnit();
                SelectUnit(cell.OccupyingUnit);
            }
            // Click on valid move tile - move there
            else if (selectedUnit != null && validMoveTiles.Contains(cell))
            {
                MoveSelectedUnitTo(cell);
            }
        }

        #endregion

        #region Unit Selection

        private void SelectUnit(GameObject unit)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status == null) return;

            // Only allow selecting player units
            if (status.Team == Team.Enemy) return;

            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement == null) return;

            // Don't select trapped or surrendered units
            if (status.IsTrapped || status.HasSurrendered) return;

            selectedUnit = unit;
            isSwapping = false;

            if (instructionText != null)
            {
                instructionText.text = "Click Blue Tile to Move\nPress 'C' Melee | 'X' Ranged";
            }

            // Calculate and highlight valid move tiles
            Vector2Int gridPos = gridManager.WorldToGridPosition(unit.transform.position);
            GridCell startCell = gridManager.GetCell(gridPos.x, gridPos.y);
            CalculateValidMoves(startCell, movement.MoveRange);

            // Highlight attack target
            HighlightAttackTarget(status);

            GameEvents.TriggerUnitSelected(unit);
        }

        private void DeselectUnit()
        {
            ResetHighlights();
            ClearAttackTargetHighlight();
            selectedUnit = null;
            isSwapping = false;
            validMoveTiles.Clear();

            if (instructionText != null)
            {
                instructionText.text = "";
            }

            GameEvents.TriggerUnitDeselected();
        }

        /// <summary>
        /// Get the currently selected unit.
        /// </summary>
        public GameObject GetSelectedUnit()
        {
            return selectedUnit;
        }

        #endregion

        #region Movement

        private void MoveSelectedUnitTo(GridCell targetCell)
        {
            UnitMovement movement = selectedUnit.GetComponent<UnitMovement>();
            if (movement.HasAttacked) return;

            // Update grid state
            Vector2Int oldPos = gridManager.WorldToGridPosition(selectedUnit.transform.position);
            GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
            oldCell.RemoveUnit();

            // Move unit
            movement.MoveToCell(targetCell);
            targetCell.PlaceUnit(selectedUnit);

            DeselectUnit();
        }

        private void CalculateValidMoves(GridCell startCell, int range)
        {
            validMoveTiles.Clear();
            
            Queue<GridCell> queue = new Queue<GridCell>();
            Dictionary<GridCell, int> distances = new Dictionary<GridCell, int>();
            
            queue.Enqueue(startCell);
            distances[startCell] = 0;

            Vector2Int[] directions = 
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            while (queue.Count > 0)
            {
                GridCell current = queue.Dequeue();
                
                if (distances[current] >= range) continue;

                foreach (Vector2Int dir in directions)
                {
                    GridCell neighbor = gridManager.GetCell(
                        current.XPosition + dir.x,
                        current.YPosition + dir.y
                    );

                    if (neighbor != null && 
                        !distances.ContainsKey(neighbor) && 
                        neighbor.IsPassable())
                    {
                        distances[neighbor] = distances[current] + 1;
                        queue.Enqueue(neighbor);
                        validMoveTiles.Add(neighbor);
                        HighlightTile(neighbor);
                    }
                }
            }
        }

        #endregion

        #region Attacks

        private void TryMeleeAttack()
        {
            UnitAttack attacker = selectedUnit.GetComponent<UnitAttack>();
            if (attacker != null)
            {
                attacker.TryMeleeAttack();
                DeselectUnit();
            }
        }

        private void TryRangedAttack()
        {
            UnitAttack attacker = selectedUnit.GetComponent<UnitAttack>();
            if (attacker != null)
            {
                attacker.TryRangedAttack();
                DeselectUnit();
            }
        }

        #endregion

        #region Swap System

        /// <summary>
        /// Enter swap mode for the selected unit.
        /// </summary>
        public void InitiateSwapMode()
        {
            if (selectedUnit == null) return;

            UnitStatus status = selectedUnit.GetComponent<UnitStatus>();
            var config = GameConfig.Instance;

            // Check swap limits
            if (!turnManager.CanSwap())
            {
                Debug.Log($"Cannot Swap: Limit reached ({config.maxSwapsPerRound} per round)!");
                return;
            }

            if (status.SwapCooldown > 0)
            {
                Debug.Log($"Cannot Swap: Unit is recovering ({status.SwapCooldown} turns left).");
                return;
            }

            if (!energyManager.HasEnergy(config.swapEnergyCost))
            {
                Debug.Log("Not enough Energy to Swap!");
                return;
            }

            if (status.HasSurrendered) return;

            if (status.HPPercent < config.minHPPercentToSwap)
            {
                Debug.Log($"Unit is too injured to swap! (<{config.minHPPercentToSwap * 100}% HP)");
                return;
            }

            isSwapping = true;
            Debug.Log("Click on a unit or empty grid to swap.");
            
            if (instructionText != null)
            {
                instructionText.text = "Select Target for Swap...";
            }
        }

        private void ExecuteSwap(RaycastHit hit)
        {
            UnitStatus sourceStatus = selectedUnit.GetComponent<UnitStatus>();
            Vector2Int sourceGridPos = gridManager.WorldToGridPosition(selectedUnit.transform.position);
            GridCell sourceCell = gridManager.GetCell(sourceGridPos.x, sourceGridPos.y);

            GridCell targetCell = null;
            GameObject targetUnit = null;

            // Check if we hit a unit
            if (hit.collider.CompareTag("Unit"))
            {
                targetUnit = hit.collider.gameObject;
                Vector2Int targetPos = gridManager.WorldToGridPosition(targetUnit.transform.position);
                targetCell = gridManager.GetCell(targetPos.x, targetPos.y);
            }
            else
            {
                targetCell = hit.collider.GetComponent<GridCell>();
            }

            if (targetCell == null || targetCell == sourceCell)
            {
                DeselectUnit();
                return;
            }

            // Spend resources
            energyManager.TrySpendEnergy(GameConfig.Instance.swapEnergyCost);
            turnManager.UseSwap();
            
            // Apply swap penalty
            sourceStatus.ApplySwapPenalty();

            // Execute swap
            if (targetUnit != null)
            {
                // Swap with another unit
                targetUnit.transform.position = sourceCell.GetWorldPosition();
                sourceCell.PlaceUnit(targetUnit);
                
                selectedUnit.transform.position = targetCell.GetWorldPosition();
                targetCell.PlaceUnit(selectedUnit);
                
                Debug.Log("Units Swapped Positions!");
            }
            else if (targetCell.CanPlaceUnit())
            {
                // Swap to empty tile
                sourceCell.RemoveUnit();
                selectedUnit.transform.position = targetCell.GetWorldPosition();
                targetCell.PlaceUnit(selectedUnit);
                
                Debug.Log("Swapped to Empty Grid!");
            }

            DeselectUnit();
        }

        #endregion

        #region Highlighting

        private void HighlightTile(GridCell cell)
        {
            MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (!originalMaterials.ContainsKey(cell))
                {
                    originalMaterials[cell] = renderer.sharedMaterial;
                }
                renderer.material.color = moveRangeColor;
            }
        }

        private void ResetHighlights()
        {
            foreach (var entry in originalMaterials)
            {
                if (entry.Key != null)
                {
                    entry.Key.GetComponent<MeshRenderer>().material = entry.Value;
                }
            }
            originalMaterials.Clear();
        }

        #endregion

        #region Attack Target Highlighting

        private void HighlightAttackTarget(UnitStatus attacker)
        {
            // Clear previous target highlight
            ClearAttackTargetHighlight();

            // Find nearest enemy using TargetFinder
            UnitStatus target = TargetFinder.FindNearestEnemy(attacker);
            
            if (target == null) return;

            currentAttackTarget = target.gameObject;

            // Highlight the target unit
            MeshRenderer renderer = currentAttackTarget.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                originalTargetColor = renderer.material.color;
                renderer.material.color = attackTargetColor;
            }
        }

        private void ClearAttackTargetHighlight()
        {
            if (currentAttackTarget != null)
            {
                MeshRenderer renderer = currentAttackTarget.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = originalTargetColor;
                }
                currentAttackTarget = null;
            }
        }

        #endregion
    }
}