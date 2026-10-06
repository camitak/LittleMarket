using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Refresh")]
    [Min(0.05f)]
    [SerializeField]
    private float refreshInterval = 0.25f;

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