using TMPro;
using UnityEngine;

namespace _Scripts.MainGame.UI
{
    public class FloatingIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private SpriteRenderer criticalIcon;
        [SerializeField] private float criticalIconOffset = 0.65f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float lifeTime = 1f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);

        private float _timer;
        private Color _initialColor;
        private Color _initialIconColor;

        public void Setup(string text, Color color, Sprite icon = null)
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.color = color;
            _initialColor = color;
            _timer = 0;
            transform.localScale = Vector3.one;

            if (icon != null)
            {
                if (criticalIcon == null)
                {
                    GameObject iconObject = new GameObject("CriticalIcon");
                    iconObject.transform.SetParent(transform, false);
                    criticalIcon = iconObject.AddComponent<SpriteRenderer>();
                }

                criticalIcon.sprite = icon;
                criticalIcon.transform.localPosition = Vector3.right * criticalIconOffset;
                criticalIcon.transform.localScale = Vector3.one * 0.35f;
                criticalIcon.enabled = true;
                _initialIconColor = Color.white;
            }
            else if (criticalIcon != null)
            {
                criticalIcon.enabled = false;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float progress = _timer / lifeTime;

            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.Translate(Vector3.up * (moveSpeed * Time.deltaTime));
            transform.localScale = Vector3.one * scaleCurve.Evaluate(progress);

            Color color = _initialColor;
            color.a = alphaCurve.Evaluate(progress);
            textMesh.color = color;

            if (criticalIcon != null && criticalIcon.enabled)
            {
                Color iconColor = _initialIconColor;
                iconColor.a = color.a;
                criticalIcon.color = iconColor;
            }

            if (UnityEngine.Camera.main != null)
                transform.rotation = UnityEngine.Camera.main.transform.rotation;
        }
    }
}