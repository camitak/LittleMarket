using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StockWatchUI : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Transform stockAlertContent;
    [SerializeField] private StockAlertRow stockAlertRowPrefab;
    [SerializeField] private TMP_Text allStockedText;
    [SerializeField] private ScrollRect stockScrollRect;

    [Header("Store")] 
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private StoreStockOverview stockOverview;

    [Header("Other UI")] [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private ProductCatalogueUI productCatalogueUI;
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private EndOfDayUI endOfDayUI;

    [Header("Refresh")] 
    [Min(0.05f)] [SerializeField] private float refreshInterval = 0.25f;

    [Header("Gameplay Scrolling")] 
    [Range(0.05f, 0.5f)] [SerializeField] private float mouseWheelStep = 0.18f;

    private readonly List<StockAlertRow> spawnedRows = new();

    private int builtUnlockedProductCount = -1;

    private float refreshTimer;

    private void Start()
    {
        RebuildRows();

        RefreshRows();
    }

    private void Update()
    {
        HandleGameplayMouseWheel();

        refreshTimer +=
            Time.unscaledDeltaTime;

        if (refreshTimer
            < refreshInterval)
            return;

        refreshTimer =
            0f;

        if (storeProgression != null &&
            builtUnlockedProductCount
            != storeProgression
                .UnlockedProductCount)
            RebuildRows();

        RefreshRows();
    }

    private void HandleGameplayMouseWheel()
    {
        if (IsAnotherModalUIOpen())
        {
            return;
        }

        if (Mouse.current == null || stockScrollRect == null)
        {
            return;
        }

        if (!stockScrollRect.gameObject.activeInHierarchy)
        {
            return;
        }

        if (stockScrollRect.content == null ||
            stockScrollRect.viewport == null)
            return;

        var scrollDelta =
            Mouse.current.scroll
                .ReadValue().y;

        if (Mathf.Approximately(
                scrollDelta,
                0f
            ))
            return;

        Canvas.ForceUpdateCanvases();

        var contentHeight =
            stockScrollRect
                .content.rect.height;

        var viewportHeight =
            stockScrollRect
                .viewport.rect.height;

        if (contentHeight
            <= viewportHeight + 1f)
        {
            stockScrollRect
                    .verticalNormalizedPosition =
                1f;

            return;
        }

        var direction =
            scrollDelta > 0f
                ? 1f
                : -1f;

        var newPosition =
            stockScrollRect
                .verticalNormalizedPosition
            + direction
            * mouseWheelStep;

        stockScrollRect
                .verticalNormalizedPosition =
            Mathf.Clamp01(
                newPosition
            );
    }

    private bool IsAnotherModalUIOpen()
    {
        if (orderingUI != null &&
            orderingUI.IsOpen)
            return true;

        if (productCatalogueUI != null &&
            productCatalogueUI.IsOpen)
            return true;

        if (pauseMenuUI != null &&
            pauseMenuUI.IsOpen)
            return true;

        if (endOfDayUI != null &&
            endOfDayUI.IsOpen)
            return true;

        return false;
    }

    private void RebuildRows()
    {
        ClearRows();

        if (storeProgression == null) return;

        if (stockAlertContent == null) return;

        if (stockAlertRowPrefab == null) return;

        for (var i = 0;
             i < storeProgression
                 .UnlockedProductCount;
             i++)
        {
            var product =
                storeProgression
                    .GetUnlockedProduct(
                        i
                    );

            if (product == null) continue;

            var newRow =
                Instantiate(
                    stockAlertRowPrefab,
                    stockAlertContent
                );

            newRow.Configure(
                product
            );

            spawnedRows.Add(
                newRow
            );
        }

        builtUnlockedProductCount =
            storeProgression
                .UnlockedProductCount;

        Canvas.ForceUpdateCanvases();

        if (stockScrollRect != null)
            stockScrollRect
                    .verticalNormalizedPosition =
                1f;
    }

    private void RefreshRows()
    {
        if (stockOverview == null) return;

        var warningCount =
            0;

        for (var i = 0;
             i < spawnedRows.Count;
             i++)
        {
            var row =
                spawnedRows[i];

            if (row == null) continue;

            var showingWarning =
                row.Refresh(
                    stockOverview
                );

            if (showingWarning) warningCount++;
        }

        if (allStockedText != null)
            allStockedText
                .gameObject
                .SetActive(
                    warningCount <= 0
                );

        Canvas.ForceUpdateCanvases();

        ClampScrollPositionIfNeeded();
    }

    private void ClampScrollPositionIfNeeded()
    {
        if (stockScrollRect == null) return;

        if (stockScrollRect.content == null ||
            stockScrollRect.viewport == null)
            return;

        var contentHeight =
            stockScrollRect
                .content.rect.height;

        var viewportHeight =
            stockScrollRect
                .viewport.rect.height;

        if (contentHeight
            <= viewportHeight + 1f)
            stockScrollRect
                    .verticalNormalizedPosition =
                1f;
    }

    private void ClearRows()
    {
        for (var i = 0;
             i < spawnedRows.Count;
             i++)
        {
            var row =
                spawnedRows[i];

            if (row == null) continue;

            row.gameObject.SetActive(
                false
            );

            Destroy(
                row.gameObject
            );
        }

        spawnedRows.Clear();

        builtUnlockedProductCount =
            -1;
    }
}