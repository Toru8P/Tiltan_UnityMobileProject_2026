using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace _Scripts.MainGame.Inventory
{
    public class ArmorSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private ArmorSlot slotType;
        [SerializeField] private Image icon;

        private void Start()
        {
            if (PlayerArmorManager.Instance != null)
            {
                PlayerArmorManager.Instance.OnArmorChanged += HandleArmorChanged;
                Refresh(PlayerArmorManager.Instance.GetEquippedItem(slotType));
            }
        }

        private void OnDestroy()
        {
            if (PlayerArmorManager.Instance != null)
                PlayerArmorManager.Instance.OnArmorChanged -= HandleArmorChanged;
        }

        private void HandleArmorChanged(ArmorSlot slot, ItemData item)
        {
            if (slot == slotType)
            {
                Refresh(item);
            }
        }

        public void Refresh(ItemData item)
        {
            if (item != null)
            {
                if (icon != null)
                {
                    icon.sprite = item.icon;
                    icon.enabled = true;
                }
            }
            else
            {
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            int draggingIndex = InventoryUI.Instance.GetDraggingIndex();
            if (draggingIndex != -1)
            {
                var slot = InventoryManager.Instance.slots[draggingIndex];
                if (!slot.IsEmpty && slot.item.category == ItemCategory.Armor && slot.item.armorSlot == slotType)
                {
                    ItemData item = slot.item;
                    if (PlayerArmorManager.Instance.Equip(item))
                    {
                        slot.quantity--;
                        if (slot.quantity <= 0) slot.Clear();
                        InventoryManager.Instance.NotifySlotChanged(draggingIndex);
                    }
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                PlayerArmorManager.Instance.Unequip(slotType);
            }
        }
    }
}
