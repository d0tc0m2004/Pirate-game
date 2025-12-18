using UnityEngine;

namespace TacticalGame.UI
{
    /// <summary>
    /// Makes a UI element always face the camera.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private UnityEngine.Camera mainCamera;

        private void Start()
        {
            mainCamera = UnityEngine.Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.forward);
            }
        }
    }
}