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