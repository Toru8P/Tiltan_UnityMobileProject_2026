using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.CharacterCreation
{
    public class CharacterSelectionCard : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedHighlight;
    }
}
