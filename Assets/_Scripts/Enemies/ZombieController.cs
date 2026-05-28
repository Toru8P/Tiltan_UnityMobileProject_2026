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

        // Called whenever the difficulty changes. Rescales this zombie's stats based on the new multipliers.
        // We preserve the current health *percentage* (so a half-dead zombie stays half-dead after rescaling),
        // and update agent speed, damage, and turning responsiveness all at once.
        public void ApplyDifficulty(DifficultySettings settings)
        {
            if (isDead) return;

            // Update Max Health
            float healthRatio = maxHealth > 0 ? (float)currentHealth / maxHealth : 1f;
            maxHealth = Mathf.RoundToInt(baseMaxHealth * settings.enemyMaxHealthMultiplier);
            currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);

            // Update Speed and Responsiveness
            if (agent != null)
            {
                agent.speed = baseSpeed * settings.enemySpeedMultiplier;
                agent.acceleration = 30f * settings.enemySpeedMultiplier; // Scale responsiveness
                agent.angularSpeed = 400f * settings.enemySpeedMultiplier;
            }

            // Update Damage
            currentDamage = baseDamage * settings.enemyDamageMultiplier;

            Debug.Log($"{name} updated: Speed={agent.speed}, Health={maxHealth}, Damage={currentDamage}");
        }

        // Runs once when the zombie first spawns.
        // Grabs components, sets initial speed and health, finds the player, and subscribes to difficulty changes.
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

        // Unsubscribe from difficulty events when destroyed — avoids leaks.
        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(ApplyDifficulty);
            }
        }

        // OnEnable fires every time the zombie is activated — including when it's reused from the pool.
        // We reset its state so a recycled corpse comes back as a fresh enemy.
        void OnEnable()
        {
            ResetState();
        }

        // Resets every variable to its starting value so a pooled zombie behaves like a brand-new one.
        // Important: pooled objects do NOT re-run Start(), so we need this manual reset.
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
                rb.isKinematic = true; // Kinematic while alive to prevent slipping/physics fighting
            }
        }

        // Turns the NavMeshAgent and all colliders back on, and warps the zombie onto the NavMesh
        // if it spawned slightly off it. Without this, a pooled zombie might be invisible/non-interactive.
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

        // The main "brain" loop. Every frame:
        // 1. Kill the zombie if health hit zero.
        // 2. If dead, skip everything else.
        // 3. Make sure we have a player reference.
        // 4. Run the right state handler based on the current state (Idle, Wander, Chase, Attack).
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
        // Locates the player in the scene by tag (or name as a fallback) and caches the reference.
        void FindPlayer()
        {
            if (player != null) return;
            GameObject pObj = GameObject.FindWithTag("Player");
            if (!pObj) pObj = GameObject.Find("Player");
            if (pObj) player = pObj.transform;
        }

        // IDLE STATE: standing still. After waiting `wanderDelay` seconds, switch to wandering.
        // If the player gets close enough, switch straight to chasing.
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

        // WANDER STATE: pick a random point within wanderRadius and walk there.
        // Once we arrive, go back to Idle. If the player shows up nearby, abandon wandering and start chasing.
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

        // CHASE STATE: walk toward the player using NavMesh pathfinding.
        // Switch to Attack if we're in melee range. If the player runs way out of range, give up and go back to Idle.
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

        // ATTACK STATE: stop, face the player, and play the attack animation for `attackDuration` seconds.
        // After the swing finishes, either attack again (player still in range) or chase (player ran).
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


        // Called by the player's attack hitbox. Subtracts damage and triggers Die() if health hits zero.
        public void TakeDamage(int dmg)
        {
            if (isDead) return;
            currentHealth -= dmg;
            if (currentHealth <= 0) Die();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        // Handles death: marks the zombie as dead, plays the death animation, disables agent/colliders/physics
        // so the corpse doesn't interfere with the game, and schedules a return to the pool in 5 seconds.
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

        // Coroutine: wait `delay` seconds (so the death animation can play), then return the zombie to the pool
        // so it can be reused for the next spawn. Also tells the spawner we're gone so it can spawn another.
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

        // Quick helper: is the player within `range` units of this zombie?
        bool PlayerInRange(float range)
        {
            return DistanceToPlayer() <= range;
        }

        // Returns the distance to the player. Returns infinity if there's no player reference
        // so range checks always return false until the player is found.
        float DistanceToPlayer()
        {
            if (!player)
                return Mathf.Infinity;
            return Vector3.Distance(transform.position, player.position);
        }
        // Picks a random point within `dist` of `origin` that's actually on the NavMesh (so the zombie can walk there).
        // Returns Vector3.zero if no valid point was found.
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