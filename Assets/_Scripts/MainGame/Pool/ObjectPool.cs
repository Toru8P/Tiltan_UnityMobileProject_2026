using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.MainGame.Pool
{
    [CreateAssetMenu(fileName = "NewObjectPool", menuName = "Pooling/Object Pool")]
    public class ObjectPool : ScriptableObject
    {
        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        private Dictionary<GameObject, HashSet<GameObject>> _inPoolCheck = new Dictionary<GameObject, HashSet<GameObject>>();
        private Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();
        
        public GameObject Get(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
                _inPoolCheck[prefab] = new HashSet<GameObject>();
            }

            GameObject instance;
            if (_pools[prefab].Count > 0)
            {
                instance = _pools[prefab].Dequeue();
                _inPoolCheck[prefab].Remove(instance);
            }
            else
            {
                instance = Instantiate(prefab, Vector3.zero, new Quaternion());
                _instanceToPrefab[instance] = prefab;
            }
            instance.SetActive(true);

            return instance;
        }
        
        public void Return(GameObject instance)
        {
            if (!instance) return;
            instance.SetActive(false);

            if (_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                if (!_pools.ContainsKey(prefab))
                {
                    _pools[prefab] = new Queue<GameObject>();
                    _inPoolCheck[prefab] = new HashSet<GameObject>();
                }
                
                if (!_inPoolCheck[prefab].Contains(instance))
                {
                    instance.SetActive(false);
                    _pools[prefab].Enqueue(instance);
                    _inPoolCheck[prefab].Add(instance);
                }
            }
            else
            {
                Debug.LogWarning($"Object {instance.name} was not spawned from the pool or its prefab is unknown.");
                Destroy(instance); // Fallback
            }
        }
    }
}