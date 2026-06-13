using UnityEngine;

[System.Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public int quantity;
}

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Survival/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public string recipeName;
    public Sprite icon; // can reuse output item icon
    public RecipeIngredient[] ingredients;
    public ItemData outputItem;
    public int outputQuantity = 1;
    public bool requiresWorkstation;
    public string requiredWorkstation; // e.g. "grinding_stone", empty if none
}