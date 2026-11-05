using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BuildCatalogUI : MonoBehaviour
{
    [Header("Panel")] [SerializeField] private GameObject catalogPanel;

    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Button closeButton;

    [Header("Categories")] [SerializeField]
    private Button allCategoryButton;

    [SerializeField] private Button fixturesCategoryButton;

    [SerializeField] private Button storageCategoryButton;

    [SerializeField] private Button refrigerationCategoryButton;

    [Header("Catalog List")] [SerializeField]
    private ScrollRect catalogScrollRect;

    [SerializeField] private Transform furnitureListContent;

    [SerializeField] private FurnitureCatalogRow furnitureRowPrefab;

    [Header("Store Design")] [SerializeField]
    private StoreLayoutModeController layoutMode;

    [SerializeField] private FurnitureShop furnitureShop;

    [SerializeField] private FurniturePlacementController placementController;

    private readonly List<FurnitureCatalogRow> spawnedRows = new();

    /*
     * Null means ALL.
     */
    private FurnitureCatalogCategory? activeCategory;

    private bool isOpen;
    private bool reopenAfterPlacement;

    private const string DefaultStatus = "Choose an item to place. Payment happens only when placement is confirmed.";

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.AddListener(ShowAll);
        }

        if (fixturesCategoryButton != null)
        {
            fixturesCategoryButton.onClick.AddListener(ShowFixtures);
        }

        if (storageCategoryButton != null)
        {
            storageCategoryButton.onClick.AddListener(ShowStorage);
        }

        if (refrigerationCategoryButton != null)
        {
            refrigerationCategoryButton.onClick.AddListener(ShowRefrigeration);
        }

        activeCategory = null;

        if (catalogPanel != null)
        {
            catalogPanel.SetActive(false);
        }

        RefreshCategoryButtons();
    }

    private void Update()
    {
        if (layoutMode == null || !layoutMode.IsActive)
        {
            reopenAfterPlacement = false;

            if (isOpen)
            {
                SetPanelVisible(false);
            }

            return;
        }

        bool placementActive = placementController != null && placementController.IsPlacementActive;

        /*
         * Picking up an existing object while the
         * catalog is visible should temporarily
         * minimize the catalog as well.
         */
        if (placementActive)
        {
            if (isOpen)
            {
                SetPanelVisible(false);

                reopenAfterPlacement = true;
            }

            return;
        }

        if (reopenAfterPlacement)
        {
            reopenAfterPlacement = false;

            Open();
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.bKey.wasPressedThisFrame)
        {
            return;
        }

        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (isOpen || catalogPanel == null || layoutMode == null || !layoutMode.IsActive)
        {
            return;
        }

        if (placementController != null && placementController.IsPlacementActive)
        {
            return;
        }

        SetPanelVisible(true);

        catalogPanel.transform.SetAsLastSibling();

        RebuildRows(true);

        SetStatus(DefaultStatus);
    }

    public void Close()
    {
        reopenAfterPlacement = false;

        SetPanelVisible(false);
    }

    private void ShowAll()
    {
        SetCategory(null);
    }

    private void ShowFixtures()
    {
        SetCategory(FurnitureCatalogCategory.Fixtures);
    }

    private void ShowStorage()
    {
        SetCategory(FurnitureCatalogCategory.Storage);
    }

    private void ShowRefrigeration()
    {
        SetCategory(FurnitureCatalogCategory.Refrigeration);
    }

    private void SetCategory(FurnitureCatalogCategory? category)
    {
        activeCategory = category;

        RefreshCategoryButtons();

        RebuildRows(true);

        SetStatus(DefaultStatus);
    }

    private void RebuildRows(bool resetScroll)
    {
        ClearRows();

        if (furnitureShop == null || furnitureListContent == null || furnitureRowPrefab == null)
        {
            SetStatus("Build Catalog is not configured.");

            return;
        }

        int visibleItemCount = 0;

        for (int i = 0; i < furnitureShop.CatalogCount; i++)
        {
            FurnitureDefinition definition = furnitureShop.GetCatalogItem(i);

            if (definition == null)
            {
                continue;
            }

            if (activeCategory.HasValue && definition.CatalogCategory != activeCategory.Value)
            {
                continue;
            }

            FurnitureCatalogRow row = Instantiate(furnitureRowPrefab, furnitureListContent);

            row.Configure(definition, BeginFurniturePurchase);

            spawnedRows.Add(row);

            visibleItemCount++;
        }

        RefreshCategoryButtons();

        RefreshScrollLayout(resetScroll);

        if (visibleItemCount == 0)
        {
            SetStatus("No furniture is available in this category yet.");
        }
    }

    private void BeginFurniturePurchase(FurnitureDefinition definition)
    {
        if (definition == null || furnitureShop == null)
        {
            SetStatus("Furniture purchasing is not configured.");

            return;
        }

        if (!furnitureShop.CanBeginPurchase(definition, out string reason))
        {
            SetStatus(reason);

            return;
        }

        /*
         * Hide before starting the preview so the
         * floor immediately becomes the player's
         * full placement workspace.
         */
        SetPanelVisible(false);

        bool started = furnitureShop.TryBeginPurchase(definition, out reason);

        if (!started)
        {
            SetPanelVisible(true);

            RebuildRows(false);

            SetStatus(reason);

            return;
        }

        /*
         * FurniturePlacementController now owns
         * the preview transaction.
         *
         * We simply wait for placement or cancel.
         */
        reopenAfterPlacement = true;
    }

    private void RefreshScrollLayout(bool resetScroll)
    {
        if (catalogScrollRect == null || furnitureListContent is not RectTransform contentRect)
        {
            return;
        }

        float previousPosition = catalogScrollRect.verticalNormalizedPosition;

        if (float.IsNaN(previousPosition))
        {
            previousPosition = 1f;
        }

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();

        catalogScrollRect.StopMovement();

        catalogScrollRect.verticalNormalizedPosition = resetScroll ? 1f : Mathf.Clamp01(previousPosition);
    }

    private void RefreshCategoryButtons()
    {
        SetCategoryButtonState(allCategoryButton, !activeCategory.HasValue, "ALL");

        SetCategoryButtonState(fixturesCategoryButton, activeCategory == FurnitureCatalogCategory.Fixtures, "FIXTURES");

        SetCategoryButtonState(storageCategoryButton, activeCategory == FurnitureCatalogCategory.Storage, "STORAGE");

        SetCategoryButtonState(refrigerationCategoryButton, activeCategory == FurnitureCatalogCategory.Refrigeration,
            "REFRIGERATION");
    }

    private static void SetCategoryButtonState(Button button, bool active, string label)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = !active;

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
        {
            buttonText.text = active ? $"✓ {label}" : label;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message ?? "";
    }

    private void SetPanelVisible(bool visible)
    {
        isOpen = visible;

        if (catalogPanel != null)
        {
            catalogPanel.SetActive(visible);
        }
    }

    private void ClearRows()
    {
        foreach (FurnitureCatalogRow row in spawnedRows)
        {
            if (row == null)
            {
                continue;
            }

            row.gameObject.SetActive(false);

            Destroy(row.gameObject);
        }

        spawnedRows.Clear();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (allCategoryButton != null)
        {
            allCategoryButton.onClick.RemoveListener(ShowAll);
        }

        if (fixturesCategoryButton != null)
        {
            fixturesCategoryButton.onClick.RemoveListener(ShowFixtures);
        }

        if (storageCategoryButton != null)
        {
            storageCategoryButton.onClick.RemoveListener(ShowStorage);
        }

        if (refrigerationCategoryButton != null)
        {
            refrigerationCategoryButton.onClick.RemoveListener(ShowRefrigeration);
        }
    }
}