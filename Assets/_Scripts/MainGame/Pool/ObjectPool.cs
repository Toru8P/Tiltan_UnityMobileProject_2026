using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.MainGame.Pool
{
    /// <summary>
    /// Scene-lifetime object pool singleton. Because it lives in the scene (rather than as a
    /// persistent ScriptableObject asset), it is destroyed and recreated with every scene load,
    /// so pooled instances can never linger as stale references across a save/load reload.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, HashSet<GameObject>> _inPoolCheck = new();
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Retrieves an active instance of the given prefab from the pool, creating one if needed.</summary>
        public GameObject Get(GameObject prefab)
        {
            EnsurePoolExists(prefab);

            GameObject instance = null;

            // Discard any dead (destroyed) queued instances instead of handing back a destroyed
            // object and throwing MissingReferenceException.
            while (_pools[prefab].Count > 0)
            {
                GameObject candidate = _pools[prefab].Dequeue();
                _inPoolCheck[prefab].Remove(candidate);
                if (candidate != null)
                {
                    instance = candidate;
                    break;
                }

                _instanceToPrefab.Remove(candidate);
            }

            if (instance == null)
            {
                instance = Instantiate(prefab);
                _instanceToPrefab[instance] = prefab;
            }

            instance.SetActive(true);
            return instance;
        }

        /// <summary>Retrieves an active instance positioned and rotated as specified.</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Get(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        /// <summary>Pre-instantiates a number of inactive instances of the given prefab into the pool.</summary>
        public void PreWarm(GameObject prefab, int count)
        {
            EnsurePoolExists(prefab);

            for (int i = 0; i < count; i++)
            {
                GameObject instance = Instantiate(prefab);
                _instanceToPrefab[instance] = prefab;
                instance.SetActive(false);
                _pools[prefab].Enqueue(instance);
                _inPoolCheck[prefab].Add(instance);
            }
        }

        /// <summary>Returns an instance to the pool, deactivating it for later reuse.</summary>
        public void Return(GameObject instance)
        {
            if (!instance) return;

            if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                Debug.LogWarning($"Object {instance.name} was not spawned from this pool.");
                Destroy(instance);
                return;
            }

            EnsurePoolExists(prefab);

            if (!_inPoolCheck[prefab].Contains(instance))
            {
                instance.SetActive(false);
                _pools[prefab].Enqueue(instance);
                _inPoolCheck[prefab].Add(instance);
            }
        }

        private void EnsurePoolExists(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
                _inPoolCheck[prefab] = new HashSet<GameObject>();
            }
        }
    }
}
