using UnityEngine;
using System.Collections.Generic;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Enums;

namespace TacticalGame.Combat
{
    /// <summary>
    /// Manages Dead Man's Locker spawning, tribute quota, and loose tribute tracking.
    /// Spawn constraints from design doc:
    ///   - Player side only (not middle column, not enemy side)
    ///   - Not on cells occupied by player units (spawn tiles)
    ///   - At least 1 tile from any player unit (Manhattan distance)
    ///   - Never two lockers on the same row
    ///   - Not adjacent to another locker (Manhattan distance >= 2)
    ///   - Can't spawn in the column directly next to middle (neutral zone buffer)
    ///   - 1-4 lockers based on grid width
    /// </summary>
    public class DeadMansLockerManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Locker Prefab")]
        [SerializeField] private GameObject lockerPrefab;

        [Header("Fallback Visual")]
        [SerializeField] private Material lockerMaterial;

        #endregion

        #region Private State

        private GridManager gridManager;
        private List<DeadMansLocker> activeLockers = new List<DeadMansLocker>();
        private float tributeQuota;
        private float totalCollectedTribute;

        // Loose tribute: keyed by "x,y" string, value = amount on that tile
        private Dictionary<string, float> looseTribute = new Dictionary<string, float>();

        #endregion

        #region Public Properties

