using System.Collections.Generic;
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

    private readonly List<PickupItem>
        storedItems =
            new List<PickupItem>();

    private ShelfRegistry registeredShelfRegistry;

    private StorageRegistry registeredStorageRegistry;

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
        StoredItemCount > 0;

    public int StoredItemCount =>
        storedItems.Count;

    public int Capacity
    {
        get
        {
            /*
             * Back-room storage is still the old
             * one-object system until its dedicated
             * container refactor.
             */
            if (!customerAccessible)
            {
                return 1;
            }

            if (acceptedProduct == null)
            {
                return 1;
            }

            return acceptedProduct
                .SalesShelfCapacity;
        }
    }

    public bool IsFull =>
        StoredItemCount >= Capacity;

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
        RemoveNullEntries();

        if (storedItems.Count <= 0)
        {
            return null;
        }

        PickupItem item =
            storedItems[0];

        if (item == null)
        {
            return null;
        }

        return item.GetProductData();
    }

    public int GetStoredProductCount(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return 0;
        }

        RemoveNullEntries();

        int count =
            0;

        for (int i = 0;
             i < storedItems.Count;
             i++)
        {
            PickupItem item =
                storedItems[i];

            if (item == null)
            {
                continue;
            }

            if (item.GetProductData()
                == productData)
            {
                count++;
            }
        }

        return count;
    }

    public Vector3 GetDisplayWorldPosition(
        int displayIndex
    )
    {
        if (!customerAccessible ||
            acceptedProduct == null)
        {
            return transform.position;
        }

        Vector3 localPosition =
            acceptedProduct
                .GetSalesShelfLocalPosition(
                    displayIndex
                );

        return transform.TransformPoint(
            localPosition
        );
    }

    public Quaternion GetDisplayWorldRotation()
    {
        return transform.rotation;
    }

    public void ClearStoredItemForLoad()
    {
        if (storedItems.Count <= 0)
        {
            return;
        }

        List<PickupItem> itemsToDestroy =
            new List<PickupItem>(
                storedItems
            );

        storedItems.Clear();

        for (int i = 0;
             i < itemsToDestroy.Count;
             i++)
        {
            PickupItem item =
                itemsToDestroy[i];

            if (item == null)
            {
                continue;
            }

            Destroy(
                item.gameObject
            );
        }
    }

    public bool RestoreProduct(
        ProductData productData,
        int quantity = 1
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

        int restoreCount =
            Mathf.Clamp(
                quantity,
                1,
                Capacity
            );

        for (int i = 0;
             i < restoreCount;
             i++)
        {
            PickupItem restoredItem =
                Instantiate(
                    productData.WorldPrefab,
                    transform.position,
                    transform.rotation
                );

            AddStoredItem(
                restoredItem
            );
        }

        if (quantity > Capacity)
        {
            Debug.LogWarning(
                "Saved ShelfSlot '"
                + slotID
                + "' contained "
                + quantity
                + " units, but its current capacity is "
                + Capacity
                + ". Extra units were not restored.",
                this
            );
        }

        return storedItems.Count > 0;
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        RemoveNullEntries();

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            return GetBasketInteractionPrompt(
                heldBasket
            );
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            return GetHeldItemPrompt(
                heldItem
            );
        }

        if (HasStoredItem)
        {
            return GetOccupiedEmptyHandsPrompt();
        }

        return GetEmptySlotPrompt();
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        RemoveNullEntries();

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            HandleBasketInteraction(
                heldBasket
            );

            return;
        }

        if (player.GetHeldItem() != null)
        {
            TryStoreHeldItem(
                player
            );

            return;
        }

        if (HasStoredItem)
        {
            TryRemoveStoredItem(
                player
            );
        }
    }

    private string GetHeldItemPrompt(
        PickupItem heldItem
    )
    {
        if (heldItem == null)
        {
            return "";
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

        if (IsFull)
        {
            if (customerAccessible)
            {
                return "Shelf full  "
                       + StoredItemCount
                       + " / "
                       + Capacity;
            }

            return "Storage occupied";
        }

        if (customerAccessible)
        {
            return "[E] Stock "
                   + heldProduct.ProductName
                   + "  •  "
                   + StoredItemCount
                   + " / "
                   + Capacity;
        }

        return "[E] Store "
               + heldProduct.ProductName;
    }

    private string GetBasketInteractionPrompt(
        RestockBasket basket
    )
    {
        if (basket == null)
        {
            return "";
        }

        /*
         * Sales shelves:
         *
         * Matching products in the basket take
         * priority and one E press fills as many
         * shelf spaces as possible.
         */
        if (customerAccessible)
        {
            if (acceptedProduct == null)
            {
                return "Shelf slot not configured";
            }

            int matchingCount =
                basket.GetProductCount(
                    acceptedProduct
                );

            if (matchingCount > 0)
            {
                if (IsFull)
                {
                    return "Shelf full  "
                           + StoredItemCount
                           + " / "
                           + Capacity;
                }

                return "[E] Restock "
                       + acceptedProduct.ProductName
                       + "  •  "
                       + StoredItemCount
                       + " / "
                       + Capacity
                       + "  •  Hold for more";
            }

            if (HasStoredItem)
            {
                if (basket.IsFull)
                {
                    return "Restock basket full";
                }

                return "[E] Take "
                       + acceptedProduct.ProductName
                       + " into basket  •  "
                       + StoredItemCount
                       + " / "
                       + Capacity;
            }

            return "Basket has no "
                   + acceptedProduct.ProductName;
        }

        /*
         * Back-room storage stays single-unit
         * until its container redesign.
         */
        if (HasStoredItem)
        {
            if (basket.IsFull)
            {
                return "Restock basket full";
            }

            ProductData storedProduct =
                GetStoredProductData();

            string productName =
                storedProduct != null
                    ? storedProduct.ProductName
                    : "product";

            return "[E] Load "
                   + productName
                   + " into basket";
        }

        if (basket.IsEmpty)
        {
            return "Restock basket empty";
        }

        ProductData firstProduct =
            basket.GetFirstProductData();

        if (firstProduct == null)
        {
            return "Restock basket empty";
        }

        return "[E] Store "
               + firstProduct.ProductName
               + " from basket";
    }

    private string GetOccupiedEmptyHandsPrompt()
    {
        ProductData storedProduct =
            GetStoredProductData();

        string productName =
            storedProduct != null
                ? storedProduct.ProductName
                : "product";

        if (customerAccessible)
        {
            return "[E] Pick up "
                   + productName
                   + "  •  "
                   + StoredItemCount
                   + " / "
                   + Capacity;
        }

        return "[E] Take "
               + productName
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
                   + acceptedProduct.ProductName
                   + "  •  0 / "
                   + Capacity;
        }

        return "Empty storage - "
               + acceptedProduct.ProductName;
    }

    private void HandleBasketInteraction(
        RestockBasket basket
    )
    {
        if (basket == null)
        {
            return;
        }

        if (customerAccessible)
        {
            HandleSalesShelfBasketInteraction(
                basket
            );

            return;
        }

        HandleStorageBasketInteraction(
            basket
        );
    }

    private void HandleSalesShelfBasketInteraction(
        RestockBasket basket
    )
    {
        if (basket == null ||
            acceptedProduct == null)
        {
            return;
        }

        /*
         * Basket -> Shelf
         *
         * Transfer exactly ONE matching unit for
         * every interaction.
         *
         * PlayerInteraction will handle repeating
         * this action when E is held.
         */
        if (!IsFull)
        {
            bool tookMatchingItem =
                basket.TryTakeMatchingProduct(
                    acceptedProduct,
                    out PickupItem basketItem
                );

            if (tookMatchingItem &&
                basketItem != null)
            {
                AddStoredItem(
                    basketItem
                );

                return;
            }
        }

        /*
         * Shelf -> Basket
         *
         * If the basket contains no matching product
         * to stock, allow one displayed unit to be
         * moved back into the basket.
         */
        if (!HasStoredItem ||
            basket.IsFull)
        {
            return;
        }

        PickupItem shelfItem =
            GetLastStoredItem();

        if (shelfItem == null)
        {
            return;
        }

        basket.TryAddItem(
            shelfItem
        );
    }

    private void HandleStorageBasketInteraction(
        RestockBasket basket
    )
    {
        if (HasStoredItem)
        {
            if (basket.IsFull)
            {
                return;
            }

            PickupItem item =
                GetLastStoredItem();

            if (item != null)
            {
                basket.TryAddItem(
                    item
                );
            }

            return;
        }

        if (basket.IsEmpty)
        {
            return;
        }

        ProductData firstProduct =
            basket.GetFirstProductData();

        if (!CanAcceptProduct(
                firstProduct
            ))
        {
            return;
        }

        bool tookItem =
            basket.TryTakeFirstItem(
                out PickupItem basketItem
            );

        if (!tookItem ||
            basketItem == null)
        {
            return;
        }

        AddStoredItem(
            basketItem
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

        if (!CanAcceptProduct(
                heldProduct
            ))
        {
            return;
        }

        if (IsFull)
        {
            return;
        }

        player.RemoveHeldItem();

        AddStoredItem(
            heldItem
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

        PickupItem item =
            GetLastStoredItem();

        if (item == null)
        {
            return;
        }

        player.TryPickUp(
            item
        );
    }

    private void AddStoredItem(
        PickupItem item
    )
    {
        if (item == null)
        {
            return;
        }

        if (IsFull)
        {
            return;
        }

        if (storedItems.Contains(
                item
            ))
        {
            return;
        }

        storedItems.Add(
            item
        );

        item.PlaceOnShelf(
            this,
            storedItems.Count - 1
        );
    }

    public void RemoveItem(
        PickupItem item
    )
    {
        if (item == null)
        {
            return;
        }

        bool removed =
            storedItems.Remove(
                item
            );

        if (!removed)
        {
            return;
        }

        RepackStoredItems();
    }

    public bool ContainsProduct(
        ProductData productData
    )
    {
        return GetStoredProductCount(
                   productData
               )
               > 0;
    }

    public bool TryTakeItemForCustomer(
        Transform carryPoint,
        out PickupItem takenItem
    )
    {
        takenItem =
            null;

        if (!customerAccessible)
        {
            return false;
        }

        RemoveNullEntries();

        if (storedItems.Count <= 0)
        {
            return false;
        }

        PickupItem itemToTake =
            storedItems[0];

        if (itemToTake == null)
        {
            return false;
        }

        /*
         * PickUpForCustomer calls back into
         * RemoveItem(), which removes this item
         * from the section and repacks the rest.
         */
        itemToTake.PickUpForCustomer(
            carryPoint
        );

        takenItem =
            itemToTake;

        return true;
    }

    private PickupItem GetLastStoredItem()
    {
        RemoveNullEntries();

        if (storedItems.Count <= 0)
        {
            return null;
        }

        return storedItems[
            storedItems.Count - 1
        ];
    }

    private void RepackStoredItems()
    {
        RemoveNullEntries();

        for (int i = 0;
             i < storedItems.Count;
             i++)
        {
            PickupItem item =
                storedItems[i];

            if (item == null)
            {
                continue;
            }

            item.PlaceOnShelf(
                this,
                i
            );
        }
    }

    private void RemoveNullEntries()
    {
        for (int i =
                 storedItems.Count - 1;
             i >= 0;
             i--)
        {
            if (storedItems[i] != null)
            {
                continue;
            }

            storedItems.RemoveAt(
                i
            );
        }
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
            return "Shelf section is for "
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
                return "that shelf section accepts "
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

            registeredShelfRegistry.RegisterSlot(
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
            registeredShelfRegistry.UnregisterSlot(
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