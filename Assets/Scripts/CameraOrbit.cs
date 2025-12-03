using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Control Settings")]
    public float mouseSensitivity = 3.0f;
    public float panSpeed = 0.5f;
    public float zoomSpeed = 2.0f;
    public float minZoomDistance = 5.0f;
    public float maxZoomDistance = 30.0f;

    [Header("Angle Limits")]
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 85f;

    private float currentYaw = 0f;
    private float currentPitch = 45f;
    private float currentDistance = 10f;
    private Vector3 targetPosition;

    private void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        Invoke("CenterOnGrid", 0.1f);
    }

    void CenterOnGrid()
    {
        if (gridManager == null) return;

        targetPosition = Vector3.zero;

        float totalWidth = gridManager.gridWidth * gridManager.cellSize;
        currentDistance = totalWidth * 0.8f;

        currentYaw = -45f;
        currentPitch = 45f;
    }

    private void LateUpdate()
    {
        HandleInput();
        UpdateCameraTransform();
    }

    void HandleInput()
    {
        float scrollInput = Input.mouseScrollDelta.y;
        if (scrollInput != 0)
        {
            currentDistance -= scrollInput * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }

        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 cameraRight = transform.right;
            Vector3 cameraForward = transform.forward;

            cameraRight.y = 0;
            cameraForward.y = 0;
            
            cameraRight.Normalize();
            cameraForward.Normalize();

            float panSpeedAdjusted = panSpeed * (currentDistance / 10f);

            Vector3 moveVector = (cameraRight * mouseX) + (cameraForward * mouseY);
            targetPosition -= moveVector * panSpeedAdjusted;
        }
    }

    void UpdateCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 position = targetPosition + (rotation * Vector3.back * currentDistance);

        transform.position = position;
        transform.rotation = rotation;
    }
}