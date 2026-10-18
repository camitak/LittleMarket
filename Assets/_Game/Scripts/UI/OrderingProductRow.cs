using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderingProductRow : MonoBehaviour
{
    [Header("UI")] [SerializeField] private TMP_Text productNameText;

    [SerializeField] private TMP_Text priceText;

    [SerializeField] private TMP_Text stockText;

    [SerializeField] private Button orderButton;

    [SerializeField] private TMP_Text orderButtonText;

    private ProductData productData;

    private Action<ProductData> orderAction;

    private StoreStockOverview stockOverview;

    private bool isLocked;

    private float stockRefreshTimer;

    private const float StockRefreshInterval =
        0.25f;

    private const string PositiveColor =
        "#4F7F62";

    private const string NegativeColor =
        "#9A4F4F";

    private const string NewColor =
        "#4F7F62";

    private static readonly Color32 HealthyStockColor =
        new(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 LowStockColor =
        new(
            139,
            106,
            31,
            255
        );

    private static readonly Color32 RestockColor =
        new(
            79,
            113,
            135,
            255
        );

    private static readonly Color32 OutOfStockColor =
        new(
            154,
            79,
            79,
            255
        );

    /*
     * Compatibility overload.
     */
    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> newOrderAction
    )
    {
        Configure(
            newProductData,
            quantityPerBox,
            newOrderAction,
            null,
            null
        );
    }

    /*
     * Compatibility overload from Lesson 44.
     */
    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> newOrderAction,
        StoreProgression storeProgression
    )
    {
        Configure(
            newProductData,
            quantityPerBox,
            newOrderAction,
            storeProgression,
            null
        );
    }

    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> newOrderAction,
        StoreProgression storeProgression,
        StoreStockOverview newStockOverview
    )
    {
        productData =
            newProductData;

        orderAction =
            newOrderAction;

        stockOverview =
            newStockOverview;

        isLocked =
            false;

        stockRefreshTimer =
            0f;

        if (productData == null) return;

        var showNewLabel =
            storeProgression != null
            &&
            storeProgression
                .WasUnlockedInLastRefresh(
                    productData
                );

        if (showNewLabel)
            productNameText.text =
                "<color="
                + NewColor
                + "><b>NEW</b></color>"
                + " • "
                + productData.ProductName;
        else
            productNameText.text =
                productData.ProductName;

        var safeQuantity =
            Mathf.Max(
                1,
                quantityPerBox
            );

        var boxPrice =
            productData.BuyPrice
            * safeQuantity;

        priceText.text =
            "Box of "
            + safeQuantity
            + " - £"
            + boxPrice.ToString("0.00");

        orderButton.interactable =
            true;

        orderButtonText.text =
            "ORDER";

        orderButton.onClick
            .RemoveAllListeners();

        orderButton.onClick.AddListener(
            HandleOrderClicked
        );

        RefreshStockText();
    }

    /*
     * Compatibility overload.
     */
    public void ConfigureLocked(
        ProductData newProductData
    )
    {
        ConfigureLocked(
            newProductData,
            null,
            null
        );
    }

    /*
     * Compatibility overload from Lesson 44.
     */
    public void ConfigureLocked(
        ProductData newProductData,
        StoreProgression storeProgression
    )
    {
        ConfigureLocked(
            newProductData,
            storeProgression,
            null
        );
    }

    public void ConfigureLocked(
        ProductData newProductData,
        StoreProgression storeProgression,
        StoreStockOverview newStockOverview
    )
    {
        productData =
            newProductData;

        orderAction =
            null;

        stockOverview =
            newStockOverview;

        isLocked =
            true;

        if (productData == null) return;

        productNameText.text =
            productData.ProductName;

        priceText.text =
            BuildRequirementText(
                productData,
                storeProgression
            );

        if (stockText != null)
        {
            stockText.text =
                "";

            stockText.gameObject
                .SetActive(
                    false
                );
        }

        orderButton.interactable =
            false;

        orderButtonText.text =
            "LOCKED";

        orderButton.onClick
            .RemoveAllListeners();
    }

    private void Update()
    {
        if (isLocked) return;

        if (productData == null) return;

        if (stockOverview == null) return;

        stockRefreshTimer +=
            Time.unscaledDeltaTime;

        if (stockRefreshTimer
            < StockRefreshInterval)
            return;

        stockRefreshTimer =
            0f;

        RefreshStockText();
    }

    private void RefreshStockText()
    {
        if (stockText == null) return;

        if (productData == null ||
            stockOverview == null)
        {
            stockText.text =
                "";

            stockText.gameObject
                .SetActive(
                    false
                );

            return;
        }

        stockText.gameObject
            .SetActive(
                true
            );

        var shelfCount =
            stockOverview.GetShelfCount(
                productData
            );

        var storageCount =
            stockOverview.GetStorageCount(
                productData
            );

        var basketCount =
            stockOverview.GetBasketCount(
                productData
            );

        var deliveryCount =
            stockOverview.GetDeliveryCount(
                productData
            );

        var looseCount =
            stockOverview.GetLooseCount(
                productData
            );

        var heldCount =
            stockOverview.GetHeldCount(
                productData
            );

        var totalStock =
            stockOverview.GetTotalStockCount(
                productData
            );

        stockText.text =
            "Shelf "
            + shelfCount
            + "  •  Storage "
            + storageCount
            + "\nBasket "
            + basketCount
            + "  •  Delivery "
            + deliveryCount
            + "\nLoose "
            + looseCount
            + "  •  Held "
            + heldCount
            + "  •  Total "
            + totalStock;

        var status =
            stockOverview.GetStockStatus(
                productData
            );

        switch (status)
        {
            case StockStatus.Low:
                stockText.color =
                    LowStockColor;
                break;

            case StockStatus.Restock:
                stockText.color =
                    RestockColor;
                break;

            case StockStatus.OutOfStock:
                stockText.color =
                    OutOfStockColor;
                break;

            default:
                stockText.color =
                    HealthyStockColor;
                break;
        }
    }

    private string BuildRequirementText(
        ProductData product,
        StoreProgression storeProgression
    )
    {
        var requiresReputation =
            product.RequiredReputation > 0f;

        var requiresStoreLevel =
            product.RequiredStoreLevel > 1;

        if (storeProgression == null)
            return BuildStaticRequirementText(
                product
            );

        var reputationLine =
            "";

        var storeLevelLine =
            "";

        if (requiresReputation)
        {
            var reputationMet =
                storeProgression
                    .MeetsReputationRequirement(
                        product
                    );

            reputationLine =
                "REP "
                + storeProgression
                    .CurrentReputation
                    .ToString("0")
                + " / "
                + product
                    .RequiredReputation
                    .ToString("0")
                + " "
                + GetStatusMarker(
                    reputationMet
                );
        }

        if (requiresStoreLevel)
        {
            var storeLevelMet =
                storeProgression
                    .MeetsStoreLevelRequirement(
                        product
                    );

            storeLevelLine =
                "STORE LV "
                + storeProgression
                    .CurrentStoreLevel
                + " / "
                + product
                    .RequiredStoreLevel
                + " "
                + GetStatusMarker(
                    storeLevelMet
                );
        }

        if (requiresReputation &&
            requiresStoreLevel)
            return reputationLine
                   + "\n"
                   + storeLevelLine;

        if (requiresReputation) return reputationLine;

        if (requiresStoreLevel) return storeLevelLine;

        return "Locked";
    }

    private string BuildStaticRequirementText(
        ProductData product
    )
    {
        var requiresReputation =
            product.RequiredReputation > 0f;

        var requiresStoreLevel =
            product.RequiredStoreLevel > 1;

        if (requiresReputation &&
            requiresStoreLevel)
            return "Requires REP "
                   + product
                       .RequiredReputation
                       .ToString("0")
                   + "\nSTORE LV "
                   + product
                       .RequiredStoreLevel;

        if (requiresReputation)
            return "Unlock at REP "
                   + product
                       .RequiredReputation
                       .ToString("0");

        if (requiresStoreLevel)
            return "Unlock at STORE LV "
                   + product
                       .RequiredStoreLevel;

        return "Locked";
    }

    private string GetStatusMarker(
        bool completed
    )
    {
        if (completed)
            return "<color="
                   + PositiveColor
                   + ">✓</color>";

        return "<color="
               + NegativeColor
               + ">✗</color>";
    }

    private void HandleOrderClicked()
    {
        if (productData == null) return;

        if (orderAction == null) return;

        orderAction.Invoke(
            productData
        );
    }
}