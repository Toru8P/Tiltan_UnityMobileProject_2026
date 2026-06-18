using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Slot References")]
    public Image slotBackground;
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Image categoryDot;
    public Image selectionBorder;

    [Header("Slot Colors")]
    public Color emptySlotColor = new Color(0.133f, 0.121f, 0.101f, 1f); // #221F1A
    public Color filledSlotColor = new Color(0.180f, 0.164f, 0.133f, 1f); // #2E2A22
    public Color selectedSlotColor = new Color(0.207f, 0.180f, 0.125f, 1f); // #352E20

    [Header("Category Dot Colors")]
    public Color resourceColor = new Color(0.35f, 0.60f, 0.35f, 1f);
    public Color foodColor = new Color(0.78f, 0.63f, 0.13f, 1f);
    public Color toolColor = new Color(0.29f, 0.48f, 0.69f, 1f);
    public Color weaponColor = new Color(0.75f, 0.31f, 0.25f, 1f);
    public Color armorColor = new Color(0.50f, 0.47f, 0.87f, 1f);
    public Color defaultColor = new Color(0.42f, 0.40f, 0.38f, 1f);

    private int slotIndex;
    private bool isSelected;
    private bool isMoving;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;

    public void Init(int index)
    {
        slotIndex = index;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // Add CanvasGroup if missing for drag transparency/raycasting
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetSelected(false);
        SetMoving(false);
    }

    public void Refresh(InventorySlot slot)
    {
        bool hasItem = !slot.IsEmpty;

        slotBackground.color = isMoving ? selectedSlotColor
                             : isSelected ? selectedSlotColor
                             : hasItem ? filledSlotColor
                             : emptySlotColor;

        itemIcon.enabled = hasItem;
        categoryDot.enabled = hasItem;

        if (!hasItem)
        {
            quantityText.text = "";
            return;
        }

        itemIcon.sprite = slot.item.icon;
        itemIcon.color = Color.white;

        bool stackable = slot.item.maxStackSize > 1;
        quantityText.text = stackable ? slot.quantity.ToString() : "";

        categoryDot.color = GetCategoryColor(slot.item.category);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (selectionBorder != null)
        {
            selectionBorder.enabled = isSelected || isMoving;
            if (isMoving)
                selectionBorder.color = Color.cyan;
            else
                selectionBorder.color = new Color(0.784f, 0.659f, 0.290f, 0.706f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance.slots[slotIndex].IsEmpty) return;

        originalPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Bring to front
        transform.SetAsLastSibling();
        
        InventoryUI.Instance.OnBeginDrag(slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance.slots[slotIndex].IsEmpty) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = originalPosition;
        
        // Ensure it goes back to its correct sibling index if needed, 
        // but InitializeSlots re-parents them anyway.
        // Actually, LayoutGroup handles position, so anchoredPosition reset is good.

        InventoryUI.Instance.OnEndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryUI.Instance.OnDropOnSlot(slotIndex);
    }

    private Color GetCategoryColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Resource => resourceColor,
            ItemCategory.Food => foodColor,
            ItemCategory.Tool => toolColor,
            ItemCategory.Weapon => weaponColor,
            ItemCategory.Armor => armorColor,
            _ => defaultColor,
        };
    }
}
