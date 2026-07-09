using _Scripts.MainGame.Audio;
using _Scripts.MainGame.Inventory;
using _Scripts.MainGame.Combat;
using _Scripts.MainGame.Enemies;
using _Scripts.MainGame.Loot;
using UnityEngine;

namespace _Scripts.MainGame.Player
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
        public bool IsRolling => _isRolling;
        private float _rollTimer = 0f;
        private float _rollCooldownTimer = 0f;
        public float RollCooldownTimer => _rollCooldownTimer;
        private Vector3 _rollDirection;

        private float _staggerTimer = 0f;
        public bool IsStaggered => _staggerTimer > 0f;

        [Header("Attack Settings")]
        public float attackCooldown = 0.5f;
        public float attackRange = 1.5f;
        public int attackDamage = 25;
        public float attackAngle = 90f;
        private float _attackCooldownTimer = 0f;
public float AttackCooldownTimer => _attackCooldownTimer;
        private bool _isAttackHeld = false;

        private Rigidbody _rb;
private Animator _animator;
        private PlayerAnimationController _animationController;
        private PlayerEquipment _equipment;

        [Header("Audio")]
        [SerializeField] private AudioClip swingSound;

        private const float MoveDeadzoneSqr = 0.01f;

        private PlayerStatsController _stats;

        // Runs once when the game starts. Grabs references to Rigidbody and Animator,
        // and locks rotation on the Rigidbody so physics collisions don't spin the player.
        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            _animationController = GetComponent<PlayerAnimationController>();
            _equipment = GetComponent<PlayerEquipment>();
            _stats = GetComponent<PlayerStatsController>();

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

        public void SetAttackHeld(bool held)
        {
            _isAttackHeld = held;
        }

        // Called when the Roll button is pressed. Only rolls if not already rolling and cooldown is done.
public void PerformRoll()
        {
            if (!_isRolling && _rollCooldownTimer <= 0f && !IsStaggered)
                StartRoll();
        }

        // Called when the Attack button is pressed. Plays the attack animation if not rolling and cooldown is done,
        // then starts the attack cooldown timer.
        public void PerformAttack()
        {
            if (!_isRolling && _attackCooldownTimer <= 0f)
            {
                float atkSpd = _stats != null ? _stats.EffectiveAttackSpeed : 1f;
                _attackCooldownTimer = attackCooldown / atkSpd;

                // 1. Visuals and Sound
                if (_equipment && _equipment.CurrentItem)
                {
                    ItemCategory cat = _equipment.CurrentItem.category;
                    if (cat == ItemCategory.Tool || cat == ItemCategory.Weapon || cat == ItemCategory.Bow)
                    {
                        if (swingSound)
                        {
                            SingletonPoint.Instance.AudioManager.PlaySFX(swingSound);
                        }
                    }
                }

                if (_animator)
                {
                    _animator.SetFloat("AttackSpeedMultiplier", atkSpd);
                }

                if (_animationController)
                    _animationController.PlayAttack();
                else if (_animator)
                    _animator.SetTrigger("Attack");

                // 2. Hit Detection
                if (_equipment && _equipment.CurrentItem && _equipment.CurrentItem.category == ItemCategory.Bow)
                {
                    ShootProjectile();
                }
                else
                {
                    ApplyAttackDamage();
                }
            }
        }

        private void ShootProjectile()
        {
            if (_equipment && _equipment.CurrentItem && _equipment.CurrentItem.projectilePrefab)
            {
                int damage = _stats != null ? _stats.EffectiveAttack : (int)attackDamage;

                // Spawn position: slightly in front and up
                Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 1f;
                GameObject projectileObj = Instantiate(_equipment.CurrentItem.projectilePrefab, spawnPos, transform.rotation);

                if (projectileObj.TryGetComponent(out Projectile projectile))
                {
                    projectile.Initialize(damage);
                }
            }
        }

        private void ApplyAttackDamage()
        {
            // Center of the attack sphere is slightly in front of the player
            Vector3 attackCenter = transform.position + transform.forward * (attackRange * 0.5f) + Vector3.up * 1f;
            Collider[] hits = Physics.OverlapSphere(attackCenter, attackRange);

            foreach (var hit in hits)
            {
                // Look for EnemyController in the hit object or its parents
                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                
                if (enemy)
                {
                    // Check if the enemy is within the attack angle
                    Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, dirToEnemy);

                    if (angle <= attackAngle * 0.5f)
                    {
                        int damage = _stats ? _stats.EffectiveAttack : (int)attackDamage;
                        enemy.TakeDamage(damage);
                    }
                }
                
                Resource resource = hit.GetComponentInParent<Resource>();
                if (resource)
                {
                    int amount = resource.GatherResource(1);
                    InventoryManager.Instance.AddItem(resource.ItemData, amount);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize attack range
            Gizmos.color = Color.red;
            Vector3 attackCenter = transform.position + transform.forward * (attackRange * 0.5f) + Vector3.up * 1f;
            Gizmos.DrawWireSphere(attackCenter, attackRange);
            
            // Visualize attack angle
            Gizmos.color = Color.yellow;
            Vector3 forward = transform.forward * attackRange;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-attackAngle * 0.5f, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(attackAngle * 0.5f, Vector3.up);
            Vector3 leftRayDirection = leftRayRotation * forward;
            Vector3 rightRayDirection = rightRayRotation * forward;
            
            Gizmos.DrawRay(transform.position + Vector3.up * 1f, leftRayDirection);
            Gizmos.DrawRay(transform.position + Vector3.up * 1f, rightRayDirection);
        }

        // Physics update — runs at a fixed timestep, perfect for moving Rigidbodies.
        // While rolling, run the roll instead of normal movement.
        // Otherwise: walk, rotate, and tick the cooldown timers down.
        public void Stagger(float duration)
        {
            _isRolling = false;
            _staggerTimer = duration;
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
            _animationController?.SetMoving(false);
        }

        private void FixedUpdate()
        {
            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.fixedDeltaTime;
                return;
            }

            if (_isRolling)
            {
                ApplyRoll();
                return;
            }

            MovePlayer();
            RotatePlayer();

            if (_isAttackHeld)
                PerformAttack();

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

            float currentSpeed = (_stats != null ? _stats.EffectiveMoveSpeed : moveSpeed) * inputMagnitude;

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

            if (_animationController)
            {
                _animationController.ResetHit();
                _animationController.PlayRoll();
            }
            else
            {
                _animator?.SetTrigger("Roll");
            }
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
