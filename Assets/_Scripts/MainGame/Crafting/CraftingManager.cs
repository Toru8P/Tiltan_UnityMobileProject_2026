using System.Collections.Generic;
using _Scripts.MainGame.Inventory;
using UnityEngine;

namespace _Scripts.MainGame.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }

        [SerializeField] private CraftingRecipe[] allRecipes;


        [Header("Combination Audio")]
        [SerializeField] private AudioClip tier2CombinationSound;
        [SerializeField] private AudioClip tier3CombinationSound;
        [SerializeField] private AudioClip tier4CombinationSound;
        [SerializeField] private AudioClip tier5CombinationSound;

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
                if (recipe == null) continue;

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
        public CraftingRecipe FindRecipe(List<ItemData> inputItems, bool exactMatch = true)
        {
            if (inputItems == null || inputItems.Count == 0) return null;

            foreach (var recipe in allRecipes)
            {
                // Skip empty slots left in the recipe list to avoid null dereferences.
                if (recipe == null) continue;

                // First check workstation requirement
                bool workstationMatch = !recipe.requiresWorkstation || recipe.requiredWorkstation == activeWorkstation;
                if (!workstationMatch) continue;

                if (IsMatch(recipe, inputItems, exactMatch))
                    return recipe;
            }
            return null;
        }

        private bool IsMatch(CraftingRecipe recipe, List<ItemData> inputItems, bool exactMatch = true)
        {
            // Group provided input items by ID
            Dictionary<string, int> inputCounts = new Dictionary<string, int>();
            foreach (var item in inputItems)
            {
                if (item == null) continue;
                if (inputCounts.ContainsKey(item.itemId))
                    inputCounts[item.itemId]++;
                else
                    inputCounts[item.itemId] = 1;
            }

            // Group recipe ingredients by ID
            Dictionary<string, int> recipeCounts = new Dictionary<string, int>();
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient.item == null) continue;
                if (recipeCounts.ContainsKey(ingredient.item.itemId))
                    recipeCounts[ingredient.item.itemId] += ingredient.quantity;
                else
                    recipeCounts[ingredient.item.itemId] = ingredient.quantity;
            }

            // If we have no items provided but recipe needs some, or vice versa
            if (recipeCounts.Count == 0 && inputCounts.Count == 0) return true;
            if (recipeCounts.Count == 0 || inputCounts.Count == 0) return false;

            // In exact match, we must have EXACTLY the number of unique item types
            if (exactMatch && recipeCounts.Count != inputCounts.Count) return false;
            // In non-exact match (merge shortcut), we must at least have all ingredient types
            if (!exactMatch && inputCounts.Count < recipeCounts.Count) return false;

            foreach (var pair in recipeCounts)
            {
                string itemId = pair.Key;
                int requiredQty = pair.Value;

                if (!inputCounts.ContainsKey(itemId)) return false;
                
                if (exactMatch)
                {
                    if (inputCounts[itemId] != requiredQty) return false;
                }
                else
                {
                    if (inputCounts[itemId] < requiredQty) return false;
                }
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
        /// Returns whether the selected stack can satisfy a recipe made only from that same item.
        /// </summary>
        public bool HasSameItemMergeRecipe(ItemData item, int availableQuantity)
        {
            return FindSameItemMergeRecipe(item, availableQuantity) != null;
        }

        /// <summary>
        /// Consumes the required quantity from one inventory stack and adds the recipe output.
        /// </summary>
        public bool TryMergeSameItemStack(int index)
        {
            if (InventoryManager.Instance == null || index < 0 || index >= InventoryManager.Instance.slots.Count)
                return false;

            InventorySlot slot = InventoryManager.Instance.slots[index];
            if (slot.IsEmpty) return false;

            CraftingRecipe recipe = FindSameItemMergeRecipe(slot.item, slot.quantity);
            if (recipe == null) return false;

            int requiredQuantity = 0;
            foreach (RecipeIngredient ingredient in recipe.ingredients)
                requiredQuantity += ingredient.quantity;

            slot.quantity -= requiredQuantity;
            if (slot.quantity <= 0) slot.Clear();

            int leftover = InventoryManager.Instance.AddItem(recipe.outputItem, recipe.outputQuantity);
            InventoryManager.Instance.NotifySlotChanged(index);
            PlayCombinationSound(recipe.outputItem);

            if (leftover > 0)
                Debug.LogWarning($"Inventory full - {leftover}x {recipe.outputItem.displayName} could not be added.");

            return true;
        }

        private CraftingRecipe FindSameItemMergeRecipe(ItemData item, int availableQuantity)
        {
            if (item == null || allRecipes == null) return null;

            foreach (CraftingRecipe recipe in allRecipes)
            {
                if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
                    continue;

                string ingredientId = null;
                int requiredQuantity = 0;
                bool validSameItemRecipe = true;

                foreach (RecipeIngredient ingredient in recipe.ingredients)
                {
                    if (ingredient.item == null || !IsSameItem(item, ingredient.item))
                    {
                        validSameItemRecipe = false;
                        break;
                    }

                    ingredientId ??= ingredient.item.itemId;
                    if (ingredient.item.itemId != ingredientId)
                    {
                        validSameItemRecipe = false;
                        break;
                    }

                    requiredQuantity += ingredient.quantity;
                }

                if (validSameItemRecipe && requiredQuantity >= 2 && availableQuantity >= requiredQuantity)
                    return recipe;
            }

            return null;
        }

        public bool TryMergeSlots(int index1, int index2)
        {
            var slot1 = InventoryManager.Instance.slots[index1];
            var slot2 = InventoryManager.Instance.slots[index2];

            if (slot1.IsEmpty || slot2.IsEmpty) 
            {
                Debug.Log($"TryMergeSlots: One of the slots is empty ({index1}:{slot1.IsEmpty}, {index2}:{slot2.IsEmpty})");
                return false;
            }

            Debug.Log($"TryMergeSlots: Attempting to merge {slot1.item.displayName} (x{slot1.quantity}) and {slot2.item.displayName} (x{slot2.quantity})");

            // Collect TOTAL items available in these two slots
            List<ItemData> inputs = new List<ItemData>();
            for (int i = 0; i < slot1.quantity; i++) inputs.Add(slot1.item);
            if (slot1 != slot2) 
            {
                for (int i = 0; i < slot2.quantity; i++) inputs.Add(slot2.item);
            }

            // We look for a recipe that can be satisfied by these two slots.
            // We use exactMatch=false because the user might have extra items in the stacks.
            var recipe = FindRecipe(inputs, false);
            
            if (recipe != null)
            {
                Debug.Log($"TryMergeSlots: Found potential recipe: {recipe.recipeName}");
                
                // Get unique ingredient types required by the recipe
                var uniqueRecipeIngredients = new HashSet<string>();
                foreach (var ing in recipe.ingredients)
                {
                    if (ing.item != null) uniqueRecipeIngredients.Add(ing.item.itemId);
                }

                int uniqueInSlots = (slot1.item.itemId == slot2.item.itemId) ? 1 : 2;

                if (uniqueRecipeIngredients.Count > uniqueInSlots) 
                {
                    Debug.Log($"TryMergeSlots: Recipe {recipe.recipeName} needs {uniqueRecipeIngredients.Count} unique ingredient types, but we only have {uniqueInSlots} types in these slots.");
                    return false;
                }

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
            
                PlayCombinationSound(recipe.outputItem);

                return true;
            }

            Debug.Log("TryMergeSlots: No matching recipe found for these ingredients.");
            return false;
        }

        private void PlayCombinationSound(ItemData outputItem)
        {
            if (outputItem == null || outputItem.tier < 2 || outputItem.tier > 5) return;
            if (SingletonPoint.Instance == null || SingletonPoint.Instance.AudioManager == null)
            {
                Debug.LogWarning($"CraftingManager: Cannot play tier {outputItem.tier} combination sound because AudioManager is unavailable.");
                return;
            }

            AudioClip sound = outputItem.tier switch
            {
                2 => tier2CombinationSound,
                3 => tier3CombinationSound,
                4 => tier4CombinationSound,
                5 => tier5CombinationSound,
                _ => null
            };

            if (sound == null)
            {
                Debug.LogWarning($"CraftingManager: No combination sound assigned for tier {outputItem.tier} output {outputItem.itemId}.");
                return;
            }

            SingletonPoint.Instance.AudioManager.PlaySFX(sound);
        }

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

            PlayCombinationSound(recipe.outputItem);

            return true;
        }
    }
}