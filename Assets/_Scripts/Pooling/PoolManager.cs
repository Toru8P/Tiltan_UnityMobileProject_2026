using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Pooling
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();

        // Singleton setup. DontDestroyOnLoad keeps the pool alive across scene loads
        // so we don't lose all our pre-warmed objects when changing scenes.
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

        // Pulls an inactive instance out of the pool and reactivates it at the requested position/rotation.
        // If the pool is empty, creates a brand new one and remembers which prefab it came from.
        // This is way cheaper than Instantiate every time — pooling avoids garbage collection hitches.
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

        // Sends an object back into the pool: disables it and queues it for reuse.
        // If we can't find which prefab it came from, fall back to destroying it (and log a warning).
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

        // Pre-creates `count` copies of a prefab and stashes them disabled in the pool.
        // Called at the start of the game so the first spawns don't cause a hitch
        // — instantiating dozens of objects mid-gameplay would cause a frame drop.
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