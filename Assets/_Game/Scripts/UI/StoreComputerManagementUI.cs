using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreComputerManagementUI : MonoBehaviour
{
    private enum StoreComputerTab
    {
        Products,
        Furniture,
        Expansion
    }

    [Header("Existing Terminal")]
    [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private GameObject productsScrollView;
    [SerializeField] private GameObject furnitureScrollView;
    [SerializeField] private GameObject expansionScrollView;
    [SerializeField] private TMP_Text orderStatusText;

    [Header("Tabs")]
    [SerializeField] private Button productsTabButton;
    [SerializeField] private Button furnitureTabButton;
    [SerializeField] private Button expansionTabButton;

    [Header("Furniture List")]
    [SerializeField] private Transform furnitureListContent;
    [SerializeField] private FurnitureCatalogRow furnitureRowPrefab;

    [Header("Expansion List")]
    [SerializeField] private Transform expansionListContent;
    [SerializeField] private StoreExpansionRow expansionRowPrefab;

    [Header("Business Rules")]
    [SerializeField] private FurnitureShop furnitureShop;
    [SerializeField] private StoreExpansionSystem storeExpansionSystem;

    private readonly List<FurnitureCatalogRow> spawnedFurnitureRows = new();
    private readonly List<StoreExpansionRow> spawnedExpansionRows = new();

    private bool lastOpen;
    private bool startingPlacement;
    private FurnitureDefinition pendingFurnitureDefinition;
    private StoreExpansionZone pendingExpansionConfirmation;

    private void Awake()
    {
        if (productsTabButton != null)
            productsTabButton.onClick.AddListener(ShowProducts);

        if (furnitureTabButton != null)
            furnitureTabButton.onClick.AddListener(ShowFurniture);

        if (expansionTabButton != null)
            expansionTabButton.onClick.AddListener(ShowExpansion);

        SetPanelState(StoreComputerTab.Products);
    }

    private void Update()
    {
        bool open =
            orderingUI != null &&
            orderingUI.IsOpen;

        if (open && !lastOpen)
            ShowProducts();

        lastOpen = open;
    }

    private void ShowProducts()
    {
        pendingExpansionConfirmation = null;

        SetPanelState(StoreComputerTab.Products);

        if (orderingUI != null && orderingUI.IsOpen)
            orderingUI.RefreshTerminalStatus();
    }

    private void ShowFurniture()
    {
        pendingExpansionConfirmation = null;

        SetPanelState(StoreComputerTab.Furniture);
        RebuildFurnitureRows();

        if (orderStatusText != null)
        {
            orderStatusText.text =
                "Furniture is charged only after valid placement. Store must be closed and empty.";
        }
    }

    private void ShowExpansion()
    {
        pendingExpansionConfirmation = null;

        SetPanelState(StoreComputerTab.Expansion);
        RebuildExpansionRows();

        if (orderStatusText != null)
        {
            orderStatusText.text =
                "Store expansions are permanent. Select an expansion to review the purchase.";
        }
    }

    private void SetPanelState(StoreComputerTab activeTab)
    {
        if (productsScrollView != null)
        {
            productsScrollView.SetActive(
                activeTab == StoreComputerTab.Products
            );
        }

        if (furnitureScrollView != null)
        {
            furnitureScrollView.SetActive(
                activeTab == StoreComputerTab.Furniture
            );
        }

        if (expansionScrollView != null)
        {
            expansionScrollView.SetActive(
                activeTab == StoreComputerTab.Expansion
            );
        }

        if (productsTabButton != null)
        {
            productsTabButton.interactable =
                activeTab != StoreComputerTab.Products;
        }

        if (furnitureTabButton != null)
        {
            furnitureTabButton.interactable =
                activeTab != StoreComputerTab.Furniture;
        }

        if (expansionTabButton != null)
        {
            expansionTabButton.interactable =
                activeTab != StoreComputerTab.Expansion;
        }
    }

    private void RebuildFurnitureRows()
    {
        ClearFurnitureRows();

        if (furnitureListContent == null ||
            furnitureRowPrefab == null ||
            furnitureShop == null)
        {
            return;
        }

        for (int i = 0; i < furnitureShop.CatalogCount; i++)
        {
            FurnitureDefinition definition =
                furnitureShop.GetCatalogItem(i);

            if (definition == null)
                continue;

            FurnitureCatalogRow row = Instantiate(
                furnitureRowPrefab,
                furnitureListContent
            );

            row.Configure(definition, BuyFurniture);
            spawnedFurnitureRows.Add(row);
        }
    }

    private void RebuildExpansionRows()
    {
        ClearExpansionRows();

        if (expansionListContent == null ||
            expansionRowPrefab == null ||
            storeExpansionSystem == null)
        {
            return;
        }

        for (int i = 0;
             i < storeExpansionSystem.ExpansionCount;
             i++)
        {
            StoreExpansionZone zone =
                storeExpansionSystem.GetExpansion(i);

            if (zone == null)
                continue;

            StoreExpansionRow row = Instantiate(
                expansionRowPrefab,
                expansionListContent
            );

            row.Configure(
                zone,
                HandleExpansionPurchaseRequested
            );

            row.Refresh(
                pendingExpansionConfirmation == zone
            );

            spawnedExpansionRows.Add(row);
        }
    }

    private void ClearFurnitureRows()
    {
        foreach (FurnitureCatalogRow row in spawnedFurnitureRows)
        {
            if (row == null)
                continue;

            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        spawnedFurnitureRows.Clear();
    }

    private void ClearExpansionRows()
    {
        foreach (StoreExpansionRow row in spawnedExpansionRows)
        {
            if (row == null)
                continue;

            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        spawnedExpansionRows.Clear();
    }

    private void BuyFurniture(FurnitureDefinition definition)
    {
        if (startingPlacement ||
            definition == null ||
            orderingUI == null ||
            !orderingUI.IsOpen ||
            furnitureShop == null)
        {
            return;
        }

        if (!furnitureShop.CanBeginPurchase(
                definition,
                out string reason))
        {
            if (orderStatusText != null)
                orderStatusText.text = reason;

            return;
        }

        pendingFurnitureDefinition = definition;
        startingPlacement = true;

        orderingUI.Close();
        StartCoroutine(BeginFurnitureWhenCursorReturns());
    }

    private void HandleExpansionPurchaseRequested(
        StoreExpansionZone zone)
    {
        if (zone == null ||
            orderingUI == null ||
            !orderingUI.IsOpen ||
            storeExpansionSystem == null)
        {
            return;
        }

        if (!storeExpansionSystem.CanPurchase(
                zone,
                out string reason))
        {
            if (orderStatusText != null)
                orderStatusText.text = reason;

            return;
        }

        StoreExpansionDefinition definition =
            zone.Definition;

        if (pendingExpansionConfirmation != zone)
        {
            pendingExpansionConfirmation = zone;

            if (orderStatusText != null)
            {
                orderStatusText.text =
                    $"{definition.DisplayName} is permanent and costs £{definition.PurchasePrice:0.00}. Click CONFIRM to purchase.";
            }

            RebuildExpansionRows();
            return;
        }

        bool purchased =
            storeExpansionSystem.TryPurchase(
                zone,
                out reason
            );

        pendingExpansionConfirmation = null;

        if (orderStatusText != null)
            orderStatusText.text = reason;

        RebuildExpansionRows();

        if (!purchased)
            return;
    }

    private IEnumerator BeginFurnitureWhenCursorReturns()
    {
        for (int i = 0;
             i < 5 &&
             Cursor.lockState != CursorLockMode.Locked;
             i++)
        {
            yield return null;
        }

        FurnitureDefinition definition =
            pendingFurnitureDefinition;

        pendingFurnitureDefinition = null;

        if (definition != null &&
            !furnitureShop.TryBeginPurchase(
                definition,
                out string reason))
        {
            Debug.LogWarning(
                $"Furniture purchase not started: {reason}",
                this
            );
        }

        startingPlacement = false;
    }

    private void OnDestroy()
    {
        if (productsTabButton != null)
            productsTabButton.onClick.RemoveListener(ShowProducts);

        if (furnitureTabButton != null)
            furnitureTabButton.onClick.RemoveListener(ShowFurniture);

        if (expansionTabButton != null)
            expansionTabButton.onClick.RemoveListener(ShowExpansion);
    }
}