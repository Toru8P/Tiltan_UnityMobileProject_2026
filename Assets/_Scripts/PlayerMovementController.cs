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
        public float RollCooldownTimer => _rollCooldownTimer;
        private Vector3 _rollDirection;

        [Header("Attack Settings")]
        public float attackCooldown = 0.5f;
        private float _attackCooldownTimer = 0f;
        public float AttackCooldownTimer => _attackCooldownTimer;

        private Rigidbody _rb;
        private Animator _animator;
        private PlayerAnimationController _animationController;

        private const float MoveDeadzoneSqr = 0.01f;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();

            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Logic methods called by input handler
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;

            if (_moveInput.sqrMagnitude < MoveDeadzoneSqr)
                _moveInput = Vector2.zero;
        }

        public void PerformRoll()
        {
            if (!_isRolling && _rollCooldownTimer <= 0f)
                StartRoll();
        }

        public void PerformAttack()
        {
            if (!_isRolling && _attackCooldownTimer <= 0f)
            {
                _attackCooldownTimer = attackCooldown;
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

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.fixedDeltaTime;
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

            // Calculate input magnitude for proportional speed
            float inputMagnitude = Mathf.Clamp01(_moveInput.magnitude);
            
            // World-space movement (NOT camera-relative)
            _moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

            float currentSpeed = moveSpeed * inputMagnitude;

            Vector3 desiredVelocity = new Vector3(
                _moveDirection.x * currentSpeed,
                _rb.linearVelocity.y,
                _moveDirection.z * currentSpeed
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
