using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeploymentManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public TurnManager turnManager;
    public BattleManager battleManager;
    public GlobalUIManager globalUI;
    
    // Managers needed for game start
    public EnergyManager energyManager;
    public HazardManager hazardManager;     
    public EnemyManager enemyManager;

    [Header("UI Buttons")]
    public GameObject endTurnButton; 

    [Header("Unit Settings")]
    public GameObject captainPrefab; 
    public GameObject unitPrefab;    
    public int maxUnits = 4; 
    
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public Transform[] enemySpawnPoints;

    [Header("Colors")]
    public Color validHoverColor = Color.green;
    public Color invalidHoverColor = Color.red;
    public Color selectedColor = Color.yellow; 
    
    // State Tracking
    private bool isDeploymentPhase = true;
    private int currentUnitCount = 0;
    private bool isCaptainPlaced = false; 
    
    // Selection Logic
    private GameObject selectedUnitToMove = null; 
    private GridCell lastHoveredCell = null;
    private Material originalMaterialAsset; 

    private void Start()
    {
        // Find all managers
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (globalUI == null) globalUI = FindFirstObjectByType<GlobalUIManager>();
        if (energyManager == null) energyManager = FindFirstObjectByType<EnergyManager>();
        
        if (hazardManager == null) hazardManager = FindFirstObjectByType<HazardManager>();
        if (enemyManager == null) enemyManager = FindFirstObjectByType<EnemyManager>();

        // Setup State
        if (battleManager != null) battleManager.isBattleActive = false;
        if (endTurnButton != null) endTurnButton.SetActive(false);
        isDeploymentPhase = true;
    }

    private void Update()
    {
        if (!isDeploymentPhase) return;
        HandleMouseInteraction();
    }

    void HandleMouseInteraction()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GridCell cell = null;

            // 1. Hit a Unit? Find its cell.
            if (hit.collider.CompareTag("Unit")) 
            {
                if (gridManager != null)
                {
                    Vector2Int pos = gridManager.WorldToGridPosition(hit.collider.transform.position);
                    cell = gridManager.GetCell(pos.x, pos.y);
                }
            }
            // 2. Hit a Cell?
            else
            {
                cell = hit.collider.GetComponent<GridCell>();
            }

            if (cell != null)
            {
                HighlightCell(cell);

                if (Input.GetMouseButtonDown(0)) HandleLeftClick(cell);
                if (Input.GetMouseButtonDown(1)) HandleRightClick(); // Changed to simple Deselect
            }
        }
    }

    void HandleLeftClick(GridCell cell)
    {
        // A. Pick Up Unit (Only if Player unit)
        if (cell.isOccupied && cell.occupyingUnit != null)
        {
            if (cell.occupyingUnit.name.Contains("Player") || cell.occupyingUnit.name.Contains("Captain"))
            {
                selectedUnitToMove = cell.occupyingUnit;
                Debug.Log($"Picked up {selectedUnitToMove.name}");
            }
        }
        // B. Place or Move Unit to Empty Cell
        else if (!cell.isOccupied && IsValidPlacement(cell))
        {
            // 1. Move existing unit
            if (selectedUnitToMove != null)
            {
                Vector2Int oldGridPos = gridManager.WorldToGridPosition(selectedUnitToMove.transform.position);
                GridCell oldCell = gridManager.GetCell(oldGridPos.x, oldGridPos.y);
                if (oldCell != null) oldCell.RemoveUnit();

                selectedUnitToMove.transform.position = cell.GetWorldPosition();
                cell.PlaceUnit(selectedUnitToMove);
                
                // Deselect after moving
                selectedUnitToMove = null; 
            }
            // 2. Spawn NEW unit
            else if (currentUnitCount < maxUnits)
            {
                SpawnNewUnit(cell);
            }
        }
    }

    void SpawnNewUnit(GridCell cell)
    {
        GameObject prefabToSpawn;
        string unitName = "Unit_Player_";

        if (!isCaptainPlaced)
        {
            prefabToSpawn = captainPrefab;
            isCaptainPlaced = true;
            unitName += "Captain";
        }
        else
        {
            prefabToSpawn = unitPrefab;
            unitName += currentUnitCount;
        }

        if (prefabToSpawn != null)
        {
            GameObject newUnit = Instantiate(prefabToSpawn, cell.GetWorldPosition(), Quaternion.identity);
            newUnit.name = unitName;
            newUnit.tag = "Unit"; 
            
            // Inject Managers
            UnitAttack attack = newUnit.GetComponent<UnitAttack>();
            if (attack != null) attack.SetupManagers(gridManager, energyManager);

            cell.PlaceUnit(newUnit);
            currentUnitCount++;
        }
    }

    void HandleRightClick()
    {
        // ONLY DESELECT. No deletion logic here anymore.
        if (selectedUnitToMove != null)
        {
            Debug.Log("Selection Cancelled");
            selectedUnitToMove = null;
            ResetLastHighlight();
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
                renderer.material.color = IsValidPlacement(cell) ? selectedColor : invalidHoverColor;
            else
                renderer.material.color = IsValidPlacement(cell) ? validHoverColor : invalidHoverColor;
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
        return !cell.isBlocked && (cell.isPlayerSide || cell.xPosition < 4); 
    }
    
    public void FinishDeploymentAndStartBattle()
    {
        if (!isDeploymentPhase) return;
        
        if (!isCaptainPlaced) { Debug.Log("Need Captain!"); return; }
        if (currentUnitCount == 0) return;

        // Lock Deployment
        isDeploymentPhase = false;
        selectedUnitToMove = null;
        ResetLastHighlight(); 

        // Start Systems
        if (battleManager != null) battleManager.isBattleActive = true; 
        if (turnManager != null) turnManager.StartGameLoop();
        if (hazardManager != null) hazardManager.GenerateRandomHazards();
        
        if (enemyManager != null) enemyManager.SpawnEnemies();
        else SpawnEnemiesInternal();
        
        if (globalUI != null) globalUI.GenerateUnitIcons(); 
        if (endTurnButton != null) endTurnButton.SetActive(true);
        
        gameObject.SetActive(false); 
    }

    void SpawnEnemiesInternal()
    {
        if (enemySpawnPoints == null) return;
        foreach (Transform spawnPoint in enemySpawnPoints)
        {
            if (spawnPoint != null && enemyPrefab != null)
            {
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
                enemy.name = "Unit_Enemy";
                
                UnitAttack attack = enemy.GetComponent<UnitAttack>();
                if (attack != null) attack.SetupManagers(gridManager, energyManager);

                if (gridManager != null)
                {
                    Vector2Int gridPos = gridManager.WorldToGridPosition(enemy.transform.position);
                    GridCell cell = gridManager.GetCell(gridPos.x, gridPos.y);
                    if (cell != null) cell.PlaceUnit(enemy);
                }
            }
        }
    }
}