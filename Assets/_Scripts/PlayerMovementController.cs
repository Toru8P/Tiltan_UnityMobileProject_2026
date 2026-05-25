using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float rotationSpeed = 240f;

        [Header("Roll Settings")]
        public float rollForce = 12f;
        public float rollDuration = 0.35f;
        public float rollCooldown = 0.8f;

        private Vector2 _moveInput;
        private Vector3 _moveDirection;

        private bool _isRolling = false;
        private float _rollTimer = 0f;
        private float _rollCooldownTimer = 0f;
        private Vector3 _rollDirection;

        private Rigidbody _rb;
        private Animator _animator;
        private PlayerAnimationController _animationController;

        private const float MoveDeadzoneSqr = 0.01f;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();

            // Freeze all rotations so physics doesn't rotate the player.
            // We handle rotation manually in RotatePlayer().
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (context.canceled || _moveInput.sqrMagnitude < MoveDeadzoneSqr)
                _moveInput = Vector2.zero;
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            if (context.performed && !_isRolling && _rollCooldownTimer <= 0f)
                StartRoll();
        }

        public void OnAttack(InputValue value)
        {
            if (value.isPressed && !_isRolling)
            {
                if (_animationController != null)
                    _animationController.PlayAttack();
                else if (_animator != null)
                    _animator.SetTrigger("Attack");
            }
        }

        private void FixedUpdate()
        {
            if (_isRolling)
            {
                ApplyRoll();
                return;
            }

            MovePlayer();
            RotatePlayer();

            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= Time.fixedDeltaTime;
        }

        private void MovePlayer()
        {
            if (_moveInput.sqrMagnitude < MoveDeadzoneSqr)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                _moveDirection = Vector3.zero;
                _animationController?.SetMoving(false);
                return;
            }

            // World-space movement (NOT camera-relative)
            _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

            Vector3 desiredVelocity = new Vector3(
                _moveDirection.x * moveSpeed,
                _rb.linearVelocity.y,
                _moveDirection.z * moveSpeed
            );

            _rb.linearVelocity = desiredVelocity;

            _animationController?.SetMoving(true);
        }

        private void RotatePlayer()
        {
            if (_moveDirection.sqrMagnitude < MoveDeadzoneSqr)
                return;

            Quaternion targetRot = Quaternion.LookRotation(_moveDirection);

            Quaternion newRot = Quaternion.RotateTowards(
                _rb.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );

            _rb.MoveRotation(newRot);
        }

        // ROLL SYSTEM
        private void StartRoll()
        {
            _rollDirection = _moveDirection.sqrMagnitude > 0.1f
                ? _moveDirection.normalized
                : transform.forward;

            _isRolling = true;
            _rollTimer = rollDuration;
            _rollCooldownTimer = rollCooldown;

            _animator?.SetTrigger("Roll");
        }

        private void ApplyRoll()
        {
            _rollTimer -= Time.fixedDeltaTime;

            _rb.linearVelocity = _rollDirection * rollForce;

            if (_rollTimer <= 0f)
                _isRolling = false;
        }
    }
}
