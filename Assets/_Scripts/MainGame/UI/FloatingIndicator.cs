using UnityEngine;
using TMPro;

namespace _Scripts.MainGame.UI
{
    public class FloatingIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float lifeTime = 1f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);

        private float _timer;
        private Color _initialColor;

        public void Setup(string text, Color color)
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.color = color;
            _initialColor = color;
            _timer = 0;
            transform.localScale = Vector3.one;
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

            // Float up
            transform.Translate(Vector3.up * (moveSpeed * Time.deltaTime));

            // Scale and Alpha
            transform.localScale = Vector3.one * scaleCurve.Evaluate(progress);
            
            Color c = _initialColor;
            c.a = alphaCurve.Evaluate(progress);
            textMesh.color = c;

            // Face camera (Billboard)
            if (UnityEngine.Camera.main != null)
            {
                transform.rotation = UnityEngine.Camera.main.transform.rotation;
            }
}
    }
}
