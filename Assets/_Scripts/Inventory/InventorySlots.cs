[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;
    
    public bool CanStack(ItemData other) => item != null && item == other && quantity < item.maxStackSize;

    public void Clear() 
    { 
        item = null; 
        quantity = 0; 
    }
}