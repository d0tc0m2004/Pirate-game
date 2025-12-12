using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public TMP_Text instructionText; 
    public EnergyManager energyManager; 
    public TurnManager turnManager; 
    
    [Header("Selection Visuals")]
    public Color moveRangeColor = Color.blue;
    
    public bool isBattleActive = false;
    private GameObject selectedUnit;
    private bool isSwapping = false; 

    private List<GridCell> validMoveTiles = new List<GridCell>();
    private Dictionary<GridCell, Material> originalMaterials = new Dictionary<GridCell, Material>();

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (instructionText != null) instructionText.text = ""; 
    }

    public GameObject GetSelectedUnit() { return selectedUnit; }

    private void Update()
    {
        if (!isBattleActive) return;

        if (Input.GetMouseButtonDown(0)) HandleClick();
        if (Input.GetMouseButtonDown(1)) DeselectUnit(); 

        if (selectedUnit != null && !isSwapping)
        {
             if (Input.GetKeyDown(KeyCode.C)) 
             {
                 UnitAttack attacker = selectedUnit.GetComponent<UnitAttack>();
                 if (attacker) { attacker.TryMeleeAttack(); DeselectUnit(); }
             }
             if (Input.GetKeyDown(KeyCode.X)) 
             {
                 UnitAttack attacker = selectedUnit.GetComponent<UnitAttack>();
                 if (attacker) { attacker.TryRangedAttack(); DeselectUnit(); }
             }
        }
    }

    public void InitiateSwapMode()
    {
        if (selectedUnit == null) return;
        UnitStatus status = selectedUnit.GetComponent<UnitStatus>();
        
        if (turnManager.swapsUsedThisRound >= 1)
        {
            Debug.Log("Cannot Swap: Limit reached (1 per round)!");
            return;
        }
        if (status.swapCooldown > 0)
        {
            Debug.Log($"Cannot Swap: Unit is recovering ({status.swapCooldown} turns left).");
            return;
        }

        if (energyManager.currentEnergy < 1) 
        {
            Debug.Log("Not enough Energy to Swap!");
            return;
        }
        if (status.hasSurrendered) return;

        float hpPercent = (float)status.currentHP / (float)status.maxHP;
        if (hpPercent < 0.2f)
        {
            Debug.Log("Unit is too injured to swap! (<20% HP)");
            return;
        }

        isSwapping = true;
        Debug.Log("Click on an unit or empty grid to swap.");
        if (instructionText) instructionText.text = "Select Target for Swap...";
    }

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (isSwapping && selectedUnit != null)
            {
                ExecuteSwap(hit);
                return;
            }

            UnitMovement unitClicked = hit.collider.GetComponent<UnitMovement>();
            if (unitClicked != null)
            {
                if (selectedUnit != null) DeselectUnit();
                SelectUnit(unitClicked.gameObject);
                return;
            }

            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null)
            {
                if (cell.isOccupied && cell.occupyingUnit != null)
                {
                    if (selectedUnit != null) DeselectUnit();
                    SelectUnit(cell.occupyingUnit);
                }
                else if (selectedUnit != null && validMoveTiles.Contains(cell))
                {
                    MoveSelectedUnitTo(cell);
                }
            }
        }
    }

    void ExecuteSwap(RaycastHit hit)
    {
        UnitStatus sourceStatus = selectedUnit.GetComponent<UnitStatus>();
        Vector2Int sourceGridPos = gridManager.WorldToGridPosition(selectedUnit.transform.position);
        GridCell sourceCell = gridManager.GetCell(sourceGridPos.x, sourceGridPos.y);

        GridCell targetCell = null;
        GameObject targetUnit = null;

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

        if (targetCell == null) return;
        if (targetCell == sourceCell) return;
        energyManager.TrySpendEnergy(1);
        turnManager.swapsUsedThisRound++;
        sourceStatus.swapCooldown = 3; 

        if (!selectedUnit.name.Contains("Captain")) sourceStatus.ApplySwapPenalty();

        if (targetUnit != null)
        {
            targetUnit.transform.position = sourceCell.GetWorldPosition();
            sourceCell.PlaceUnit(targetUnit);
            selectedUnit.transform.position = targetCell.GetWorldPosition();
            targetCell.PlaceUnit(selectedUnit);
            Debug.Log("Units Swapped Positions!");
        }
        else if (!targetCell.isBlocked && !targetCell.isOccupied)
        {
            sourceCell.RemoveUnit();
            selectedUnit.transform.position = targetCell.GetWorldPosition();
            targetCell.PlaceUnit(selectedUnit);
            Debug.Log("Swapped to Empty Grid!");
        }

        DeselectUnit(); 
    }

    void SelectUnit(GameObject unit)
    {
        if (unit.name.Contains("Enemy")) return;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        UnitStatus status = unit.GetComponent<UnitStatus>();
        
        if (movement == null) return;
        if (status != null && (status.isTrapped || status.hasSurrendered)) return;

        selectedUnit = unit;
        isSwapping = false; 

        if (instructionText != null) instructionText.text = "Click Blue Tile to Move\nPress 'C' Melee | 'X' Ranged";

        Vector2Int gridPos = gridManager.WorldToGridPosition(unit.transform.position);
        GridCell startCell = gridManager.GetCell(gridPos.x, gridPos.y);
        CalculateValidMoves(startCell, movement.moveRange);
    }

    void DeselectUnit()
    {
        ResetHighlights();
        selectedUnit = null;
        isSwapping = false;
        validMoveTiles.Clear();
        if (instructionText != null) instructionText.text = ""; 
    }

    void MoveSelectedUnitTo(GridCell targetCell)
    {
        if (selectedUnit.GetComponent<UnitMovement>().hasAttacked) return;

        Vector2Int oldPos = gridManager.WorldToGridPosition(selectedUnit.transform.position);
        GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
        oldCell.RemoveUnit();

        UnitMovement movement = selectedUnit.GetComponent<UnitMovement>();
        movement.MoveToCell(targetCell);
        targetCell.PlaceUnit(selectedUnit);

        DeselectUnit();
    }
    
    void CalculateValidMoves(GridCell startCell, int range) {
        validMoveTiles.Clear();
        Queue<GridCell> queue = new Queue<GridCell>();
        Dictionary<GridCell, int> distances = new Dictionary<GridCell, int>();
        queue.Enqueue(startCell);
        distances[startCell] = 0;
        while (queue.Count > 0) {
            GridCell current = queue.Dequeue();
            if (distances[current] >= range) continue;
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in dirs) {
                GridCell neighbor = gridManager.GetCell(current.xPosition + dir.x, current.yPosition + dir.y);
                if (neighbor != null && !distances.ContainsKey(neighbor) && !neighbor.isBlocked && !neighbor.isOccupied) {
                    distances[neighbor] = distances[current] + 1;
                    queue.Enqueue(neighbor);
                    validMoveTiles.Add(neighbor);
                    HighlightTile(neighbor);
                }
            }
        }
    }
    void HighlightTile(GridCell cell) {
        MeshRenderer r = cell.GetComponent<MeshRenderer>();
        if (r != null) {
            if (!originalMaterials.ContainsKey(cell)) originalMaterials[cell] = r.sharedMaterial;
            r.material.color = moveRangeColor;
        }
    }
    void ResetHighlights() {
        foreach (KeyValuePair<GridCell, Material> entry in originalMaterials) {
             if (entry.Key != null) entry.Key.GetComponent<MeshRenderer>().material = entry.Value;
        }
        originalMaterials.Clear();
    }
}