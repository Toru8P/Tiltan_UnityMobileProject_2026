using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Scripts;

namespace _Scripts.UI
{
    public class AbilityCooldownUI : MonoBehaviour
    {
        public enum AbilityType { Roll, Attack }
        [SerializeField] private AbilityType abilityType;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private UnityEngine.UI.Image fillImage;
        [SerializeField] private GameObject labelObject;
        
        private PlayerMovementController _playerController;

        private void Start()
        {
            _playerController = Object.FindAnyObjectByType<PlayerMovementController>();
            
            // Auto-find references if they are missing
            if (cooldownText == null) cooldownText = GetComponentInChildren<TextMeshProUGUI>();
            if (fillImage == null) fillImage = transform.Find("CooldownFill")?.GetComponent<UnityEngine.UI.Image>();
            if (labelObject == null) labelObject = transform.Find("Label")?.gameObject;
        }

        private void Update()
        {
            if (_playerController == null) return;

            float currentCooldown = 0f;
            float maxCooldown = 1f;

            if (abilityType == AbilityType.Roll)
            {
                currentCooldown = _playerController.RollCooldownTimer;
                maxCooldown = _playerController.rollCooldown;
            }
            else if (abilityType == AbilityType.Attack)
            {
                currentCooldown = _playerController.AttackCooldownTimer;
                maxCooldown = _playerController.attackCooldown;
            }

            bool isOnCooldown = currentCooldown > 0f;

            if (isOnCooldown)
            {
                if (cooldownText != null)
                {
                    cooldownText.gameObject.SetActive(true);
                    cooldownText.text = currentCooldown.ToString("F1");
                }
                
                if (fillImage != null && maxCooldown > 0)
                {
                    fillImage.gameObject.SetActive(true);
                    fillImage.fillAmount = currentCooldown / maxCooldown;
                }
                
                if (labelObject != null) labelObject.SetActive(false);
            }
            else
            {
                if (cooldownText != null) cooldownText.gameObject.SetActive(false);
                if (fillImage != null) fillImage.gameObject.SetActive(false);
                if (labelObject != null) labelObject.SetActive(true);
            }
        }
    }
}