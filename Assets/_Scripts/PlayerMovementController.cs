using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float rotationSpeed = 40f;

        [Header("Roll Settings")]
        public float rollForce = 12f;
        public float rollDuration = 0.35f;
        public float rollCooldown = 0.8f;
        
        [Header("Mouse Look Settings")] 
        [SerializeField] private float mouseSensitivity = 2f;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector3 _moveDirection;

        private bool _isRolling = false;
        private bool _jumpQueued;
        private float _rollTimer = 0f;
        private float _rollCooldownTimer = 0f;
        private Vector3 _rollDirection;
        private float _yaw;

        private Rigidbody _rb;
        private Animator _animator;
        private PlayerAnimationController _animationController;

        private const float MoveDeadzoneSqr = 0.01f;
        
        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();

            // Prevent Rigidbody from tipping over
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (context.canceled || _moveInput.sqrMagnitude < MoveDeadzoneSqr)
                _moveInput = Vector2.zero;
        }

        public void OnRoll(InputValue value)
        {
            if (value.isPressed && !_isRolling && _rollCooldownTimer <= 0f)
                StartRoll();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();

            if (context.canceled)
                _lookInput = Vector2.zero;
        }
        
        public void OnAttack(InputValue value)
        {
            if (value.isPressed && !_isRolling)
            {
                Debug.Log("Attack Pressed");
                if (_animationController != null)
                    _animationController.PlayAttack();
                else if (_animator != null)
                    _animator.SetTrigger("Attack");
            }
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

        void FixedUpdate()
        {
            if (_isRolling)
            {
                ApplyRoll();
                return;
            }

            MovePlayerUpdate();
            RotatePlayerUpdate();
            JumpUpdate();
            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= Time.deltaTime;
        }
        
        void JumpUpdate()
        {
            if (_jumpQueued)
            {
                _rb.AddForce(Vector3.up * 7f, ForceMode.Impulse);
                _jumpQueued = false;
            }
        }

        void MovePlayerUpdate()
        {
            if (_moveInput.sqrMagnitude < MoveDeadzoneSqr)
            {
                // stop horizontal movement but preserve vertical velocity
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                _moveDirection = Vector3.zero;
            }
            else
            {
                Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y);
                if (move.sqrMagnitude > 1f) move.Normalize();

                Vector3 worldMove = transform.TransformDirection(move);
                _moveDirection = worldMove; // keep for roll direction

                Vector3 desiredVelocity = new Vector3(worldMove.x * moveSpeed, _rb.linearVelocity.y, worldMove.z * moveSpeed);
                _rb.linearVelocity = desiredVelocity;
            }
        }

        private void RotatePlayerUpdate()
        {
            _yaw += _lookInput.x * mouseSensitivity;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _lookInput = Vector2.zero; // Reset after applying
        }

        // ROLL SYSTEM
        void StartRoll()
        {
            _rollDirection = _moveDirection.magnitude > 0.1f ? _moveDirection.normalized : transform.forward;

            _isRolling = true;
            _rollTimer = rollDuration;
            _rollCooldownTimer = rollCooldown;

            _animator.SetTrigger("Roll");
        }

        void ApplyRoll()
        {
            _rollTimer -= Time.fixedDeltaTime;

            _rb.linearVelocity = _rollDirection * rollForce;

            if (_rollTimer <= 0f)
                _isRolling = false;
        }
    }
}
