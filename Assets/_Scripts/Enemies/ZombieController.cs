using UnityEngine;
using UnityEngine.AI;

namespace _Scripts.Enemies
{
    public class ZombieController : MonoBehaviour
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

        [Header("Health")]
        public int maxHealth = 50;
        public int currentHealth;
        public bool isDead = false;

        private bool isAttacking = false;
        private float attackTimer = 0f;
        public float attackDuration = 1.2f;

        
        private float wanderTimer = 0f;

        void Start()
        {
            if (currentHealth <= 0 && !isDead) 
            {
                currentHealth = maxHealth;
            }
        
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (!animator) animator = GetComponentInChildren<Animator>();
        
            FindPlayer();
        
            if (!isDead)
            {
                RestoreComponents();
            }
        }

        void OnEnable()
        {
            if (!isDead)
            {
                RestoreComponents();
            }
        }

        void RestoreComponents()
        {
            if (agent) agent.enabled = true;
        
            Collider col = GetComponent<Collider>();
            if (col) col.enabled = true;
        
            if (agent && agent.isActiveAndEnabled && !agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
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