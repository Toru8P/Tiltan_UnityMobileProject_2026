using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.MainGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Camera Settings")] [SerializeField]
        private GameObject playerCamera;

        [SerializeField] private float cameraDistance = 5.0f;
        [SerializeField] private float cameraHeight = 2.0f;

        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 6f;

        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpForce = 6f;

        [Header("Mouse Look Settings")] [SerializeField]
        private float mouseSensitivity = 2f;

        private Rigidbody _rb;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _yaw;
        private bool _jumpQueued;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            _yaw += _lookInput.x * mouseSensitivity;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            if (playerCamera)
            {
                Vector3 targetPosition = transform.position - (transform.forward * cameraDistance) +
                                         (transform.up * cameraHeight);
                playerCamera.transform.position = targetPosition;
                playerCamera.transform.LookAt(transform.position + Vector3.up);
            }
        }

        private void FixedUpdate()
        {
            Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            Vector3 worldMove = transform.TransformDirection(move);
            Vector3 targetVelocity =
                new Vector3(worldMove.x * moveSpeed, _rb.linearVelocity.y, worldMove.z * moveSpeed);
            _rb.linearVelocity = targetVelocity;

            if (_jumpQueued)
            {
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _jumpQueued = false;
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (context.canceled)
                _moveInput = Vector2.zero;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();

            if (context.canceled)
                _lookInput = Vector2.zero;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed && IsGrounded())
                _jumpQueued = true;
        }
        
        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }
    }
}