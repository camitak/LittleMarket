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

    private ShelfRegistry shelfRegistry;

    public string SlotID => slotID;

    public Transform CustomerStandPoint => customerStandPoint;
    
    public bool CustomerAccessible => customerAccessible;

    private void Start()
    {
        RegisterWithCorrectRegistry();
    }
    
    public bool IsGenericStorage => !customerAccessible && acceptedProduct == null;

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

        return acceptedProduct == productData;
    }

    private void OnDestroy()
    {
        UnregisterFromCurrentRegistry();
            
        if (shelfRegistry == null)
        {
            return;
        }

        shelfRegistry.UnregisterSlot(this);
    }

    private void FindAndRegisterWithShelfRegistry()
    {
        shelfRegistry = Object.FindAnyObjectByType<ShelfRegistry>();

        if (shelfRegistry == null)
        {
            Debug.LogError(
                "ShelfSlot '"
                + gameObject.name
                + "' could not find a "
                + "ShelfRegistry in the scene.",
                this
            );

            return;
        }

        shelfRegistry.RegisterSlot(this);
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

        PickupItem itemToDestroy = storedItem;

        storedItem = null;

        Destroy(itemToDestroy.gameObject);
    }

    public bool RestoreProduct(ProductData productData)
    {
        if (productData == null)
        {
            return false;
        }

        if (productData.WorldPrefab == null)
        {
            return false;
        }

        if (acceptedProduct != null &&
            !CanAcceptProduct(productData))
        {
            Debug.LogWarning(
                "Cannot restore "
                + productData.ProductName
                + " into ShelfSlot '"
                + slotID
                + "' because that slot accepts "
                + acceptedProduct.ProductName
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

        storedItem = restoredItem;

        restoredItem.PlaceOnShelf(this);

        return true;
    }
    
    private string GetAcceptedProductLabel()
    {
        if (acceptedProduct != null)
        {
            return acceptedProduct.ProductName;
        }

        if (IsGenericStorage)
        {
            return "any product";
        }

        return "unconfigured";
    }
    
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

        if (acceptedProduct == null && !IsGenericStorage)
        {
            return "Shelf slot not configured";
        }

        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
        {
            return "Empty - " + GetAcceptedProductLabel();
        }

        ProductData heldProduct = heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot be stocked here";
        }

        if (!CanAcceptProduct(heldProduct))
        {
            return "This slot is for " + GetAcceptedProductLabel();
        }

        return "[E] Stock " + GetAcceptedProductLabel();
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
        // if (acceptedProduct == null)
        // {
        //     return;
        // }

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

        if (!CanAcceptProduct(heldProduct))
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

            registeredShelfRegistry.RegisterSlot(this);
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

        registeredStorageRegistry.Register(this);
    }

    private void UnregisterFromCurrentRegistry()
    {
        if (registeredShelfRegistry != null)
        {
            registeredShelfRegistry.UnregisterSlot(this);

            registeredShelfRegistry = null;
        }

        if (registeredStorageRegistry != null)
        {
            registeredStorageRegistry.Unregister(this);

            registeredStorageRegistry = null;
        }
    }
}