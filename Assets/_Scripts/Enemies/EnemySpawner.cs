using System.Collections;
using System.Collections.Generic;
using _Scripts.Difficulty;
using UnityEngine;

namespace _Scripts.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float spawnDistance = 25f;
        [SerializeField] private float sideVariance = 15f;
        
        private Transform _enemyParent;
        private DifficultySettings _currentSettings;
        private Coroutine _spawnCoroutine;
        private List<GameObject> _activeEnemies = new List<GameObject>();
        private HashSet<GameObject> _preWarmedPrefabs = new HashSet<GameObject>();

        // Singleton setup so other scripts (like EnemyController) can find the spawner easily.
        private void Awake()
        {
            if (Instance == null) Instance = this;
            _enemyParent = new GameObject("Active_Enemies").transform;
        }

        // Subscribes to difficulty change events. When difficulty updates, restart the spawn loop with the new settings.
        // Also immediately applies the current difficulty if one is already set.
        private void Start()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.AddListener(HandleDifficultyChanged);
                if (DifficultyManager.Instance.CurrentSettings != null)
                {
                    HandleDifficultyChanged(DifficultyManager.Instance.CurrentSettings);
                }
            }
        }

        // Unsubscribe when destroyed — prevents the DifficultyManager from trying to call a destroyed object.
        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(HandleDifficultyChanged);
            }
        }

        // Difficulty just changed. Save the new settings, stop the old spawn coroutine,
        // and start a fresh one with the new spawn interval and enemy mix.
        // Also pre-warms the pools for any NEW enemy types introduced by this difficulty.
        private void HandleDifficultyChanged(DifficultySettings newSettings)
        {
            _currentSettings = newSettings;
            
            StartCoroutine(PreWarmDifficultyEnemiesRoutine(newSettings));

            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        // Ensures that all enemy types for the current difficulty are loaded into the pool.
        // This prevents "Instantiate" hits during gameplay when a new enemy type appears.
        // Optimized to spread instantiation over multiple frames to avoid hitching.
        private IEnumerator PreWarmDifficultyEnemiesRoutine(DifficultySettings settings)
        {
            if (Pooling.PoolManager.Instance == null || settings.enemyTypeDistribution == null) yield break;

            foreach (var distribution in settings.enemyTypeDistribution)
            {
                if (distribution.prefab != null && !_preWarmedPrefabs.Contains(distribution.prefab))
                {
                    // Pre-warm a reasonable amount (e.g., half the max capacity per type)
                    int count = settings.maxActiveEnemies / Mathf.Max(1, settings.enemyTypeDistribution.Length);
                    
                    // Instantiate one by one with a frame gap
                    for (int i = 0; i < count; i++)
                    {
                        Pooling.PoolManager.Instance.PreWarm(distribution.prefab, 1);
                        yield return null; // Wait for next frame
                    }
                    
                    _preWarmedPrefabs.Add(distribution.prefab);
                }
            }
        }

        // Infinite spawn loop (runs as a coroutine so we can yield/wait without blocking).
        // Each cycle: compute the wait time based on current intensity, wait, then spawn if we're under the cap.
        // Higher intensity = shorter wait between spawns.
        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                if (_currentSettings != null && _currentSettings.enemyTypeDistribution != null && _currentSettings.enemyTypeDistribution.Length > 0)
                {
                    // Calculate intensity multiplier
                    float intensity = DifficultyManager.Instance != null ? DifficultyManager.Instance.GetCurrentIntensity() : 1f;
                    float actualInterval = _currentSettings.spawnInterval / Mathf.Max(intensity, 0.1f);

                    yield return new WaitForSeconds(actualInterval);
                    
                    if (_activeEnemies.Count < _currentSettings.maxActiveEnemies)
                    {
                        SpawnEnemy();
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        // Adds an enemy to the active list. We track this so we can enforce the maxActiveEnemies cap.
        public void RegisterEnemy(GameObject enemy)
        {
            if (!_activeEnemies.Contains(enemy))
                _activeEnemies.Add(enemy);
        }

        // Called by an enemy when it dies or is cleaned up, so the spawner knows there's room for more.
        public void UnregisterEnemy(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
        }

        // Picks a random enemy prefab (weighted by difficulty settings) and a position AHEAD of the player.
        // Requirement: "Spawn area: around +5 units (or more) ahead of player world position"
        // Pulls the enemy from the object pool if possible (cheap), otherwise instantiates a new one.
        private void SpawnEnemy()
        {
            GameObject prefab = GetWeightedRandomPrefab();
            if (prefab == null) return;

            // Calculate spawn position ahead of the player
            // Using player forward + some random side variance to keep them outside the immediate screen view but ahead.
            Vector3 spawnPos = playerTransform.position + (playerTransform.forward * spawnDistance) + (playerTransform.right * Random.Range(-sideVariance, sideVariance));
            
            // Snap to NavMesh to ensure they can move
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject enemy;
            if (Pooling.PoolManager.Instance != null)
            {
                enemy = Pooling.PoolManager.Instance.Get(prefab, spawnPos, Quaternion.LookRotation(-playerTransform.forward));
            }
            else
            {
                enemy = Instantiate(prefab, spawnPos, Quaternion.LookRotation(-playerTransform.forward));
            }

            enemy.transform.SetParent(_enemyParent);
            
            // Add or configure PooledObject for distance-based cleanup (+10 units behind player)
            Pooling.PooledObject pooled = enemy.GetComponent<Pooling.PooledObject>();
            if (pooled == null) pooled = enemy.AddComponent<Pooling.PooledObject>();
            pooled.Setup(playerTransform, spawnDistance * 1.5f); // Use a buffer for general distance, PooledObject now handles the "behind" logic.

            RegisterEnemy(enemy);
        }

        // Weighted random selection. Adds all weights together, picks a random value in that range,
        // then walks the list adding weights until we hit the random value — that's our chosen prefab.
        // A prefab with weight 3 is 3x as likely to be picked as one with weight 1.
        private GameObject GetWeightedRandomPrefab()
        {
            float totalWeight = 0;
            foreach (var entry in _currentSettings.enemyTypeDistribution)
            {
                totalWeight += entry.weight;
            }

            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;

            foreach (var entry in _currentSettings.enemyTypeDistribution)
            {
                currentWeight += entry.weight;
                if (randomValue <= currentWeight)
                {
                    return entry.prefab;
                }
            }

            return null;
        }
    }
}
