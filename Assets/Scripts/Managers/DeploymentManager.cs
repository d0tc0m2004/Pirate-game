using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TacticalGame.Core;
using TacticalGame.Config;
using TacticalGame.Grid;
using TacticalGame.Units;
using TacticalGame.Hazards;
using TacticalGame.Enums;
using TacticalGame.Combat;
using TacticalGame.Equipment;

namespace TacticalGame.Managers
{
    /// <summary>
    /// Manages the unit deployment phase before battle.
    /// Uses FlexibleUnitEquipment for slot-based relic storage.
    /// </summary>
    public class DeploymentManager : MonoBehaviour
    {
        #region Nested Types

        [System.Serializable]
        public struct RolePrefab
        {
            public UnitRole role;
            public GameObject playerPrefab;
            public GameObject enemyPrefab;
        }

        #endregion

        #region Serialized Fields

        [Header("UI Buttons")]
        [SerializeField] private GameObject finishDeploymentButton;
        [SerializeField] private GameObject endTurnButton;

        [Header("Role Settings")]
        [SerializeField] private List<RolePrefab> rolePrefabs;
        [SerializeField] private GameObject defaultUnitPrefab;

        [Header("Placement Settings")]
        [SerializeField] private Color validHoverColor = Color.green;
        [SerializeField] private Color invalidHoverColor = Color.red;
        [SerializeField] private Color selectedColor = Color.yellow;

        #endregion

        #region Private State

        private GridManager gridManager;
        private TurnManager turnManager;
        private BattleManager battleManager;
        private GlobalUIManager globalUI;
        private EnergyManager energyManager;
        private HazardManager hazardManager;

        private bool isDeploymentPhase = false;
        private Queue<UnitData> unitsToPlace = new Queue<UnitData>();
        private List<UnitData> enemyUnitsToSpawn = new List<UnitData>();

        private GameObject selectedUnitToMove = null;
        private GridCell lastHoveredCell = null;
        private Material originalMaterialAsset;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<DeploymentManager>();
        }

        private void Start()
        {
            CacheReferences();

            if (finishDeploymentButton != null) finishDeploymentButton.SetActive(false);
            if (endTurnButton != null) endTurnButton.SetActive(false);
        }

        private void Update()
        {
            if (!isDeploymentPhase) return;

            UpdateDeploymentButtonVisibility();
            HandleMouseInteraction();
        }

        #endregion

        #region Initialization

        private void CacheReferences()
        {
            gridManager = ServiceLocator.Get<GridManager>();
            turnManager = ServiceLocator.Get<TurnManager>();
            battleManager = ServiceLocator.Get<BattleManager>();
            globalUI = ServiceLocator.Get<GlobalUIManager>();
            energyManager = ServiceLocator.Get<EnergyManager>();
            hazardManager = ServiceLocator.Get<HazardManager>();
        }

        #endregion

        #region Deployment Flow

        /// <summary>
        /// Start the manual deployment phase.
        /// </summary>
        public void StartManualDeployment(List<UnitData> playerUnits, List<UnitData> enemyUnits)
        {
            unitsToPlace.Clear();
            foreach (var unit in playerUnits)
            {
                unitsToPlace.Enqueue(unit);
            }
            
            enemyUnitsToSpawn = enemyUnits;
            isDeploymentPhase = true;

            if (battleManager != null) battleManager.IsBattleActive = false;
            if (endTurnButton != null) endTurnButton.SetActive(false);

            GameEvents.TriggerDeploymentStart();
        }

        /// <summary>
        /// Finish deployment and start the battle.
        /// </summary>
        public void FinishDeploymentAndStartBattle()
        {
            if (unitsToPlace.Count > 0) return;

            SpawnEnemyUnits();

            isDeploymentPhase = false;
            selectedUnitToMove = null;
            ResetLastHighlight();

            if (battleManager != null) battleManager.IsBattleActive = true;
            if (turnManager != null) turnManager.StartGameLoop();
            if (hazardManager != null) hazardManager.GenerateRandomHazards();
            if (globalUI != null) globalUI.GenerateUnitIcons();

            if (finishDeploymentButton != null) finishDeploymentButton.SetActive(false);
            if (endTurnButton != null) endTurnButton.SetActive(true);

            GameEvents.TriggerDeploymentEnd();
            
            gameObject.SetActive(false);
        }

        #endregion

        #region Mouse Interaction

        private void HandleMouseInteraction()
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridCell cell = GetCellFromHit(hit);

