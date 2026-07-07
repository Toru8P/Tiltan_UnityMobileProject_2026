using UnityEngine;
using _Scripts.Enemies;

namespace _Scripts.MainGame.Combat
{
    public class Projectile : MonoBehaviour
    {
        public float speed = 20f;
        public float lifetime = 5f;
        
        private int _damage;
        private bool _hasHit = false;

        public void Initialize(int damage)
        {
            _damage = damage;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;

            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                _hasHit = true;
                Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                // Hit a wall or something solid
                _hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}