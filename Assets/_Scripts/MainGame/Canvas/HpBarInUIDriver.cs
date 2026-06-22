using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.Canvas
{
    public class HpBarInUIDriver : MonoBehaviour
    {
        [SerializeField] private Image hpFillImage;

        private static readonly int FillId = Shader.PropertyToID("_Fill");

        public void Awake()
        {
            EnsureUniqueMaterialInstance();
        }

        public void EnsureUniqueMaterialInstance()
        {
            if (!hpFillImage) return;
            if (!hpFillImage.material) return;

            // Clone the material so this Image no longer shares it with other bars.
            hpFillImage.material = new Material(hpFillImage.material);
        }
        
        public void SetFill(float t)
        {
            t = Mathf.Clamp01(t);

            Material mat = hpFillImage.materialForRendering;
            mat.SetFloat(FillId, t);

            // Assign back so the Image uses the updated instance.
            hpFillImage.material = mat;
        }
        
        public void SetFill(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return;
            float t = Mathf.Clamp01((float)currentHP / maxHP);

            SetFill(t);
        }
    }
}