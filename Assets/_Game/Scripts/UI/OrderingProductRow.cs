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

    [SerializeField]
    private TMP_Text orderButtonText;

    private ProductData productData;

    private Action<ProductData> orderAction;

    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> newOrderAction
    )
    {
        productData =
            newProductData;

        orderAction =
            newOrderAction;

        if (productData == null)
        {
            return;
        }

        productNameText.text =
            productData.ProductName;

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
    }

    public void ConfigureLocked(
        ProductData newProductData
    )
    {
        productData =
            newProductData;

        orderAction =
            null;

        if (productData == null)
        {
            return;
        }

        productNameText.text =
            productData.ProductName;

        priceText.text =
            BuildRequirementText(
                productData
            );

        orderButton.interactable =
            false;

        orderButtonText.text =
            "LOCKED";

        orderButton.onClick
            .RemoveAllListeners();
    }

    private string BuildRequirementText(
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
                   + product.RequiredReputation
                       .ToString("0")
                   + " • STORE LV "
                   + product.RequiredStoreLevel;
        }

        if (requiresReputation)
        {
            return "Unlock at REP "
                   + product.RequiredReputation
                       .ToString("0");
        }

        if (requiresStoreLevel)
        {
            return "Unlock at STORE LV "
                   + product.RequiredStoreLevel;
        }

        return "Locked";
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