using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingRecipeEntryUI : MonoBehaviour
{
    [SerializeField] private Image recipeIcon;
    [SerializeField] private TextMeshProUGUI recipeName;
    [SerializeField] private TextMeshProUGUI ingredientText;
    [SerializeField] private Button craftButton;
    [SerializeField] private Color craftableColor = new Color(0.21f, 0.56f, 0.31f);
    [SerializeField] private Color unCraftableColor = new Color(0.35f, 0.32f, 0.28f);

    private CraftingRecipe recipe;

    public void Setup(CraftingRecipe r, bool canCraft)
    {
        recipe = r;
        recipeIcon.sprite = r.icon;
        recipeName.text = r.recipeName;
        ingredientText.text = BuildIngredientString(r);
        craftButton.onClick.AddListener(OnCraftPressed);
        SetCraftable(canCraft);
    }

    public void SetCraftable(bool canCraft)
    {
        craftButton.interactable = canCraft;
        craftButton.image.color = canCraft ? craftableColor : unCraftableColor;
    }

    void OnCraftPressed()
    {
        bool success = CraftingManager.Instance.TryCraft(recipe);
        if (!success) Debug.Log("Craft failed — ingredients missing");
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