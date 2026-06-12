using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private int slotCount = 24;
    public List<InventorySlot> slots = new();

    public event Action<int> OnSlotChanged;
    public event Action OnInventoryFull;

    void Awake()
    {
        if (Instance != null) 
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
    }

    public int AddItem(ItemData item, int qty = 1)
    {
        int remaining = qty;

        foreach (var slot in slots)
        {
            if (remaining <= 0) 
                break;
            if (!slot.CanStack(item)) 
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
            
            if (slot.item != item) 
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

    public bool HasItem(ItemData item, int qty = 1)
    {
        int count = 0;
        
        foreach (var slot in slots)
            if (slot.item == item) count += slot.quantity;
        
        return count >= qty;
    }

    public int CountItem(ItemData item)
    {
        int count = 0;
        
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.item == item) count += slot.quantity;
        
        return count;
    }

    public void SwapSlots(int a, int b)
    {
        (slots[a], slots[b]) = (slots[b], slots[a]);
        
        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);

    }
}