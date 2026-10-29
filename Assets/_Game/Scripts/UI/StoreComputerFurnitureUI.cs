using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lives on the always-active GameUI root because OrderingPanel is disabled on Close().
[DisallowMultipleComponent]
public class StoreComputerFurnitureUI : MonoBehaviour
{
    [Header("Existing Terminal")]
    [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private GameObject productsScrollView;
    [SerializeField] private GameObject furnitureScrollView;
    [SerializeField] private TMP_Text orderStatusText;

    [Header("Tabs")]
    [SerializeField] private Button productsTabButton;
    [SerializeField] private Button furnitureTabButton;

    [Header("Furniture List")]
    [SerializeField] private Transform furnitureListContent;
    [SerializeField] private FurnitureCatalogRow furnitureRowPrefab;

    [Header("Business Rules")]
    [SerializeField] private FurnitureShop furnitureShop;

    private readonly List<FurnitureCatalogRow> spawnedRows = new();

    private bool lastOpen;
    private bool startingPlacement;
    private FurnitureDefinition pendingDefinition;

    private void Awake()
    {
        if (productsTabButton != null)
            productsTabButton.onClick.AddListener(ShowProducts);

        if (furnitureTabButton != null)
            furnitureTabButton.onClick.AddListener(ShowFurniture);

        if (productsScrollView != null)
            productsScrollView.SetActive(true);

        if (furnitureScrollView != null)
            furnitureScrollView.SetActive(false);

        RefreshTabState(true);
    }

    private void Update()
    {
        bool open = orderingUI != null && orderingUI.IsOpen;

        if (open && !lastOpen)
            ShowProducts();

        lastOpen = open;
    }

    private void ShowProducts()
    {
        if (productsScrollView != null)
            productsScrollView.SetActive(true);

        if (furnitureScrollView != null)
            furnitureScrollView.SetActive(false);

        RefreshTabState(true);

        if (orderingUI != null && orderingUI.IsOpen)
            orderingUI.RefreshTerminalStatus();
    }

    private void ShowFurniture()
    {
        if (productsScrollView != null)
            productsScrollView.SetActive(false);

        if (furnitureScrollView != null)
            furnitureScrollView.SetActive(true);

        RefreshTabState(false);
        RebuildFurnitureRows();

        if (orderStatusText != null)
        {
            orderStatusText.text =
                "Furniture is charged only after valid placement. Store must be closed and empty.";
        }
    }

    private void RefreshTabState(bool showingProducts)
    {
        if (productsTabButton != null)
            productsTabButton.interactable = !showingProducts;

        if (furnitureTabButton != null)
            furnitureTabButton.interactable = showingProducts;
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
            FurnitureDefinition definition = furnitureShop.GetCatalogItem(i);

            if (definition == null)
                continue;

            FurnitureCatalogRow row = Instantiate(
                furnitureRowPrefab,
                furnitureListContent
            );

            row.Configure(definition, BuyFurniture);
            spawnedRows.Add(row);
        }
    }

    private void ClearFurnitureRows()
    {
        foreach (FurnitureCatalogRow row in spawnedRows)
        {
            if (row == null)
                continue;

            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        spawnedRows.Clear();
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

        if (!furnitureShop.CanBeginPurchase(definition, out string reason))
        {
            if (orderStatusText != null)
                orderStatusText.text = reason;

            return;
        }

        pendingDefinition = definition;
        startingPlacement = true;

        orderingUI.Close();
        StartCoroutine(BeginWhenCursorReturns());
    }

    private IEnumerator BeginWhenCursorReturns()
    {
        for (int i = 0;
             i < 5 && Cursor.lockState != CursorLockMode.Locked;
             i++)
        {
            yield return null;
        }

        FurnitureDefinition definition = pendingDefinition;
        pendingDefinition = null;

        if (definition != null &&
            !furnitureShop.TryBeginPurchase(definition, out string reason))
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
    }
}