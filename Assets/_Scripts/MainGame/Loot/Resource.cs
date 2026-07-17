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

        [Header("Death Animation")]
        [SerializeField] private float shrinkDuration = 0.5f;
        [SerializeField] private GameObject poofEffectPrefab;

        private Vector3 _originalPosition;
        private Vector3 _initialScale;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private Coroutine _feedbackCoroutine;
        private bool _isDown;

        public int Amount => amount;
        public ResourceType ResourceType => resourceType;
        public ToolType RequiredTool => requiredTool;

        private float _initialHealth;
        private int _initialAmount;

        private void Awake()
        {
            _initialHealth = health;
            _initialAmount = amount;
            _initialScale = transform.localScale;
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>();
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = null;
            }

            _isDown = false;
            health = _initialHealth;
            amount = _initialAmount;
            transform.localScale = _initialScale;
            _originalPosition = transform.localPosition;
            
            EnsureInitialized();
            SetFlashColor(Color.white);
        }

        public int GatherResource(float effectiveness = 1f)
        {
            if (_isDown) return 0;

            health -= effectiveness;
            
            EnsureInitialized();
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackRoutine());

            if (health <= 0)
            {
                _isDown = true;
                int finalAmount = this.amount;
                this.amount = 0;
                
                if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
                _feedbackCoroutine = StartCoroutine(ShrinkAndPoofRoutine());
                
                return finalAmount;
            }
            return 0;
        }

        private IEnumerator ShrinkAndPoofRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkDuration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            transform.localScale = Vector3.zero;

            // Poof effect
            if (poofEffectPrefab)
            {
                var effect = Instantiate(poofEffectPrefab, transform.position, Quaternion.identity);
                // Ensure custom prefabs are cleaned up if they don't have a self-destruct mechanism
                if (effect.TryGetComponent<ParticleSystem>(out var ps))
                {
                    var main = ps.main;
                    if (main.loop) main.loop = false;
                    main.stopAction = ParticleSystemStopAction.Destroy;
                }
                else
                {
                    Destroy(effect, 2f);
                }
            }
            else
            {
                CreateDefaultPoof();
            }

            Loot();
            _feedbackCoroutine = null;
        }

        private void CreateDefaultPoof()
        {
            GameObject poof = new GameObject("TempPoof");
            poof.transform.position = transform.position;
            ParticleSystem ps = poof.AddComponent<ParticleSystem>();
            
            // Use a standard built-in material to avoid leaking 'new Material' instances
            ParticleSystemRenderer psr = poof.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = Canvas.GetDefaultCanvasMaterial();

            var main = ps.main;
            main.startColor = Color.white;
            main.startSize = 0.5f;
            main.startSpeed = 1f;
            main.duration = 0.5f;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            
            // Safety cleanup in case ParticleSystem doesn't stop
            Destroy(poof, 2f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0, 15));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            ps.Play();
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
            EnsureInitialized();
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
