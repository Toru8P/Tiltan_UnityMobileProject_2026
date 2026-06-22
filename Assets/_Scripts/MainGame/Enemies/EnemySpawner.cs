using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.Difficulty;
using _Scripts.Enemies;
using _Scripts.MainGame.Difficulty;
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

        [Header("Spawn Settings")] 
        [SerializeField] private float spawnDistance = 25f;

        [SerializeField] private float sideVariance = 15f;
        
        [SerializeField] private string terrainLayerName = "TerrainGround";
        
        [Header("Difficulty Settings")] 
        [SerializeField] private List<DifficultySpawnerEntry> settingsList = new();

        private readonly Dictionary<DifficultyPhase, EnemySpawnerSettings> settingsByDifficulty = new();

        private DifficultyPhase _currentDifficulty = DifficultyPhase.None;

        private Transform _enemyParent;
        private int _activeEnemies = 0;

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
            {
                SingletonPoint.Instance.DifficultyManager.SubscribeOnChange(HandleDifficultyChanged);
            }
        }

        private void OnDisable()
        {
            if (SingletonPoint.Instance?.DifficultyManager)
            {
                SingletonPoint.Instance.DifficultyManager.UnsubscribeOnChange(HandleDifficultyChanged);
            }
        }

        private void RebuildSettingsDictionary()
        {
            settingsByDifficulty.Clear();

            for (int i = 0; i < settingsList.Count; i++)
            {
                DifficultySpawnerEntry entry = settingsList[i];
                if (entry == null) continue;

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
                if (settingsByDifficulty.TryGetValue(_currentDifficulty, out var currentSettings))
                {
                    if (currentSettings &&
                        currentSettings.enemyTypeDistribution != null &&
                        currentSettings.enemyTypeDistribution.Length > 0)
                    {
                        float actualInterval = currentSettings.spawnInterval;
                        yield return new WaitForSeconds(actualInterval);

                        if (_activeEnemies < currentSettings.maxActiveEnemies)
                        {
                            SpawnEnemy(currentSettings);
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private void SpawnEnemy(EnemySpawnerSettings settings)
        {
            if (!playerTransform) return;

            GameObject prefab = GetWeightedRandomPrefab(settings);
            if (!prefab) return;

            Vector3 spawnPos =
                playerTransform.position +
                (playerTransform.forward * spawnDistance) +
                (playerTransform.right * Random.Range(-sideVariance, sideVariance));
            
            int terrainMask = LayerMask.GetMask(terrainLayerName);
            Vector3 rayOrigin = spawnPos + Vector3.up * 200f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit terrainHit, 500f, terrainMask, QueryTriggerInteraction.Ignore))
            {
                spawnPos.y = terrainHit.point.y;
            }
            else
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(-playerTransform.forward);

            GameObject enemy;
            enemy = Pooling.PoolManager.Instance ? Pooling.PoolManager.Instance.Get(prefab, spawnPos, rotation) : Instantiate(prefab, spawnPos, rotation);

            if (_enemyParent)
            {
                enemy.transform.SetParent(_enemyParent);
            }

            ZombieController zc = enemy.GetComponent<ZombieController>();
            zc.SetPlayer(playerTransform);
        }

        private GameObject GetWeightedRandomPrefab(EnemySpawnerSettings settings)
        {
            float totalWeight = 0f;

            foreach (var entry in settings.enemyTypeDistribution)
            {
                totalWeight += entry.weight;
            }

            if (totalWeight <= 0f)
                return null;

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var entry in settings.enemyTypeDistribution)
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