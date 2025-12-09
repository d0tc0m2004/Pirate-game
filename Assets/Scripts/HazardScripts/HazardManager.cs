using System.Collections.Generic;
using UnityEngine;

public class HazardManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GridManager gridManager;

    [Header("Hazard Configuration")]
    public List<HazardData> possibleHazards; 
    
    [Header("Spawn Settings")]
    [Tooltip("Both sides will have at least this many tiles covered by hazards.")]
    public int minOccupiedTilesPerSide = 5; 
    public int maxOccupiedTilesPerSide = 8; 

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    public void GenerateRandomHazards()
    {
        if (possibleHazards == null || possibleHazards.Count == 0) return;

        int targetTiles = Random.Range(minOccupiedTilesPerSide, maxOccupiedTilesPerSide + 1);
        Debug.Log($"Target Balance: Occupying ~{targetTiles} tiles per side.");

        SpawnHazardsUntilTargetReached(true, targetTiles);
        SpawnHazardsUntilTargetReached(false, targetTiles);
    }

    void SpawnHazardsUntilTargetReached(bool isPlayerSide, int targetTileCount)
    {
        int occupiedCount = 0;
        int attempts = 0;
        int maxAttempts = 100; 

        while (occupiedCount < targetTileCount && attempts < maxAttempts)
        {
            attempts++;

            HazardData selectedHazard = possibleHazards[Random.Range(0, possibleHazards.Count)];
            
            int shapeSize = GetShapeSize(selectedHazard.shapePattern);

            if (occupiedCount + shapeSize > targetTileCount + 2) 
            {
                continue; 
            }

            int middle = gridManager.GetMiddleColumnIndex();
            int startX = isPlayerSide ? Random.Range(0, middle) : Random.Range(middle + 1, gridManager.gridWidth);
            int startY = Random.Range(0, gridManager.gridHeight);

            List<Vector2Int> targetCoords = GetShapeCoordinates(selectedHazard.shapePattern, startX, startY);

            bool shapeIsValid = true;
            foreach (Vector2Int coord in targetCoords)
            {
                GridCell cell = gridManager.GetCell(coord.x, coord.y);
                
                if (cell == null || cell.isMiddleColumn || cell.hasHazard)
                {
                    shapeIsValid = false;
                    break;
                }
            }

            if (shapeIsValid)
            {
                foreach (Vector2Int coord in targetCoords)
                {
                    GridCell cell = gridManager.GetCell(coord.x, coord.y);
                    
                    if (cell.isOccupied && selectedHazard.causesDisplacement)
                    {
                        DisplaceUnit(cell);
                    }

                    cell.ApplyHazard(selectedHazard.hazardPrefab, selectedHazard.isBlocking);

                    GameObject spawnedObj = cell.hazardVisualObject;
                    if (spawnedObj != null)
                    {
                        HazardInstance instance = spawnedObj.GetComponent<HazardInstance>();
                        if (instance == null) instance = spawnedObj.AddComponent<HazardInstance>();
                        
                        instance.Initialize(selectedHazard, cell);

                        if (cell.isOccupied && cell.occupyingUnit != null)
                        {
                            Debug.Log($"Hazard spawned under {cell.occupyingUnit.name}. Triggering effect!");
                            instance.OnUnitEnter(cell.occupyingUnit);
                        }
                    }
                }

                occupiedCount += shapeSize;
            }
        }
        
        Debug.Log($"Side {(isPlayerSide ? "Left" : "Right")} finished with {occupiedCount} tiles occupied.");
    }

    int GetShapeSize(HazardData.HazardShape shape)
    {
        switch (shape)
        {
            case HazardData.HazardShape.Single: return 1;
            case HazardData.HazardShape.Row: return 3;
            case HazardData.HazardShape.Column: return 3;
            case HazardData.HazardShape.Square: return 4;
            case HazardData.HazardShape.Plus: return 5;
            default: return 1;
        }
    }

    List<Vector2Int> GetShapeCoordinates(HazardData.HazardShape shape, int x, int y)
    {
        List<Vector2Int> coords = new List<Vector2Int>();
        coords.Add(new Vector2Int(x, y));

        switch (shape)
        {
            case HazardData.HazardShape.Row:
                coords.Add(new Vector2Int(x + 1, y));
                coords.Add(new Vector2Int(x - 1, y));
                break;
            case HazardData.HazardShape.Column:
                coords.Add(new Vector2Int(x, y + 1));
                coords.Add(new Vector2Int(x, y - 1));
                break;
            case HazardData.HazardShape.Square:
                coords.Add(new Vector2Int(x + 1, y));
                coords.Add(new Vector2Int(x, y + 1));
                coords.Add(new Vector2Int(x + 1, y + 1));
                break;
            case HazardData.HazardShape.Plus:
                coords.Add(new Vector2Int(x + 1, y));
                coords.Add(new Vector2Int(x - 1, y));
                coords.Add(new Vector2Int(x, y + 1));
                coords.Add(new Vector2Int(x, y - 1));
                break;
        }
        return coords;
    }

    void DisplaceUnit(GridCell currentCell)
    {
        GameObject unit = currentCell.occupyingUnit;
        currentCell.RemoveUnit(); 
        
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int dir in dirs)
        {
            GridCell n = gridManager.GetCell(currentCell.xPosition + dir.x, currentCell.yPosition + dir.y);
            if (n != null && !n.isOccupied && !n.isBlocked && !n.isMiddleColumn)
            {
                n.PlaceUnit(unit);
                return; 
            }
        }
    }
}