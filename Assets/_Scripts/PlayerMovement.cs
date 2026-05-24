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

        private Vector2 _moveInput;
        private Vector3 _moveDirection;

        private bool _isRolling = false;
        private float _rollTimer = 0f;
        private float _rollCooldownTimer = 0f;
        private Vector3 _rollDirection;

        private Rigidbody _rb;
        private Animator _animator;
        private PlayerAnimationController _animationController;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();

            // Prevent Rigidbody from tipping over
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnRoll(InputValue value)
        {
            if (value.isPressed && !_isRolling && _rollCooldownTimer <= 0f)
                StartRoll();
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

        void FixedUpdate()
        {
            if (_isRolling)
            {
                ApplyRoll();
                return;
            }

            MovePlayer();
            RotatePlayer();

            if (_rollCooldownTimer > 0f)
                _rollCooldownTimer -= Time.deltaTime;
        }

        // CAMERA-RELATIVE MOVEMENT
        Vector3 GetCameraRelativeDirection()
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();

            return camForward * _moveInput.y + camRight * _moveInput.x;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        void MovePlayer()
        {
            _moveDirection = GetCameraRelativeDirection();

            Vector3 velocity = _moveDirection * moveSpeed;
            velocity.y = _rb.linearVelocity.y; // keep gravity from Rigidbody

            _rb.linearVelocity = velocity;

            _animator.SetBool("IsMoving", _moveDirection.magnitude > 0.1f);
        }

        private void RotatePlayer()
        {
            if (_moveDirection.sqrMagnitude < 0.01f)
                return;

            Quaternion targetRot = Quaternion.LookRotation(_moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
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
