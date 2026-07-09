using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.Loot
{
    public class WorldItem : Lootable
    {
        public void SpawnSetup(ItemData itemData, int quantity)
        {
            this.itemData = itemData;
            this.quantity = quantity;
        }
        
        public int quantity = 1;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            int leftover = InventoryManager.Instance.AddItem(itemData, quantity);

            if (leftover == 0)
                Destroy(gameObject); // fully picked up
            else
                quantity = leftover; // partially picked up (inventory was full)
        }
    }
}