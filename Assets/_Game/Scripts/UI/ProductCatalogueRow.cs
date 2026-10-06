using TMPro;
using UnityEngine;

public class ProductCatalogueRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text productNameText;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private TMP_Text statusText;

    private static readonly Color32 NormalNameColor =
        new Color32(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 NormalTextColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 UnlockedColor =
        new Color32(
            111,
            207,
            151,
            255
        );

    private static readonly Color32 LockedColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    public void Configure(
        ProductData productData,
        StoreProgression storeProgression
    )
    {
        if (productData == null)
        {
            return;
        }

        productNameText.color =
            NormalNameColor;

        categoryText.color =
            NormalTextColor;

        priceText.color =
            NormalTextColor;

        bool isUnlocked =
            storeProgression != null
            &&
            storeProgression
                .IsProductUnlocked(
                    productData
                );

        bool isNew =
            isUnlocked
            &&
            storeProgression
                .WasUnlockedInLastRefresh(
                    productData
                );

        if (isNew)
        {
            productNameText.text =
                "<color=#6FCF97><b>NEW</b></color>"
                + " • "
                + productData.ProductName;
        }
        else
        {
            productNameText.text =
                productData.ProductName;
        }

        categoryText.text =
            productData.Category
                .ToString()
                .ToUpperInvariant();

        priceText.text =
            "BUY  £"
            + productData.BuyPrice
                .ToString("0.00")
            + "\nSELL £"
            + productData.SellPrice
                .ToString("0.00");

        if (isUnlocked)
        {
            statusText.text =
                "UNLOCKED";

            statusText.color =
                UnlockedColor;

            return;
        }

        statusText.color =
            NormalTextColor;

        statusText.text =
            BuildLockedStatus(
                productData,
                storeProgression
            );
    }

    private string BuildLockedStatus(
        ProductData productData,
        StoreProgression storeProgression
    )
    {
        if (storeProgression == null)
        {
            return "LOCKED";
        }

        string result =
            "<color=#C46B6B><b>LOCKED</b></color>";

        if (productData.RequiredReputation > 0f)
        {
            bool reputationMet =
                storeProgression
                    .MeetsReputationRequirement(
                        productData
                    );

            result +=
                "\nREP "
                + storeProgression
                    .CurrentReputation
                    .ToString("0")
                + " / "
                + productData
                    .RequiredReputation
                    .ToString("0")
                + " "
                + GetMarker(
                    reputationMet
                );
        }

        if (productData.RequiredStoreLevel > 1)
        {
            bool levelMet =
                storeProgression
                    .MeetsStoreLevelRequirement(
                        productData
                    );

            result +=
                "\nSTORE LV "
                + storeProgression
                    .CurrentStoreLevel
                + " / "
                + productData
                    .RequiredStoreLevel
                + " "
                + GetMarker(
                    levelMet
                );
        }

        return result;
    }

    private string GetMarker(
        bool completed
    )
    {
        if (completed)
        {
            return "<color=#6FCF97>✓</color>";
        }

        return "<color=#C46B6B>✗</color>";
    }
}