using UnityEngine;

namespace _Scripts.Difficulty
{
    public interface IDifficultyScalable
    {
        void ApplyDifficulty(DifficultySettings settings);
    }

    [System.Serializable]
    public struct EnemyWeight
    {
        public GameObject prefab;
        public float weight;
    }

    [CreateAssetMenu(fileName = "NewDifficultySettings", menuName = "Difficulty/Settings")]
    public class DifficultySettings : ScriptableObject
    {
        [Header("Display Settings")]
        public string levelName;
        public Color levelColor = Color.white;

        [Header("Spawn Settings")]
        public float spawnInterval = 2f;
        public int maxActiveEnemies = 20;
        public EnemyWeight[] enemyTypeDistribution;

        [Header("Intensity Scaling")]
        [Tooltip("Multiplier for spawn rate or intensity over time while this setting is active.")]
        public AnimationCurve spawnIntensityCurve = AnimationCurve.Linear(0, 1, 300, 2);

        [Header("Enemy Scaling")]
        public float enemySpeedMultiplier = 1f;
        public float enemyMaxHealthMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        
        [Header("Progression Settings")]
        public float scoreMultiplier = 1f;
    }
}
