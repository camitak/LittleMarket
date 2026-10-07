using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StockWatchUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Transform stockAlertContent;

    [SerializeField]
    private StockAlertRow stockAlertRowPrefab;

    [SerializeField]
    private TMP_Text allStockedText;

    [SerializeField]
    private ScrollRect stockScrollRect;

    [Header("Store")]
    [SerializeField]
    private StoreProgression storeProgression;

    [SerializeField]
    private StoreStockOverview stockOverview;

    [Header("Other UI")]
    [SerializeField]
    private OrderingUI orderingUI;

    [SerializeField]
    private ProductCatalogueUI productCatalogueUI;

    [SerializeField]
    private PauseMenuUI pauseMenuUI;

    [SerializeField]
    private EndOfDayUI endOfDayUI;

    [Header("Refresh")]
    [Min(0.05f)]
    [SerializeField]
    private float refreshInterval = 0.25f;

    [Header("Gameplay Scrolling")]
    [Range(0.05f, 0.5f)]
    [SerializeField]
    private float mouseWheelStep = 0.18f;

    private readonly List<StockAlertRow>
        spawnedRows =
            new List<StockAlertRow>();

    private int builtUnlockedProductCount =
        -1;

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
        {
            return;
        }

        refreshTimer =
            0f;

        if (storeProgression != null &&
            builtUnlockedProductCount
            != storeProgression
                .UnlockedProductCount)
        {
            RebuildRows();
        }

        RefreshRows();
    }

    private void HandleGameplayMouseWheel()
    {
        if (IsAnotherModalUIOpen())
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (stockScrollRect == null)
        {
            return;
        }

        if (stockScrollRect.content == null ||
            stockScrollRect.viewport == null)
        {
            return;
        }

        float scrollDelta =
            Mouse.current.scroll
                .ReadValue().y;

        if (Mathf.Approximately(
                scrollDelta,
                0f
            ))
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float contentHeight =
            stockScrollRect
                .content.rect.height;

        float viewportHeight =
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

        float direction =
            scrollDelta > 0f
            ? 1f
            : -1f;

        float newPosition =
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
        {
            return true;
        }

        if (productCatalogueUI != null &&
            productCatalogueUI.IsOpen)
        {
            return true;
        }

        if (pauseMenuUI != null &&
            pauseMenuUI.IsOpen)
        {
            return true;
        }

        if (endOfDayUI != null &&
            endOfDayUI.IsOpen)
        {
            return true;
        }

        return false;
    }

    private void RebuildRows()
    {
        ClearRows();

        if (storeProgression == null)
        {
            return;
        }

        if (stockAlertContent == null)
        {
            return;
        }

        if (stockAlertRowPrefab == null)
        {
            return;
        }

        for (int i = 0;
             i < storeProgression
                 .UnlockedProductCount;
             i++)
        {
            ProductData product =
                storeProgression
                    .GetUnlockedProduct(
                        i
                    );

            if (product == null)
            {
                continue;
            }

            StockAlertRow newRow =
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
        {
            stockScrollRect
                .verticalNormalizedPosition =
                1f;
        }
    }

    private void RefreshRows()
    {
        if (stockOverview == null)
        {
            return;
        }

        int warningCount =
            0;

        for (int i = 0;
             i < spawnedRows.Count;
             i++)
        {
            StockAlertRow row =
                spawnedRows[i];

            if (row == null)
            {
                continue;
            }

            bool showingWarning =
                row.Refresh(
                    stockOverview
                );

            if (showingWarning)
            {
                warningCount++;
            }
        }

        if (allStockedText != null)
        {
            allStockedText
                .gameObject
                .SetActive(
                    warningCount <= 0
                );
        }

        Canvas.ForceUpdateCanvases();

        ClampScrollPositionIfNeeded();
    }

    private void ClampScrollPositionIfNeeded()
    {
        if (stockScrollRect == null)
        {
            return;
        }

        if (stockScrollRect.content == null ||
            stockScrollRect.viewport == null)
        {
            return;
        }

        float contentHeight =
            stockScrollRect
                .content.rect.height;

        float viewportHeight =
            stockScrollRect
                .viewport.rect.height;

        if (contentHeight
            <= viewportHeight + 1f)
        {
            stockScrollRect
                .verticalNormalizedPosition =
                1f;
        }
    }

    private void ClearRows()
    {
        for (int i = 0;
             i < spawnedRows.Count;
             i++)
        {
            StockAlertRow row =
                spawnedRows[i];

            if (row == null)
            {
                continue;
            }

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