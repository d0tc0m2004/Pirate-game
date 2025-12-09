using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public GameObject enemyCaptainPrefab; 
    public GameObject enemyPrefab;        

    [Header("Spawn Settings")]
    public int minEnemies = 3;
    public int maxEnemies = 4;
    
    [Range(0f, 1f)] 
    public float captainChance = 0.5f;

    private void Awake()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
    }

    public void SpawnEnemies()
    {
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

    void SpawnSingleUnit(GameObject prefabToSpawn)
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
                    GameObject unit = Instantiate(prefabToSpawn, cell.GetWorldPosition(), Quaternion.identity);
                    
                    unit.transform.rotation = Quaternion.LookRotation(Vector3.left);
                    
                    cell.PlaceUnit(unit);
                    placed = true;
                }
            }
        }
    }
}