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

    public void Configure(
        FurnitureDefinition newDefinition,
        Action<FurnitureDefinition> newBuyAction)
    {
        definition = newDefinition;
        buyAction = newBuyAction;

        buyButton.onClick.RemoveAllListeners();

        if (definition == null)
        {
            ShowUnconfiguredState();
            return;
        }

        furnitureNameText.text = definition.DisplayName;
        descriptionText.text = definition.Description;
        priceText.text = $"£{definition.PurchasePrice:0.00}";
        buyButtonText.text = "BUY & PLACE";

        buyButton.interactable = definition.IsConfigured;
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
        if (definition == null || buyAction == null)
            return;

        buyAction.Invoke(definition);
    }
}