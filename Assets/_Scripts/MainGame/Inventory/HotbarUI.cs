using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.Inventory
{
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotParent;
        [SerializeField] private int maxSlots = 8;

        private List<InventorySlotUI> slots = new();
        private List<ItemData> currentItems = new();
        private int selectedHotbarIndex = 0;

        public System.Action<ItemData> OnItemSelected;

        private void Start()
        {
            InitializeHotbar();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnSlotChanged += (idx) => RefreshHotbar();
            }
            RefreshHotbar();
            SelectSlot(0);
        }

        private void InitializeHotbar()
        {
            foreach (Transform child in slotParent) 
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
            slots.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                GameObject obj = Instantiate(slotPrefab, slotParent);
                obj.name = $"HotbarSlot_{i}";
                InventorySlotUI slotUI = obj.GetComponent<InventorySlotUI>();
                slotUI.Init(i);
            
                Button btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    int index = i;
                    btn.onClick.AddListener(() => OnSlotClicked(index));
                }

                slots.Add(slotUI);
            }
        }

        public void RefreshHotbar()
        {
            if (InventoryManager.Instance == null) return;

            // Find all inventory slots that contain Tools, Food, or Potions
            var eligibleSlots = InventoryManager.Instance.slots
                .Where(s => !s.IsEmpty)
                .Where(s => IsHotbarCategory(s.item.category))
                .Take(maxSlots)
                .ToList();

            currentItems.Clear();

            for (int i = 0; i < maxSlots; i++)
            {
                if (i < eligibleSlots.Count)
                {
                    var slot = eligibleSlots[i];
                    slots[i].SetSelected(i == selectedHotbarIndex);
                    slots[i].Refresh(slot);
                    currentItems.Add(slot.item);
                
                    // If this is the selected slot, ensure it's equipped
                    if (i == selectedHotbarIndex)
                    {
                        OnItemSelected?.Invoke(slot.item);
                    }
                }
                else
                {
                    slots[i].SetSelected(i == selectedHotbarIndex);
                    slots[i].Refresh(new InventorySlot());
                    currentItems.Add(null);

                    if (i == selectedHotbarIndex)
                    {
                        OnItemSelected?.Invoke(null);
                    }
                }
            }
        }

        private void OnSlotClicked(int index)
        {
            SelectSlot(index);
        
            if (index < 0 || index >= currentItems.Count) return;
            ItemData item = currentItems[index];
            if (item != null)
            {
                Debug.Log($"Hotbar selected: {item.displayName}");
                // Optional: Call UseItem or Equip logic here
            }
        }

        private void SelectSlot(int index)
        {
            selectedHotbarIndex = index;
            RefreshHotbar();

            if (index >= 0 && index < currentItems.Count)
            {
                OnItemSelected?.Invoke(currentItems[index]);
            }
        }

        private bool IsHotbarCategory(ItemCategory category)
        {
            return category == ItemCategory.Tool || 
                   category == ItemCategory.Food || 
                   category == ItemCategory.Potion;
        }
    }
}
