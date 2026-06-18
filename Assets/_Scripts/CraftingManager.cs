using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [SerializeField] private CraftingRecipe[] allRecipes;

    // Currently active workstation (null = inventory crafting)
    private string activeWorkstation = null;

    public event System.Action OnCraftingContextChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this when player interacts with a machine
    public void OpenWorkstation(string workstationId)
    {
        activeWorkstation = workstationId;
        OnCraftingContextChanged?.Invoke();
    }

    // Call this when player opens inventory crafting tab
    public void OpenInventoryCrafting()
    {
        activeWorkstation = null;
        OnCraftingContextChanged?.Invoke();
    }

    // Returns all recipes visible in current context
    public List<CraftingRecipe> GetAvailableRecipes()
    {
        var result = new List<CraftingRecipe>();
        foreach (var recipe in allRecipes)
        {
            bool workstationMatch = !recipe.requiresWorkstation ||
                                    recipe.requiredWorkstation == activeWorkstation;
            if (workstationMatch)
                result.Add(recipe);
        }
        return result;
    }

    /// <summary>
    /// Finds a recipe that matches the exact set of items provided.
    /// Useful for "Combination" UI where items are placed in specific slots.
    /// </summary>
    public CraftingRecipe FindRecipe(List<ItemData> inputItems)
    {
        if (inputItems == null || inputItems.Count == 0) return null;

        foreach (var recipe in allRecipes)
        {
            // First check workstation requirement
            bool workstationMatch = !recipe.requiresWorkstation || recipe.requiredWorkstation == activeWorkstation;
            if (!workstationMatch) continue;

            if (IsMatch(recipe, inputItems))
                return recipe;
        }
        return null;
    }

    private bool IsMatch(CraftingRecipe recipe, List<ItemData> inputItems)
    {
        // For "Combination" UI, we sum up the quantities of each item provided
        Dictionary<string, int> inputCounts = new Dictionary<string, int>();
        foreach (var item in inputItems)
        {
            if (item == null) continue;
            if (inputCounts.ContainsKey(item.itemId))
                inputCounts[item.itemId]++;
            else
                inputCounts[item.itemId] = 1;
        }

        // Compare with recipe ingredients
        if (recipe.ingredients.Length != inputCounts.Count) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) return false;
            if (!inputCounts.ContainsKey(ingredient.item.itemId)) return false;
            if (inputCounts[ingredient.item.itemId] != ingredient.quantity) return false;
        }

        return true;
    }

    public bool IsSameItem(ItemData a, ItemData b)
    {
        if (a == null || b == null) return false;
        if (a == b) return true;
        return a.itemId == b.itemId;
    }

    /// <summary>
    /// Attempts to merge items from two inventory slots.
    /// </summary>
    public bool TryMergeSlots(int index1, int index2)
    {
        var slot1 = InventoryManager.Instance.slots[index1];
        var slot2 = InventoryManager.Instance.slots[index2];

        if (slot1.IsEmpty || slot2.IsEmpty) return false;

        // Collect items for matching. 
        // We treat this as "merging the contents of these two slots"
        List<ItemData> inputs = new List<ItemData>();
        
        // If they are the same item, we are merging two stacks of the same thing.
        // We need to decide how many items we are "putting in".
        // Simple approach: Check if a recipe exists for (slot1.item x qty1 + slot2.item x qty2)
        // Or more likely: Check if a recipe exists that can be satisfied by these two slots.
        
        // Let's try matching with just 1 of each first, then if that fails, try with full quantities?
        // User said "dragging them on each other", which usually means "Combine these two".
        
        // Try all combinations of quantities? No, that's too much.
        // Let's try matching with the TOTAL quantities available in these two specific slots.
        if (slot1.item == slot2.item)
        {
            for (int q = 0; q < slot1.quantity + slot2.quantity; q++)
                inputs.Add(slot1.item);
        }
        else
        {
            for (int q = 0; q < slot1.quantity; q++) inputs.Add(slot1.item);
            for (int q = 0; q < slot2.quantity; q++) inputs.Add(slot2.item);
        }

        var recipe = FindRecipe(inputs);
        if (recipe != null)
        {
            // Consume specific quantities from these slots
            foreach (var ingredient in recipe.ingredients)
            {
                int toConsume = ingredient.quantity;
                
                // Remove from slot1 first
                if (IsSameItem(slot1.item, ingredient.item))
                {
                    int take = Mathf.Min(slot1.quantity, toConsume);
                    slot1.quantity -= take;
                    toConsume -= take;
                    if (slot1.quantity <= 0) slot1.Clear();
                }

                // Then from slot2
                if (toConsume > 0 && IsSameItem(slot2.item, ingredient.item))
                {
                    int take = Mathf.Min(slot2.quantity, toConsume);
                    slot2.quantity -= take;
                    toConsume -= take;
                    if (slot2.quantity <= 0) slot2.Clear();
                }
            }

            // Add output
            InventoryManager.Instance.AddItem(recipe.outputItem, recipe.outputQuantity);
            
            // Notify UI
            InventoryManager.Instance.NotifySlotChanged(index1);
            InventoryManager.Instance.NotifySlotChanged(index2);
            
            Debug.Log($"Merged items into {recipe.outputItem.displayName}");
            return true;
        }

        return false;
    }

    // Returns true if player has all ingredients
public bool CanCraft(CraftingRecipe recipe)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("CraftingManager: InventoryManager.Instance is null");
            return false;
        }

        if (recipe == null)
        {
            Debug.LogError("CraftingManager: recipe is null");
            return false;
        }

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null)
            {
                Debug.LogWarning($"CraftingManager: Ingredient item is null in recipe {recipe.recipeName}");
                continue;
            }

            int have = InventoryManager.Instance.CountItem(ingredient.item);
            if (have < ingredient.quantity)
            {
                // Only log if we are actually trying to craft, or if we need diagnostic info
                // Debug.Log($"CraftingManager: Missing ingredient {ingredient.item.displayName}. Have {have}, need {ingredient.quantity}");
                return false;
            }
        }
        return true;
    }

        // Attempts to craft - returns true on success
    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null) return false;

        Debug.Log($"CraftingManager: Attempting to craft {recipe.recipeName}");

        if (!CanCraft(recipe))
        {
            Debug.Log($"CraftingManager: Cannot craft {recipe.recipeName} - requirements not met");
            return false;
        }

        foreach (var ingredient in recipe.ingredients)
            InventoryManager.Instance.RemoveItem(ingredient.item, ingredient.quantity);

        int leftover = InventoryManager.Instance.AddItem(recipe.outputItem, recipe.outputQuantity);

        if (leftover > 0)
            Debug.LogWarning($"Inventory full - {leftover}x {recipe.outputItem.displayName} dropped");

        Debug.Log($"CraftingManager: Successfully crafted {recipe.recipeName}");
        return true;
    }
}