using UnityEngine;
using System;
using System.Collections.Generic;

namespace _Scripts.MainGame.Inventory
{
    public class PlayerArmorManager : MonoBehaviour
    {
        public static PlayerArmorManager Instance { get; private set; }

        [SerializeField] private Transform armorParent;

        private Dictionary<ArmorSlot, ItemData> equippedArmor = new Dictionary<ArmorSlot, ItemData>();
        private Dictionary<ArmorSlot, GameObject> activeModels = new Dictionary<ArmorSlot, GameObject>();

        public event Action<ArmorSlot, ItemData> OnArmorChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            foreach (ArmorSlot slot in Enum.GetValues(typeof(ArmorSlot)))
            {
                if (slot != ArmorSlot.None)
                {
                    equippedArmor[slot] = null;
                    activeModels[slot] = null;
                }
            }
        }

        // The armor meshes live on the character prefab and are authored ACTIVE, so without this
        // the player starts already wearing the full set and equipping appears to do nothing.
        private void Start()
        {
            HideAllArmorModels();
        }

        // Walks every armor ItemData and deactivates the mesh it points at, giving a clean "no armor" start.
        private void HideAllArmorModels()
        {
            if (armorParent == null) return;

            foreach (ItemData item in Resources.LoadAll<ItemData>(string.Empty))
            {
                if (item == null || item.category != ItemCategory.Armor) continue;
                if (string.IsNullOrEmpty(item.armorModelPath)) continue;

                Transform modelTransform = armorParent.Find(item.armorModelPath);
                if (modelTransform != null)
                    modelTransform.gameObject.SetActive(false);
                else
                    Debug.LogWarning($"[PlayerArmorManager] '{item.displayName}' points at model path " +
                                     $"'{item.armorModelPath}', which does not exist under '{armorParent.name}'.");
            }
        }

        public bool Equip(ItemData item)
        {
            if (item == null || item.category != ItemCategory.Armor || item.armorSlot == ArmorSlot.None)
                return false;

            ArmorSlot slot = item.armorSlot;
            ItemData oldItem = equippedArmor[slot];

            if (oldItem != null)
            {
                // AddItem returns what it could NOT place. Swapping into a full inventory used to
                // drop the old piece on the floor of memory, so refuse the swap instead.
                if (InventoryManager.Instance.AddItem(oldItem, 1) != 0)
                {
                    Debug.LogWarning($"[PlayerArmorManager] No inventory room to take off '{oldItem.displayName}' — " +
                                     $"'{item.displayName}' was not equipped.");
                    return false;
                }
                HideModel(slot);
            }

            equippedArmor[slot] = item;
            ShowModel(slot, item);
            OnArmorChanged?.Invoke(slot, item);
            
            return true;
        }

        // Returns true if the piece came off. It stays on when there is no inventory room for it,
        // which used to fail silently and look like the button was broken.
        public bool Unequip(ArmorSlot slot)
        {
            if (!equippedArmor.ContainsKey(slot) || equippedArmor[slot] == null)
                return false;

            ItemData item = equippedArmor[slot];
            if (InventoryManager.Instance.AddItem(item, 1) != 0)
            {
                Debug.LogWarning($"[PlayerArmorManager] Inventory is full — '{item.displayName}' stays equipped.");
                return false;
            }

            equippedArmor[slot] = null;
            HideModel(slot);
            OnArmorChanged?.Invoke(slot, null);
            return true;
        }

        private void ShowModel(ArmorSlot slot, ItemData item)
        {
            if (armorParent == null || string.IsNullOrEmpty(item.armorModelPath)) return;

            Transform modelTransform = armorParent.Find(item.armorModelPath);
            if (modelTransform != null)
            {
                modelTransform.gameObject.SetActive(true);
                activeModels[slot] = modelTransform.gameObject;

                Renderer r = modelTransform.GetComponent<Renderer>();
                if (r != null)
                {
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", item.armorColor);
                    mpb.SetColor("_Color", item.armorColor);
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        private void HideModel(ArmorSlot slot)
        {
            if (activeModels.ContainsKey(slot) && activeModels[slot] != null)
            {
                activeModels[slot].SetActive(false);
                activeModels[slot] = null;
            }
        }

        public ItemData GetEquippedItem(ArmorSlot slot)
        {
            return equippedArmor.ContainsKey(slot) ? equippedArmor[slot] : null;
        }
    }
}
