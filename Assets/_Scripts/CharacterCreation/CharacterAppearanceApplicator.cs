using UnityEngine;

namespace _Scripts.CharacterCreation
{
    public class CharacterAppearanceApplicator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Skin Renderers")]
        [SerializeField] private SkinnedMeshRenderer[] skinRenderers;

        [Header("Outfit Renderers")]
        [SerializeField] private SkinnedMeshRenderer[] outfitRenderers;

        [Header("Gameplay Color Options")]
        [SerializeField] private Color[] skinColorOptions =
        {
            new Color(1f, 0.8f, 0.6f, 1f),
            new Color(0.8f, 0.55f, 0.35f, 1f),
            new Color(0.5f, 0.3f, 0.15f, 1f),
            new Color(0.25f, 0.15f, 0.05f, 1f)
        };

        [SerializeField] private Color[] outfitColorOptions =
        {
            Color.red,
            Color.blue,
            Color.green,
            new Color(0.1f, 0.1f, 0.1f, 1f)
        };

        private void Start()
        {
            ApplyCurrentCustomization();
        }

        private void ApplyCurrentCustomization()
        {
            CharacterCustomization customization = GameInitData.GetCustomizationOrSaved();
            if (customization != null)
                ApplyCustomization(skinColorOptions, outfitColorOptions, customization);
        }

        public void ApplyCustomization(Color[] skinColors, Color[] outfitColors, CharacterCustomization customization)
        {
            if (customization == null) return;

            SetColor(skinRenderers, GetColor(skinColors, customization.SkinColorIndex));
            SetColor(outfitRenderers, GetColor(outfitColors, customization.OutfitColorIndex));
        }

        public static void ApplyToInstance(GameObject instance, Color[] skinColors, Color[] outfitColors, CharacterCustomization customization)
        {
            CharacterAppearanceApplicator applicator = instance.GetComponentInChildren<CharacterAppearanceApplicator>();
            applicator?.ApplyCustomization(skinColors, outfitColors, customization);
        }

        private void SetColor(SkinnedMeshRenderer[] renderers, Color color)
        {
            if (renderers == null) return;
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static Color GetColor(Color[] options, int index)
        {
            if (options == null || options.Length == 0) return Color.white;
            return options[Mathf.Clamp(index, 0, options.Length - 1)];
        }
    }
}