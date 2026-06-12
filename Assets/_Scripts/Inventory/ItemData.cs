using UnityEngine;

public enum ItemCategory { Resource, Food, Tool, Weapon, Armor, Potion, Consumable }

[CreateAssetMenu(fileName = "NewItem", menuName = "Survival/Item")]
public class ItemData : ScriptableObject
{
    public string itemId;         
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemCategory category;
    public int maxStackSize = 100;
    public bool isConsumable;
    public int hungerRestore;
    public GameObject heldPrefab;
    public Vector3 holdRotation;
}