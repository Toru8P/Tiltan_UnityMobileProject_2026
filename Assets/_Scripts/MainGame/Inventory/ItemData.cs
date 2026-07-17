using UnityEngine;

namespace _Scripts.MainGame.Inventory
{
    public enum ItemCategory { Resource, Food, Tool, Weapon, Armor, Potion, Consumable, Bow }
    public enum ArmorSlot { None, Helmet, Chest, Shoulders, Gloves, Pants, Boots }
    public enum StatType { Attack, Defense, MovementSpeed, AttackSpeed }
    public enum ToolType { None, Axe, Pickaxe, Hammer, Shovel, Sickle, Hoe }

    [System.Serializable]
    public struct ItemStatModifier
{
        public StatType statType;
        public float flatAmount;
        public float percentageAmount; // 0.1f = 10%
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Survival/Item")]
    public class ItemData : ScriptableObject
    {
        public string itemId;         
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public ItemCategory category;
        public ArmorSlot armorSlot;
        public string armorModelPath;
        public Color armorColor = Color.white;
        public int maxStackSize = 100;
        public bool isConsumable;
        public int hungerRestore;
        public int healthRestore;
        public GameObject heldPrefab;
        public Vector3 holdPosition;
        public Vector3 holdRotation;

        [Header("Bow Settings")]
        public GameObject projectilePrefab;

        [Header("Tool Settings")]
        public ToolType toolType;
        public int tier = 1;
        public float effectiveness = 1f;

        [Header("Stats")]
        public ItemStatModifier[] statModifiers;
}
}