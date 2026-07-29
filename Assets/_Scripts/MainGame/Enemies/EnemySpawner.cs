using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.Enemies;
using _Scripts.MainGame.Difficulty;
using _Scripts.MainGame.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.MainGame.Enemies
{
    [Serializable]
    public class DifficultySpawnerEntry
    {
        public DifficultyPhase phase;
        public EnemySpawnerSettings settings;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;

        // Resolves to the scene's singleton pool, which is recreated on every scene load.
        private ObjectPool enemyPool => ObjectPool.Instance;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistance = 25f;
        [SerializeField] private float sideVariance = 15f;
        [SerializeField] private float despawnRange = 40f;
        [SerializeField] private string terrainLayerName = "TerrainGround";

        [Header("Difficulty Settings")]
        [SerializeField] private List<DifficultySpawnerEntry> settingsList = new();

        private readonly Dictionary<DifficultyPhase, EnemySpawnerSettings> settingsByDifficulty = new();
        private DifficultyPhase _currentDifficulty = DifficultyPhase.None;

        private Transform _enemyParent;
        private List<GameObject> _activeEnemies = new();

        private void Awake()
        {
            RebuildSettingsDictionary();
        }

        private void Start()
        {
            _enemyParent = new GameObject("[Enemies]").transform;
            _enemyParent.SetParent(transform);
            StartCoroutine(SpawnRoutine());
        }

        private void OnEnable()
        {
            if (SingletonPoint.Instance?.DifficultyManager)
                SingletonPoint.Instance.DifficultyManager.SubscribeOnChange(HandleDifficultyChanged);
        }

        private void OnDisable()
        {
            if (SingletonPoint.Instance?.DifficultyManager)
                SingletonPoint.Instance.DifficultyManager.UnsubscribeOnChange(HandleDifficultyChanged);
        }

        private void Update()
        {
            if (!playerTransform) return;

            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                GameObject enemy = _activeEnemies[i];
                if (!enemy || !enemy.activeInHierarchy)
                {
                    _activeEnemies.RemoveAt(i);
                    if (enemy) enemyPool.Return(enemy);
                    continue;
                }

                if (Vector3.Distance(enemy.transform.position, playerTransform.position) > despawnRange)
                    ReturnEnemyToPool(enemy);
            }
        }

        private void RebuildSettingsDictionary()
        {
            settingsByDifficulty.Clear();
            foreach (DifficultySpawnerEntry entry in settingsList)
            {
                if (entry != null)
                    settingsByDifficulty[entry.phase] = entry.settings;
            }
        }

        private void HandleDifficultyChanged(DifficultyPhase difficulty)
        {
            _currentDifficulty = difficulty;
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                if (settingsByDifficulty.TryGetValue(_currentDifficulty, out EnemySpawnerSettings currentSettings) &&
                    currentSettings &&
                    currentSettings.enemyTypeDistribution != null &&
                    currentSettings.enemyTypeDistribution.Length > 0)
                {
                    yield return new WaitForSeconds(currentSettings.spawnInterval);

                    if (_activeEnemies.Count < currentSettings.maxActiveEnemies)
                        SpawnEnemy(currentSettings);
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private void SpawnEnemy(EnemySpawnerSettings settings)
        {
            if (!playerTransform || enemyPool == null) return;

            GameObject prefab = GetWeightedRandomPrefab(settings);
            if (!prefab) return;

            Vector3 spawnPos = playerTransform.position
                + playerTransform.forward * spawnDistance
                + playerTransform.right * Random.Range(-sideVariance, sideVariance);

            int terrainMask = LayerMask.GetMask(terrainLayerName);
            if (Physics.Raycast(spawnPos + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 500f, terrainMask, QueryTriggerInteraction.Ignore))
                spawnPos.y = hit.point.y;
            else
                return;

            Quaternion rotation = Quaternion.LookRotation(-playerTransform.forward);
            GameObject enemy = enemyPool.Get(prefab, spawnPos, rotation);
            enemy.transform.SetParent(_enemyParent);

            EnemyController ec = enemy.GetComponent<EnemyController>();
            if (ec) ec.SetPlayer(playerTransform);

            _activeEnemies.Add(enemy);
        }

        public void ReturnEnemyToPool(GameObject enemy)
        {
            _activeEnemies.Remove(enemy);
            enemyPool.Return(enemy);
        }

        private GameObject GetWeightedRandomPrefab(EnemySpawnerSettings settings)
        {
            float totalWeight = 0f;
            foreach (var entry in settings.enemyTypeDistribution)
                totalWeight += entry.weight;

            if (totalWeight <= 0f) return null;

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var entry in settings.enemyTypeDistribution)
            {
                currentWeight += entry.weight;
                if (randomValue <= currentWeight)
                    return entry.prefab;
            }

            return null;
        }
    }
}
