using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.MainGame.Pool
{
    [CreateAssetMenu(fileName = "NewObjectPool", menuName = "Pooling/Object Pool")]
    public class ObjectPool : ScriptableObject
    {
        private Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private Dictionary<GameObject, HashSet<GameObject>> _inPoolCheck = new();
        private Dictionary<GameObject, GameObject> _instanceToPrefab = new();

        public GameObject Get(GameObject prefab)
        {
            EnsurePoolExists(prefab);

            GameObject instance;
            if (_pools[prefab].Count > 0)
            {
                instance = _pools[prefab].Dequeue();
                _inPoolCheck[prefab].Remove(instance);
            }
            else
            {
                instance = Instantiate(prefab);
                _instanceToPrefab[instance] = prefab;
            }

            instance.SetActive(true);
            return instance;
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Get(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

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
