using UnityEngine;

public class DeploymentManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public HazardManager hazardManager;
    public EnemyManager enemyManager;
    
    [Header("Unit Settings")]
    public GameObject unitPrefab; 
    public int maxUnits = 5;
    
    [Header("Colors")]
    public Color validHoverColor = Color.green;
    public Color invalidHoverColor = Color.red;
    
    private bool isDeploymentPhase = true;
    private int currentUnitCount = 0;
    
    private GridCell lastHoveredCell = null;
    private Material originalMaterialAsset;

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (hazardManager == null) hazardManager = FindFirstObjectByType<HazardManager>();
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
            GridCell cell = hit.collider.GetComponent<GridCell>();

            if (cell != null)
            {
                if (lastHoveredCell != cell)
                {
                    ResetLastHighlight();
                    HighlightCell(cell); 
                }

                if (Input.GetMouseButtonDown(0)) TryPlaceUnit(cell);
                if (Input.GetMouseButtonDown(1)) TryRemoveUnit(cell);
            }
            else
            {
                ResetLastHighlight();
            }
        }
        else
        {
            ResetLastHighlight();
        }
    }

    void HighlightCell(GridCell cell)
    {
        lastHoveredCell = cell;
        
        MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            originalMaterialAsset = renderer.sharedMaterial;
            
            renderer.material.color = IsValidPlacement(cell) ? validHoverColor : invalidHoverColor;
        }
    }

    void ResetLastHighlight()
    {
        if (lastHoveredCell != null)
        {
            MeshRenderer renderer = lastHoveredCell.GetComponent<MeshRenderer>();
            if (renderer != null && originalMaterialAsset != null)
            {
                renderer.material = originalMaterialAsset;
            }
            lastHoveredCell = null;
            originalMaterialAsset = null;
        }
    }

    bool IsValidPlacement(GridCell cell)
    {
        return cell.isPlayerSide && !cell.isOccupied && !cell.isBlocked && !cell.isMiddleColumn;
    }

    void TryPlaceUnit(GridCell cell)
    {
        if (!IsValidPlacement(cell) || currentUnitCount >= maxUnits) return;

        GameObject newUnit = Instantiate(unitPrefab, cell.GetWorldPosition(), Quaternion.identity);
        cell.PlaceUnit(newUnit);
        currentUnitCount++;
    }

    void TryRemoveUnit(GridCell cell)
    {
        if (cell.isOccupied && cell.occupyingUnit != null)
        {
            Destroy(cell.occupyingUnit);
            cell.RemoveUnit();
            currentUnitCount--;
        }
    }

    public void FinishDeploymentAndStartBattle()
    {
        if (!isDeploymentPhase) return;

        if (currentUnitCount == 0)
        {
            Debug.Log("Place at least one unit!");
            return;
        }

        isDeploymentPhase = false;
        ResetLastHighlight();
        
        Debug.Log("Deployment Finished. Spawning Hazards...");
        hazardManager.GenerateRandomHazards();
        enemyManager.SpawnEnemies();
    }
}