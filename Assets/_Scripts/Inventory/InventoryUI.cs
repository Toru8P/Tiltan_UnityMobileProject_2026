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
    public Image itemCategoryBg;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemStackText;
    public Image itemIcon;
    public Button useButton;

    [Header("Unified UI Integration")]
    [SerializeField] private GameObject inventoryContent;

    public static InventoryUI Instance { get; private set; }

    private List<InventorySlotUI> slotUIs = new();
    private int selectedIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        InitializeSlots();
        // inventoryPanel.SetActive(false); // Managed by CraftingUI
        detailsPanel.SetActive(false);

        InventoryManager.Instance.OnSlotChanged += UpdateSlot;
        if (useButton != null) useButton.onClick.AddListener(UseItem);
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
        if (CraftingUI.Instance != null) CraftingUI.Instance.ToggleMenu();
    }

    public void ShowContent(bool show)
    {
        if (inventoryContent != null) inventoryContent.SetActive(show);
        if (!show && detailsPanel != null) detailsPanel.SetActive(false);
        if (show) RefreshAll();
    }

    public void HideInventory()
    {
        if (CraftingUI.Instance != null) CraftingUI.Instance.Hide();
        else inventoryPanel.SetActive(false);
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
        if (itemCategoryText != null) 
        {
            itemCategoryText.text = slot.item.category.ToString().ToUpper();
            UpdateCategoryBadge(slot.item.category);
        }
        if (itemDescriptionText != null) itemDescriptionText.text = slot.item.description;
        if (itemStackText != null) itemStackText.text = $"Stack {slot.quantity}/{slot.item.maxStackSize}";
        
        if (itemIcon != null) 
        {
            itemIcon.sprite = slot.item.icon;
            itemIcon.enabled = slot.item.icon != null;
        }
    }

    private void UpdateCategoryBadge(ItemCategory category)
    {
        if (itemCategoryText == null || itemCategoryBg == null) return;

        Color bgColor, textColor;
        switch (category)
        {
            case ItemCategory.Resource:
                ColorUtility.TryParseHtmlString("#1A2E1A", out bgColor);
                ColorUtility.TryParseHtmlString("#5A9A5A", out textColor);
                break;
            case ItemCategory.Food:
                ColorUtility.TryParseHtmlString("#2E2010", out bgColor);
                ColorUtility.TryParseHtmlString("#C8A020", out textColor);
                break;
            case ItemCategory.Tool:
                ColorUtility.TryParseHtmlString("#101828", out bgColor);
                ColorUtility.TryParseHtmlString("#4A7AB0", out textColor);
                break;
            case ItemCategory.Weapon:
                ColorUtility.TryParseHtmlString("#2E1010", out bgColor);
                ColorUtility.TryParseHtmlString("#C05040", out textColor);
                break;
            default:
                bgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                textColor = Color.white;
                break;
        }

        itemCategoryBg.color = bgColor;
        itemCategoryText.color = textColor;
    }

    public void UseItem()
    {
        if (selectedIndex == -1) return;
        var slot = InventoryManager.Instance.slots[selectedIndex];
        if (!slot.IsEmpty)
        {
            // Assuming UseItem exists on InventoryManager or similar
            // For now just log or call a placeholder
            Debug.Log($"Using {slot.item.displayName}");
            // InventoryManager.Instance.UseItem(slot.item);
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

