using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreExpansionRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text expansionNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;

    private StoreExpansionZone zone;
    private Action<StoreExpansionZone> purchaseAction;

    public void Configure(
        StoreExpansionZone newZone,
        Action<StoreExpansionZone> newPurchaseAction)
    {
        zone = newZone;
        purchaseAction = newPurchaseAction;

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);
        }

        Refresh(false);
    }

    public void Refresh(bool confirmationPending)
    {
        if (zone == null || zone.Definition == null)
        {
            ShowUnconfiguredState();
            return;
        }

        StoreExpansionDefinition definition =
            zone.Definition;

        expansionNameText.text =
            definition.DisplayName;

        descriptionText.text =
            definition.Description;

        if (zone.IsUnlocked)
        {
            priceText.text = "OWNED";
            purchaseButtonText.text = "PURCHASED";
            purchaseButton.interactable = false;
            return;
        }

        priceText.text =
            $"£{definition.PurchasePrice:0.00}  •  STORE LV {definition.RequiredStoreLevel}";

        purchaseButtonText.text =
            confirmationPending
                ? "CONFIRM"
                : "BUY EXPANSION";

        purchaseButton.interactable = true;
    }

    private void ShowUnconfiguredState()
    {
        expansionNameText.text =
            "Expansion not configured";

        descriptionText.text = "";
        priceText.text = "";

        purchaseButtonText.text =
            "UNAVAILABLE";

        purchaseButton.interactable = false;
    }

    private void HandlePurchaseClicked()
    {
        if (zone == null ||
            purchaseAction == null)
        {
            return;
        }

        purchaseAction.Invoke(zone);
    }
}