using UnityEngine;

public class ShelfSlot : MonoBehaviour, IInteractable
{
    [Header("Shelf Rules")]
    [SerializeField]
    private ProductCategory acceptedCategory = ProductCategory.Cereal;

    private PickupItem storedItem;

    public string GetInteractionPrompt()
    {
        if (storedItem != null)
        {
            return "[E] Pick up " + storedItem.GetItemName();
        }

        return "[E] Stock item";
    }

    public void Interact(PlayerInteraction player)
    {
        // CASE 1:
        // There is already a product in this slot.
        if (storedItem != null)
        {
            TryRemoveStoredItem(player);
            return;
        }

        // CASE 2:
        // The slot is empty, so try to stock
        // whatever the player is currently holding.
        TryStoreHeldItem(player);
    }

    private void TryStoreHeldItem(PlayerInteraction player)
    {
        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
        {
            return;
        }

        ProductData productData = heldItem.GetProductData();

        if (productData == null)
        {
            return;
        }

        if (productData.Category != acceptedCategory)
        {
            return;
        }

        StoreItem(heldItem, player);
    }

    private void StoreItem(
        PickupItem item,
        PlayerInteraction player
    )
    {
        storedItem = item;

        player.RemoveHeldItem();

        item.PlaceOnShelf(this);
    }

    private void TryRemoveStoredItem(PlayerInteraction player)
    {
        // The player cannot pick up another product
        // if they are already carrying something.
        if (player.GetHeldItem() != null)
        {
            return;
        }

        player.TryPickUp(storedItem);
    }

    public void RemoveItem(PickupItem item)
    {
        if (storedItem != item)
        {
            return;
        }

        storedItem = null;
    }
}