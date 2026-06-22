using System.Collections.Generic;
using _Scripts.MainGame.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.MainGame.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        public static CraftingUI Instance { get; private set; }

        [SerializeField] private GameObject recipeEntryPrefab;
        [SerializeField] private Transform recipeList;
        [SerializeField] private GameObject craftingPanel;

        [Header("Unified UI Integration")]
        [SerializeField] private GameObject unifiedPanel;
        [SerializeField] private GameObject craftingContent;
        [SerializeField] private GameObject backgroundCloser;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button craftingTabButton;

        // IDE0044: Make field readonly
        private readonly List<CraftingRecipeEntryUI> entries = new();

        private bool isInitialized = false;

        private bool isSubscribed = false;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            Initialize();
        }

        void OnEnable()
        {
            if (isInitialized)
            {
                Subscribe();
                Refresh();
            }
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        private void Initialize()
        {
            if (isInitialized) return;
        
            // Ensure buttons are wired up
            if (inventoryTabButton != null) inventoryTabButton.gameObject.SetActive(false);
            if (craftingTabButton != null) craftingTabButton.gameObject.SetActive(false);
        
            if (backgroundCloser != null)
            {
                var btn = backgroundCloser.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(Hide);
                backgroundCloser.SetActive(false);
            }

            Subscribe();
            // Refresh(); // No more recipe list to refresh
            isInitialized = true;
        }

        private void Subscribe()
        {
            if (isSubscribed) return;

            // No need to subscribe to crafting context if menu is gone
        
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnSlotChanged += HandleSlotChanged;

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed) return;

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnSlotChanged -= HandleSlotChanged;

            isSubscribed = false;
        }

        private void HandleSlotChanged(int index) { } // No-op

        public void Show(bool isWorkstation = false, string workstationId = null)
        {
            if (unifiedPanel == null) return;

            unifiedPanel.SetActive(true);
            if (backgroundCloser != null) backgroundCloser.SetActive(true);

            // Always show inventory now
            if (InventoryUI.Instance != null) InventoryUI.Instance.ShowContent(true);
            if (craftingContent != null) craftingContent.SetActive(false);
        
            UpdateTabStyles(true);
        }

        public void Hide()
        {
            if (unifiedPanel != null) unifiedPanel.SetActive(false);
            if (backgroundCloser != null) backgroundCloser.SetActive(false);
        }

        public void ToggleMenu()
        {
            if (unifiedPanel == null) return;
            if (unifiedPanel.activeSelf) Hide();
            else Show();
        }

        [Header("Tab Styling")]
        [SerializeField] private Color activeTabColor = new Color(0.165f, 0.149f, 0.125f); // #2A2620
        [SerializeField] private Color inactiveTabColor = new Color(0.102f, 0.094f, 0.086f); // #1A1816
        [SerializeField] private Color activeTextColor = new Color(0.831f, 0.769f, 0.627f); // #D4C4A0
        [SerializeField] private Color inactiveTextColor = new Color(0.353f, 0.329f, 0.282f); // #5A5448

        public void SwitchToInventory()
        {
            if (InventoryUI.Instance != null) InventoryUI.Instance.ShowContent(true);
            if (craftingContent != null) craftingContent.SetActive(false);
            UpdateTabStyles(true);
        }

        public void SwitchToCrafting()
        {
            // No-op or redirect to inventory
            SwitchToInventory();
        }

        private void UpdateTabStyles(bool isInventory)
        {
            // Tabs are hidden, so styling is just for safety
        }

        void Refresh() { }

        // Called when inventory changes - just update craftable state, no full rebuild
        public void RefreshCraftability() { }
    }
}
