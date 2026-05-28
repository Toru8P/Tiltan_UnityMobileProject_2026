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

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

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

        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged.RemoveListener(HandleDifficultyChanged);
            }
        }

        private void HandleDifficultyChanged(DifficultySettings newSettings)
        {
            _currentSettings = newSettings;
            
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

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

        public void RegisterEnemy(GameObject enemy)
        {
            if (!_activeEnemies.Contains(enemy))
                _activeEnemies.Add(enemy);
        }

        public void UnregisterEnemy(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
        }

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
