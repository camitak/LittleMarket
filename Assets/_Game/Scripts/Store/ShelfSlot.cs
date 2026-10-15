using System.Collections.Generic;
using UnityEngine;

public class ShelfSlot :
    MonoBehaviour,
    IInteractable,
    ISecondaryInteractable,
    IRepeatableInteractable
{
    [Header("Identity")]
    [SerializeField]
    private string slotID = "";

    [Header("Shelf Rules")]
    [SerializeField]
    private ProductData acceptedProduct;
    
    [Header("Placement")]
    [SerializeField]
    private Transform displayOrigin;

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
        Transform placementFrame =
            GetDisplayPlacementFrame();

        if (!customerAccessible ||
            acceptedProduct == null)
        {
            return placementFrame.position;
        }

        Vector3 localPosition =
            acceptedProduct
                .GetSalesShelfLocalPosition(
                    displayIndex
                );

        return placementFrame.TransformPoint(
            localPosition
        );
    }

    public Quaternion GetDisplayWorldRotation()
    {
        return GetDisplayPlacementFrame()
            .rotation;
    }
    
    private Transform GetDisplayPlacementFrame()
    {
        if (displayOrigin != null)
        {
            return displayOrigin;
        }

        /*
         * Backward-compatible fallback while older
         * shelf prefabs are being migrated.
         */
        return transform;
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
        
        DeliveryBox heldDeliveryBox =
            player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            return GetDeliveryBoxInteractionPrompt(
                heldDeliveryBox
            );
        }

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
        
        DeliveryBox heldDeliveryBox =
            player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            TryStockFromDeliveryBox(
                heldDeliveryBox
            );

            return;
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        /*
         * E with basket:
         *
         * Basket -> target only.
         */
        if (heldBasket != null)
        {
            HandleBasketInteraction(
                heldBasket
            );

            return;
        }

        /*
         * E with individual product:
         *
         * Hand -> target only.
         */
        if (player.GetHeldItem() != null)
        {
            TryStoreHeldItem(
                player
            );
        }

        /*
         * E with empty hands does not remove stock.
         *
         * R owns that action now.
         */
    }
    
    public void TakeOne(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        RemoveNullEntries();

        if (!HasStoredItem)
        {
            return;
        }
        
        DeliveryBox heldDeliveryBox =
            player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            TryTakeOneIntoDeliveryBox(
                heldDeliveryBox
            );

            return;
        }

        RestockBasket basket =
            player.GetHeldRestockBasket();

        /*
         * R while carrying basket:
         *
         * Shelf/Storage -> Basket
         */
        if (basket != null)
        {
            if (basket.IsFull)
            {
                return;
            }

            PickupItem item =
                GetLastStoredItem();

            if (item == null)
            {
                return;
            }

            basket.TryAddItem(
                item
            );

            return;
        }

        /*
         * R with empty hands:
         *
         * Shelf/Storage -> Hand
         */
        if (player.GetHeldItem() != null)
        {
            return;
        }

        TryRemoveStoredItem(
            player
        );
    }

    public void SecondaryInteract(
        PlayerInteraction player
    )
    {
        TakeOne(
            player
        );
    }

    public bool CanRepeatPrimary(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return false;
        }

        return player.GetHeldRestockBasket()
               != null
               ||
               player.GetHeldDeliveryBox()
               != null;
    }

    public bool CanRepeatSecondary(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return false;
        }

        return player.GetHeldRestockBasket()
               != null
               ||
               player.GetHeldDeliveryBox()
               != null;
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
    
    private string GetDeliveryBoxInteractionPrompt(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            deliveryBox.ProductData == null)
        {
            return "";
        }

        ProductData boxProduct =
            deliveryBox.ProductData;

        if (!CanAcceptProduct(
                boxProduct
            ))
        {
            return GetRejectedProductPrompt();
        }

        bool canStock =
            deliveryBox.RemainingQuantity > 0
            && !IsFull;

        ProductData storedProduct =
            GetStoredProductData();

        bool canReturn =
            HasStoredItem
            && storedProduct == boxProduct
            && !deliveryBox.IsFull;

        string locationCount =
            StoredItemCount
            + " / "
            + Capacity;

        string boxCount =
            deliveryBox.RemainingQuantity
            + " / "
            + deliveryBox.Capacity;

        string putVerb =
            customerAccessible
                ? "Stock "
                : "Store ";

        if (canStock && canReturn)
        {
            return "[E] "
                   + putVerb
                   + boxProduct.ProductName
                   + " from box  •  "
                   + locationCount
                   + "  •  Hold"
                   + "\n[R] Return one to box  •  Box "
                   + boxCount;
        }

        if (canStock)
        {
            return "[E] "
                   + putVerb
                   + boxProduct.ProductName
                   + " from box  •  "
                   + locationCount
                   + "  •  Hold";
        }

        if (canReturn)
        {
            return "[R] Return "
                   + boxProduct.ProductName
                   + " to box  •  Box "
                   + boxCount
                   + "  •  Hold";
        }

        if (IsFull)
        {
            return customerAccessible
                ? "Shelf full  " + locationCount
                : "Storage occupied";
        }

        if (deliveryBox.IsEmpty)
        {
            return "Delivery box empty";
        }

        return "";
    }

    private void TryStockFromDeliveryBox(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            deliveryBox.ProductData == null ||
            deliveryBox.ProductData.WorldPrefab == null)
        {
            return;
        }

        ProductData product =
            deliveryBox.ProductData;

        if (!CanAcceptProduct(
                product
            ))
        {
            return;
        }

        if (IsFull)
        {
            return;
        }

        bool tookUnit =
            deliveryBox.TryTakeOneUnit(
                out ProductData takenProduct
            );

        if (!tookUnit ||
            takenProduct == null)
        {
            return;
        }

        PickupItem item =
            Instantiate(
                takenProduct.WorldPrefab,
                transform.position,
                transform.rotation
            );

        AddStoredItem(
            item
        );
    }

    private void TryTakeOneIntoDeliveryBox(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            deliveryBox.IsFull ||
            !HasStoredItem)
        {
            return;
        }

        PickupItem item =
            GetLastStoredItem();

        if (item == null)
        {
            return;
        }

        ProductData itemProduct =
            item.GetProductData();

        if (itemProduct == null ||
            itemProduct != deliveryBox.ProductData)
        {
            return;
        }

        bool accepted =
            deliveryBox.TryAddOneUnit(
                itemProduct
            );

        if (!accepted)
        {
            return;
        }

        RemoveItem(
            item
        );

        Destroy(
            item.gameObject
        );
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
         * SALES SHELF
         */
        if (customerAccessible)
        {
            if (acceptedProduct == null)
            {
                return "Shelf section not configured";
            }

            int matchingCount =
                basket.GetProductCount(
                    acceptedProduct
                );

            bool canStock =
                matchingCount > 0
                && !IsFull;

            bool canTake =
                HasStoredItem
                && !basket.IsFull;

            string shelfCount =
                StoredItemCount
                + " / "
                + Capacity;

            if (canStock && canTake)
            {
                return "[E] Stock "
                       + acceptedProduct.ProductName
                       + "  •  "
                       + shelfCount
                       + "  •  Hold"
                       + "\n[R] Take one";
            }

            if (canStock)
            {
                return "[E] Stock "
                       + acceptedProduct.ProductName
                       + "  •  "
                       + shelfCount
                       + "  •  Hold";
            }

            if (canTake)
            {
                return "[R] Take "
                       + acceptedProduct.ProductName
                       + " into basket  •  "
                       + shelfCount
                       + "  •  Hold";
            }

            if (IsFull)
            {
                return "Shelf full  "
                       + shelfCount;
            }

            if (basket.IsFull)
            {
                return "Restock basket full";
            }

            return "Basket has no "
                   + acceptedProduct.ProductName;
        }

        /*
         * CURRENT SINGLE-UNIT STORAGE
         */
        if (HasStoredItem)
        {
            ProductData storedProduct =
                GetStoredProductData();

            string productName =
                storedProduct != null
                    ? storedProduct.ProductName
                    : "product";

            if (basket.IsFull)
            {
                return "Restock basket full";
            }

            return "[R] Take "
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

        if (!CanAcceptProduct(
                firstProduct
            ))
        {
            return GetRejectedProductPrompt();
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
            return "[R] Take "
                   + productName
                   + "  •  "
                   + StoredItemCount
                   + " / "
                   + Capacity;
        }

        return "[R] Take "
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

        if (IsFull)
        {
            return;
        }

        /*
         * E has ONE direction:
         *
         * Basket -> Shelf
         */
        bool tookMatchingItem =
            basket.TryTakeMatchingProduct(
                acceptedProduct,
                out PickupItem basketItem
            );

        if (!tookMatchingItem ||
            basketItem == null)
        {
            return;
        }

        AddStoredItem(
            basketItem
        );
    }

    private void HandleStorageBasketInteraction(
        RestockBasket basket
    )
    {
        if (basket == null)
        {
            return;
        }

        /*
         * E never removes an existing storage item.
         *
         * Use R for that.
         */
        if (HasStoredItem)
        {
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
        /*
         * ShelfSlot is now exclusively registered
         * as a customer-facing sales shelf.
         *
         * Back-room storage uses StorageBay instead.
         *
         * During the Lesson 61 prefab conversion,
         * old non-customer ShelfSlots may still exist
         * temporarily. They simply do not register.
         */
        if (!customerAccessible)
        {
            return;
        }

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
    }

    private void UnregisterFromCurrentRegistry()
    {
        if (registeredShelfRegistry == null)
        {
            return;
        }

        registeredShelfRegistry.UnregisterSlot(
            this
        );

        registeredShelfRegistry =
            null;
    }
    
    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            GetDisplayPlacementFrame(),
            0.14f
        );
    }
}