                if (cell != null)
                {
                    HighlightCell(cell);
                    
                    if (Input.GetMouseButtonDown(0)) HandleLeftClick(cell);
                    if (Input.GetMouseButtonDown(1)) HandleRightClick();
                }
            }
        }

        private GridCell GetCellFromHit(RaycastHit hit)
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();

            if (cell == null && hit.collider.CompareTag("Unit"))
            {
                Vector2Int pos = gridManager.WorldToGridPosition(hit.collider.transform.position);
                cell = gridManager.GetCell(pos.x, pos.y);
            }

            return cell;
        }

        private void HandleLeftClick(GridCell cell)
        {
            if (!IsValidPlacement(cell)) return;

            // Moving a selected unit
            if (selectedUnitToMove != null)
            {
                if (!cell.IsOccupied)
                {
                    MoveSelectedUnit(cell);
                }
                return;
            }

            // Selecting an existing unit
            if (cell.IsOccupied && cell.OccupyingUnit != null)
            {
                TrySelectUnit(cell.OccupyingUnit);
                return;
            }

            // Placing a new unit
            if (unitsToPlace.Count > 0 && !cell.IsOccupied)
            {
                UnitData nextUnit = unitsToPlace.Dequeue();
                SpawnUnit(cell, nextUnit);
            }
        }

        private void HandleRightClick()
        {
            selectedUnitToMove = null;
        }

        #endregion

        #region Unit Placement

        private void TrySelectUnit(GameObject unit)
        {
            UnitStatus status = unit.GetComponent<UnitStatus>();
            if (status != null && status.Team == Team.Player)
            {
                selectedUnitToMove = unit;
            }
        }

        private void MoveSelectedUnit(GridCell targetCell)
        {
            Vector2Int oldPos = gridManager.WorldToGridPosition(selectedUnitToMove.transform.position);
            GridCell oldCell = gridManager.GetCell(oldPos.x, oldPos.y);

            if (oldCell != null) oldCell.RemoveUnit();

            selectedUnitToMove.transform.position = targetCell.GetWorldPosition();
            targetCell.PlaceUnit(selectedUnitToMove);

            selectedUnitToMove = null;
        }

        private void SpawnUnit(GridCell cell, UnitData data)
        {
            GameObject prefab = GetPrefabForRole(data.role, data.team);
            
            GameObject newUnit = Instantiate(prefab, cell.GetWorldPosition(), Quaternion.identity);
            newUnit.name = data.unitName;
            newUnit.tag = "Unit";

            // Initialize UnitStatus
            UnitStatus status = newUnit.GetComponent<UnitStatus>();
            if (status != null)
            {
                status.Initialize(data);
            }

            // Initialize UnitAttack with weapon relic
            UnitAttack attack = newUnit.GetComponent<UnitAttack>();
            if (attack != null)
            {
                attack.SetupManagers(gridManager, energyManager);
                
                // Set the primary weapon relic from UnitData
                WeaponRelic primaryWeapon = data.GetWeaponRelic(0) ?? data.defaultWeaponRelic;
                if (primaryWeapon != null)
                {
                    attack.SetWeaponRelic(primaryWeapon);
                }
            }

            // Add StatusEffectManager if not present
            if (newUnit.GetComponent<StatusEffectManager>() == null)
            {
                newUnit.AddComponent<StatusEffectManager>();
            }

            // Setup equipment using the FLEXIBLE slot-based system
            SetupUnitEquipment(newUnit, data);
            
            // Setup card deck
            SetupCardDeck(newUnit);

            cell.PlaceUnit(newUnit);
            
            Debug.Log($"<color=green>Spawned {data.unitName}</color>");
        }

        /// <summary>
        /// Setup the runtime equipment component with relics from UnitData.
        /// Uses FlexibleUnitEquipment for SLOT-BASED storage (not category-based).
        /// This preserves whatever the player equipped in whatever slot they chose!
        /// </summary>
        private void SetupUnitEquipment(GameObject unitObj, UnitData data)
        {
            Debug.Log($"<color=yellow>[Deployment] Setting up equipment for {data.unitName}</color>");

            // Use FlexibleUnitEquipment for slot-based storage
            FlexibleUnitEquipment equipment = unitObj.GetComponent<FlexibleUnitEquipment>();
            if (equipment == null)
            {
                equipment = unitObj.AddComponent<FlexibleUnitEquipment>();
            }

            // Initialize (this auto-assigns Ultimate and PassiveUnique based on role)
            equipment.Initialize(data.role, data.weaponFamily);

            // Transfer relics from UnitData BY SLOT INDEX
            // This preserves whatever the player equipped to each slot!
            for (int i = 0; i < 5; i++) // Slots 0-4 are flexible
            {
                // Check for weapon in this slot
                WeaponRelic weapon = data.GetWeaponRelic(i);
                if (weapon != null)
                {
                    equipment.EquipWeapon(i, weapon);
                    Debug.Log($"<color=cyan>  Slot {i}: Weapon - {weapon.relicName}</color>");
                    continue; // Slot filled, move to next
                }

                // Check for category relic in this slot
                EquippedRelic categoryRelic = data.GetCategoryRelic(i);
                if (categoryRelic != null)
                {
                    equipment.EquipCategory(i, categoryRelic);
                    Debug.Log($"<color=cyan>  Slot {i}: {categoryRelic.category} - {categoryRelic.relicName}</color>");
                }
            }

            // Log final equipment state
            equipment.LogEquipmentState();
        }

        /// <summary>
        /// Setup the card deck manager for battle.
        /// </summary>
        private void SetupCardDeck(GameObject unitObj)
        {
            CardDeckManager deck = unitObj.GetComponent<CardDeckManager>();
            if (deck == null)
            {
                deck = unitObj.AddComponent<CardDeckManager>();
            }
            
            // Deck will build itself in Start() using FlexibleUnitEquipment
        }

        private GameObject GetPrefabForRole(UnitRole role, Team team)
        {
            foreach (var rp in rolePrefabs)
            {
                if (rp.role == role)
                {
                    return team == Team.Player ? rp.playerPrefab : rp.enemyPrefab;
                }
            }
            return defaultUnitPrefab;
        }

        #endregion

        #region Enemy Spawning

        private void SpawnEnemyUnits()
        {
            List<GridCell> validSpots = GetValidEnemySpawnCells();

            foreach (var data in enemyUnitsToSpawn)
            {
                if (validSpots.Count == 0)
                {
                    Debug.LogWarning("Enemy Zone is full!");
                    break;
                }

                int randomIndex = Random.Range(0, validSpots.Count);
                GridCell chosenCell = validSpots[randomIndex];

                SpawnUnit(chosenCell, data);
                validSpots.RemoveAt(randomIndex);
            }
        }

        private List<GridCell> GetValidEnemySpawnCells()
        {
            List<GridCell> validCells = new List<GridCell>();

            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                for (int y = 0; y < gridManager.GridHeight; y++)
                {
                    GridCell cell = gridManager.GetCell(x, y);
                    if (cell != null && 
                        gridManager.IsEnemySide(x) && 
                        cell.CanPlaceUnit())
                    {
                        validCells.Add(cell);
                    }
                }
            }

            return validCells;
        }

        #endregion

        #region Highlighting

        private void HighlightCell(GridCell cell)
        {
            if (lastHoveredCell != cell)
            {
                ResetLastHighlight();
            }
            
            lastHoveredCell = cell;

            MeshRenderer renderer = cell.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (originalMaterialAsset == null)
            {
                originalMaterialAsset = renderer.sharedMaterial;
            }

            // Determine highlight color
            Color highlightColor;
            if (selectedUnitToMove != null)
            {
                highlightColor = IsValidPlacement(cell) ? selectedColor : invalidHoverColor;
            }
            else
            {
                bool canPlace = IsValidPlacement(cell) && (unitsToPlace.Count > 0 || cell.IsOccupied);
                highlightColor = canPlace ? validHoverColor : invalidHoverColor;
            }

            renderer.material.color = highlightColor;
        }

        private void ResetLastHighlight()
        {
            if (lastHoveredCell != null && originalMaterialAsset != null)
            {
                lastHoveredCell.GetComponent<MeshRenderer>().material = originalMaterialAsset;
                originalMaterialAsset = null;
            }
            lastHoveredCell = null;
        }

        #endregion

        #region Validation

        private bool IsValidPlacement(GridCell cell)
        {
            return !cell.IsBlocked && gridManager.IsPlayerSide(cell.XPosition);
        }

        private void UpdateDeploymentButtonVisibility()
        {
            bool deploymentComplete = (unitsToPlace.Count == 0 && selectedUnitToMove == null);

            if (finishDeploymentButton != null)
            {
                if (deploymentComplete != finishDeploymentButton.activeSelf)
                {
                    finishDeploymentButton.SetActive(deploymentComplete);
                }
            }
        }

        #endregion
    }
}