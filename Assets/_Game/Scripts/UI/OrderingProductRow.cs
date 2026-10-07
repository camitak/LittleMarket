using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrderingProductRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text productNameText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private TMP_Text stockText;

    [SerializeField]
    private Button orderButton;

    [SerializeField]
    private TMP_Text orderButtonText;

    private ProductData productData;

    private Action<ProductData> orderAction;

    private StoreStockOverview stockOverview;

    private bool isLocked;

    private float stockRefreshTimer;

    private const float StockRefreshInterval =
        0.25f;

    private const string PositiveColor =
        "#6FCF97";

    private const string NegativeColor =
        "#C46B6B";

    private const string NewColor =
        "#6FCF97";

    private static readonly Color32 HealthyStockColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 LowStockColor =
        new Color32(
            246,
            215,
            122,
            255
        );

    private static readonly Color32 RestockColor =
        new Color32(
            143,
            197,
            232,
            255
        );

    private static readonly Color32 OutOfStockColor =
        new Color32(
            196,
            107,
            107,
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

        if (productData == null)
        {
            return;
        }

        bool showNewLabel =
            storeProgression != null
            &&
            storeProgression
                .WasUnlockedInLastRefresh(
                    productData
                );

        if (showNewLabel)
        {
            productNameText.text =
                "<color="
                + NewColor
                + "><b>NEW</b></color>"
                + " • "
                + productData.ProductName;
        }
        else
        {
            productNameText.text =
                productData.ProductName;
        }

        int safeQuantity =
            Mathf.Max(
                1,
                quantityPerBox
            );

        float boxPrice =
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

        if (productData == null)
        {
            return;
        }

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
        if (isLocked)
        {
            return;
        }

        if (productData == null)
        {
            return;
        }

        if (stockOverview == null)
        {
            return;
        }

        stockRefreshTimer +=
            Time.unscaledDeltaTime;

        if (stockRefreshTimer
            < StockRefreshInterval)
        {
            return;
        }

        stockRefreshTimer =
            0f;

        RefreshStockText();
    }

    private void RefreshStockText()
    {
        if (stockText == null)
        {
            return;
        }

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

        int shelfCount =
            stockOverview.GetShelfCount(
                productData
            );

        int storageCount =
            stockOverview.GetStorageCount(
                productData
            );

        int deliveryCount =
            stockOverview.GetDeliveryCount(
                productData
            );

        int looseCount =
            stockOverview.GetLooseCount(
                productData
            );

        int heldCount =
            stockOverview.GetHeldCount(
                productData
            );

        int totalStock =
            shelfCount
            + storageCount
            + deliveryCount
            + looseCount
            + heldCount;

        stockText.text =
            "Shelf "
            + shelfCount
            + "  •  Storage "
            + storageCount
            + "  •  Delivery "
            + deliveryCount
            + "\nLoose "
            + looseCount
            + "  •  Held "
            + heldCount
            + "  •  Total "
            + totalStock;

        StockStatus status =
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
        bool requiresReputation =
            product.RequiredReputation > 0f;

        bool requiresStoreLevel =
            product.RequiredStoreLevel > 1;

        if (storeProgression == null)
        {
            return BuildStaticRequirementText(
                product
            );
        }

        string reputationLine =
            "";

        string storeLevelLine =
            "";

        if (requiresReputation)
        {
            bool reputationMet =
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
            bool storeLevelMet =
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
        {
            return reputationLine
                   + "\n"
                   + storeLevelLine;
        }

        if (requiresReputation)
        {
            return reputationLine;
        }

        if (requiresStoreLevel)
        {
            return storeLevelLine;
        }

        return "Locked";
    }

    private string BuildStaticRequirementText(
        ProductData product
    )
    {
        bool requiresReputation =
            product.RequiredReputation > 0f;

        bool requiresStoreLevel =
            product.RequiredStoreLevel > 1;

        if (requiresReputation &&
            requiresStoreLevel)
        {
            return "Requires REP "
                   + product
                       .RequiredReputation
                       .ToString("0")
                   + "\nSTORE LV "
                   + product
                       .RequiredStoreLevel;
        }

        if (requiresReputation)
        {
            return "Unlock at REP "
                   + product
                       .RequiredReputation
                       .ToString("0");
        }

        if (requiresStoreLevel)
        {
            return "Unlock at STORE LV "
                   + product
                       .RequiredStoreLevel;
        }

        return "Locked";
    }

    private string GetStatusMarker(
        bool completed
    )
    {
        if (completed)
        {
            return "<color="
                   + PositiveColor
                   + ">✓</color>";
        }

        return "<color="
               + NegativeColor
               + ">✗</color>";
    }

    private void HandleOrderClicked()
    {
        if (productData == null)
        {
            return;
        }

        if (orderAction == null)
        {
            return;
        }

        orderAction.Invoke(
            productData
        );
    }
}