using UnityEngine;
using System.Collections;

public class UnitMovement : MonoBehaviour
{
    [Header("Stats")]
    public int moveRange = 3;
    public float moveAnimationSpeed = 5f;

    [Header("State")]
    public bool isMoving = false;
    
    
    public void MoveToCell(GridCell targetCell)
    {
        if (isMoving) return;
        
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
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 2f))
        {
            GridCell cell = hit.collider.GetComponent<GridCell>();
            if (cell != null && cell.hasHazard && cell.hazardVisualObject != null)
            {
                HazardInstance hazard = cell.hazardVisualObject.GetComponent<HazardInstance>();
                if (hazard != null)
                {
                    hazard.OnUnitEnter(this.gameObject);
                }
            }
        }
    }
}