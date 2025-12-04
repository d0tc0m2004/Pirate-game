using UnityEngine;

public class DeploymentManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public HazardManager hazardManager;
    public EnemyManager enemyManager;
    public GameObject endTurnButton;
    public UIManager uiManager; 

    [Header("Unit Settings")]
    public GameObject unitPrefab; 
    public int maxUnits = 4; 
    
    [Header("Colors")]
    public Color validHoverColor = Color.green;
    public Color invalidHoverColor = Color.red;
    public Color selectedColor = Color.yellow; 
    
    private bool isDeploymentPhase = true;
    private int currentUnitCount = 0;
    
    private GameObject selectedUnitToMove = null; 
    private GridCell lastHoveredCell = null;
    private Material originalMaterialAsset; 

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (hazardManager == null) hazardManager = FindFirstObjectByType<HazardManager>();
        if (enemyManager == null) enemyManager = FindFirstObjectByType<EnemyManager>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        
        if (endTurnButton != null) endTurnButton.SetActive(false);
    }

    private void Update()
    {
        if (!isDeploymentPhase) return;
        HandleMouseInteraction();
    }

    void HandleMouseInteraction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GridCell cell = null;

            if (hit.collider.CompareTag("Unit")) 
            {
                Vector2Int pos = gridManager.WorldToGridPosition(hit.collider.transform.position);
                cell = gridManager.GetCell(pos.x, pos.y);
            }
            else
            {
                cell = hit.collider.GetComponent<GridCell>();
            }

            if (cell != null)
            {
                HighlightCell(cell);

                if (Input.GetMouseButtonDown(0))
                {
                    HandleLeftClick(cell);
                }
            }
        }
    }

    void HandleLeftClick(GridCell cell)
    {
        if (cell.isOccupied && cell.occupyingUnit != null)
        {
            if (cell.occupyingUnit.name.Contains("Player") || cell.occupyingUnit.CompareTag("Unit"))
            {
                selectedUnitToMove = cell.occupyingUnit;
                Debug.Log($"Picked up {selectedUnitToMove.name}");
            }
        }
        else if (!cell.isOccupied && IsValidPlacement(cell))
        {
            if (selectedUnitToMove != null)
            {
                // Remove from old cell
                Vector2Int oldGridPos = gridManager.WorldToGridPosition(selectedUnitToMove.transform.position);
                GridCell oldCell = gridManager.GetCell(oldGridPos.x, oldGridPos.y);
                oldCell.RemoveUnit();

                selectedUnitToMove.transform.position = cell.GetWorldPosition();

                cell.PlaceUnit(selectedUnitToMove);
                
                selectedUnitToMove = null;
                Debug.Log("Unit Moved!");
            }
            else if (currentUnitCount < maxUnits)
            {
                GameObject newUnit = Instantiate(unitPrefab, cell.GetWorldPosition(), Quaternion.identity);

                newUnit.tag = "Unit"; 
                cell.PlaceUnit(newUnit);
                currentUnitCount++;
            }
        }
    }

    void HighlightCell(GridCell cell)
    {
        if (lastHoveredCell != cell) ResetLastHighlight();

        lastHoveredCell = cell;
        MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (originalMaterialAsset == null) originalMaterialAsset = renderer.sharedMaterial;
            if (selectedUnitToMove != null)
            {
                renderer.material.color = IsValidPlacement(cell) ? selectedColor : invalidHoverColor;
            }
            else
            {
                renderer.material.color = IsValidPlacement(cell) ? validHoverColor : invalidHoverColor;
            }
        }
    }

    void ResetLastHighlight()
    {
        if (lastHoveredCell != null && originalMaterialAsset != null)
        {
            lastHoveredCell.GetComponent<MeshRenderer>().material = originalMaterialAsset;
            originalMaterialAsset = null;
        }
        lastHoveredCell = null;
    }

    bool IsValidPlacement(GridCell cell)
    {
        return cell.isPlayerSide && !cell.isBlocked && !cell.isMiddleColumn;
    }

    public void FinishDeploymentAndStartBattle()
    {
        if (!isDeploymentPhase) return;
        if (currentUnitCount == 0) return;

        isDeploymentPhase = false;
        selectedUnitToMove = null;
        ResetLastHighlight(); 
        
        hazardManager.GenerateRandomHazards();
        enemyManager.SpawnEnemies();
        
        if (uiManager != null) uiManager.SetupBattleUI();
        
        if (endTurnButton != null) endTurnButton.SetActive(true);
        FindFirstObjectByType<TurnManager>().StartGameLoop();
    }
}