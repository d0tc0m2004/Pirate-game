using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DeploymentManager : MonoBehaviour
{
    [System.Serializable]
    public struct RolePrefab
    {
        public string roleName;
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
    }

    [Header("References")]
    public GridManager gridManager;
    public TurnManager turnManager;
    public BattleManager battleManager;
    public GlobalUIManager globalUI;
    public EnergyManager energyManager;
    public HazardManager hazardManager;     
    public EnemyManager enemyManager;

    [Header("UI Buttons")]
    public GameObject finishDeploymentButton;
    public GameObject endTurnButton;

    [Header("Role Settings")]
    public List<RolePrefab> rolePrefabs; 
    public GameObject defaultUnitPrefab; 

    [Header("Placement Settings")]
    public Color validHoverColor = Color.green;
    public Color invalidHoverColor = Color.red;
    public Color selectedColor = Color.yellow; 
    
    // Internal State
    private bool isDeploymentPhase = false;
    private Queue<UnitData> unitsToPlace = new Queue<UnitData>();
    private List<UnitData> enemyUnitsToSpawn = new List<UnitData>();
    
    private GameObject selectedUnitToMove = null; 
    private GridCell lastHoveredCell = null;
    private Material originalMaterialAsset; 

    private void Start()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (battleManager == null) battleManager = FindFirstObjectByType<BattleManager>();
        if (globalUI == null) globalUI = FindFirstObjectByType<GlobalUIManager>();
        
        if (finishDeploymentButton != null) finishDeploymentButton.SetActive(false);
        if (endTurnButton != null) endTurnButton.SetActive(false);
    }

    public void StartManualDeployment(List<UnitData> playerUnits, List<UnitData> enemyUnits)
    {
        unitsToPlace.Clear();
        foreach (var u in playerUnits) unitsToPlace.Enqueue(u);
        enemyUnitsToSpawn = enemyUnits;

        isDeploymentPhase = true;
        if (battleManager) battleManager.isBattleActive = false;
        if (endTurnButton != null) endTurnButton.SetActive(false);
    }

    private void Update()
    {
        if (!isDeploymentPhase) return;
        bool deploymentComplete = (unitsToPlace.Count == 0 && selectedUnitToMove == null);
        
        if (finishDeploymentButton != null)
        {
            if (deploymentComplete && !finishDeploymentButton.activeSelf)
                finishDeploymentButton.SetActive(true);
            else if (!deploymentComplete && finishDeploymentButton.activeSelf)
                finishDeploymentButton.SetActive(false);
        }

        HandleMouseInteraction();
    }

    void HandleMouseInteraction()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell == null && hit.collider.CompareTag("Unit"))
            {
                 if (gridManager != null)
                 {
                     Vector2Int pos = gridManager.WorldToGridPosition(hit.collider.transform.position);
                     cell = gridManager.GetCell(pos.x, pos.y);
                 }
            }

            if (cell != null)
            {
                HighlightCell(cell);
                if (Input.GetMouseButtonDown(0)) HandleLeftClick(cell);
                if (Input.GetMouseButtonDown(1)) HandleRightClick();
            }
        }
    }

    void HandleLeftClick(GridCell cell)
    {
        if (!IsValidPlacement(cell)) return;
        if (selectedUnitToMove != null)
        {
            if (!cell.isOccupied)
            {
                Vector2Int oldPos = gridManager.WorldToGridPosition(selectedUnitToMove.transform.position);
                GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);
                
                if (oldCell) oldCell.RemoveUnit();

                selectedUnitToMove.transform.position = cell.GetWorldPosition();
                cell.PlaceUnit(selectedUnitToMove);
                
                selectedUnitToMove = null; 
                return;
            }
        }
        if (cell.isOccupied && cell.occupyingUnit != null)
        {
            string uName = cell.occupyingUnit.name;
            if (uName.Contains("Player") || uName.Contains("Captain"))
            {
                selectedUnitToMove = cell.occupyingUnit;
                Debug.Log($"Selected {selectedUnitToMove.name} to move.");
                return;
            }
        }
        if (unitsToPlace.Count > 0 && !cell.isOccupied)
        {
            UnitData nextUnit = unitsToPlace.Dequeue();
            SpawnSpecificUnit(cell, nextUnit);
        }
    }

    void HandleRightClick()
    {
        if (selectedUnitToMove != null)
        {
            selectedUnitToMove = null;
            Debug.Log("Cancelled Move.");
        }
    }

    void SpawnSpecificUnit(GridCell cell, UnitData data)
    {
        GameObject prefabToUse = defaultUnitPrefab;
        foreach (var rp in rolePrefabs)
        {
            if (rp.roleName == data.role)
            {
                prefabToUse = data.isPlayer ? rp.playerPrefab : rp.enemyPrefab;
                break;
            }
        }
        if (prefabToUse == null) prefabToUse = defaultUnitPrefab;

        GameObject newUnit = Instantiate(prefabToUse, cell.GetWorldPosition(), Quaternion.identity);
        newUnit.name = data.unitName;
        newUnit.tag = "Unit"; 

        UnitStatus status = newUnit.GetComponent<UnitStatus>();
        if (status) status.Initialize(data);

        UnitAttack attack = newUnit.GetComponent<UnitAttack>();
        if (attack) attack.SetupManagers(gridManager, energyManager);

        cell.PlaceUnit(newUnit);
    }

    public void FinishDeploymentAndStartBattle()
    {
        if (unitsToPlace.Count > 0) return;
        int eRow = 0;
        int eCol = 7; 
        foreach (var data in enemyUnitsToSpawn)
        {
            GridCell cell = null;
            if (gridManager.GetCell(eCol, eRow) != null && !gridManager.GetCell(eCol, eRow).isOccupied)
                cell = gridManager.GetCell(eCol, eRow);
            else if (gridManager.GetCell(eCol - 1, eRow) != null)
                cell = gridManager.GetCell(eCol - 1, eRow);
            
            if (cell != null) SpawnSpecificUnit(cell, data);
            
            eRow++;
            if (eRow > 7) { eRow = 0; eCol--; }
        }
        isDeploymentPhase = false;
        selectedUnitToMove = null;
        ResetLastHighlight();
        
        if (battleManager) battleManager.isBattleActive = true; 
        if (turnManager) turnManager.StartGameLoop();
        if (hazardManager) hazardManager.GenerateRandomHazards();
        if (globalUI) globalUI.GenerateUnitIcons(); 
        
        if (finishDeploymentButton) finishDeploymentButton.SetActive(false); 
        if (endTurnButton) endTurnButton.SetActive(true);
        
        gameObject.SetActive(false); 
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
                renderer.material.color = (IsValidPlacement(cell) && (unitsToPlace.Count > 0 || cell.isOccupied)) ? validHoverColor : invalidHoverColor;
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
        return !cell.isBlocked && cell.isPlayerSide; 
    }
}