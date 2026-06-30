using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace _Scripts.CharacterCreation
{
    // Attach to the preview pivot (parent of the character model).
    // Single-finger drag rotates the model on Y; two-finger pinch zooms the preview camera.
    public class CharacterPreviewController : MonoBehaviour
    {
        [Header("Rotation")]
        [SerializeField] private float rotationSensitivity = 0.3f;

        [Header("Zoom")]
        [SerializeField] private Camera previewCamera;
        [SerializeField] private float minFov = 20f;
        [SerializeField] private float maxFov = 60f;
        [SerializeField] private float zoomSensitivity = 0.05f;

        private float _baseFov;
        private float _pinchStartDistance;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            if (previewCamera != null)
                _baseFov = previewCamera.fieldOfView;
        }

        private void Update()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 1)
                HandleRotation(touches[0]);
            else if (touches.Count == 2)
                HandlePinchZoom(touches[0], touches[1]);
        }

        private void HandleRotation(Touch touch)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Moved) return;

            // Scale delta by DPI so gesture speed is consistent across screen densities.
            float dpiScale = Screen.dpi > 0 ? 160f / Screen.dpi : 1f;
            float deltaX = touch.delta.x * dpiScale * rotationSensitivity;
            transform.Rotate(Vector3.up, -deltaX, Space.World);
        }

        private void HandlePinchZoom(Touch t0, Touch t1)
        {
            if (previewCamera == null) return;

            float currentDistance = Vector2.Distance(t0.screenPosition, t1.screenPosition);

            // Record reference distance when the second finger first touches.
            if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                t1.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _pinchStartDistance = currentDistance;
                _baseFov = previewCamera.fieldOfView;
                return;
            }

            if (_pinchStartDistance <= 0f) return;

            float dpiScale = Screen.dpi > 0 ? 160f / Screen.dpi : 1f;
            float pinchDelta = (_pinchStartDistance - currentDistance) * dpiScale * zoomSensitivity;
            previewCamera.fieldOfView = Mathf.Clamp(_baseFov + pinchDelta, minFov, maxFov);
        }
    }
}
