using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject inventoryPanel;
    public Transform slotContainer;
    public GameObject slotPrefab;

    [Header("Details Section")]
    public GameObject detailsPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemCategoryText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStackText;
    public Image itemIcon;

    private List<InventorySlotUI> slotUIs = new();
    private int selectedIndex = -1;

    void Start()
    {
        InitializeSlots();
        inventoryPanel.SetActive(false);
        detailsPanel.SetActive(false);

        InventoryManager.Instance.OnSlotChanged += UpdateSlot;
    }

    private void InitializeSlots()
    {
        // Clear existing children
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        for (int i = 0; i < InventoryManager.Instance.slots.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Init(i);
            
            Button btn = slotUI.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnSlotClicked(slotUI));
            }

            slotUIs.Add(slotUI);
            slotUI.Refresh(InventoryManager.Instance.slots[i]);
        }
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        if (inventoryPanel.activeSelf)
        {
            RefreshAll();
        }
    }

    private void OnSlotClicked(InventorySlotUI slotUI)
    {
        int index = slotUIs.IndexOf(slotUI);
        selectedIndex = index;
        UpdateDetails(index);
    }

    private void UpdateSlot(int index)
    {
        if (index >= 0 && index < slotUIs.Count)
        {
            slotUIs[index].Refresh(InventoryManager.Instance.slots[index]);
            if (selectedIndex == index) UpdateDetails(index);
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].Refresh(InventoryManager.Instance.slots[i]);
        }
    }

    private void UpdateDetails(int index)
    {
        var slot = InventoryManager.Instance.slots[index];
        if (slot.IsEmpty)
        {
            detailsPanel.SetActive(false);
            return;
        }

        detailsPanel.SetActive(true);
        if (itemNameText != null) itemNameText.text = slot.item.displayName;
        if (itemCategoryText != null) itemCategoryText.text = slot.item.category.ToString();
        if (itemDescriptionText != null) itemDescriptionText.text = slot.item.description;
        if (itemStackText != null) itemStackText.text = $"Stack {slot.quantity}/{slot.item.maxStackSize}";
        
        if (itemIcon != null) 
        {
            itemIcon.sprite = slot.item.icon;
            itemIcon.enabled = slot.item.icon != null;
        }
    }

    public void DropOne()
    {
        if (selectedIndex == -1) return;
        var item = InventoryManager.Instance.slots[selectedIndex].item;
        if (item != null)
        {
            InventoryManager.Instance.RemoveItem(item, 1);
        }
    }

    public void DropAll()
    {
        if (selectedIndex == -1) return;
        var item = InventoryManager.Instance.slots[selectedIndex].item;
        if (item != null)
        {
            int qty = InventoryManager.Instance.slots[selectedIndex].quantity;
            InventoryManager.Instance.RemoveItem(item, qty);
        }
    }
}

