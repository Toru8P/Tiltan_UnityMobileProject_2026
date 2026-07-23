using System;
using _Scripts.MainGame.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.CharacterCreation
{
    // Drives a single save-slot row in the Load Game scroll list.
    // Rows are pooled and reused, so Bind() must fully reset the row's state each time.
    public class SaveSlotUI : MonoBehaviour
    {
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI playtimeText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;

        private SaveSlotData _data;
        private Action _onChanged;

        // Binds this (possibly reused) row to a slot. onChanged is invoked after a delete so the
        // owning list can refresh its pooled rows.
        public void Bind(SaveSlotData data, Action onChanged)
        {
            _data = data;
            _onChanged = onChanged;

            if (characterNameText) characterNameText.text = data.characterName;
            if (playtimeText) playtimeText.text = data.FormattedPlaytime();
            if (dateText) dateText.text = data.FormattedDate();

            // Clear any texture from a previous binding before the new one loads in.
            if (thumbnailImage) thumbnailImage.texture = null;

            // Re-bind buttons cleanly — reused rows would otherwise stack listeners.
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadClicked);
            if (deleteButton)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }

            // Cancel a pending thumbnail load from a previous binding, then start the new one.
            StopAllCoroutines();
            if (!string.IsNullOrEmpty(data.thumbnailFileName) && SaveSlotManager.Instance)
                StartCoroutine(SaveSlotManager.Instance.LoadThumbnail(data.thumbnailFileName, ApplyThumbnail));
        }

        private void ApplyThumbnail(Texture2D tex)
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
            _onChanged?.Invoke(); // let the list re-pool instead of destroying this row
        }
    }
}
