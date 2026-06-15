using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [SerializeField] private GameObject recipeEntryPrefab;
[SerializeField] private Transform recipeList;
    [SerializeField] private GameObject craftingPanel;

    [Header("Unified UI Integration")]
    [SerializeField] private GameObject unifiedPanel;
    [SerializeField] private GameObject craftingContent;
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
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(SwitchToInventory);
        if (craftingTabButton != null) craftingTabButton.onClick.AddListener(SwitchToCrafting);

        Subscribe();
        Refresh();
        isInitialized = true;
    }

    private void Subscribe()
    {
        if (isSubscribed) return;

        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnCraftingContextChanged += Refresh;
        
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnSlotChanged += HandleSlotChanged;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnCraftingContextChanged -= Refresh;
        
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnSlotChanged -= HandleSlotChanged;

        isSubscribed = false;
    }

    private void HandleSlotChanged(int index) => RefreshCraftability();

    public void Show(bool isWorkstation = false, string workstationId = null)
    {
        if (unifiedPanel == null) return;

        unifiedPanel.SetActive(true);
        if (isWorkstation)
        {
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.OpenWorkstation(workstationId);
            SwitchToCrafting();
        }
        else
        {
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.OpenInventoryCrafting();
            SwitchToInventory();
        }
    }

    public void Hide()
    {
        if (unifiedPanel != null) unifiedPanel.SetActive(false);
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
        if (InventoryUI.Instance != null) InventoryUI.Instance.ShowContent(false);
        if (craftingContent != null) craftingContent.SetActive(true);
        UpdateTabStyles(false);
        Refresh();
    }

    private void UpdateTabStyles(bool isInventory)
    {
        if (inventoryTabButton != null)
        {
            inventoryTabButton.image.color = isInventory ? activeTabColor : inactiveTabColor;
            var text = inventoryTabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.color = isInventory ? activeTextColor : inactiveTextColor;
            
            var border = inventoryTabButton.transform.Find("ActiveBorder");
            if (border != null) border.gameObject.SetActive(isInventory);
        }

        if (craftingTabButton != null)
        {
            craftingTabButton.image.color = !isInventory ? activeTabColor : inactiveTabColor;
            var text = craftingTabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.color = !isInventory ? activeTextColor : inactiveTextColor;

            var border = craftingTabButton.transform.Find("ActiveBorder");
            if (border != null) border.gameObject.SetActive(!isInventory);
        }
    }

    void Refresh()
    {
        foreach (var e in entries) Destroy(e.gameObject);
        entries.Clear();

        var recipes = CraftingManager.Instance.GetAvailableRecipes();
        foreach (var recipe in recipes)
        {
            var go = Instantiate(recipeEntryPrefab, recipeList);
            var entry = go.GetComponent<CraftingRecipeEntryUI>();
            entry.Setup(recipe, CraftingManager.Instance.CanCraft(recipe));
            entries.Add(entry);
        }
    }

        // Called when inventory changes - just update craftable state, no full rebuild
    public void RefreshCraftability()
    {
        if (CraftingManager.Instance == null) return;

        var recipes = CraftingManager.Instance.GetAvailableRecipes();
        
        // If counts don't match, full refresh
        if (entries.Count != recipes.Count)
        {
            Refresh();
            return;
        }

        foreach (var entry in entries)
        {
            if (entry != null)
            {
                entry.RefreshDisplay();
            }
        }
    }
}