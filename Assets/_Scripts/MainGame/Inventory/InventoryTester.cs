using UnityEngine;

namespace _Scripts.MainGame.Inventory
{
    public class InventoryTester : MonoBehaviour
    {
        public ItemData testItem;
        public int testQuantity = 5;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
                InventoryManager.Instance.AddItem(testItem, testQuantity);

            if (Input.GetKeyDown(KeyCode.R))
                InventoryManager.Instance.RemoveItem(testItem, 1);
        }
    }
}