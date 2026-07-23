using System;
using System.Collections.Generic;
using _Scripts.MainGame.SaveLoad;
using UnityEngine;

namespace _Scripts.MainGame.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private int slotCount = 15;
        [SerializeField] private AudioClip pickupSound;
        public List<InventorySlot> slots = new();

        [Header("Save / Load")]
        [Tooltip("DEBUG ONLY: start with an empty inventory, ignoring (and overwriting) any existing save.")]
        [SerializeField] private bool createNewInventory = false;

        public event Action<int> OnSlotChanged;
        public event Action OnInventoryFull;

        // itemId -> ItemData, built once from all ItemData assets under a Resources folder.
        private Dictionary<string, ItemData> _itemsById;
        // Set whenever a slot changes; coalesces many changes in a frame into a single disk write.
        private bool _dirty;

        // Returns the shared save manager, or null (with a warning) if this scene isn't wired for saving.
        private SaveLoadManager SaveOrNull()
        {
            if (SingletonPoint.Instance == null)
            {
                Debug.LogWarning("[Inventory] No SingletonPoint in scene — inventory won't save/load.");
                return null;
            }
            if (SingletonPoint.Instance.SaveLoad == null)
            {
                Debug.LogWarning("[Inventory] SingletonPoint.SaveLoad is not assigned — inventory won't save/load.");
                return null;
            }
            return SingletonPoint.Instance.SaveLoad;
        }

        void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (slots.Count != slotCount)
            {
                slots.Clear();
                for (int i = 0; i < slotCount; i++)
                    slots.Add(new InventorySlot());
            }

            // General logic: load the saved inventory if one exists. The debug flag forces a fresh one.
            SaveLoadManager save = SaveOrNull();
            if (!createNewInventory && save != null && save.HasSaveFile && save.Current.hasInventory)
                Load(save.Current.inventory);

            // Auto-save on any slot change (all mutation paths funnel through OnSlotChanged).
            OnSlotChanged += _ => _dirty = true;
        }

        void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            SaveToFile();
        }

        public int AddItem(ItemData item, int qty = 1)
        {
            int startQty = qty;
            int remaining = qty;

            foreach (var slot in slots)
            {
                if (remaining <= 0) 
                    break;
                if (slot.item == null || !IsMatchingItem(slot.item, item) || slot.quantity >= item.maxStackSize) 
                    continue;
            
                int space = item.maxStackSize - slot.quantity;
                int toAdd = Mathf.Min(space, remaining);
                slot.quantity += toAdd;
                remaining -= toAdd;
            
                OnSlotChanged?.Invoke(slots.IndexOf(slot));
            }

            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (!slot.IsEmpty) continue;
            
                int toAdd = Mathf.Min(item.maxStackSize, remaining);
                slot.item = item;
                slot.quantity = toAdd;
                remaining -= toAdd;
                OnSlotChanged?.Invoke(slots.IndexOf(slot));
            }

            if (remaining < startQty && pickupSound)
            {
                SingletonPoint.Instance.AudioManager.PlaySFX(pickupSound);
            }

            if (remaining > 0) OnInventoryFull?.Invoke();
            return remaining;
        }

        public bool RemoveItem(ItemData item, int qty = 1)
        {
            if (!HasItem(item, qty)) return false;
            int remaining = qty;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = slots[i];
            
                if (slot.item == null || !IsMatchingItem(slot.item, item)) 
                    continue;
            
                int take = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= take;
                remaining -= take;
            
                if (slot.quantity <= 0) 
                    slot.Clear();
           
                OnSlotChanged?.Invoke(i);
            }
            return true;
        }

        private bool IsMatchingItem(ItemData a, ItemData b)
        {
            if (a == null || b == null) return false;
            // Check references first (fastest)
            if (a == b) return true;
            // Fallback to name-based (robust for instances/clones)
            string nameA = a.name.Replace("(Clone)", "").Trim();
            string nameB = b.name.Replace("(Clone)", "").Trim();
            return nameA == nameB;
        }

        public bool HasItem(ItemData item, int qty = 1)
        {
            int count = CountItem(item);
            return count >= qty;
        }

        public int CountItem(ItemData item)
        {
            if (item == null) return 0;
            int count = 0;
            foreach (var slot in slots)
            {
                if (slot.item != null && IsMatchingItem(slot.item, item))
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        public void SwapSlots(int a, int b)
        {
            (slots[a], slots[b]) = (slots[b], slots[a]);
        
            OnSlotChanged?.Invoke(a);
            OnSlotChanged?.Invoke(b);
        }

        public void NotifySlotChanged(int index)
        {
            if (index >= 0 && index < slots.Count)
                OnSlotChanged?.Invoke(index);
        }

        // ---------------------------------------------------------------------
        // Save / Load
        // ---------------------------------------------------------------------

        // Snapshots every slot (including empties, to preserve positions).
        public InventorySave BuildSaveData()
        {
            InventorySave data = new InventorySave();
            foreach (InventorySlot slot in slots)
            {
                data.slots.Add(new InventorySlotSave
                {
                    itemId = slot.item != null ? slot.item.itemId : string.Empty,
                    quantity = slot.item != null ? slot.quantity : 0
                });
            }
            return data;
        }

        // Writes the inventory section into the shared save and persists the whole file.
        [ContextMenu("Save Inventory")]
        public void SaveToFile()
        {
            SaveLoadManager save = SaveOrNull();
            if (save == null) return;

            save.Current.inventory = BuildSaveData();
            save.Current.hasInventory = true;
            save.MarkDirty(); // staged in memory; SaveLoadManager flushes to disk on its interval
        }

        // Restores slots from saved data, resolving itemIds back to ItemData assets.
        public void Load(InventorySave data)
        {
            if (data == null) return;
            EnsureItemRegistry();

            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                slot.Clear();

                if (i >= data.slots.Count) continue;

                InventorySlotSave saved = data.slots[i];
                if (string.IsNullOrEmpty(saved.itemId) || saved.quantity <= 0) continue;

                if (_itemsById.TryGetValue(saved.itemId, out ItemData item) && item != null)
                {
                    slot.item = item;
                    slot.quantity = saved.quantity;
                }
                else
                {
                    Debug.LogWarning($"[Inventory] Saved item '{saved.itemId}' not found; slot {i} left empty.");
                }

                OnSlotChanged?.Invoke(i);
            }
        }

        // Builds the itemId -> ItemData lookup from all ItemData assets under Resources (once).
        private void EnsureItemRegistry()
        {
            if (_itemsById != null) return;
            _itemsById = new Dictionary<string, ItemData>();

            foreach (ItemData item in Resources.LoadAll<ItemData>(string.Empty))
            {
                if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
                if (!_itemsById.ContainsKey(item.itemId)) _itemsById[item.itemId] = item;
                else Debug.LogWarning($"[Inventory] Duplicate itemId '{item.itemId}' on asset '{item.name}'; keeping first.");
            }
        }

        // Deletes the active slot's world save file. Debug helper.
        [ContextMenu("Delete Game Save File (All)")]
        private void DeleteSaveFile()
        {
            SaveOrNull()?.Delete();
            Debug.Log("[Inventory] Deleted world save for active slot.");
        }
    }
}
