using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.CharacterCreation
{
    // Drives a single color swatch button in the customization UI.
    // Init() is called by CharacterCustomizationController after instantiation.
    public class ColorSwatchUI : MonoBehaviour
    {
        [SerializeField] private Image swatchImage;
        [SerializeField] private GameObject selectedRing;
        [SerializeField] private Button button;

        private System.Action<int> _onSelected;
        private int _index;

        public void Init(Color color, int index, System.Action<int> onSelected)
        {
            _index = index;
            _onSelected = onSelected;

            if (swatchImage != null) swatchImage.color = color;
            button.onClick.AddListener(() => _onSelected?.Invoke(_index));
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedRing != null) selectedRing.SetActive(selected);
        }
    }
}
