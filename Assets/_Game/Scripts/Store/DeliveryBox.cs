using UnityEngine;

public class DeliveryBox :
    MonoBehaviour,
    IInteractable
{
    [Header("Contents")]
    [SerializeField]
    private ProductData productData;

    [Min(0)]
    [SerializeField]
    private int quantity = 4;

    [Header("References")]
    [SerializeField]
    private Transform spawnPoint;

    private bool isOpen;

    public ProductData ProductData =>
        productData;

    public int RemainingQuantity =>
        quantity;

    public bool IsOpen =>
        isOpen;

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (productData == null)
        {
            return "Delivery box not configured";
        }

        if (productData.WorldPrefab == null)
        {
            return "Product prefab not configured";
        }

        if (!isOpen)
        {
            return "[E] Open delivery box";
        }

        if (quantity <= 0)
        {
            if (player.GetHeldItem() != null ||
                player.GetHeldRestockBasket() != null)
            {
                return "Put down what you're carrying "
                       + "to remove box";
            }

            return "[E] Remove empty box";
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket.IsFull)
            {
                return "Restock basket full"
                       + " ("
                       + heldBasket.ItemCount
                       + " / "
                       + heldBasket.Capacity
                       + ")";
            }

            return "[E] Load "
                   + productData.ProductName
                   + " into basket"
                   + " ("
                   + heldBasket.ItemCount
                   + " / "
                   + heldBasket.Capacity
                   + ")";
        }

        if (player.GetHeldItem() != null)
        {
            return "Hands full";
        }

        return "[E] Take "
               + productData.ProductName
               + " ("
               + quantity
               + " left)";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (!isOpen)
        {
            OpenBox();

            return;
        }

        if (quantity <= 0)
        {
            if (player.GetHeldItem() != null ||
                player.GetHeldRestockBasket() != null)
            {
                return;
            }

            Destroy(
                gameObject
            );

            return;
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket.IsFull)
            {
                return;
            }

            DispenseItemToBasket(
                heldBasket
            );

            return;
        }

        if (player.GetHeldItem() != null)
        {
            return;
        }

        if (productData == null ||
            productData.WorldPrefab == null)
        {
            return;
        }

        DispenseItem(
            player
        );
    }

    private void OpenBox()
    {
        isOpen =
            true;
    }

    private void DispenseItem(
        PlayerInteraction player
    )
    {
        PickupItem spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        bool pickupSucceeded =
            player.TryPickUp(
                spawnedItem
            );

        if (pickupSucceeded)
        {
            quantity--;
        }
        else
        {
            Destroy(
                spawnedItem.gameObject
            );
        }
    }

    private void DispenseItemToBasket(
        RestockBasket basket
    )
    {
        PickupItem spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        bool added =
            basket.TryAddItem(
                spawnedItem
            );

        if (added)
        {
            quantity--;
        }
        else
        {
            Destroy(
                spawnedItem.gameObject
            );
        }
    }

    public void Configure(
        ProductData newProductData,
        int newQuantity
    )
    {
        RestoreState(
            newProductData,
            newQuantity,
            false
        );
    }

    public void RestoreState(
        ProductData newProductData,
        int newQuantity,
        bool newIsOpen
    )
    {
        productData =
            newProductData;

        quantity =
            Mathf.Max(
                0,
                newQuantity
            );

        isOpen =
            newIsOpen;
    }
}