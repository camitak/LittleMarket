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
    private Button orderButton;

    private ProductData productData;

    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> onOrderRequested
    )
    {
        productData =
            newProductData;

        orderButton.onClick.RemoveAllListeners();

        SetButtonLabel(
            "ORDER"
        );

        if (productData == null)
        {
            ShowUnconfiguredState();

            return;
        }

        productNameText.text =
            productData.ProductName;

        float orderCost =
            productData.BuyPrice
            * quantityPerBox;

        priceText.text =
            "Box of "
            + quantityPerBox
            + " - £"
            + orderCost.ToString("0.00");

        orderButton.interactable =
            true;

        orderButton.onClick.AddListener(
            () =>
            {
                onOrderRequested?.Invoke(
                    productData
                );
            }
        );
    }

    public void ConfigureLocked(
        ProductData lockedProduct
    )
    {
        productData =
            lockedProduct;

        orderButton.onClick.RemoveAllListeners();

        if (productData == null)
        {
            ShowUnconfiguredState();

            return;
        }

        productNameText.text =
            productData.ProductName;

        priceText.text =
            "Unlock at REP "
            + productData.RequiredReputation
                .ToString("0");

        orderButton.interactable =
            false;

        SetButtonLabel(
            "LOCKED"
        );
    }

    private void ShowUnconfiguredState()
    {
        productNameText.text =
            "Product not configured";

        priceText.text = "";

        orderButton.interactable =
            false;

        SetButtonLabel(
            "LOCKED"
        );
    }

    private void SetButtonLabel(
        string label
    )
    {
        TMP_Text buttonText =
            orderButton
                .GetComponentInChildren<
                    TMP_Text
                >();

        if (buttonText == null)
        {
            return;
        }

        buttonText.text =
            label;
    }
}