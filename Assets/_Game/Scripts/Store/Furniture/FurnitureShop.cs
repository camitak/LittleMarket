using UnityEngine;

[DisallowMultipleComponent]
public class FurnitureShop : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private FurnitureCatalog furnitureCatalog;

    [Header("Store")]
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreFurnitureLayout storeFurnitureLayout;
    [SerializeField] private StoreLayoutModeController layoutMode;

    [Header("Player")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private FurniturePlacementController placementController;

    [Header("Selling")]
    [Range(0f, 1f)]
    [SerializeField] private float sellRefundFraction = 0.5f;

    public int CatalogCount => furnitureCatalog != null ? furnitureCatalog.Count : 0;

    public FurnitureDefinition GetCatalogItem(int index)
    {
        return furnitureCatalog != null ? furnitureCatalog.GetAt(index) : null;
    }

    public bool IsPurchasableCatalogItem(FurnitureDefinition definition)
    {
        if (!IsPurchaseDefinitionEnabled(definition) || storeFurnitureLayout == null)
        {
            return false;
        }

        return !HasReachedOwnershipLimit(definition);
    }

    public float GetSellRefund(FurnitureDefinition definition)
    {
        if (definition == null)
        {
            return 0f;
        }

        float refund = definition.PurchasePrice * Mathf.Clamp01(sellRefundFraction);

        return Mathf.Round(refund * 100f) / 100f;
    }

    public bool CanSellFurniture(FurnitureIdentity furniture, out float refund, out string reason)
    {
        refund = 0f;

        if (storeEconomy == null || storeFurnitureLayout == null)
        {
            reason = "Furniture selling is not configured.";
            return false;
        }

        if (furniture == null || furniture.FurnitureDefinition == null)
        {
            reason = "This furniture has no valid definition.";
            return false;
        }

        FurnitureDefinition definition = furniture.FurnitureDefinition;

        if (!definition.Sellable)
        {
            reason = $"{definition.DisplayName} cannot be sold.";
            return false;
        }

        if (!storeFurnitureLayout.CanSellFurniture(furniture, out reason))
        {
            return false;
        }

        refund = GetSellRefund(definition);
        reason = "";
        return true;
    }

    public bool TrySellFurniture(FurnitureIdentity furniture, out float refund, out string reason)
    {
        if (!CanSellFurniture(furniture, out refund, out reason))
        {
            return false;
        }

        if (!storeFurnitureLayout.TryRemoveFurniture(furniture, out reason))
        {
            refund = 0f;
            return false;
        }

        storeEconomy.AddMoney(refund);

        return true;
    }

    public bool CanBeginPurchase(FurnitureDefinition definition, out string reason)
    {
        if (furnitureCatalog == null ||
            storeEconomy == null ||
            storeFurnitureLayout == null ||
            layoutMode == null ||
            playerInteraction == null ||
            placementController == null)
        {
            reason = "Furniture purchasing is not configured.";
            return false;
        }

        if (definition == null || !furnitureCatalog.Contains(definition))
        {
            reason = "This furniture is not part of the current catalog.";
            return false;
        }

        if (!definition.IsConfigured)
        {
            reason = $"{definition.DisplayName} is not configured correctly.";
            return false;
        }

        if (!definition.Purchasable)
        {
            reason = $"{definition.DisplayName} cannot be purchased.";
            return false;
        }

        if (HasReachedOwnershipLimit(definition))
        {
            reason = $"You already own the maximum number of {definition.DisplayName}.";
            return false;
        }

        if (playerInteraction.HasAnythingHeld || placementController.IsPlacementActive)
        {
            reason = "Put down carried items and finish existing placement first.";
            return false;
        }

        if (storeEconomy.CurrentMoney < definition.PurchasePrice)
        {
            reason = $"Not enough money to buy {definition.DisplayName}.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryBeginPurchase(FurnitureDefinition definition, out string reason, float? initialYaw = null)
    {
        if (!CanBeginPurchase(definition, out reason))
        {
            return false;
        }

        bool enteredLayoutMode = !layoutMode.IsActive;

        if (enteredLayoutMode && !layoutMode.TryEnterLayoutMode(out reason))
        {
            return false;
        }

        if (!layoutMode.FocusPlacementArea(definition.PlacementArea))
        {
            if (enteredLayoutMode)
            {
                layoutMode.ExitLayoutMode();
            }

            reason = $"No available {GetPlacementAreaDisplayName(definition.PlacementArea)} build area.";
            return false;
        }

        if (!storeFurnitureLayout.TryCreatePreviewFurniture(definition, out FurnitureIdentity furniture))
        {
            if (enteredLayoutMode)
            {
                layoutMode.ExitLayoutMode();
            }

            reason = $"Could not create {definition.DisplayName}. Check the furniture catalog and prefab.";
            return false;
        }

        if (!placementController.BeginPurchasedFurniturePreview(furniture, this, initialYaw))
        {
            storeFurnitureLayout.DiscardUnconfirmedFurniture(furniture);

            if (enteredLayoutMode)
            {
                layoutMode.ExitLayoutMode();
            }

            reason = "Couldn't start furniture placement. Make sure the Game view is active.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryCompletePurchase(FurnitureDefinition definition)
    {
        if (definition == null ||
            !definition.Purchasable ||
            furnitureCatalog == null ||
            !furnitureCatalog.Contains(definition) ||
            storeEconomy == null)
        {
            return false;
        }

        return storeEconomy.TrySpend(definition.PurchasePrice);
    }

    public void CancelPurchase(FurnitureIdentity previewFurniture)
    {
        if (storeFurnitureLayout == null)
        {
            return;
        }

        storeFurnitureLayout.DiscardUnconfirmedFurniture(previewFurniture);
    }

    private bool IsPurchaseDefinitionEnabled(FurnitureDefinition definition)
    {
        return furnitureCatalog != null &&
               definition != null &&
               furnitureCatalog.Contains(definition) &&
               definition.IsConfigured &&
               definition.Purchasable;
    }

    private bool HasReachedOwnershipLimit(FurnitureDefinition definition)
    {
        if (definition == null ||
            definition.MaxOwned <= 0 ||
            storeFurnitureLayout == null)
        {
            return false;
        }

        return storeFurnitureLayout.CountFurnitureByDefinition(definition) >= definition.MaxOwned;
    }

    private static string GetPlacementAreaDisplayName(StoreAreaType areaType)
    {
        return areaType == StoreAreaType.Backroom ? "Backroom" : "Sales Floor";
    }
}