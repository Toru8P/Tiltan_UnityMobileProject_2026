using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRecipeEntryUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image recipeIcon;
    [SerializeField] private TextMeshProUGUI recipeName;
    [SerializeField] private TextMeshProUGUI ingredientText;
    [SerializeField] private UnityEngine.UI.Button craftButton;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Color craftableColor = new Color(0.29f, 0.87f, 0.50f); // #4ade80
    [SerializeField] private Color unCraftableColor = new Color(0.29f, 0.27f, 0.22f); // #4a4438

    public CraftingRecipe recipe { get; private set; }

    public void Setup(CraftingRecipe r, bool canCraft)
    {
        recipe = r;
        recipeIcon.sprite = r.icon;
        recipeName.text = r.recipeName;
        if (buttonSprite != null) craftButton.image.sprite = buttonSprite;
        RefreshDisplay();
        craftButton.onClick.AddListener(OnCraftPressed);
    }

    public void RefreshDisplay()
    {
        if (recipe == null) return;
        ingredientText.text = BuildIngredientString(recipe);
        bool canCraft = CraftingManager.Instance.CanCraft(recipe);
        SetCraftable(canCraft);
    }

    public void SetCraftable(bool canCraft)
    {
        if (recipe != null && recipe.name == "RopeRed")
        {
            // Debug.Log($"SetCraftable for Rope: {canCraft}");
        }
        craftButton.interactable = canCraft;
        craftButton.image.color = canCraft ? craftableColor : unCraftableColor;
    }

    public void UpdateDisplay(CraftingRecipe r, bool canCraft)
    {
        recipe = r;
        ingredientText.text = BuildIngredientString(r);
        SetCraftable(canCraft);
    }

    void OnCraftPressed()
    {
        if (recipe == null) return;
        Debug.Log($"CraftButton clicked for: {recipe.recipeName}");
        
        bool success = CraftingManager.Instance.TryCraft(recipe);
        if (!success)
        {
            Debug.Log($"Craft failed - ingredients missing for {recipe.recipeName}");
        }
        else
        {
            Debug.Log($"Craft succeeded for: {recipe.recipeName}");
        }
        
        // Refresh all crafting entries to update counts
        if (CraftingUI.Instance != null)
        {
            CraftingUI.Instance.RefreshCraftability();
        }
        else
        {
            RefreshDisplay();
        }
    }

    string BuildIngredientString(CraftingRecipe r)
    {
        var inv = InventoryManager.Instance;
        var parts = new System.Text.StringBuilder();
        foreach (var ing in r.ingredients)
        {
            int have = inv.CountItem(ing.item);
            parts.Append($"{ing.item.displayName} {have}/{ing.quantity}  ");
        }
        return parts.ToString().TrimEnd();
    }
}