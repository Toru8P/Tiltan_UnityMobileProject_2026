using System.Collections.Generic;
using _Scripts.MainGame.SaveLoad;
using TMPro;
using UnityEngine;

namespace _Scripts.CharacterCreation
{
    // Shows one row per save slot. The rows are authored directly in the scene under slotContainer
    // (no prefab, no instantiation): the controller just collects them once, then binds + activates
    // the ones with a save and deactivates the rest. Add/remove rows by editing the scene hierarchy.
    public class LoadGameScreenController : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        private SaveSlotUI[] _rows;

        private void Awake()
        {
            // Grab the slot rows placed under the container (include inactive ones).
            _rows = slotContainer.GetComponentsInChildren<SaveSlotUI>(true);
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!SaveSlotManager.Instance)
            {
                ShowEmpty(true);
                SetActiveRows(0);
                return;
            }

            List<SaveSlotData> slots = SaveSlotManager.Instance.LoadAllSlots();
            if (slots.Count > SaveSlotManager.MaxSaveSlots)
                slots.RemoveRange(SaveSlotManager.MaxSaveSlots, slots.Count - SaveSlotManager.MaxSaveSlots);
            ShowEmpty(slots.Count == 0);

            for (int i = 0; i < _rows.Length; i++)
            {
                bool hasData = i < slots.Count;
                _rows[i].gameObject.SetActive(hasData);
                if (hasData) _rows[i].Bind(slots[i], Refresh);
            }

            if (slots.Count > _rows.Length)
                Debug.LogWarning($"[LoadGame] {slots.Count} saves but only {_rows.Length} slot rows in the scene; extras won't show.");
        }

        private void SetActiveRows(int count)
        {
            for (int i = 0; i < _rows.Length; i++)
                _rows[i].gameObject.SetActive(i < count);
        }

        private void ShowEmpty(bool isEmpty)
        {
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(isEmpty);
        }
    }
}
