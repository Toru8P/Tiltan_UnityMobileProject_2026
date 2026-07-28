using _Scripts.MainGame.Inventory;
using _Scripts.MainGame.UI;
using UnityEngine;

namespace _Scripts.MainGame.Loot
{
    public class WorldItem : Lootable
    {
        public int quantity = 1;

        public void SpawnSetup(ItemData itemData, int quantity)
        {
            this.itemData = itemData;
            this.quantity = quantity;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (itemData == null || InventoryManager.Instance == null) return;

            int leftover = InventoryManager.Instance.AddItem(itemData, quantity);
            int addedQuantity = quantity - leftover;

            if (addedQuantity > 0 && IndicatorManager.Instance != null)
            {
                string itemName = string.IsNullOrWhiteSpace(itemData.displayName) ? itemData.name : itemData.displayName;
                IndicatorManager.Instance.SpawnItemPickup(other.transform.position, addedQuantity, itemName);
            }

            if (leftover == 0)
                Destroy(gameObject);
            else
                quantity = leftover;
        }
    }
}
