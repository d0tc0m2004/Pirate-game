using UnityEngine;
using TacticalGame.Core;

namespace TacticalGame.Grid
{
    /// <summary>
    /// Manages the battle grid creation and access.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Grid Size Settings")]
        [SerializeField] private int gridWidth = 13;
        [SerializeField] private int gridHeight = 7;

        [Header("Grid Size Limits")]
        [SerializeField] private int minWidth = 7;
        [SerializeField] private int maxWidth = 15;
        [SerializeField] private int minHeight = 3;
        [SerializeField] private int maxHeight = 9;

        [Header("Cell Settings")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] [Range(0.5f, 1f)] private float cellScaleFactor = 0.95f;

        [Header("Prefab")]
        [SerializeField] private GameObject cellPrefab;

        [Header("Materials")]
        [SerializeField] private Material playerCellMaterial;
        [SerializeField] private Material enemyCellMaterial;
        [SerializeField] private Material middleCellMaterial;
        [SerializeField] private Material basePlaneMaterial;

        #endregion

        #region Private State

        private GridCell[,] gridCells;
        private int middleColumnIndex;
        private Transform gridParent;
        private GameObject basePlane;

        #endregion

        #region Public Properties

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GridManager>();
        }

        private void Start()
        {
            GameObject gridObject = new GameObject("Grid");
            gridParent = gridObject.transform;

            GenerateGrid();
        }

        #endregion

        #region Grid Generation

        /// <summary>
        /// Generate a new random grid.
        /// </summary>
        public void GenerateGrid()
        {
            ClearGrid();

            // Random dimensions
            gridWidth = Random.Range(minWidth, maxWidth + 1);
            gridHeight = Random.Range(minHeight, maxHeight + 1);

            // Ensure odd width for middle column
            if (gridWidth % 2 == 0)
            {
                gridWidth++;
                if (gridWidth > maxWidth)
                {
                    gridWidth -= 2;
                }
            }

            middleColumnIndex = gridWidth / 2;
            gridCells = new GridCell[gridWidth, gridHeight];

            CreateBasePlane();

            float totalWidth = gridWidth * cellSize;
            float totalHeight = gridHeight * cellSize;
            float offsetX = -totalWidth / 2f + cellSize / 2f;
            float offsetZ = -totalHeight / 2f + cellSize / 2f;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    CreateCell(x, y, offsetX, offsetZ);
                }
            }

            Debug.Log($"Generated Random Grid: {gridWidth}x{gridHeight}, Middle Column Index: {middleColumnIndex}");
        }

        private void CreateBasePlane()
        {
            basePlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlane.name = "BasePlane";
            basePlane.transform.parent = gridParent;

            float planeWidth = gridWidth * cellSize + 1f;
            float planeHeight = gridHeight * cellSize + 1f;
            float planeThickness = 0.3f;

            basePlane.transform.localScale = new Vector3(planeWidth, planeThickness, planeHeight);
            basePlane.transform.position = new Vector3(0, -planeThickness / 2f - 0.01f, 0);

            Renderer planeRenderer = basePlane.GetComponent<Renderer>();
            if (planeRenderer != null && basePlaneMaterial != null)
            {
                planeRenderer.material = basePlaneMaterial;
            }
        }

        private void CreateCell(int x, int y, float offsetX, float offsetZ)
        {
            float worldX = x * cellSize + offsetX;
            float worldZ = y * cellSize + offsetZ;
            float worldY = 0.01f;
            Vector3 position = new Vector3(worldX, worldY, worldZ);

            GameObject cellObject = Instantiate(cellPrefab, position, Quaternion.Euler(90, 0, 0), gridParent);
            cellObject.name = $"Cell_{x}_{y}";

            float scaledSize = cellSize * cellScaleFactor;
            cellObject.transform.localScale = new Vector3(scaledSize, scaledSize, 1);

            GridCell cell = cellObject.GetComponent<GridCell>();
            if (cell != null)
            {
                cell.InitializeCell(x, y, middleColumnIndex);
                gridCells[x, y] = cell;

                // Assign material based on side
                if (x == middleColumnIndex)
                {
                    cell.SetMaterial(middleCellMaterial);
                }
                else if (x < middleColumnIndex)
                {
                    cell.SetMaterial(playerCellMaterial);
                }
                else
                {
                    cell.SetMaterial(enemyCellMaterial);
                }
            }
        }

        /// <summary>
        /// Clear the entire grid.
        /// </summary>
        public void ClearGrid()
        {
            if (gridParent != null)
            {
                foreach (Transform child in gridParent)
                {
                    Destroy(child.gameObject);
                }
            }

            gridCells = null;
            basePlane = null;
        }

        #endregion

        #region Cell Access

        /// <summary>
        /// Get a cell at grid coordinates.
        /// </summary>
        public GridCell GetCell(int x, int y)
        {
            if (IsValidPosition(x, y))
            {
                return gridCells[x, y];
            }
            return null;
        }

        /// <summary>
        /// Check if coordinates are within grid bounds.
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        #endregion

        #region Coordinate Conversion

        /// <summary>
        /// Convert world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            float totalWidth = gridWidth * cellSize;
            float totalHeight = gridHeight * cellSize;
            float offsetX = -totalWidth / 2f + cellSize / 2f;
            float offsetZ = -totalHeight / 2f + cellSize / 2f;

            int x = Mathf.RoundToInt((worldPosition.x - offsetX) / cellSize);
            int y = Mathf.RoundToInt((worldPosition.z - offsetZ) / cellSize);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Convert grid coordinates to world position.
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            float totalWidth = gridWidth * cellSize;
            float totalHeight = gridHeight * cellSize;
            float offsetX = -totalWidth / 2f + cellSize / 2f;
            float offsetZ = -totalHeight / 2f + cellSize / 2f;

            float worldX = x * cellSize + offsetX;
            float worldZ = y * cellSize + offsetZ;

            return new Vector3(worldX, 0.01f, worldZ);
        }

        #endregion

        #region Side Checks

        /// <summary>
        /// Check if x coordinate is on the player side.
        /// </summary>
        public bool IsPlayerSide(int x)
        {
            return x < middleColumnIndex;
        }

        /// <summary>
        /// Check if x coordinate is on the enemy side.
        /// </summary>
        public bool IsEnemySide(int x)
        {
            return x > middleColumnIndex;
        }

        /// <summary>
        /// Get the middle column index.
        /// </summary>
        public int GetMiddleColumnIndex()
        {
            return middleColumnIndex;
        }

        #endregion
    }
}