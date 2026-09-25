using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class OrderCatalogEntry
{
    [SerializeField]
    private TMP_Text productNameText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private Button orderButton;

    private ProductData productData;

    public ProductData ProductData =>
        productData;

    public Button OrderButton =>
        orderButton;

    public void SetProductData(
        ProductData newProductData
    )
    {
        productData =
            newProductData;
    }

    public void RefreshDisplay(
        int quantityPerBox
    )
    {
        if (productData == null)
        {
            if (productNameText != null)
            {
                productNameText.text =
                    "Product not configured";
            }

            if (priceText != null)
            {
                priceText.text = "";
            }

            if (orderButton != null)
            {
                orderButton.interactable =
                    false;
            }

            return;
        }

        if (productNameText != null)
        {
            productNameText.text =
                productData.ProductName;
        }

        if (priceText != null)
        {
            float orderCost =
                productData.BuyPrice
                * quantityPerBox;

            priceText.text =
                "Box of "
                + quantityPerBox
                + " - £"
                + orderCost.ToString("0.00");
        }

        if (orderButton != null)
        {
            orderButton.interactable =
                true;
        }
    }
}