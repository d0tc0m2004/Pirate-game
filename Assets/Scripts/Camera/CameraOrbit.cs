using UnityEngine;
using TacticalGame.Core;
using TacticalGame.Grid;

namespace TacticalGame.Camera
{
    /// <summary>
    /// Orbital camera controller with pan, zoom, and rotation.
    /// </summary>
    public class CameraOrbit : MonoBehaviour
    {
        [Header("Control Settings")]
        [SerializeField] private float mouseSensitivity = 3.0f;
        [SerializeField] private float panSpeed = 0.5f;
        [SerializeField] private float zoomSpeed = 2.0f;
        [SerializeField] private float minZoomDistance = 5.0f;
        [SerializeField] private float maxZoomDistance = 30.0f;

        [Header("Angle Limits")]
        [SerializeField] private float minVerticalAngle = 10f;
        [SerializeField] private float maxVerticalAngle = 85f;

        private float currentYaw = 0f;
        private float currentPitch = 45f;
        private float currentDistance = 10f;
        private Vector3 targetPosition;
        private GridManager gridManager;

        private void Start()
        {
            gridManager = ServiceLocator.Get<GridManager>();
            Invoke(nameof(CenterOnGrid), 0.1f);
        }

        private void CenterOnGrid()
        {
            if (gridManager == null)
                gridManager = ServiceLocator.Get<GridManager>();
            
            if (gridManager == null) return;

            targetPosition = Vector3.zero;
            currentDistance = gridManager.GridWidth * gridManager.CellSize * 0.8f;
            currentYaw = -45f;
            currentPitch = 45f;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            // Zoom
            float scrollInput = Input.mouseScrollDelta.y;
            if (scrollInput != 0)
            {
                currentDistance -= scrollInput * zoomSpeed;
                currentDistance = Mathf.Clamp(currentDistance, minZoomDistance, maxZoomDistance);
            }

            // Rotation (right mouse)
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                currentYaw += mouseX;
                currentPitch -= mouseY;
                currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
            }

            // Pan (middle mouse)
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

                float adjustedPanSpeed = panSpeed * (currentDistance / 10f);
                Vector3 moveVector = (cameraRight * mouseX) + (cameraForward * mouseY);
                targetPosition -= moveVector * adjustedPanSpeed;
            }
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 position = targetPosition + (rotation * Vector3.back * currentDistance);

            transform.position = position;
            transform.rotation = rotation;
        }
    }
}