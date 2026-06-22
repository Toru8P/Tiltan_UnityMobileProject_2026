using UnityEngine;

namespace _Scripts.MainGame.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemySpawnerSettings", menuName = "Difficulty/Settings")]
    public class EnemySpawnerSettings : ScriptableObject
    {
        public float spawnInterval = 2f;
        public int maxActiveEnemies = 20;
        public EnemyWeight[] enemyTypeDistribution;
    }
    
    [System.Serializable]
    public struct EnemyWeight
    {
        public GameObject prefab;
        public float weight;
    }
}