        public float TributeQuota => tributeQuota;
        public float TotalCollectedTribute => totalCollectedTribute;
        public int LockerCount => activeLockers.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DeadMansLockerManager>();
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
            tributeQuota = GameConfig.Instance.lockerTributeQuota;
        }

        private void Update()
        {
            // DEBUG: Press L to hit a random surviving locker
            if (Input.GetKeyDown(KeyCode.L))
            {
                var surviving = GetSurvivingLockers();
                if (surviving.Count > 0)
                {
                    var target = surviving[Random.Range(0, surviving.Count)];
                    Debug.Log($"<color=red>[DEBUG] Hitting locker at ({target.GridX},{target.GridY})</color>");
                    target.TakeHit(null);
                }
                else
                {
                    Debug.Log("<color=red>[DEBUG] No surviving lockers to hit</color>");
                }
            }
        }

        #endregion

        #region Spawning

        /// <summary>
        /// Spawn lockers after player deployment, before enemy deployment.
        /// Called by DeploymentManager.
        /// </summary>
        public void SpawnLockers()
        {
            var config = GameConfig.Instance;
            int lockerCount = CalculateLockerCount();

            Debug.Log($"<color=yellow>[LockerManager] Spawning {lockerCount} Dead Man's Lockers on {gridManager.GridWidth}x{gridManager.GridHeight} grid</color>");

            List<GridCell> validCells = GetValidLockerSpawnCells();
            List<GridCell> chosenCells = new List<GridCell>();
            List<int> usedRows = new List<int>();

            for (int i = 0; i < lockerCount; i++)
            {
                GridCell bestCell = PickLockerCell(validCells, chosenCells, usedRows);
                if (bestCell == null)
                {
                    Debug.LogWarning($"[LockerManager] Could not find valid cell for locker {i + 1}/{lockerCount}");
                    break;
                }

                chosenCells.Add(bestCell);
                usedRows.Add(bestCell.YPosition);

                // Remove this cell and now-invalid neighbors from candidates
                PruneInvalidCells(validCells, bestCell, usedRows);

                // Spawn the locker
                DeadMansLocker locker = CreateLocker(bestCell, config.lockerBasePips);
                activeLockers.Add(locker);
            }

            Debug.Log($"<color=green>[LockerManager] Successfully spawned {activeLockers.Count} lockers</color>");
        }

        /// <summary>
        /// Calculate how many lockers to spawn based on total grid area.
        /// Uses player-side cell count (columns * rows) to scale 1-4.
        /// 7x3=9 cells -> 1, 9x5=20 -> 2, 11x5=25 -> 3, 13x7=42 -> 4.
        /// </summary>
        private int CalculateLockerCount()
        {
            var config = GameConfig.Instance;
            int playerColumns = gridManager.GetMiddleColumnIndex(); // cols on player side
            int rows = gridManager.GridHeight;
            int playerArea = playerColumns * rows;

            // Scale by area: <15 = 1, 15-24 = 2, 25-35 = 3, 36+ = 4
            int count;
            if (playerArea < 15) count = 1;
            else if (playerArea < 25) count = 2;
            else if (playerArea < 36) count = 3;
            else count = 4;

            // Also cap by available rows (can't have more lockers than rows)
            count = Mathf.Min(count, rows);

            Debug.Log($"<color=cyan>[LockerManager] Player area: {playerColumns}x{rows}={playerArea}, locker count: {count}</color>");

            return Mathf.Clamp(count, config.lockerMinCount, config.lockerMaxCount);
        }

        /// <summary>
        /// Get all cells valid for locker placement.
        /// Filters: player side, not occupied, not blocked, not adjacent to middle column.
        /// </summary>
        private List<GridCell> GetValidLockerSpawnCells()
        {
            var config = GameConfig.Instance;
            List<GridCell> validCells = new List<GridCell>();
            int middleCol = gridManager.GetMiddleColumnIndex();

            // Neutral zone: the column directly next to middle on player side
            int neutralZoneCol = middleCol - 1;

            for (int x = 0; x < middleCol; x++)
            {
                // Skip neutral zone column (too close to middle)
                if (x == neutralZoneCol) continue;

                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell == null) continue;
                    if (cell.IsOccupied || cell.IsBlocked) continue;
                    if (cell.HasHazard) continue;

                    // Check minimum distance from all player units
                    if (!IsFarEnoughFromUnits(cell, config.lockerMinDistanceFromUnits))
                        continue;

                    validCells.Add(cell);
                }
            }

            return validCells;
        }

        /// <summary>
        /// Check that a cell is at least minDist Manhattan distance from all player units.
        /// </summary>
        private bool IsFarEnoughFromUnits(GridCell cell, int minDist)
        {
            var allUnits = FindObjectsByType<UnitStatus>(FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit.Team != Team.Player) continue;

                Vector2Int unitPos = gridManager.WorldToGridPosition(unit.transform.position);
                int dist = Mathf.Abs(cell.XPosition - unitPos.x) + Mathf.Abs(cell.YPosition - unitPos.y);
                if (dist <= minDist)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Pick the best cell for next locker from remaining valid cells.
        /// Ensures: different row from all chosen, min distance from other lockers.
        /// </summary>
        private GridCell PickLockerCell(List<GridCell> validCells, List<GridCell> chosenCells, List<int> usedRows)
        {
            var config = GameConfig.Instance;
            List<GridCell> candidates = new List<GridCell>();

            foreach (var cell in validCells)
            {
                // Must not be on a used row
                if (usedRows.Contains(cell.YPosition)) continue;

                // Must be far enough from all already-chosen lockers
                bool tooClose = false;
                foreach (var chosen in chosenCells)
                {
                    int dist = Mathf.Abs(cell.XPosition - chosen.XPosition) + Mathf.Abs(cell.YPosition - chosen.YPosition);
                    if (dist < config.lockerMinDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                candidates.Add(cell);
            }

            if (candidates.Count == 0) return null;

            // Pick randomly from valid candidates
            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Remove cells that are no longer valid after placing a locker.
        /// </summary>
        private void PruneInvalidCells(List<GridCell> validCells, GridCell placed, List<int> usedRows)
        {
            var config = GameConfig.Instance;

            validCells.RemoveAll(cell =>
            {
                // Same row as placed locker
                if (cell.YPosition == placed.YPosition) return true;

                // Too close to placed locker
                int dist = Mathf.Abs(cell.XPosition - placed.XPosition) + Mathf.Abs(cell.YPosition - placed.YPosition);
                if (dist < config.lockerMinDistance) return true;

                return false;
            });
        }

        /// <summary>
        /// Create a locker GameObject on the given cell.
        /// </summary>
        private DeadMansLocker CreateLocker(GridCell cell, int pips)
        {
            GameObject lockerObj;

            if (lockerPrefab != null)
            {
                lockerObj = Instantiate(lockerPrefab, cell.GetWorldPosition(), Quaternion.identity);
            }
            else
            {
                // Fallback: create a primitive chest visual
                lockerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lockerObj.transform.position = cell.GetWorldPosition();
                lockerObj.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                // Apply material or default color
                var renderer = lockerObj.GetComponent<MeshRenderer>();
                if (lockerMaterial != null)
                {
                    renderer.material = lockerMaterial;
                }
                else
                {
                    renderer.material.color = new Color(0.6f, 0.4f, 0.1f); // Brown/wood color
                }
            }

            lockerObj.name = $"DeadMansLocker_{cell.XPosition}_{cell.YPosition}";

            // Add the locker component
            DeadMansLocker locker = lockerObj.AddComponent<DeadMansLocker>();
            locker.Initialize(cell, pips);

            return locker;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get the number of surviving (non-destroyed) lockers.
        /// </summary>
        public int GetSurvivingLockerCount()
        {
            int count = 0;
            foreach (var locker in activeLockers)
            {
                if (locker != null && !locker.IsDestroyed)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Get all surviving lockers.
        /// </summary>
        public List<DeadMansLocker> GetSurvivingLockers()
        {
            var surviving = new List<DeadMansLocker>();
            foreach (var locker in activeLockers)
            {
                if (locker != null && !locker.IsDestroyed)
                    surviving.Add(locker);
            }
            return surviving;
        }

        /// <summary>
        /// Get the nearest surviving locker to a grid position.
        /// </summary>
        public DeadMansLocker GetNearestLocker(int x, int y)
        {
            DeadMansLocker nearest = null;
            int minDist = int.MaxValue;

            foreach (var locker in activeLockers)
            {
                if (locker == null || locker.IsDestroyed) continue;

                int dist = locker.DistanceTo(x, y);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = locker;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Get the tribute quota for this battle.
        /// </summary>
        public float GetTributeQuota()
        {
            return tributeQuota;
        }

        /// <summary>
        /// Get total tribute stored across all surviving lockers.
        /// </summary>
        public float GetTotalStoredTribute()
        {
            float total = 0f;
            foreach (var locker in activeLockers)
            {
                if (locker != null && !locker.IsDestroyed)
                    total += locker.StoredTribute;
            }
            return total;
        }

        /// <summary>
        /// Check if all lockers have been destroyed (triggers No-Tales loop).
        /// </summary>
        public bool AreAllLockersDestroyed()
        {
            return GetSurvivingLockerCount() == 0 && activeLockers.Count > 0;
        }

        #endregion

        #region Loose Tribute

        /// <summary>
        /// Add loose tribute at a grid position (from destroyed/hit lockers).
        /// </summary>
        public void AddLooseTribute(int x, int y, float amount)
        {
            // Spread to adjacent cells
            var adjacentOffsets = new Vector2Int[]
            {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };

            float perCell = amount / adjacentOffsets.Length;

            foreach (var offset in adjacentOffsets)
            {
                int nx = x + offset.x;
                int ny = y + offset.y;

                GridCell cell = gridManager.GetCell(nx, ny);
                if (cell == null || cell.IsBlocked || cell.IsMiddleColumn) continue;

                string key = $"{nx},{ny}";
                if (looseTribute.ContainsKey(key))
                    looseTribute[key] += perCell;
                else
                    looseTribute[key] = perCell;

                Debug.Log($"<color=yellow>[LooseTribute] {perCell:F1} tribute at ({nx},{ny})</color>");
            }
        }

        /// <summary>
        /// Pick up loose tribute at a position (e.g., when a unit walks over it).
        /// Returns the amount picked up.
        /// </summary>
        public float PickUpLooseTribute(int x, int y)
        {
            string key = $"{x},{y}";
            if (looseTribute.TryGetValue(key, out float amount))
            {
                looseTribute.Remove(key);
                return amount;
            }
            return 0f;
        }

        /// <summary>
        /// Check if there's loose tribute at a position.
        /// </summary>
        public bool HasLooseTributeAt(int x, int y)
        {
            return looseTribute.ContainsKey($"{x},{y}");
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Called when a locker is destroyed. Checks if all are gone.
        /// </summary>
        public void OnLockerDestroyedCheck()
        {
            if (AreAllLockersDestroyed())
            {
                Debug.Log("<color=red>[LockerManager] ALL LOCKERS DESTROYED - No-Tales condition!</color>");
                GameEvents.TriggerAllLockersDestroyed();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLockerDestroyed += HandleLockerDestroyed;
        }

        private void OnDisable()
        {
            GameEvents.OnLockerDestroyed -= HandleLockerDestroyed;
        }

        private void HandleLockerDestroyed(GameObject lockerObj)
        {
            OnLockerDestroyedCheck();
        }

        #endregion
    }
}
