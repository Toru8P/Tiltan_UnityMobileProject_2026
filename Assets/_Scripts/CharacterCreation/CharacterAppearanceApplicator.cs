using UnityEngine;

namespace _Scripts.CharacterCreation
{
    // Attach to the root of the player preview prefab.
    // Assign the SkinnedMeshRenderers for skin and outfit in the inspector.
    // ApplyCustomization() is called live during character creation.
    // ApplyToInstance() is called from the gameplay scene after spawning the player.
    public class CharacterAppearanceApplicator : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Skin Renderers")]
        [SerializeField] private SkinnedMeshRenderer[] skinRenderers;

        [Header("Outfit Renderers")]
        [SerializeField] private SkinnedMeshRenderer[] outfitRenderers;

        public void ApplyCustomization(Color[] skinColors, Color[] outfitColors, CharacterCustomization customization)
        {
            if (customization == null) return;

            SetColor(skinRenderers, GetColor(skinColors, customization.SkinColorIndex));
            SetColor(outfitRenderers, GetColor(outfitColors, customization.OutfitColorIndex));
        }

        // Called from the gameplay scene after instantiating the player prefab.
        // Pass the same color arrays that were set in CharacterCustomizationController.
        public static void ApplyToInstance(GameObject instance, Color[] skinColors, Color[] outfitColors,
            CharacterCustomization customization)
        {
            var applicator = instance.GetComponentInChildren<CharacterAppearanceApplicator>();
            applicator?.ApplyCustomization(skinColors, outfitColors, customization);
        }

        private void SetColor(SkinnedMeshRenderer[] renderers, Color color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                r.SetPropertyBlock(block);
            }
        }

        private Color GetColor(Color[] options, int index)
        {
            if (options == null || options.Length == 0) return Color.white;
            return options[Mathf.Clamp(index, 0, options.Length - 1)];
        }
    }
}
