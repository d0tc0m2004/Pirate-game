using UnityEngine;
using System.Collections;

public class UnitMovement : MonoBehaviour
{
    [Header("Stats")]
    public int moveRange = 3; 
    public float moveAnimationSpeed = 5f;

    [Header("State")]
    public bool isMoving = false;
    public bool hasMoved = false;

    public void BeginTurn()
    {
        hasMoved = false;
        
        GetComponent<MeshRenderer>().material.color = Color.blue; 
    }

    public void MoveToCell(GridCell targetCell)
    {
        if (isMoving || hasMoved) return; 
        
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
        hasMoved = true; 

        CheckHazardOnTile();

        GetComponent<MeshRenderer>().material.color = Color.gray;
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
                    Debug.Log($"Stepped on {hazard.name}!");
                    hazard.OnUnitEnter(this.gameObject);
                    return;
                }
            }
        }
    }
}