using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Size Settings")]
    public int gridWidth = 13;
    public int gridHeight = 7;
    
    [Header("Grid Size Limits")]
    public int minWidth = 7;
    public int maxWidth = 15;
    public int minHeight = 3;
    public int maxHeight = 9;
    
    [Header("Cell Settings")]
    public float cellSize = 1f;
    [Range(0.5f, 1f)]
    public float cellScaleFactor = 0.95f;
    
    [Header("Prefab")]
    public GameObject cellPrefab;
    
    [Header("Materials - Drag Your Materials Here")]
    public Material playerCellMaterial;
    public Material enemyCellMaterial;
    public Material middleCellMaterial;
    public Material basePlaneMaterial;
    
    [Header("Grid Data")]
    public GridCell[,] gridCells;
    
    private int middleColumnIndex;
    private Transform gridParent;
    private GameObject basePlane;
    
    private void Start()
    {
        GameObject gridObject = new GameObject("Grid");
        gridParent = gridObject.transform;
        
        GenerateGrid();
    }
    
    public void GenerateGrid()
    {
        ClearGrid();

        gridWidth = Random.Range(minWidth, maxWidth + 1); 
        gridHeight = Random.Range(minHeight, maxHeight + 1);

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
    
    public GridCell GetCell(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
        {
            return gridCells[x, y];
        }
        return null;
    }
    
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
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
    
    public bool IsPlayerSide(int x)
    {
        return x < middleColumnIndex;
    }
    
    public bool IsEnemySide(int x)
    {
        return x > middleColumnIndex;
    }
    
    public int GetMiddleColumnIndex()
    {
        return middleColumnIndex;
    }
}