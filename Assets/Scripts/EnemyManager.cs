using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    public int minEnemies = 2;
    public int maxEnemies = 4;

    private void Awake()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
    }

    public void SpawnEnemies()
    {
        int count = Random.Range(minEnemies, maxEnemies + 1);
        Debug.Log($"Spawning {count} Enemies...");

        for (int i = 0; i < count; i++)
        {
            SpawnSingleEnemy();
        }
    }

    void SpawnSingleEnemy()
    {
        int attempts = 0;
        bool placed = false;

        while (!placed && attempts < 100)
        {
            attempts++;

            int x = Random.Range(0, gridManager.gridWidth);
            int y = Random.Range(0, gridManager.gridHeight);
            if (gridManager.IsEnemySide(x))
            {
                GridCell cell = gridManager.GetCell(x, y);
                if (cell != null && !cell.isOccupied && !cell.isBlocked && !cell.hasHazard)
                {
                    GameObject enemy = Instantiate(enemyPrefab, cell.GetWorldPosition(), Quaternion.identity);

                    cell.PlaceUnit(enemy);
                    placed = true;
                }
            }
        }
    }
}