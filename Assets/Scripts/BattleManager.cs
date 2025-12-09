using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public TMP_Text instructionText; 
    public EnergyManager energyManager;
    
    [Header("Selection Visuals")]
    public Color moveRangeColor = Color.blue;
    
    public bool isBattleActive = false;
    private GameObject selectedUnit;
    private List<GridCell> validMoveTiles = new List<GridCell>();
    private Dictionary<GridCell, Material> originalMaterials = new Dictionary<GridCell, Material>();

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        if (instructionText != null) instructionText.text = ""; 
    }

    public GameObject GetSelectedUnit() 
    { 
        return selectedUnit; 
    }
    // ----------------------------------------------------

    private void Update()
    {
        if (!isBattleActive) return;

        if (Input.GetMouseButtonDown(0)) HandleClick();
        if (Input.GetMouseButtonDown(1)) DeselectUnit();

        if (selectedUnit != null)
        {
            if (selectedUnit.name.Contains("Player") || selectedUnit.name.Contains("Captain"))
            {
                UnitAttack attacker = selectedUnit.GetComponent<UnitAttack>();
                if (attacker != null)
                {
                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        attacker.TryMeleeAttack();
                        DeselectUnit(); 
                    }
                    if (Input.GetKeyDown(KeyCode.X))
                    {
                        attacker.TryRangedAttack();
                        DeselectUnit();
                    }
                }
            }
        }
    }

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
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

    void SelectUnit(GameObject unit)
    {
        if (unit.name.Contains("Enemy")) return;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        UnitStatus status = unit.GetComponent<UnitStatus>();
        
        if (movement == null) return;
        if (movement.hasAttacked) { Debug.Log("Unit already attacked!"); return; }
        if (status != null && (status.isTrapped || status.hasSurrendered)) return;

        selectedUnit = unit;
        if (instructionText != null) instructionText.text = "Click Blue Tile to Move\nPress 'C' Melee | 'X' Ranged";

        Vector2Int gridPos = gridManager.WorldToGridPosition(unit.transform.position);
        GridCell startCell = gridManager.GetCell(gridPos.x, gridPos.y);
        CalculateValidMoves(startCell, movement.moveRange);
    }

    void DeselectUnit()
    {
        ResetHighlights();
        selectedUnit = null;
        validMoveTiles.Clear();
        if (instructionText != null) instructionText.text = ""; 
    }

    void MoveSelectedUnitTo(GridCell targetCell)
    {
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