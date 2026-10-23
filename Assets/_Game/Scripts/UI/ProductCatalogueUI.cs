
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProductCatalogueUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text progressionSummaryText;
    [SerializeField] private Transform catalogueListContent;
    [SerializeField] private ProductCatalogueRow catalogueRowPrefab;

    [Header("Progression")]
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private StoreLevelProgression storeLevelProgression;

    private readonly List<ProductCatalogueRow> spawnedRows = new();

    public void RefreshPage()
    {
        BuildRows();
        RefreshProgressionSummary();
    }

    private void BuildRows()
    {
        ClearRows();

        if (storeProgression == null ||
            catalogueListContent == null ||
            catalogueRowPrefab == null)
        {
            return;
        }

        for (int i = 0; i < storeProgression.CatalogProductCount; i++)
        {
            ProductData product = storeProgression.GetCatalogProduct(i);

            if (product == null)
                continue;

            ProductCatalogueRow row =
                Instantiate(catalogueRowPrefab, catalogueListContent);

            row.Configure(product, storeProgression);
            spawnedRows.Add(row);
        }
    }

    private void RefreshProgressionSummary()
    {
        if (progressionSummaryText == null)
            return;

        if (storeProgression == null || storeLevelProgression == null)
        {
            progressionSummaryText.text = "Progression not configured";
            return;
        }

        progressionSummaryText.text =
            $"REP {storeProgression.CurrentReputation:0} / 100   •   " +
            storeLevelProgression.GetProgressText();
    }

    private void ClearRows()
    {
        foreach (ProductCatalogueRow row in spawnedRows)
        {
            if (row == null)
                continue;

            row.gameObject.SetActive(false);
            Destroy(row.gameObject);
        }

        spawnedRows.Clear();
    }
}
