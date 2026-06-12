using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    private int slotIndex;

    public void Init(int index) => slotIndex = index;

    public void Refresh(InventorySlot slot)
    {
        if (slot.IsEmpty)
        {
            iconImage.enabled = false;
            quantityText.text = "";
        }
        else
        {
            iconImage.sprite = slot.item.icon;
            iconImage.enabled = slot.item.icon != null;
            quantityText.text = slot.item.maxStackSize > 1 ? slot.quantity.ToString() : "";
        }
    }
}