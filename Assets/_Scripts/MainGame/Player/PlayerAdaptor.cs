using UnityEngine;

namespace _Scripts.MainGame.Player
{
    [RequireComponent(typeof(PlayerStatsController), typeof(PlayerAnimationController))]
    public class PlayerAdaptor : MonoBehaviour
    {
        private PlayerStatsController _statsController;
        private PlayerAnimationController _animationController;
        private PlayerMovementController _movementController;
        
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private float staggerDuration = 0.4f;

        private void Awake()
        {
            _animationController = GetComponent<PlayerAnimationController>();
            _statsController = GetComponent<PlayerStatsController>();
            _movementController = GetComponent<PlayerMovementController>();
        }

        private bool _isDead;
        
        public void DealDamage(int damage)
        {
            if (_isDead) return;

            // Roll immunity
            if (_movementController != null && _movementController.IsRolling)
            {
                return;
            }

            _statsController.DealDamage(damage);

            if (_statsController.IsDead)
            {
                _isDead = true;
                _animationController.PlayDeath();
                
                if (_movementController != null)
                {
                    _movementController.enabled = false;
                }

                HandleDeath();
            }
            else
            {
                _animationController.PlayHit();
                SingletonPoint.Instance.AudioManager.PlaySFX(hitSfx);

                if (_movementController != null)
                {
                    _movementController.Stagger(staggerDuration);
                }
            }
        }

        private void HandleDeath()
        {
            var hud = Object.FindAnyObjectByType<_Scripts.MainGame.UI.SurvivalHUDController>();
            if (hud != null)
            {
                hud.Stop();
                
                var gameOverUI = Object.FindAnyObjectByType<_Scripts.MainGame.UI.GameOverUIController>();
                if (gameOverUI != null)
                {
                    // Format time similar to HUD
                    int minutes = Mathf.FloorToInt(hud.SurvivalTime / 60f);
                    int seconds = Mathf.FloorToInt(hud.SurvivalTime % 60f);
                    string formattedTime = $"{minutes:00}:{seconds:00}";
                    
                    gameOverUI.Show(hud.Score, formattedTime, hud.CurrentPhase.ToString());
                }
            }
        }
    }
}