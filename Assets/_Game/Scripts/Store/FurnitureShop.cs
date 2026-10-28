using UnityEngine;

// Shop-computer purchase rules. No UI ownership, no inventory duplication.
// Money is charged only after the placement controller confirms valid geometry.
[DisallowMultipleComponent]
public class FurnitureShop : MonoBehaviour
{
    [Header("Store")]
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private StoreFurnitureLayout storeFurnitureLayout;

    [Header("Player")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private FurniturePlacementController placementController;

    [Header("Catalog - First Item")]
    [Min(0.01f)] [SerializeField] private float shelfPrice = 40f;

    public float ShelfPrice => shelfPrice;

    public bool CanBeginShelfPurchase(out string reason)
    {
        if (storeEconomy == null || storeClock == null ||
            customerFlow == null || storeFurnitureLayout == null ||
            playerInteraction == null || placementController == null)
        {
            reason = "Furniture purchasing is not configured.";
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
        if (storeEconomy.CurrentMoney < shelfPrice)
        {
            reason = "Not enough money to buy a shelf.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryBeginShelfPurchase(out string reason)
    {
        if (!CanBeginShelfPurchase(out reason)) return false;

        if (!storeFurnitureLayout.TryCreatePreviewShelf(out ShelfIdentity shelf))
        {
            reason = "Could not create shelf preview. Check the prefab and layout service.";
            return false;
        }
        if (!placementController.BeginPurchasedShelfPreview(shelf, this))
        {
            storeFurnitureLayout.DiscardUnconfirmedShelf(shelf);
            reason = "Couldn't start placement. Make sure the Game view is active.";
            return false;
        }

        reason = "";
        return true;
    }

    public bool TryCompletePurchase()
    {
        return storeEconomy != null && storeEconomy.TrySpend(shelfPrice);
    }

    public void CancelPurchase(ShelfIdentity previewShelf)
    {
        if (storeFurnitureLayout != null)
            storeFurnitureLayout.DiscardUnconfirmedShelf(previewShelf);
    }
}
