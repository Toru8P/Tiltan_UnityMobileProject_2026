using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace _Scripts.MainGame.Inventory
{
    public class ArmorSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private ArmorSlot slotType;
        [SerializeField] private Image icon;
        // Colour used when an equipped item has no icon sprite. Without it the icon Image draws a
        // plain white quad on top of a plain white slot background, so equipping looks like nothing
        // happened. This is a fallback, not a fix — assign icons on the ItemData assets.
        [SerializeField] private Color missingIconColor = new Color(0.50f, 0.47f, 0.87f, 1f);

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
            if (icon == null) return;

            if (item == null)
            {
                icon.sprite = null;
                icon.enabled = false;
                return;
            }

            icon.sprite = item.icon;
            icon.enabled = true;
            icon.color = item.icon != null ? Color.white : missingIconColor;

            if (item.icon == null)
                Debug.LogWarning($"[ArmorSlotUI] '{item.displayName}' has no icon sprite; " +
                                 $"showing a placeholder block in the {slotType} slot.");
        }

        public void OnDrop(PointerEventData eventData)
        {
            int draggingIndex = InventoryUI.Instance.GetDraggingIndex();
            if (draggingIndex == -1)
            {
                Debug.LogWarning($"[ArmorSlotUI] Drop on {slotType} slot, but nothing is being dragged.");
                return;
            }

            var slot = InventoryManager.Instance.slots[draggingIndex];
            if (slot.IsEmpty) return;

            // A slot only accepts armor whose armorSlot matches this slot's type.
            if (slot.item.category != ItemCategory.Armor || slot.item.armorSlot != slotType)
            {
                Debug.Log($"[ArmorSlotUI] '{slot.item.displayName}' " +
                          $"(category {slot.item.category}, armorSlot {slot.item.armorSlot}) " +
                          $"does not fit the {slotType} slot.");
                return;
            }

            ItemData item = slot.item;
            if (PlayerArmorManager.Instance == null)
            {
                Debug.LogWarning("[ArmorSlotUI] No PlayerArmorManager in the scene — cannot equip.");
                return;
            }

            if (PlayerArmorManager.Instance.Equip(item))
            {
                slot.quantity--;
                if (slot.quantity <= 0) slot.Clear();
                InventoryManager.Instance.NotifySlotChanged(draggingIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (PlayerArmorManager.Instance == null) return;

            // Right-click stays as a desktop shortcut, but this is a mobile project and there is no
            // right button on a touchscreen — so a normal tap opens the details panel, where the
            // action button reads UNEQUIP.
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                PlayerArmorManager.Instance.Unequip(slotType);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left && InventoryUI.Instance != null)
            {
                InventoryUI.Instance.ShowEquippedArmorDetails(slotType);
            }
        }
    }
}
