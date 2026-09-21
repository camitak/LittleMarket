using UnityEngine;

public class ShelfSlot : MonoBehaviour, IInteractable
{
    [Header("Shelf Rules")]
    [SerializeField] private ProductData acceptedProduct;

    [Header("Customer")]
    [SerializeField] private Transform customerStandPoint;

    private PickupItem storedItem;

    public Transform CustomerStandPoint => customerStandPoint;

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (storedItem != null)
        {
            if (player.GetHeldItem() != null)
            {
                return "Hands full";
            }

            return "[E] Pick up " + storedItem.GetItemName();
        }

        if (acceptedProduct == null)
        {
            return "Shelf slot not configured";
        }

        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
        {
            return "Empty - " + acceptedProduct.ProductName;
        }

        ProductData heldProduct = heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot be stocked here";
        }

        if (heldProduct != acceptedProduct)
        {
            return "This slot is for " + acceptedProduct.ProductName;
        }

        return "[E] Stock " + acceptedProduct.ProductName;
    }

    public void Interact(PlayerInteraction player)
    {
        if (storedItem != null)
        {
            TryRemoveStoredItem(player);
            return;
        }

        TryStoreHeldItem(player);
    }

    private void TryStoreHeldItem(PlayerInteraction player)
    {
        if (acceptedProduct == null)
        {
            return;
        }

        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
        {
            return;
        }

        ProductData heldProduct = heldItem.GetProductData();

        if (heldProduct == null)
        {
            return;
        }

        if (heldProduct != acceptedProduct)
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

    public bool ContainsProduct(ProductData productData)
    {
        if (storedItem == null)
        {
            return false;
        }

        if (productData == null)
        {
            return false;
        }

        return storedItem.GetProductData() == productData;
    }

    public bool TryTakeItemForCustomer(
        Transform carryPoint,
        out PickupItem takenItem
    )
    {
        takenItem = null;

        if (storedItem == null)
        {
            return false;
        }

        PickupItem itemToTake = storedItem;

        itemToTake.PickUp(carryPoint);

        takenItem = itemToTake;

        return true;
    }
}