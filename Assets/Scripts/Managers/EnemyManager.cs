using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Grid;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages enemy unit spawning (for non-manual deployment scenarios).
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject enemyCaptainPrefab;
        [SerializeField] private GameObject enemyPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int minEnemies = 3;
        [SerializeField] private int maxEnemies = 4;
        [SerializeField] [Range(0f, 1f)] private float captainChance = 0.5f;

        private GridManager gridManager;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<EnemyManager>();
        }

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
        }

        public void SpawnEnemies()
        {
            if (gridManager == null)
                gridManager = ServiceLocator.Get<GridManager>();

            int totalUnits = Random.Range(minEnemies, maxEnemies + 1);
            bool hasCaptain = Random.value < captainChance;
            int soldiersToSpawn = totalUnits;

            Debug.Log($"Spawning Enemy Team. Total Size: {totalUnits}. Captain? {hasCaptain}");

            if (hasCaptain)
            {
                SpawnSingleUnit(enemyCaptainPrefab);
                soldiersToSpawn--;
            }

            for (int i = 0; i < soldiersToSpawn; i++)
            {
                SpawnSingleUnit(enemyPrefab);
            }
        }

        private void SpawnSingleUnit(GameObject prefabToSpawn)
        {
            const int maxAttempts = 100;
            
            for (int attempts = 0; attempts < maxAttempts; attempts++)
            {
                int x = Random.Range(0, gridManager.GridWidth);
                int y = Random.Range(0, gridManager.GridHeight);

                if (gridManager.IsEnemySide(x))
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null && cell.CanPlaceUnit())
                    {
                        GameObject unit = Instantiate(prefabToSpawn, cell.GetWorldPosition(), Quaternion.identity);
                        unit.transform.rotation = Quaternion.LookRotation(Vector3.left);
                        cell.PlaceUnit(unit);
                        return;
                    }
                }
            }
            Debug.LogWarning("Failed to place enemy unit after max attempts");
        }
    }
}