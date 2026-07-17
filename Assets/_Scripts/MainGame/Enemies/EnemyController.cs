using _Scripts.MainGame.Difficulty.Deprecated;
using _Scripts.MainGame.Inventory;
using _Scripts.MainGame.Loot;
using _Scripts.MainGame.Player;
using _Scripts.MainGame.UI;
using UnityEngine;

namespace _Scripts.MainGame.Enemies
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
        [SerializeField] private GameObject identityPrefab;

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

        [Header("Points")]
        [SerializeField] private double baseScorePoints = 100;
        private float _scoreMultiplier = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private bool isAttacking;
private float attackTimer;
        private Collider[] _cachedColliders;
        private Rigidbody _rb;
        private Vector3 _moveDirection;
        private ZombieState _lastLoggedState;
        private bool _loggedMissingPlayer;
        private PlayerAdaptor _playerAdaptor;
        private DifficultySettings _currentSettings;
        
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int AttackTriggerHash = Animator.StringToHash("ZombieAttack");

        public void SetPlayer(Transform target)
        {
            player = target;
            _playerAdaptor = player.GetComponent<PlayerAdaptor>();

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
            _scoreMultiplier = settings.scoreMultiplier;

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
            if (enableDebugLogs)
                Debug.Log($"{name} OnDisable", this);
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
                _rb.useGravity = true;
                _rb.isKinematic = false;
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
            if (isDead || !_rb) return;

            if (state == ZombieState.Chase || state == ZombieState.Attack)
                RotateTowardsPlayer();

            if (state != ZombieState.Chase) return;

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
            
            _playerAdaptor.DealDamage(currentDamage);
        }

        public void TakeDamage(int dmg)
        {
            if (isDead) return;

            currentHealth -= dmg;

            if (IndicatorManager.Instance != null)
            {
                IndicatorManager.Instance.SpawnDamageZombie(transform.position, dmg);
            }

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

            double points = baseScorePoints * _scoreMultiplier;
            if (SurvivalHUDController.Instance != null)
            {
                SurvivalHUDController.Instance.AddScore(points);
            }

            if (IndicatorManager.Instance != null)
            {
                IndicatorManager.Instance.SpawnPoints(transform.position, points);
            }

            RollForLoot();

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
                _rb.useGravity = false;
                _rb.isKinematic = true;
            }

            StartCoroutine(DeactivateAfterDelay(5f));
        }

        private void RollForLoot()
        {
            if (_currentSettings == null || _currentSettings.lootTable == null) return;

            foreach (var drop in _currentSettings.lootTable)
            {
                if (drop.item == null) continue;

                if (drop.targetEnemyPrefab != null && drop.targetEnemyPrefab != identityPrefab)
                    continue;

                float roll = Random.value;
                if (roll <= drop.dropChance)
                {
                    int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    SpawnLoot(drop.item, qty);
                }
            }
        }

        private void SpawnLoot(ItemData item, int quantity)
        {
            GameObject prefab = _currentSettings.worldItemPrefab;
            if (!prefab) return;

            GameObject lootObj = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            WorldItem worldItem = lootObj.GetComponent<WorldItem>();
            if (worldItem)
            {
                worldItem.SpawnSetup(item, quantity);
            }
        }

        private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
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