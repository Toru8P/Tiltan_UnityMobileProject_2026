using UnityEngine;
using _Scripts.MainGame.Inventory;

namespace _Scripts.MainGame.Inventory
{
    public class ArmorCheat : MonoBehaviour
    {
        // Proves the component is alive and its Update is running. If this line never appears in the
        // Console, the P key is not the problem — the script is not executing at all.
        private void Awake()
        {
            Debug.Log($"[ArmorCheat] Alive on '{gameObject.name}'. Press P to add the armor set.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[ArmorCheat] P pressed.");
                AddArmorSet();
            }
        }

        private void AddArmorSet()
        {
            // Path is relative to a Resources folder: Assets/Resources/Items/Armor.
            // If the assets ever move out of Resources this silently returns nothing, so warn instead.
            ItemData[] armorItems = Resources.LoadAll<ItemData>("Items/Armor");
            if (armorItems.Length == 0)
            {
                Debug.LogWarning("[ArmorCheat] No ItemData found at Resources/Items/Armor — " +
                                 "the armor assets must live under a folder named 'Resources'.");
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("[ArmorCheat] No InventoryManager in the scene.");
                return;
            }

            // AddItem returns the amount it could NOT place, so a full inventory silently drops the item.
            int added = 0, rejected = 0;
            foreach (var item in armorItems)
            {
                if (InventoryManager.Instance.AddItem(item, 1) == 0) added++;
                else rejected++;
            }

            Debug.Log($"[ArmorCheat] Found {armorItems.Length} armor assets — added {added}, no room for {rejected}.");
            DumpInventory();
        }

        // Prints the real contents of every slot, so "the item is missing" can be told apart
        // from "the item is there but the UI is not drawing it".
        private void DumpInventory()
        {
            var slots = InventoryManager.Instance.slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) continue;
                Debug.Log($"[ArmorCheat] slot {i}: {slots[i].item.displayName} x{slots[i].quantity} " +
                          $"(category {slots[i].item.category}, icon {(slots[i].item.icon == null ? "NULL" : "ok")})");
            }
        }
    }
}
