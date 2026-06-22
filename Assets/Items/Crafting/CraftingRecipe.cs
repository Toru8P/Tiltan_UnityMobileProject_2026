using _Scripts.MainGame.Inventory;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Survival/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Sprite icon;
    public RecipeIngredient[] ingredients;
    public ItemData outputItem;
    public int outputQuantity = 1;
    public bool requiresWorkstation;
    public string requiredWorkstation;
}
