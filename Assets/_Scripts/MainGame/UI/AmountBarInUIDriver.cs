using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.UI
{
    public class AmountBarInUIDriver : MonoBehaviour
    {
        [SerializeField] private Image amountFillImage;

        private static readonly int FillId = Shader.PropertyToID("_Fill");

        public void Awake()
        {
            EnsureUniqueMaterialInstance();
        }

        public void EnsureUniqueMaterialInstance()
        {
            if (!amountFillImage) return;
            if (!amountFillImage.material) return;

            // Clone the material so this Image no longer shares it with other bars.
            amountFillImage.material = new Material(amountFillImage.material);
        }
        
        public void SetFill(float t)
        {
            t = Mathf.Clamp01(t);

            Material mat = amountFillImage.materialForRendering;
            mat.SetFloat(FillId, t);

            // Assign back so the Image uses the updated instance.
            amountFillImage.material = mat;
        }
        
        public void SetFill(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return;
            float t = Mathf.Clamp01((float)currentHP / maxHP);

            SetFill(t);
        }
    }
}