using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }

            GameObject instance;
            if (_pools[prefab].Count > 0)
            {
                instance = _pools[prefab].Dequeue();
                instance.transform.position = position;
                instance.transform.rotation = rotation;
                instance.SetActive(true);
            }
            else
            {
                instance = Instantiate(prefab, position, rotation);
                _instanceToPrefab[instance] = prefab;
            }

            return instance;
        }

        public void Return(GameObject instance)
        {
            if (_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                instance.SetActive(false);
                _pools[prefab].Enqueue(instance);
            }
            else
            {
                Debug.LogWarning($"Object {instance.name} was not spawned from the pool or its prefab is unknown.");
                Destroy(instance); // Fallback
            }
        }

        // Helper for auto-expansion if we want to pre-warm
        public void PreWarm(GameObject prefab, int count)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }

            for (int i = 0; i < count; i++)
            {
                GameObject instance = Instantiate(prefab);
                instance.SetActive(false);
                _pools[prefab].Enqueue(instance);
                _instanceToPrefab[instance] = prefab;
            }
        }
    }
}