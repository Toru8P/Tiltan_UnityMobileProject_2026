using _Scripts.Difficulty;
using UnityEngine;
using UnityEngine.AI;

namespace _Scripts.Enemies
{
    public class ZombieController : MonoBehaviour, IDifficultyScalable
    {
        public enum ZombieState { Idle, Wander, Chase, Attack, Dead }
        public ZombieState state = ZombieState.Idle;

        [Header("References")]
        public Transform player;
        public Animator animator;
        public NavMeshAgent agent;

        [Header("Settings")]
        public float detectionRange = 10f;
        public float attackRange = 1.8f;
        public float wanderRadius = 5f;
        public float wanderDelay = 3f;

        [Header("Base Stats")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private int baseMaxHealth = 50;
        [SerializeField] private float baseDamage = 10f;

        [Header("Health")]
        public int maxHealth = 50;
        public int currentHealth;
        public bool isDead = false;

        [Header("Active Stats")]
        public float currentDamage = 10f;

        private bool isAttacking = false;
        private float attackTimer = 0f;
        public float attackDuration = 1.2f;

        private float wanderTimer = 0f;

        public void ApplyDifficulty(DifficultySettings settings)
        {
            if (isDead) return;

            // Update Max Health
            float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
            maxHealth = Mathf.RoundToInt(baseMaxHealth * settings.enemyMaxHealthMultiplier);
            currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);

            // Update Speed
            if (agent != null)
            {
                agent.speed = baseSpeed * settings.enemySpeedMultiplier;
            }

            // Update Damage
            currentDamage = baseDamage * settings.enemyDamageMultiplier;

            Debug.Log($"{name} updated: Speed={agent.speed}, Health={maxHealth}, Damage={currentDamage}");
        }

        void Start()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();
            
            // Apply base speed to agent initially
            if (agent) agent.speed = baseSpeed;

            if (currentHealth <= 0 && !isDead) 
            {
                currentHealth = maxHealth;
            }
        
            if (!animator) animator = GetComponentInChildren<Animator>();
        
            FindPlayer();
        
            if (!isDead)
            {
                RestoreComponents();
            }

            // Apply and subscribe
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.AddListener(ApplyDifficulty);
                if (DifficultyManager.Instance.CurrentSettings != null)
                {
                    ApplyDifficulty(DifficultyManager.Instance.CurrentSettings);
                }
            }
        }

        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(ApplyDifficulty);
            }
        }

        void OnEnable()
        {
            ResetState();
        }

        public void ResetState()
        {
            isDead = false;
            currentHealth = maxHealth;
            state = ZombieState.Idle;
            isAttacking = false;
            attackTimer = 0f;
            wanderTimer = 0f;

            if (animator)
            {
                animator.SetBool("IsDead", false);
                animator.SetBool("IsWalking", false);
                animator.Rebind();
                animator.Update(0);
            }

            RestoreComponents();

            if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        void RestoreComponents()
        {
            if (agent) agent.enabled = true;
        
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders) col.enabled = true;
        
            if (agent && agent.isActiveAndEnabled && !agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
        }

        void Update()
        {
            if (!isDead && currentHealth <= 0)
            {
                Die();
            }

            if (isDead)
            {
                state = ZombieState.Dead;
                return;
            }

            if (!player) FindPlayer();

            switch (state)
            {
                case ZombieState.Idle: IdleUpdate(); break;
                case ZombieState.Wander: WanderUpdate(); break;
                case ZombieState.Chase: ChaseUpdate(); break;
                case ZombieState.Attack: AttackUpdate(); break;
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        void FindPlayer()
        {
            if (player != null) return;
            GameObject pObj = GameObject.FindWithTag("Player");
            if (!pObj) pObj = GameObject.Find("Player");
            if (pObj) player = pObj.transform;
        }

        void IdleUpdate()
        {
            if (animator) animator.SetBool("IsWalking", false);
            if (PlayerInRange(detectionRange))
            {
                state = ZombieState.Chase;
                return;
            }
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderDelay)
            {
                wanderTimer = 0f;
                state = ZombieState.Wander;
            }
        }

        void WanderUpdate()
        {
            if (animator) animator.SetBool("IsWalking", true);
            if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                if (!agent.hasPath && !agent.pathPending)
                {
                    Vector3 randomPos = RandomNavSphere(transform.position, wanderRadius);
                    if (randomPos != Vector3.zero) agent.SetDestination(randomPos);
                }
                if (!agent.pathPending && agent.remainingDistance < 0.5f) state = ZombieState.Idle;
            }
            if (PlayerInRange(detectionRange)) state = ZombieState.Chase;
        }

        private void ChaseUpdate()
        {
            if (animator) 
                animator.SetBool("IsWalking", true);
            
            if (!player) 
            { state = ZombieState.Idle; return; }
            
            if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh) 
                agent.SetDestination(player.position);
            float dist = DistanceToPlayer();
            
            if (dist <= attackRange)
            {
                state = ZombieState.Attack;
                if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.ResetPath();
            }
            else if (dist > detectionRange * 1.5f) state = ZombieState.Idle;
        }

        private void AttackUpdate()
        {
            if (isAttacking)
            {
                attackTimer -= Time.deltaTime;

                // Attack finished
                if (attackTimer <= 0f)
                {
                    isAttacking = false;

                    // Re-enable movement
                    if (agent && agent.isActiveAndEnabled)
                        agent.isStopped = false;

                    // If player moved away, chase again
                    if (DistanceToPlayer() > attackRange)
                        state = ZombieState.Chase;
                    else
                        state = ZombieState.Attack; // ready for next attack
                }


                return;
            }

            // Start a new attack
            isAttacking = true;
            attackTimer = attackDuration;

            if (animator)
            {
                animator.SetBool("IsWalking", false);
                animator.SetTrigger("ZombieAttack");
            }

            // Stop movement during attack
            if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            // Face the player
            if (player != null)
            {
                Vector3 lookPos = player.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);
            }
        }


        public void TakeDamage(int dmg)
        {
            if (isDead) return;
            currentHealth -= dmg;
            if (currentHealth <= 0) Die();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        void Die()
        {
            if (isDead) return;
            isDead = true;
            state = ZombieState.Dead;
            if (animator) animator.SetBool("IsDead", true);
            if (agent != null)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
            }
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders) col.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Return to pool after a delay
            StartCoroutine(ReturnToPoolAfterDelay(5f));
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.UnregisterEnemy(gameObject);

            if (Pooling.PoolManager.Instance != null)
            {
                Pooling.PoolManager.Instance.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        bool PlayerInRange(float range)
        {
            return DistanceToPlayer() <= range;
        }

        float DistanceToPlayer()
        {
            if (!player) 
                return Mathf.Infinity; 
            return Vector3.Distance(transform.position, player.position);
        }
        public static Vector3 RandomNavSphere(Vector3 origin, float dist)
        {
            Vector3 randomDirection = Random.insideUnitSphere * dist;
            randomDirection += origin;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomDirection, out navHit, dist, NavMesh.AllAreas)) 
                return navHit.position;
            return Vector3.zero;
        }
    }
}