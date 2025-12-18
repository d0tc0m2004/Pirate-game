using UnityEngine;

namespace TacticalGame.Grid
{
    /// <summary>
    /// Represents a single cell in the battle grid.
    /// </summary>
    public class GridCell : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Cell Position")]
        [SerializeField] private int xPosition;
        [SerializeField] private int yPosition;

        [Header("Cell State")]
        [SerializeField] private bool isOccupied = false;
        [SerializeField] private bool isBlocked = false;
        [SerializeField] private bool isPlayerSide = false;
        [SerializeField] private bool isMiddleColumn = false;

        [Header("Hazard State")]
        [SerializeField] private string currentHazardName = "None";
        [SerializeField] private bool hasHazard = false;
        [SerializeField] private GameObject hazardVisualObject;

        [Header("References")]
        [SerializeField] private GameObject occupyingUnit = null;

        #endregion

        #region Private State

        private MeshRenderer meshRenderer;

        #endregion

        #region Public Properties

        public int XPosition => xPosition;
        public int YPosition => yPosition;
        public bool IsOccupied => isOccupied;
        public bool IsBlocked => isBlocked;
        public bool IsPlayerSide => isPlayerSide;
        public bool IsMiddleColumn => isMiddleColumn;
        public string CurrentHazardName => currentHazardName;
        public bool HasHazard => hasHazard;
        public GameObject HazardVisualObject => hazardVisualObject;
        public GameObject OccupyingUnit => occupyingUnit;

        // Property setters for external access
        public bool hasHazardState { get => hasHazard; set => hasHazard = value; }
        public bool isBlockedState { get => isBlocked; set => isBlocked = value; }
        public bool isOccupiedState { get => isOccupied; set => isOccupied = value; }
        public GameObject hazardVisualObjectRef { get => hazardVisualObject; set => hazardVisualObject = value; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the cell with grid coordinates.
        /// </summary>
        public void InitializeCell(int x, int y, int middleColumnIndex)
        {
            xPosition = x;
            yPosition = y;

            if (x == middleColumnIndex)
            {
                isMiddleColumn = true;
                isBlocked = true;
            }
            else if (x < middleColumnIndex)
            {
                isPlayerSide = true;
            }
            else
            {
                isPlayerSide = false;
            }
        }

        /// <summary>
        /// Set the cell's material.
        /// </summary>
        public void SetMaterial(Material material)
        {
            if (meshRenderer != null && material != null)
            {
                meshRenderer.material = material;
            }
        }

        #endregion

        #region Unit Placement

        /// <summary>
        /// Place a unit on this cell.
        /// </summary>
        /// <returns>True if placement succeeded.</returns>
        public bool PlaceUnit(GameObject unit)
        {
            if (isOccupied || isBlocked || isMiddleColumn)
            {
                return false;
            }

            occupyingUnit = unit;
            isOccupied = true;
            unit.transform.position = GetWorldPosition();

            return true;
        }

        /// <summary>
        /// Remove the unit from this cell.
        /// </summary>
        public void RemoveUnit()
        {
            occupyingUnit = null;
            isOccupied = false;
        }

        #endregion

        #region Hazard Management

        /// <summary>
        /// Apply a hazard to this cell.
        /// </summary>
        public void ApplyHazard(GameObject hazardPrefab, bool isHazardBlocking)
        {
            if (hasHazard || isMiddleColumn) return;

            hasHazard = true;
            currentHazardName = hazardPrefab.name;

            if (isHazardBlocking)
            {
                isBlocked = true;
            }

            Vector3 spawnPos = GetWorldPosition();
            hazardVisualObject = Instantiate(hazardPrefab, spawnPos, Quaternion.identity);
            hazardVisualObject.transform.SetParent(transform);
        }

        /// <summary>
        /// Clear hazard from this cell.
        /// </summary>
        public void ClearHazard()
        {
            if (hazardVisualObject != null)
            {
                Destroy(hazardVisualObject);
            }
            
            hasHazard = false;
            isBlocked = false;
            hazardVisualObject = null;
            currentHazardName = "None";
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get the world position of this cell (with vertical offset for units).
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position + Vector3.up * 0.5f;
        }

        /// <summary>
        /// Check if a unit can be placed here.
        /// </summary>
        public bool CanPlaceUnit()
        {
            return !isOccupied && !isBlocked && !isMiddleColumn;
        }

        /// <summary>
        /// Check if movement can pass through this cell.
        /// </summary>
        public bool IsPassable()
        {
            return !isBlocked && !isOccupied;
        }

        #endregion
    }
}