using UnityEngine;

namespace _Scripts.MainGame.Player
{
    [RequireComponent(typeof(PlayerStatsController), typeof(PlayerAnimationController))]
    public class PlayerAdaptor : MonoBehaviour
    {
        private PlayerStatsController _statsController;
        private PlayerAnimationController _animationController;

        private void Awake()
        {
            _animationController = GetComponent<PlayerAnimationController>();
            _statsController = GetComponent<PlayerStatsController>();
        }
        
        public void DealDamage(int damage)
        {
            _statsController.DealDamage(damage);
            _animationController.PlayHit();
        }
    }
}