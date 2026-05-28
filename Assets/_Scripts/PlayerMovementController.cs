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

        // Runs once when the game starts. Grabs references to Rigidbody and Animator,
        // and locks rotation on the Rigidbody so physics collisions don't spin the player.
        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();

            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Called by PlayerInputHandler whenever the joystick moves.
        // Stores the input, and snaps tiny values to zero (deadzone) so a near-still stick doesn't drift the player.
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;

            if (_moveInput.sqrMagnitude < MoveDeadzoneSqr)
                _moveInput = Vector2.zero;
        }

        // Called when the Roll button is pressed. Only rolls if not already rolling and cooldown is done.
        public void PerformRoll()
        {
            if (!_isRolling && _rollCooldownTimer <= 0f)
                StartRoll();
        }

        // Called when the Attack button is pressed. Plays the attack animation if not rolling and cooldown is done,
        // then starts the attack cooldown timer.
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

        // Physics update — runs at a fixed timestep, perfect for moving Rigidbodies.
        // While rolling, run the roll instead of normal movement.
        // Otherwise: walk, rotate, and tick the cooldown timers down.
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

        // Applies movement based on joystick input.
        // If the stick is in the deadzone, stop horizontal velocity (preserve Y for gravity) and tell the animator we're idle.
        // Otherwise, build a world-space velocity vector from the input and assign it to the Rigidbody.
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

        // Smoothly rotates the player to face the direction they're moving in.
        // Uses RotateTowards (not Lerp) so the rotation speed is constant in degrees/second.
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
        // Kicks off a roll: chooses the roll direction (current movement or forward if standing still),
        // sets the rolling flag, and starts both the roll-duration timer and the cooldown timer.
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

        // Runs every physics frame while rolling. Pushes the player at full rollForce in the roll direction,
        // counts the timer down, and stops the roll when the timer hits zero.
        private void ApplyRoll()
        {
            _rollTimer -= Time.fixedDeltaTime;

            _rb.linearVelocity = _rollDirection * rollForce;

            if (_rollTimer <= 0f)
                _isRolling = false;
        }
    }
}
