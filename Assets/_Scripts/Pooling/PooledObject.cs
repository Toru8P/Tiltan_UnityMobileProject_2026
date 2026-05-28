using UnityEngine;

namespace _Scripts.Pooling
{
    public class PooledObject : MonoBehaviour
    {
        private Transform _playerTransform;
        private float _cleanupDistance = 15f; // Default, can be overridden
        private bool _useDistanceCleanup = false;

        public void Setup(Transform player, float cleanupDistance)
        {
            _playerTransform = player;
            _cleanupDistance = cleanupDistance;
            _useDistanceCleanup = true;
        }

        private int _frameCount = 0;
        public int checkFrequency = 30;

        private void Update()
        {
            if (!_useDistanceCleanup || _playerTransform == null) return;

            _frameCount++;
            if (_frameCount % checkFrequency != 0) return;

            Vector3 toObject = transform.position - _playerTransform.position;
            float distanceAlongForward = Vector3.Dot(toObject, _playerTransform.forward);

            if (distanceAlongForward < -_cleanupDistance)
            {
                ReturnToPool();
            }
        }

        public void ReturnToPool()
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}