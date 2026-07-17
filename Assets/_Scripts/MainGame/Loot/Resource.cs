using System.Collections;
using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.Loot
{
    public enum ResourceType { Tree, Rock, Bush, Small }

    public class Resource : Lootable
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private ToolType requiredTool;
        [SerializeField] private float health = 5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private float shakeDuration = 0.1f;
        [SerializeField] private float shakeAmount = 0.1f;
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        private Vector3 _originalPosition;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private Coroutine _feedbackCoroutine;

        public int Amount => amount;
        public ResourceType ResourceType => resourceType;
        public ToolType RequiredTool => requiredTool;

        private void Start()
        {
            _originalPosition = transform.localPosition;
            _renderers = GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public int GatherResource(float effectiveness = 1f)
        {
            health -= effectiveness;
            
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackRoutine());

            if (health <= 0)
            {
                int finalAmount = this.amount;
                this.amount = 0;
                Loot();
                return finalAmount;
            }
            return 0;
        }

        private IEnumerator FeedbackRoutine()
        {
            float elapsed = 0f;
            
            // Flash
            SetFlashColor(flashColor);

            // Shake
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = _originalPosition + Random.insideUnitSphere * shakeAmount;
                yield return null;
            }

            transform.localPosition = _originalPosition;

            // Reset Color
            SetFlashColor(Color.white);
            
            _feedbackCoroutine = null;
        }

        private void SetFlashColor(Color color)
        {
            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", color);
                _propBlock.SetColor("_Color", color);
                r.SetPropertyBlock(_propBlock);
            }
        }
}
}
