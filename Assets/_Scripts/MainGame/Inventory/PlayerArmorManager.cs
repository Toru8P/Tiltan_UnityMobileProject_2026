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

        public bool Equip(ItemData item)
        {
            if (item == null || item.category != ItemCategory.Armor || item.armorSlot == ArmorSlot.None)
                return false;

            ArmorSlot slot = item.armorSlot;
            ItemData oldItem = equippedArmor[slot];

            if (oldItem != null)
            {
                InventoryManager.Instance.AddItem(oldItem, 1);
                HideModel(slot);
            }

            equippedArmor[slot] = item;
            ShowModel(slot, item);
            OnArmorChanged?.Invoke(slot, item);
            
            return true;
        }

        public void Unequip(ArmorSlot slot)
        {
            if (!equippedArmor.ContainsKey(slot) || equippedArmor[slot] == null)
                return;

            ItemData item = equippedArmor[slot];
            if (InventoryManager.Instance.AddItem(item, 1) == 0)
            {
                equippedArmor[slot] = null;
                HideModel(slot);
                OnArmorChanged?.Invoke(slot, null);
            }
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
