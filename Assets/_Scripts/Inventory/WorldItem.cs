using UnityEngine;

public class WorldItem : MonoBehaviour
{
    public ItemData itemData;
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