using System.Collections.Generic;
using _Scripts.MainGame.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.Inventory
{
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
        public Button combineItemButton;

        [Header("Unified UI Integration")]
        [SerializeField] private GameObject inventoryContent;

        public static InventoryUI Instance { get; private set; }

        private List<InventorySlotUI> slotUIs = new();
        private int selectedIndex = -1;
        private int draggingIndex = -1;
        private int movingIndex = -1;
        public int GetDraggingIndex() => draggingIndex;
        private float lastClickTime;
private int lastClickIndex = -1;
        [SerializeField] private float doubleClickThreshold = 0.3f;

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
            // Combine button is no longer needed with drag-and-drop merge
            if (combineItemButton != null) combineItemButton.gameObject.SetActive(false);
        }

        public void OnBeginDrag(int index)
        {
            draggingIndex = index;
            // Selection follows drag
            HandleSingleClick(index);
        }

        public void OnEndDrag()
        {
            draggingIndex = -1;
        }

        public void OnDropOnSlot(int targetIndex)
        {
            if (draggingIndex == -1) return;
            if (draggingIndex == targetIndex) return;

            // Try to merge
            if (CraftingManager.Instance != null && CraftingManager.Instance.TryMergeSlots(draggingIndex, targetIndex))
            {
                // Merge handled consumption and adding result
                HandleSingleClick(targetIndex);
                return;
            }

            // If no merge possible, swap
            InventoryManager.Instance.SwapSlots(draggingIndex, targetIndex);
            HandleSingleClick(targetIndex);
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
            if (!show)
            {
                if (detailsPanel != null) detailsPanel.SetActive(false);
                // Cancel moving when closing
                if (movingIndex != -1)
                {
                    if (movingIndex < slotUIs.Count) slotUIs[movingIndex].SetMoving(false);
                    movingIndex = -1;
                }
            }
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
        
            float timeSinceLastClick = Time.time - lastClickTime;
            bool isDoubleClick = index == lastClickIndex && timeSinceLastClick < doubleClickThreshold;
        
            lastClickTime = Time.time;
            lastClickIndex = index;

            if (isDoubleClick)
            {
                HandleDoubleClick(index);
            }
            else
            {
                HandleSingleClick(index);
            }
        }

        private void HandleSingleClick(int index)
        {
            // Selection for details (original behavior)
            if (selectedIndex != -1 && selectedIndex < slotUIs.Count) 
                slotUIs[selectedIndex].SetSelected(false);
            
            selectedIndex = index;
            slotUIs[selectedIndex].SetSelected(true);
            UpdateDetails(index);
        }

        private void HandleDoubleClick(int index)
        {
            if (movingIndex == -1)
            {
                // Start moving if slot is not empty
                if (!InventoryManager.Instance.slots[index].IsEmpty)
                {
                    movingIndex = index;
                    slotUIs[movingIndex].SetMoving(true);
                }
            }
            else if (movingIndex == index)
            {
                // Cancel moving if double clicking the same slot
                slotUIs[movingIndex].SetMoving(false);
                movingIndex = -1;
            }
            else
            {
                // Swap items
                InventoryManager.Instance.SwapSlots(movingIndex, index);
                slotUIs[movingIndex].SetMoving(false);
                movingIndex = -1;
            
                // Re-select the destination slot to update details if needed
                HandleSingleClick(index);
            }
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
        if (useButton != null)
        {
            var text = useButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = slot.item.category == ItemCategory.Armor ? "EQUIP" : "USE";
            }
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
                if (slot.item.category == ItemCategory.Armor)
                {
                    ItemData armor = slot.item;
                    if (PlayerArmorManager.Instance.Equip(armor))
                    {
                        slot.quantity--;
                        if (slot.quantity <= 0) slot.Clear();
                        InventoryManager.Instance.NotifySlotChanged(selectedIndex);
                    }
                    return;
                }

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
}

