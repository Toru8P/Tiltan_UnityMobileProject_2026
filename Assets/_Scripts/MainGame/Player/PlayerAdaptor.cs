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
        [SerializeField] private float invincibilityDuration = 1.0f;
        private float _invincibilityTimer = 0f;

        private void Awake()
{
            _animationController = GetComponent<PlayerAnimationController>();
            _statsController = GetComponent<PlayerStatsController>();
            _movementController = GetComponent<PlayerMovementController>();
        }

        private void Update()
        {
            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer -= Time.deltaTime;
            }
        }

        private bool _isDead;

        public void DealDamage(int damage)
        {
            if (_isDead || _invincibilityTimer > 0f) return;

            // Roll immunity
            if (_movementController != null && _movementController.IsRolling)
            {
                return;
            }

            _invincibilityTimer = invincibilityDuration;
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
                    string formattedTime = _Scripts.MainGame.UI.TimeFormatter.FormatTime(hud.SurvivalTime);
                    gameOverUI.Show(hud.Score, formattedTime, hud.CurrentPhase.ToString());
                }
}
        }
    }
}