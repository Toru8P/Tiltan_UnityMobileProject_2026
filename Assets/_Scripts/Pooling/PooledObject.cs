using UnityEngine;

namespace _Scripts.Pooling
{
    public class PooledObject : MonoBehaviour
    {
        private Transform _playerTransform;
        private float _cleanupDistance = 15f;
        private bool _useDistanceCleanup = false;

        // Called right after spawning. Tells the object which player to track and how far behind to allow before cleanup.
        public void Setup(Transform player, float cleanupDistance)
        {
            _playerTransform = player;
            _cleanupDistance = cleanupDistance;
            _useDistanceCleanup = true;
        }

        private int _frameCount = 0;
        public int checkFrequency = 30;

        // Every `checkFrequency` frames (not every frame — saves CPU), check if we're far enough BEHIND the player
        // to be invisible. If so, return ourselves to the pool. Uses Vector3.Dot to project onto the player's forward axis:
        // negative dot = behind, positive = in front.
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

        // Returns this object to the pool. If for some reason there's no PoolManager, just disable it as a fallback.
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