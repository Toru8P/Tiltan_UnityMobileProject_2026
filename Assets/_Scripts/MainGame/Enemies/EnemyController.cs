using _Scripts.Difficulty;
using _Scripts.MainGame.Player;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class EnemyController : MonoBehaviour, IDifficultyScalable
    {
        public enum ZombieState
        {
            Idle,
            Chase,
            Attack,
            Dead
        }

        public ZombieState state = ZombieState.Idle;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float attackDuration = 1.2f;
        [SerializeField] private float modelYawOffset = 0f;


        [Header("Base Stats")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private int baseMaxHealth = 50;
        [SerializeField] private int baseDamage = 10;

        [Header("Health")]
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private int currentHealth;
        [SerializeField] private bool isDead = false;

        [Header("Active Stats")]
        [SerializeField] private int currentDamage = 10;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private bool isAttacking;
        private float attackTimer;
        private Collider[] _cachedColliders;
        private Rigidbody _rb;
        private Vector3 _moveDirection;
        private ZombieState _lastLoggedState;
        private bool _loggedMissingPlayer;
        private PlayerStatsController _playerStatsController;
        
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int AttackTriggerHash = Animator.StringToHash("ZombieAttack");

        public void SetPlayer(Transform target)
        {
            player = target;
            _playerStatsController = player.GetComponent<PlayerStatsController>();

            if (enableDebugLogs)
            {
                Debug.Log($"{name} SetPlayer called. Target = {(player ? player.name : "NULL")}", this);
            }
        }

        public void ApplyDifficulty(DifficultySettings settings)
        {
            if (isDead || settings == null) return;

            float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
            maxHealth = Mathf.RoundToInt(baseMaxHealth * settings.enemyMaxHealthMultiplier);
            currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);

            moveSpeed = baseSpeed * settings.enemySpeedMultiplier;
            currentDamage = (int)(baseDamage * settings.enemyDamageMultiplier);

            if (enableDebugLogs)
            {
                Debug.Log($"{name} ApplyDifficulty -> moveSpeed={moveSpeed}, maxHealth={maxHealth}, currentDamage={currentDamage}", this);
            }
        }

        private void Awake()
        {
            if (!animator)
                animator = GetComponentInChildren<Animator>();

            _cachedColliders = GetComponentsInChildren<Collider>();
            _rb = GetComponent<Rigidbody>();

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"{name} Awake | rb={(_rb ? "YES" : "NO")} | animator={(animator ? "YES" : "NO")} | player={(player ? player.name : "NULL")} | constraints={(_rb ? _rb.constraints.ToString() : "NONE")}",
                    this
                );
            }
        }

        private void Start()
        {
            if (currentHealth <= 0 && !isDead)
                currentHealth = maxHealth;

            if (!isDead)
                RestoreComponents();

            if (DifficultyManager.Instance)
            {
                DifficultyManager.Instance.OnDifficultyChanged.AddListener(ApplyDifficulty);

                if (DifficultyManager.Instance.CurrentSettings)
                    ApplyDifficulty(DifficultyManager.Instance.CurrentSettings);
            }
            else
            {
                moveSpeed = baseSpeed;
                currentDamage = baseDamage;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"{name} Start | currentHealth={currentHealth} | player={(player ? player.name : "NULL")}", this);
            }
        }

        private void OnEnable()
        {
            ResetState();

            if (enableDebugLogs)
            {
                Debug.Log($"{name} OnEnable", this);
            }
        }

        private void OnDisable()
        {
            if (EnemySpawner.Instance)
                EnemySpawner.Instance.UnregisterEnemy(gameObject);

            if (enableDebugLogs)
            {
                Debug.Log($"{name} OnDisable", this);
            }
        }

        private void OnDestroy()
        {
            if (DifficultyManager.Instance)
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(ApplyDifficulty);
        }

        public void ResetState()
        {
            isDead = false;
            currentHealth = maxHealth;
            state = ZombieState.Idle;
            isAttacking = false;
            attackTimer = 0f;
            _moveDirection = Vector3.zero;
            _loggedMissingPlayer = false;
            _lastLoggedState = state;

            if (animator)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.SetBool(IsDeadHash, false);
                animator.SetBool(IsWalkingHash, false);
            }

            RestoreComponents();

            if (enableDebugLogs)
            {
                Debug.Log($"{name} ResetState", this);
            }
        }

        private void RestoreComponents()
        {
            if (_cachedColliders == null)
                _cachedColliders = GetComponentsInChildren<Collider>();

            foreach (Collider col in _cachedColliders)
                col.enabled = true;

            if (_rb)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"{name} RestoreComponents | rb={(_rb ? "YES" : "NO")} | isKinematic={(_rb ? _rb.isKinematic.ToString() : "N/A")} | constraints={(_rb ? _rb.constraints.ToString() : "N/A")}",
                    this
                );
            }
        }

        private void Update()
        {
            if (!isDead && currentHealth <= 0)
                Die();

            if (isDead)
            {
                state = ZombieState.Dead;
                _moveDirection = Vector3.zero;
                return;
            }

            if (!player)
            {
                state = ZombieState.Idle;
                _moveDirection = Vector3.zero;

                if (!_loggedMissingPlayer && enableDebugLogs)
                {
                    _loggedMissingPlayer = true;
                    Debug.LogWarning($"{name} has NO player assigned. It cannot move.", this);
                }

                if (animator) animator.SetBool(IsWalkingHash, false);
                return;
            }

            float dist = DistanceToPlayer();

            if (dist <= attackRange)
                state = ZombieState.Attack;
            else if (dist <= detectionRange)
                state = ZombieState.Chase;
            else
                state = ZombieState.Idle;

            if (enableDebugLogs && state != _lastLoggedState)
            {
                Debug.Log($"{name} State changed: {_lastLoggedState} -> {state} | distance={dist:F2}", this);
                _lastLoggedState = state;
            }

            switch (state)
            {
                case ZombieState.Idle:
                    IdleUpdate();
                    break;
                case ZombieState.Chase:
                    ChaseUpdate();
                    break;
                case ZombieState.Attack:
                    AttackUpdate();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (isDead || state != ZombieState.Chase)
                return;

            if (!_rb)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"{name} has no Rigidbody, cannot MovePosition.", this);
                return;
            }

            Vector3 nextPosition = _rb.position + _moveDirection * (moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(nextPosition);

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"{name} FixedUpdate Move | current={_rb.position} | next={nextPosition} | dir={_moveDirection} | speed={moveSpeed} | constraints={_rb.constraints}",
                    this
                );
            }
        }

        private void IdleUpdate()
        {
            _moveDirection = Vector3.zero;

            if (animator)
                animator.SetBool(IsWalkingHash, false);
        }

        private void ChaseUpdate()
        {
            if (!player) return;

            if (animator)
                animator.SetBool(IsWalkingHash, true);

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"{name} ChaseUpdate | self={transform.position} | player={player.position} | toPlayer={toPlayer}", this);
            }

            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                _moveDirection = Vector3.zero;

                if (enableDebugLogs)
                {
                    Debug.LogWarning($"{name} toPlayer is almost zero, no movement.", this);
                }

                return;
            }

            _moveDirection = toPlayer.normalized;
            RotateTowardsPlayer();

            if (enableDebugLogs)
            {
                Debug.Log($"{name} Chase direction set to {_moveDirection}", this);
            }
        }
        
        private void RotateTowardsPlayer()
        {
            if (!player) return;

            Vector3 flatDir = player.position - _rb.position;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            _rb.MoveRotation(newRotation);
        }

        private void AttackUpdate()
        {
            _moveDirection = Vector3.zero;

            if (!player) return;

            RotateTowardsPlayer();

            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;

                if (attackTimer <= 0f)
                    isAttacking = false;

                if (animator)
                    animator.SetBool(IsWalkingHash, false);

                return;
            }

            isAttacking = true;
            attackTimer = attackDuration;

            if (enableDebugLogs)
            {
                Debug.Log($"{name} started attack.", this);
            }

            if (animator)
            {
                animator.SetBool(IsWalkingHash, false);
                animator.SetTrigger(AttackTriggerHash);
            }
            
            _playerStatsController.DealDamage(currentDamage);
        }

        public void TakeDamage(int dmg)
        {
            if (isDead) return;

            currentHealth -= dmg;

            if (enableDebugLogs)
            {
                Debug.Log($"{name} TakeDamage {dmg} -> currentHealth={currentHealth}", this);
            }

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            state = ZombieState.Dead;
            _moveDirection = Vector3.zero;

            if (enableDebugLogs)
            {
                Debug.Log($"{name} Die()", this);
            }

            if (animator)
            {
                animator.SetBool(IsWalkingHash, false);
                animator.SetBool(IsDeadHash, true);
            }

            if (_cachedColliders == null)
                _cachedColliders = GetComponentsInChildren<Collider>();

            foreach (Collider col in _cachedColliders)
                col.enabled = false;

            if (_rb)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            StartCoroutine(ReturnToPoolAfterDelay(5f));
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (EnemySpawner.Instance)
                EnemySpawner.Instance.UnregisterEnemy(gameObject);

            if (Pooling.PoolManager.Instance)
                Pooling.PoolManager.Instance.Return(gameObject);
            else
                gameObject.SetActive(false);
        }

        private float DistanceToPlayer()
        {
            if (!player)
                return Mathf.Infinity;

            return Vector3.Distance(transform.position, player.position);
        }
    }
}