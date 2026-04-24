using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Enums;
using TacticalGame.Combat;
using TacticalGame.Equipment;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages battle interactions: unit selection, movement, and swapping.
    /// Attacks are now handled by the RelicCardUI (card-based system).
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TMP_Text instructionText;

        [Header("Selection Visuals")]
        [SerializeField] private Color moveRangeColor = Color.blue;

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
        
        // Attack target highlighting handled entirely by BattleDeckUI now

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

            // Right click - deselect or cancel actions
            if (Input.GetMouseButtonDown(1))
            {
                // Don't deselect the unit if right clicking over a UI element (like a card's stow menu)
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                // Don't deselect if we are currently holding a card waiting for a target 
                // (Right click will just cancel the card target mode inside BattleDeckUI)
                if (Equipment.BattleDeckUI.Instance != null && Equipment.BattleDeckUI.Instance.IsTargeting)
                    return;

                DeselectUnit();
            }

            // Note: Keyboard attacks (C and X) are removed
            // Attacks are now handled by clicking cards in RelicCardUI
        }

        private void HandleLeftClick()
        {
            // Don't process 3D clicks when clicking on UI elements (cards, buttons, etc.)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            // Handle Card Targeting (Deck System)
            if (BattleDeckUI.Instance != null && BattleDeckUI.Instance.IsTargeting)
            {
                // Try to find UnitStatus on the hit object or its parent (for child colliders)
                UnitStatus targetUnit = hit.collider.GetComponent<UnitStatus>()
                    ?? hit.collider.GetComponentInParent<UnitStatus>();
                GridCell targetCell = hit.collider.GetComponent<GridCell>();

                // If we hit a unit's cell, get the cell reference too
                if (targetUnit != null && targetCell == null)
                {
                    var gridManager = ServiceLocator.Get<GridManager>();
                    if (gridManager != null)
                    {
                        var pos = gridManager.WorldToGridPosition(targetUnit.transform.position);
                        targetCell = gridManager.GetCell(pos.x, pos.y);
                    }
                }

                BattleDeckUI.Instance.OnTargetSelected(targetUnit, targetCell);
                return; // Block normal selection/movement
            }

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
            // Legacy tile-click movement removed - movement is now handled by Boots relic cards
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

            // Updated instruction text for card-based combat
            if (instructionText != null)
            {
                instructionText.text = "Use cards to move, attack, and use abilities";
            }

            GameEvents.TriggerUnitSelected(unit);
        }

        private void DeselectUnit()
        {
            ResetHighlights();
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

            // Don't deselect after moving - player may want to attack
            // Just refresh the move tiles
            ResetHighlights();
            validMoveTiles.Clear();
            
            // Re-highlighting dropped as targeting is now exclusively card-driven
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
    }
}