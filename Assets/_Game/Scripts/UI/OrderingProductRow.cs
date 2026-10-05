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

    private const string PositiveColor =
        "#6FCF97";

    private const string NegativeColor =
        "#C46B6B";

    private const string NewColor =
        "#6FCF97";

    /*
     * Compatibility overload.
     *
     * Existing OrderingUI code can still compile
     * before we update its Configure call.
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
            null
        );
    }

    public void Configure(
        ProductData newProductData,
        int quantityPerBox,
        Action<ProductData> newOrderAction,
        StoreProgression storeProgression
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
            null
        );
    }

    public void ConfigureLocked(
        ProductData newProductData,
        StoreProgression storeProgression
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
                productData,
                storeProgression
            );

        orderButton.interactable =
            false;

        orderButtonText.text =
            "LOCKED";

        orderButton.onClick
            .RemoveAllListeners();
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
                   + ">OK</color>";
        }

        return "<color="
               + NegativeColor
               + ">NEEDED</color>";
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