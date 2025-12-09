using UnityEngine;
using System.Collections;

public class UnitMovement : MonoBehaviour
{
    [Header("Stats")]
    public int moveRange = 3; 
    public float moveAnimationSpeed = 5f;

    [Header("State")]
    public bool isMoving = false;
    public bool hasAttacked = false; 

    private Color baseColor = Color.clear; 

    private void Awake()
    {
        CaptureBaseColor();
    }

    void CaptureBaseColor()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && baseColor == Color.clear)
        {
            baseColor = renderer.material.color;
        }
    }

    public void BeginTurn()
    {
        hasAttacked = false;
        
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (baseColor == Color.clear || baseColor.a == 0) baseColor = renderer.material.color;
            renderer.material.color = baseColor;
        }
    }

    public void MoveToCell(GridCell targetCell)
    {
        if (isMoving || hasAttacked) return;
        
        StartCoroutine(AnimateMovement(targetCell.GetWorldPosition()));
    }

    IEnumerator AnimateMovement(Vector3 targetPos)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveAnimationSpeed * Time.deltaTime);
            yield return null; 
        }

        transform.position = targetPos; 
        isMoving = false;
        

        CheckHazardOnTile();
    }

    void CheckHazardOnTile()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up, Vector3.down, 2.0f);
        foreach (RaycastHit hit in hits)
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
            {
                HazardInstance hazard = cell.hazardVisualObject.GetComponent<HazardInstance>();
                if (hazard != null)
                {
                    hazard.OnUnitEnter(this.gameObject);
                    return; 
                }
            }
        }
    }
}