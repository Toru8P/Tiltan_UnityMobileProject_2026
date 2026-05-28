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
        [SerializeField] private float spawnRadius = 20f;
        
        private DifficultySettings _currentSettings;
        private Coroutine _spawnCoroutine;
        private List<GameObject> _activeEnemies = new List<GameObject>();

        // Singleton setup so other scripts (like ZombieController) can find the spawner easily.
        private void Awake()
        {
            if (Instance == null) Instance = this;
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
        private void HandleDifficultyChanged(DifficultySettings newSettings)
        {
            _currentSettings = newSettings;
            
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
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

        // Called by an enemy when it dies, so the spawner knows there's room for more.
        public void UnregisterEnemy(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
        }

        // Picks a random enemy prefab (weighted by difficulty settings) and a random position on a circle around the player.
        // Pulls the enemy from the object pool if possible (cheap), otherwise instantiates a new one.
        private void SpawnEnemy()
        {
            GameObject prefab = GetWeightedRandomPrefab();
            if (prefab == null) return;

            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = playerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            GameObject enemy;
            if (Pooling.PoolManager.Instance != null)
            {
                enemy = Pooling.PoolManager.Instance.Get(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            }

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
