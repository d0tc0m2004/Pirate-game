using System.Collections.Generic;
using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Grid;

namespace TacticalGame.Hazards
{
    /// <summary>
    /// Manages hazard spawning and balance across the battlefield.
    /// </summary>
    public class HazardManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Hazard Configuration")]
        [SerializeField] private List<HazardData> possibleHazards;

        [Header("Spawn Settings")]
        [Tooltip("Minimum tiles covered by hazards per side.")]
        [SerializeField] private int minOccupiedTilesPerSide = 5;
        [SerializeField] private int maxOccupiedTilesPerSide = 8;

        #endregion

        #region Private State

        private GridManager gridManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<HazardManager>();
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generate random hazards across the battlefield.
        /// </summary>
        public void GenerateRandomHazards()
        {
            if (possibleHazards == null || possibleHazards.Count == 0)
            {
                Debug.LogWarning("No hazards configured in HazardManager!");
                return;
            }

            if (gridManager == null)
            {
                gridManager = ServiceLocator.Get<GridManager>();
            }

            int targetTiles = Random.Range(minOccupiedTilesPerSide, maxOccupiedTilesPerSide + 1);
            Debug.Log($"Target Balance: Occupying ~{targetTiles} tiles per side.");

            SpawnHazardsUntilTargetReached(true, targetTiles);
            SpawnHazardsUntilTargetReached(false, targetTiles);
        }

        #endregion

        #region Private Spawning Logic

        private void SpawnHazardsUntilTargetReached(bool isPlayerSide, int targetTileCount)
        {
            int occupiedCount = 0;
            int attempts = 0;
            const int maxAttempts = 100;

            while (occupiedCount < targetTileCount && attempts < maxAttempts)
            {
                attempts++;

                HazardData selectedHazard = possibleHazards[Random.Range(0, possibleHazards.Count)];
                int shapeSize = GetShapeSize(selectedHazard.shapePattern);

                // Skip if this shape would exceed target by too much
                if (occupiedCount + shapeSize > targetTileCount + 2)
                {
                    continue;
                }

                // Find valid spawn position
                int middle = gridManager.GetMiddleColumnIndex();
                int startX = isPlayerSide 
                    ? Random.Range(0, middle) 
                    : Random.Range(middle + 1, gridManager.GridWidth);
                int startY = Random.Range(0, gridManager.GridHeight);

                List<Vector2Int> targetCoords = GetShapeCoordinates(selectedHazard.shapePattern, startX, startY);

                // Validate all coordinates
                bool shapeIsValid = true;
                foreach (Vector2Int coord in targetCoords)
                {
                    GridCell cell = gridManager.GetCell(coord.x, coord.y);

                    if (cell == null || cell.IsMiddleColumn || cell.HasHazard)
                    {
                        shapeIsValid = false;
                        break;
                    }
                }

                // Spawn if valid
                if (shapeIsValid)
                {
                    foreach (Vector2Int coord in targetCoords)
                    {
                        GridCell cell = gridManager.GetCell(coord.x, coord.y);

                        // Handle displacement
                        if (cell.IsOccupied && selectedHazard.causesDisplacement)
                        {
                            DisplaceUnit(cell);
                        }

                        // Spawn hazard
                        cell.ApplyHazard(selectedHazard.hazardPrefab, selectedHazard.isBlocking);

                        // Initialize hazard instance
                        GameObject spawnedObj = cell.HazardVisualObject;
                        if (spawnedObj != null)
                        {
                            HazardInstance instance = spawnedObj.GetComponent<HazardInstance>();
                            if (instance == null)
                            {
                                instance = spawnedObj.AddComponent<HazardInstance>();
                            }

                            instance.Initialize(selectedHazard, cell);

                            // Trigger effect if unit present
                            if (cell.IsOccupied && cell.OccupyingUnit != null)
                            {
                                Debug.Log($"Hazard spawned under {cell.OccupyingUnit.name}. Triggering effect!");
                                instance.OnUnitEnter(cell.OccupyingUnit);
                            }
                        }
                    }

                    occupiedCount += shapeSize;
                }
            }

            Debug.Log($"Side {(isPlayerSide ? "Left" : "Right")} finished with {occupiedCount} tiles occupied.");
        }

        private int GetShapeSize(HazardShape shape)
        {
            return shape switch
            {
                HazardShape.Single => 1,
                HazardShape.Row => 3,
                HazardShape.Column => 3,
                HazardShape.Square => 4,
                HazardShape.Plus => 5,
                _ => 1
            };
        }

        private List<Vector2Int> GetShapeCoordinates(HazardShape shape, int x, int y)
        {
            List<Vector2Int> coords = new List<Vector2Int> { new Vector2Int(x, y) };

            switch (shape)
            {
                case HazardShape.Row:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x - 1, y));
                    break;
                    
                case HazardShape.Column:
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x, y - 1));
                    break;
                    
                case HazardShape.Square:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x + 1, y + 1));
                    break;
                    
                case HazardShape.Plus:
                    coords.Add(new Vector2Int(x + 1, y));
                    coords.Add(new Vector2Int(x - 1, y));
                    coords.Add(new Vector2Int(x, y + 1));
                    coords.Add(new Vector2Int(x, y - 1));
                    break;
            }

            return coords;
        }

        private void DisplaceUnit(GridCell currentCell)
        {
            GameObject unit = currentCell.OccupyingUnit;
            currentCell.RemoveUnit();

            Vector2Int[] directions = 
            {
                Vector2Int.up, 
                Vector2Int.down, 
                Vector2Int.left, 
                Vector2Int.right
            };

            foreach (Vector2Int dir in directions)
            {
                GridCell neighbor = gridManager.GetCell(
                    currentCell.XPosition + dir.x, 
                    currentCell.YPosition + dir.y
                );

                if (neighbor != null && neighbor.CanPlaceUnit())
                {
                    neighbor.PlaceUnit(unit);
                    return;
                }
            }
        }

        #endregion
    }
}