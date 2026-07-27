using System.Collections.Generic;
using _Scripts.MainGame.SaveLoad;
using TMPro;
using UnityEngine;

namespace _Scripts.CharacterCreation
{
    public class LoadGameScreenController : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private SaveSlotUI slotPrefab;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        private SaveSlotUI[] _rows;
        private bool _rowsBuilt;

        private void Awake()
        {
            BuildRows();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            BuildRows();
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
        }

        private void BuildRows()
        {
            if (_rowsBuilt || slotContainer == null) return;

            _rows = slotContainer.GetComponentsInChildren<SaveSlotUI>(true);
            if (_rows.Length == 0 && slotPrefab != null)
            {
                _rows = new SaveSlotUI[SaveSlotManager.MaxSaveSlots];
                for (int i = 0; i < _rows.Length; i++)
                {
                    SaveSlotUI row = Instantiate(slotPrefab, slotContainer);
                    row.name = $"SaveSlot_{i}";
                    _rows[i] = row;
                }
            }

            _rowsBuilt = true;
        }

        private void SetActiveRows(int count)
        {
            if (_rows == null) return;
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
