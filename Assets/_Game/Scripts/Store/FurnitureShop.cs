using UnityEngine;

// Owns furniture purchase transaction rules.
// Furniture data such as price, prefab and footprint lives in FurnitureDefinition.
[DisallowMultipleComponent]
public class FurnitureShop : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private FurnitureCatalog furnitureCatalog;

    [Header("Store")]
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private StoreFurnitureLayout storeFurnitureLayout;

    [Header("Player")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private FurniturePlacementController placementController;

    public int CatalogCount => furnitureCatalog != null ? furnitureCatalog.Count : 0;

    public FurnitureDefinition GetCatalogItem(int index)
    {
        return furnitureCatalog != null ? furnitureCatalog.GetAt(index) : null;
    }

    public bool CanBeginPurchase(FurnitureDefinition definition, out string reason)
    {
        if (furnitureCatalog == null ||
            storeEconomy == null ||
            storeClock == null ||
            customerFlow == null ||
            storeFurnitureLayout == null ||
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

        if (storeClock.IsStoreOpen || storeClock.HasDayEnded ||
            customerFlow.ActiveCustomerCount > 0)
        {
            reason = "Buy furniture while the store is closed and empty.";
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

    public bool TryBeginPurchase(FurnitureDefinition definition, out string reason)
    {
        if (!CanBeginPurchase(definition, out reason))
            return false;

        if (!storeFurnitureLayout.TryCreatePreviewFurniture(definition, out ShelfIdentity furniture))
        {
            reason = $"Could not create {definition.DisplayName}. Check the furniture catalog and prefab.";
            return false;
        }

        if (!placementController.BeginPurchasedFurniturePreview(furniture, this))
        {
            storeFurnitureLayout.DiscardUnconfirmedFurniture(furniture);
            reason = "Couldn't start furniture placement. Make sure the Game view is active.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryCompletePurchase(FurnitureDefinition definition)
    {
        if (definition == null ||
            furnitureCatalog == null ||
            !furnitureCatalog.Contains(definition) ||
            storeEconomy == null)
        {
            return false;
        }

        return storeEconomy.TrySpend(definition.PurchasePrice);
    }

    public void CancelPurchase(ShelfIdentity previewFurniture)
    {
        if (storeFurnitureLayout != null)
            storeFurnitureLayout.DiscardUnconfirmedFurniture(previewFurniture);
    }
}