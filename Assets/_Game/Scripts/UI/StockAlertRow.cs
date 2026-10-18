using TMPro;
using UnityEngine;

public class StockAlertRow : MonoBehaviour
{
    [Header("UI")] [SerializeField] private TMP_Text productNameText;

    [SerializeField] private TMP_Text countsText;

    [SerializeField] private TMP_Text statusText;

    private ProductData productData;

    private static readonly Color32 NameColor =
        new(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 DetailColor =
        new(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 LowColor =
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

    private static readonly Color32 OutColor =
        new(
            154,
            79,
            79,
            255
        );

    public void Configure(
        ProductData newProductData
    )
    {
        productData =
            newProductData;

        if (productData == null) return;

        productNameText.text =
            productData.ProductName;

        productNameText.color =
            NameColor;

        countsText.color =
            DetailColor;
    }

    public bool Refresh(
        StoreStockOverview stockOverview
    )
    {
        if (productData == null ||
            stockOverview == null)
        {
            gameObject.SetActive(
                false
            );

            return false;
        }

        var status =
            stockOverview.GetStockStatus(
                productData
            );

        if (status
            == StockStatus.Healthy)
        {
            gameObject.SetActive(
                false
            );

            return false;
        }

        gameObject.SetActive(
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

        countsText.text =
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

        switch (status)
        {
            case StockStatus.Low:
                statusText.text =
                    "LOW";

                statusText.color =
                    LowColor;
                break;

            case StockStatus.Restock:
                statusText.text =
                    "RESTOCK";

                statusText.color =
                    RestockColor;
                break;

            case StockStatus.OutOfStock:
                statusText.text =
                    "OUT";

                statusText.color =
                    OutColor;
                break;

            default:
                statusText.text =
                    "";
                break;
        }

        return true;
    }
}