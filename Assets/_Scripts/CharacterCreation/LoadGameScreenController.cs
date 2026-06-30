using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace _Scripts.CharacterCreation
{
    // Scans the save directory and populates a ScrollView with save slot entries.
    // Requires a SaveManager in the scene. Assign slotPrefab (SaveSlotUI prefab) and
    // slotContainer (the ScrollView Content transform) in the inspector.
    public class LoadGameScreenController : MonoBehaviour
    {
        [SerializeField] private SaveSlotUI slotPrefab;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            foreach (Transform child in slotContainer)
                Destroy(child.gameObject);

            if (SaveManager.Instance == null)
            {
                ShowEmpty(true);
                return;
            }

            List<SaveSlotData> slots = SaveManager.Instance.LoadAllSlots();

            ShowEmpty(slots.Count == 0);

            foreach (var data in slots)
            {
                var entry = Instantiate(slotPrefab, slotContainer);
                entry.Init(data);
            }
        }

        private void ShowEmpty(bool isEmpty)
        {
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(isEmpty);
        }
    }
}
