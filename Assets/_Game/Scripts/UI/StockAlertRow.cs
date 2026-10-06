using TMPro;
using UnityEngine;

public class StockAlertRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text productNameText;

    [SerializeField]
    private TMP_Text countsText;

    [SerializeField]
    private TMP_Text statusText;

    private ProductData productData;

    private static readonly Color32 NameColor =
        new Color32(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 DetailColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 LowColor =
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

    private static readonly Color32 OutColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    public void Configure(
        ProductData newProductData
    )
    {
        productData =
            newProductData;

        if (productData == null)
        {
            return;
        }

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
        if (productData == null)
        {
            gameObject.SetActive(
                false
            );

            return false;
        }

        if (stockOverview == null)
        {
            gameObject.SetActive(
                false
            );

            return false;
        }

        StockStatus status =
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

        int shelfCount =
            stockOverview.GetShelfCount(
                productData
            );

        int backStockCount =
            stockOverview.GetBackStockCount(
                productData
            );

        int totalStock =
            shelfCount
            + backStockCount;

        countsText.text =
            "Shelf "
            + shelfCount
            + "  •  Back "
            + backStockCount
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