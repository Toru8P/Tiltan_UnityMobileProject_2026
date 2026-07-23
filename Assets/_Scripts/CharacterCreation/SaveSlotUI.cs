using _Scripts.MainGame.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.CharacterCreation
{
    // Drives a single save-slot row in the Load Game scroll list.
    // Call Init() after instantiation.
    public class SaveSlotUI : MonoBehaviour
    {
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI playtimeText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;

        private SaveSlotData _data;

        public void Init(SaveSlotData data)
        {
            _data = data;

            if (characterNameText != null) characterNameText.text = data.characterName;
            if (playtimeText != null) playtimeText.text = data.FormattedPlaytime();
            if (dateText != null) dateText.text = data.FormattedDate();

            loadButton.onClick.AddListener(OnLoadClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);

            if (!string.IsNullOrEmpty(data.thumbnailFileName) && SaveSlotManager.Instance != null)
                StartCoroutine(SaveSlotManager.Instance.LoadThumbnail(data.thumbnailFileName, ApplyThumbnail));
        }

        private void ApplyThumbnail(UnityEngine.Texture2D tex)
        {
            if (thumbnailImage != null && tex != null)
                thumbnailImage.texture = tex;
        }

        private void OnLoadClicked()
        {
            SaveSlotManager.Instance?.RequestLoad(_data);
        }

        private void OnDeleteClicked()
        {
            SaveSlotManager.Instance?.DeleteSlot(_data.slotIndex);
            Destroy(gameObject);
        }
    }
}
