
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FurnitureCatalogRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text furnitureNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    private FurnitureDefinition definition;
    private Action<FurnitureDefinition> buyAction;

    public void Configure(FurnitureDefinition newDefinition, Action<FurnitureDefinition> newBuyAction)
    {
        definition = newDefinition;
        buyAction = newBuyAction;

        buyButton.onClick.RemoveAllListeners();

        if (definition == null)
        {
            ShowUnconfiguredState();
            return;
        }

        FurnitureShop shop = FindFirstObjectByType<FurnitureShop>();

        int ownedCount = shop != null ? shop.GetOwnedCount(definition) : 0;
        int ownershipLimit = definition.MaxOwned;

        bool hasLimit = ownershipLimit > 0;
        bool limitReached = hasLimit && ownedCount >= ownershipLimit;

        furnitureNameText.text = definition.DisplayName;
        descriptionText.text = definition.Description;

        if (hasLimit)
        {
            descriptionText.text += $"\nOwned {ownedCount} / {ownershipLimit}";
        }

        priceText.text = $"£{definition.PurchasePrice:0.00}";

        if (limitReached)
        {
            buyButtonText.text = "SOLD OUT";
            buyButton.interactable = false;
        }
        else
        {
            buyButtonText.text = "BUY & PLACE";
            buyButton.interactable = shop != null &&
                                     shop.IsPurchasableCatalogItem(definition);
        }

        buyButton.onClick.AddListener(HandleBuyClicked);
    }

    private void ShowUnconfiguredState()
    {
        furnitureNameText.text = "Furniture not configured";
        descriptionText.text = "";
        priceText.text = "";
        buyButtonText.text = "UNAVAILABLE";
        buyButton.interactable = false;
    }

    private void HandleBuyClicked()
    {
        if (definition == null || buyAction == null || !buyButton.interactable)
        {
            return;
        }

        buyAction.Invoke(definition);
    }
}
