using System.Collections.Generic;
using UnityEngine;


public class CraftingUI : MonoBehaviour
{
    public static CraftingUI Instance { get; private set; }

    [SerializeField] private GameObject recipeEntryPrefab;
[SerializeField] private Transform recipeList;
    [SerializeField] private GameObject craftingPanel;

    // IDE0044: Make field readonly
    private readonly List<CraftingRecipeEntryUI> entries = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()
{
        CraftingManager.Instance.OnCraftingContextChanged += Refresh;
        InventoryManager.Instance.OnSlotChanged += _ => RefreshCraftability();
        Refresh();
    }

    void OnDisable()
    {
        CraftingManager.Instance.OnCraftingContextChanged -= Refresh;
        InventoryManager.Instance.OnSlotChanged -= _ => RefreshCraftability();
    }

    public void Show(bool isWorkstation = false, string workstationId = null)
    {
        if (isWorkstation)
            CraftingManager.Instance.OpenWorkstation(workstationId);
        else
            CraftingManager.Instance.OpenInventoryCrafting();

        craftingPanel.SetActive(true);
        if (InventoryUI.Instance != null) InventoryUI.Instance.HideInventory();
    }

    public void Hide() => craftingPanel.SetActive(false);

    public void Toggle()
    {
        if (craftingPanel.activeSelf) Hide();
        else Show();
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

    // Called when inventory changes � just update craftable state, no full rebuild
    void RefreshCraftability()
    {
        var recipes = CraftingManager.Instance.GetAvailableRecipes();
        for (int i = 0; i < entries.Count && i < recipes.Count; i++)
            entries[i].SetCraftable(CraftingManager.Instance.CanCraft(recipes[i]));
    }
}