using UnityEngine;

public class GridCell : MonoBehaviour
{
    [Header("Cell Position")]
    public int xPosition;
    public int yPosition;

    [Header("Cell State")]
    public bool isOccupied = false;
    public bool isBlocked = false;
    public bool isPlayerSide = false;
    public bool isMiddleColumn = false;

    public string currentHazardName = "None";
    public bool hasHazard = false;
    public GameObject hazardVisualObject;

    [Header("References")]
    public GameObject occupyingUnit = null;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

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

    public void SetMaterial(Material material)
    {
        if (meshRenderer != null && material != null)
        {
            meshRenderer.material = material;
        }
    }

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

    public void RemoveUnit()
    {
        occupyingUnit = null;
        isOccupied = false;
    }
    public void ApplyHazard(GameObject hazardPrefab, bool isBlocking)
    {
        if (hasHazard || isMiddleColumn) return;

        hasHazard = true;
        currentHazardName = hazardPrefab.name;

        if (isBlocking)
        {
            isBlocked = true;
        }

        Vector3 spawnPos = GetWorldPosition();
        hazardVisualObject = Instantiate(hazardPrefab, spawnPos, Quaternion.identity);
        hazardVisualObject.transform.SetParent(this.transform);
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position + Vector3.up * 0.5f;
    }
}