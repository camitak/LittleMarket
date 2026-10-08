using UnityEngine;

public class ShelfSlot :
    MonoBehaviour,
    IInteractable
{
    [Header("Identity")]
    [SerializeField]
    private string slotID = "";

    [Header("Shelf Rules")]
    [SerializeField]
    private ProductData acceptedProduct;

    [Header("Customer")]
    [SerializeField]
    private Transform customerStandPoint;

    [Header("Access")]
    [SerializeField]
    private bool customerAccessible = true;

    private ShelfRegistry registeredShelfRegistry;

    private StorageRegistry registeredStorageRegistry;

    private PickupItem storedItem;

    public string SlotID =>
        slotID;

    public Transform CustomerStandPoint =>
        customerStandPoint;

    public bool CustomerAccessible =>
        customerAccessible;

    public bool IsGenericStorage =>
        !customerAccessible
        && acceptedProduct == null;

    public bool HasStoredItem =>
        storedItem != null;

    private void Start()
    {
        RegisterWithCorrectRegistry();
    }

    private void OnDestroy()
    {
        UnregisterFromCurrentRegistry();
    }

    public bool CanAcceptProduct(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (IsGenericStorage)
        {
            return true;
        }

        if (acceptedProduct == null)
        {
            return false;
        }

        return acceptedProduct
               == productData;
    }

    public ProductData GetStoredProductData()
    {
        if (storedItem == null)
        {
            return null;
        }

        return storedItem.GetProductData();
    }

    public void ClearStoredItemForLoad()
    {
        if (storedItem == null)
        {
            return;
        }

        PickupItem itemToDestroy =
            storedItem;

        storedItem =
            null;

        Destroy(
            itemToDestroy.gameObject
        );
    }

    public bool RestoreProduct(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (productData.WorldPrefab == null)
        {
            return false;
        }

        if (!CanAcceptProduct(
                productData
            ))
        {
            Debug.LogWarning(
                "Cannot restore "
                + productData.ProductName
                + " into ShelfSlot '"
                + slotID
                + "' because "
                + GetRestoreRuleDescription()
                + ".",
                this
            );

            return false;
        }

        ClearStoredItemForLoad();

        PickupItem restoredItem =
            Instantiate(
                productData.WorldPrefab,
                transform.position,
                transform.rotation
            );

        storedItem =
            restoredItem;

        restoredItem.PlaceOnShelf(
            this
        );

        return true;
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (storedItem != null)
        {
            return GetOccupiedSlotPrompt(
                heldItem
            );
        }

        if (!IsGenericStorage &&
            acceptedProduct == null)
        {
            if (customerAccessible)
            {
                return "Shelf slot not configured";
            }

            return "Storage slot not configured";
        }

        if (heldItem == null)
        {
            return GetEmptySlotPrompt();
        }

        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot be stocked here";
        }

        if (!CanAcceptProduct(
                heldProduct
            ))
        {
            return GetRejectedProductPrompt();
        }

        if (customerAccessible)
        {
            return "[E] Stock "
                   + heldProduct.ProductName;
        }

        return "[E] Store "
               + heldProduct.ProductName;
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        if (storedItem != null)
        {
            TryRemoveStoredItem(
                player
            );

            return;
        }

        TryStoreHeldItem(
            player
        );
    }

    private void TryStoreHeldItem(
        PlayerInteraction player
    )
    {
        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem == null)
        {
            return;
        }

        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null)
        {
            return;
        }

        if (!CanAcceptProduct(
                heldProduct
            ))
        {
            return;
        }

        StoreItem(
            heldItem,
            player
        );
    }

    private void StoreItem(
        PickupItem item,
        PlayerInteraction player
    )
    {
        storedItem =
            item;

        player.RemoveHeldItem();

        item.PlaceOnShelf(
            this
        );
    }

    private void TryRemoveStoredItem(
        PlayerInteraction player
    )
    {
        if (player.GetHeldItem() != null)
        {
            return;
        }

        player.TryPickUp(
            storedItem
        );
    }

    public void RemoveItem(
        PickupItem item
    )
    {
        if (storedItem != item)
        {
            return;
        }

        storedItem =
            null;
    }

    public bool ContainsProduct(
        ProductData productData
    )
    {
        if (storedItem == null)
        {
            return false;
        }

        if (productData == null)
        {
            return false;
        }

        return storedItem.GetProductData()
               == productData;
    }

    public bool TryTakeItemForCustomer(
        Transform carryPoint,
        out PickupItem takenItem
    )
    {
        takenItem =
            null;

        /*
         * Storage slots should never be used
         * by customers, even if one is passed
         * here directly by mistake.
         */
        if (!customerAccessible)
        {
            return false;
        }

        if (storedItem == null)
        {
            return false;
        }

        PickupItem itemToTake =
            storedItem;

        itemToTake.PickUp(
            carryPoint
        );

        takenItem =
            itemToTake;

        return true;
    }

    private string GetOccupiedSlotPrompt(
        PickupItem heldItem
    )
    {
        string storedItemName =
            storedItem.GetItemName();

        if (heldItem != null)
        {
            if (customerAccessible)
            {
                return "Shelf occupied: "
                       + storedItemName;
            }

            return "Storage occupied: "
                   + storedItemName;
        }

        if (customerAccessible)
        {
            return "[E] Pick up "
                   + storedItemName;
        }

        return "[E] Take "
               + storedItemName
               + " from storage";
    }

    private string GetEmptySlotPrompt()
    {
        if (IsGenericStorage)
        {
            return "Empty storage slot";
        }

        if (acceptedProduct == null)
        {
            if (customerAccessible)
            {
                return "Shelf slot not configured";
            }

            return "Storage slot not configured";
        }

        if (customerAccessible)
        {
            return "Empty shelf - "
                   + acceptedProduct.ProductName;
        }

        return "Empty storage - "
               + acceptedProduct.ProductName;
    }

    private string GetRejectedProductPrompt()
    {
        if (acceptedProduct == null)
        {
            if (customerAccessible)
            {
                return "Shelf slot not configured";
            }

            return "Storage slot not configured";
        }

        if (customerAccessible)
        {
            return "Shelf slot is for "
                   + acceptedProduct.ProductName;
        }

        return "Storage slot is for "
               + acceptedProduct.ProductName;
    }

    private string GetRestoreRuleDescription()
    {
        if (acceptedProduct != null)
        {
            if (customerAccessible)
            {
                return "that shelf slot accepts "
                       + acceptedProduct.ProductName;
            }

            return "that storage slot accepts "
                   + acceptedProduct.ProductName;
        }

        if (customerAccessible)
        {
            return "that customer-facing shelf "
                   + "has no Accepted Product configured";
        }

        return "that storage slot is not configured";
    }

    private void RegisterWithCorrectRegistry()
    {
        if (customerAccessible)
        {
            registeredShelfRegistry =
                Object.FindAnyObjectByType<
                    ShelfRegistry
                >();

            if (registeredShelfRegistry == null)
            {
                Debug.LogError(
                    "ShelfSlot '"
                    + gameObject.name
                    + "' could not find a ShelfRegistry.",
                    this
                );

                return;
            }

            registeredShelfRegistry
                .RegisterSlot(
                    this
                );

            return;
        }

        registeredStorageRegistry =
            Object.FindAnyObjectByType<
                StorageRegistry
            >();

        if (registeredStorageRegistry == null)
        {
            Debug.LogError(
                "Storage ShelfSlot '"
                + gameObject.name
                + "' could not find a StorageRegistry.",
                this
            );

            return;
        }

        registeredStorageRegistry.Register(
            this
        );
    }

    private void UnregisterFromCurrentRegistry()
    {
        if (registeredShelfRegistry != null)
        {
            registeredShelfRegistry
                .UnregisterSlot(
                    this
                );

            registeredShelfRegistry =
                null;
        }

        if (registeredStorageRegistry != null)
        {
            registeredStorageRegistry.Unregister(
                this
            );

            registeredStorageRegistry =
                null;
        }
    }
}