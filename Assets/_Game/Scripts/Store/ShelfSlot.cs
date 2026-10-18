using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ShelfSlot :
    MonoBehaviour,
    IInteractable,
    ISecondaryInteractable,
    IManagementInteractable,
    IRepeatableInteractable
{
    [Header("Identity")]
    [SerializeField] private string slotID = "";

    [Header("Shelf Rules")]
    [SerializeField] private ProductCategory allowedCategory = ProductCategory.Cereal;

    [FormerlySerializedAs("acceptedProduct")]
    [SerializeField] private ProductData assignedProduct;

    [Header("Placement")]
    [SerializeField] private Transform displayOrigin;

    [Header("Customer")]
    [SerializeField] private Transform customerStandPoint;

    [Header("Access")]
    [SerializeField] private bool customerAccessible = true;

    private readonly List<PickupItem> storedItems = new List<PickupItem>();
    private ShelfRegistry registeredShelfRegistry;

    public string SlotID => slotID;
    public Transform CustomerStandPoint => customerStandPoint;
    public bool CustomerAccessible => customerAccessible;

    public bool IsAssigned => assignedProduct != null;
    public ProductData AssignedProduct => assignedProduct;

    public bool HasStoredItem => StoredItemCount > 0;
    public int StoredItemCount => storedItems.Count;

    public int Capacity =>
        assignedProduct != null
            ? assignedProduct.SalesShelfCapacity
            : 0;

    public bool IsFull =>
        IsAssigned && StoredItemCount >= Capacity;

    public float ManagementHoldDuration => 0.6f;

    private void Start()
    {
        RegisterWithCorrectRegistry();
    }

    private void OnDestroy()
    {
        UnregisterFromCurrentRegistry();
    }

    public bool CanAcceptProduct(ProductData productData)
    {
        if (productData == null || !customerAccessible)
        {
            return false;
        }

        if (productData.Category != allowedCategory)
        {
            return false;
        }

        return assignedProduct == null || assignedProduct == productData;
    }

    public ProductData GetStoredProductData()
    {
        RemoveNullEntries();

        if (storedItems.Count == 0)
        {
            return null;
        }

        return storedItems[0]?.GetProductData();
    }

    public int GetStoredProductCount(ProductData productData)
    {
        if (productData == null)
        {
            return 0;
        }

        RemoveNullEntries();

        int count = 0;

        for (int i = 0; i < storedItems.Count; i++)
        {
            PickupItem item = storedItems[i];

            if (item != null && item.GetProductData() == productData)
            {
                count++;
            }
        }

        return count;
    }

    public Vector3 GetDisplayWorldPosition(int displayIndex)
    {
        Transform placementFrame = GetDisplayPlacementFrame();

        if (assignedProduct == null)
        {
            return placementFrame.position;
        }

        Vector3 localPosition =
            assignedProduct.GetSalesShelfLocalPosition(displayIndex);

        return placementFrame.TransformPoint(localPosition);
    }

    public Quaternion GetDisplayWorldRotation()
    {
        return GetDisplayPlacementFrame().rotation;
    }

    private Transform GetDisplayPlacementFrame()
    {
        return displayOrigin != null ? displayOrigin : transform;
    }

    public void ClearStoredItemForLoad()
    {
        if (storedItems.Count == 0)
        {
            return;
        }

        List<PickupItem> itemsToDestroy =
            new List<PickupItem>(storedItems);

        storedItems.Clear();

        for (int i = 0; i < itemsToDestroy.Count; i++)
        {
            PickupItem item = itemsToDestroy[i];

            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
    }

    public void ClearForLoad()
    {
        ClearStoredItemForLoad();
        assignedProduct = null;
    }

    public bool RestoreProduct(ProductData productData, int quantity = 1)
    {
        if (productData == null || productData.WorldPrefab == null)
        {
            return false;
        }

        if (productData.Category != allowedCategory)
        {
            Debug.LogWarning(
                $"Cannot restore {productData.ProductName} into ShelfSlot '{slotID}' because the facing only accepts {allowedCategory}.",
                this
            );

            return false;
        }

        if (assignedProduct != null && assignedProduct != productData)
        {
            Debug.LogWarning(
                $"Cannot restore {productData.ProductName} into ShelfSlot '{slotID}' because it is assigned to {assignedProduct.ProductName}.",
                this
            );

            return false;
        }

        ClearStoredItemForLoad();
        assignedProduct = productData;

        int restoreCount = Mathf.Clamp(quantity, 0, Capacity);

        for (int i = 0; i < restoreCount; i++)
        {
            PickupItem restoredItem = Instantiate(
                productData.WorldPrefab,
                transform.position,
                transform.rotation
            );

            AddStoredItem(restoredItem);
        }

        if (quantity > Capacity)
        {
            Debug.LogWarning(
                $"Saved ShelfSlot '{slotID}' contained {quantity} units, but its current capacity is {Capacity}. Extra units were not restored.",
                this
            );
        }

        return true;
    }

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player == null)
        {
            return "";
        }

        RemoveNullEntries();

        DeliveryBox heldDeliveryBox = player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            return GetDeliveryBoxInteractionPrompt(heldDeliveryBox);
        }

        RestockBasket heldBasket = player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            return GetBasketInteractionPrompt(heldBasket);
        }

        PickupItem heldItem = player.GetHeldItem();

        if (heldItem != null)
        {
            return GetHeldItemPrompt(heldItem);
        }

        if (HasStoredItem)
        {
            return GetOccupiedEmptyHandsPrompt();
        }

        return GetEmptySlotPrompt();
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            return;
        }

        RemoveNullEntries();

        DeliveryBox heldDeliveryBox = player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            TryStockFromDeliveryBox(heldDeliveryBox);
            return;
        }

        RestockBasket heldBasket = player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            HandleBasketInteraction(heldBasket);
            return;
        }

        if (player.GetHeldItem() != null)
        {
            TryStoreHeldItem(player);
        }
    }

    public void TakeOne(PlayerInteraction player)
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

        DeliveryBox heldDeliveryBox = player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            TryTakeOneIntoDeliveryBox(heldDeliveryBox);
            return;
        }

        RestockBasket basket = player.GetHeldRestockBasket();

        if (basket != null)
        {
            if (basket.IsFull)
            {
                return;
            }

            PickupItem item = GetLastStoredItem();

            if (item != null)
            {
                basket.TryAddItem(item);
            }

            return;
        }

        if (player.GetHeldItem() == null)
        {
            TryRemoveStoredItem(player);
        }
    }

    public void SecondaryInteract(PlayerInteraction player)
    {
        TakeOne(player);
    }

    public bool CanRepeatPrimary(PlayerInteraction player)
    {
        if (player == null)
        {
            return false;
        }

        return player.GetHeldRestockBasket() != null ||
               player.GetHeldDeliveryBox() != null;
    }

    public bool CanRepeatSecondary(PlayerInteraction player)
    {
        if (player == null)
        {
            return false;
        }

        return player.GetHeldRestockBasket() != null ||
               player.GetHeldDeliveryBox() != null;
    }

    public bool CanUseManagementAction(PlayerInteraction player)
    {
        if (player == null || player.HasAnythingHeld)
        {
            return false;
        }

        RemoveNullEntries();

        return IsAssigned && StoredItemCount == 0;
    }

    public void ManagementInteract(PlayerInteraction player)
    {
        if (!CanUseManagementAction(player))
        {
            return;
        }

        ClearAssignment();
    }

    public void ClearAssignment()
    {
        RemoveNullEntries();

        if (StoredItemCount > 0)
        {
            return;
        }

        assignedProduct = null;
    }

    private string GetHeldItemPrompt(PickupItem heldItem)
    {
        if (heldItem == null)
        {
            return "";
        }

        ProductData heldProduct = heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot be stocked here";
        }

        if (!CanAcceptProduct(heldProduct))
        {
            return GetRejectedProductPrompt(heldProduct);
        }

        int capacity =
            IsAssigned ? Capacity : heldProduct.SalesShelfCapacity;

        if (IsAssigned && IsFull)
        {
            return $"Shelf full  {StoredItemCount} / {Capacity}";
        }

        if (!IsAssigned)
        {
            return $"[E] Stock {heldProduct.ProductName}  •  Assign facing  •  0 / {capacity}";
        }

        return $"[E] Stock {heldProduct.ProductName}  •  {StoredItemCount} / {capacity}";
    }

    private string GetDeliveryBoxInteractionPrompt(DeliveryBox deliveryBox)
    {
        if (deliveryBox == null || deliveryBox.ProductData == null)
        {
            return "";
        }

        ProductData boxProduct = deliveryBox.ProductData;

        if (!CanAcceptProduct(boxProduct))
        {
            return GetRejectedProductPrompt(boxProduct);
        }

        int capacity =
            IsAssigned ? Capacity : boxProduct.SalesShelfCapacity;

        bool canStock =
            deliveryBox.RemainingQuantity > 0 &&
            (!IsAssigned || !IsFull);

        bool canReturn =
            IsAssigned &&
            HasStoredItem &&
            assignedProduct == boxProduct &&
            !deliveryBox.IsFull;

        string shelfCount = $"{StoredItemCount} / {capacity}";
        string boxCount =
            $"{deliveryBox.RemainingQuantity} / {deliveryBox.Capacity}";

        if (canStock && canReturn)
        {
            return
                $"[E] Stock {boxProduct.ProductName} from box  •  {shelfCount}  •  Hold\n" +
                $"[R] Return one to box  •  Box {boxCount}";
        }

        if (canStock)
        {
            string assignment = IsAssigned ? "" : "  •  Assign facing";

            return
                $"[E] Stock {boxProduct.ProductName} from box  •  {shelfCount}{assignment}  •  Hold";
        }

        if (canReturn)
        {
            return
                $"[R] Return {boxProduct.ProductName} to box  •  Box {boxCount}  •  Hold";
        }

        if (IsFull)
        {
            return $"Shelf full  {shelfCount}";
        }

        if (deliveryBox.IsEmpty)
        {
            return "Delivery box empty";
        }

        return "";
    }

    private void TryStockFromDeliveryBox(DeliveryBox deliveryBox)
    {
        if (deliveryBox == null ||
            deliveryBox.ProductData == null ||
            deliveryBox.ProductData.WorldPrefab == null)
        {
            return;
        }

        ProductData product = deliveryBox.ProductData;

        if (!CanAcceptProduct(product) || IsFull)
        {
            return;
        }

        bool tookUnit =
            deliveryBox.TryTakeOneUnit(out ProductData takenProduct);

        if (!tookUnit || takenProduct == null)
        {
            return;
        }

        PickupItem item = Instantiate(
            takenProduct.WorldPrefab,
            transform.position,
            transform.rotation
        );

        if (AddStoredItem(item))
        {
            return;
        }

        Destroy(item.gameObject);
        deliveryBox.TryAddOneUnit(takenProduct);
    }

    private void TryTakeOneIntoDeliveryBox(DeliveryBox deliveryBox)
    {
        if (deliveryBox == null ||
            deliveryBox.IsFull ||
            !HasStoredItem)
        {
            return;
        }

        PickupItem item = GetLastStoredItem();

        if (item == null)
        {
            return;
        }

        ProductData itemProduct = item.GetProductData();

        if (itemProduct == null ||
            itemProduct != deliveryBox.ProductData)
        {
            return;
        }

        if (!deliveryBox.TryAddOneUnit(itemProduct))
        {
            return;
        }

        RemoveItem(item);
        Destroy(item.gameObject);
    }

    private string GetBasketInteractionPrompt(RestockBasket basket)
    {
        if (basket == null)
        {
            return "";
        }

        if (!IsAssigned)
        {
            ProductData candidate =
                basket.GetFirstProductData(allowedCategory);

            if (candidate == null)
            {
                return $"Basket has no {allowedCategory} product";
            }

            return
                $"[E] Stock {candidate.ProductName}  •  Assign facing  •  0 / {candidate.SalesShelfCapacity}  •  Hold";
        }

        int matchingCount =
            basket.GetProductCount(assignedProduct);

        bool canStock =
            matchingCount > 0 &&
            !IsFull;

        bool canTake =
            HasStoredItem &&
            !basket.IsFull;

        string shelfCount =
            $"{StoredItemCount} / {Capacity}";

        if (canStock && canTake)
        {
            return
                $"[E] Stock {assignedProduct.ProductName}  •  {shelfCount}  •  Hold\n" +
                $"[R] Take one into basket";
        }

        if (canStock)
        {
            return
                $"[E] Stock {assignedProduct.ProductName}  •  {shelfCount}  •  Hold";
        }

        if (canTake)
        {
            return
                $"[R] Take {assignedProduct.ProductName} into basket  •  {shelfCount}  •  Hold";
        }

        if (IsFull)
        {
            return $"Shelf full  {shelfCount}";
        }

        if (basket.IsFull)
        {
            return "Restock basket full";
        }

        return $"Basket has no {assignedProduct.ProductName}";
    }

    private string GetOccupiedEmptyHandsPrompt()
    {
        string productName =
            assignedProduct != null
                ? assignedProduct.ProductName
                : "product";

        return $"[R] Take {productName}  •  {StoredItemCount} / {Capacity}";
    }

    private string GetEmptySlotPrompt()
    {
        if (!IsAssigned)
        {
            return
                $"Empty {allowedCategory} facing\nStock a matching product to assign";
        }

        return
            $"{assignedProduct.ProductName}  •  0 / {Capacity}\n" +
            "[Hold X] Clear facing";
    }

    private void HandleBasketInteraction(RestockBasket basket)
    {
        if (basket == null)
        {
            return;
        }

        ProductData targetProduct =
            assignedProduct ??
            basket.GetFirstProductData(allowedCategory);

        if (targetProduct == null ||
            !CanAcceptProduct(targetProduct) ||
            IsFull)
        {
            return;
        }

        bool tookItem =
            basket.TryTakeMatchingProduct(
                targetProduct,
                out PickupItem basketItem
            );

        if (!tookItem || basketItem == null)
        {
            return;
        }

        if (AddStoredItem(basketItem))
        {
            return;
        }

        basket.TryAddItem(basketItem);
    }

    private void TryStoreHeldItem(PlayerInteraction player)
    {
        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
        {
            return;
        }

        ProductData heldProduct = heldItem.GetProductData();

        if (!CanAcceptProduct(heldProduct) || IsFull)
        {
            return;
        }

        if (AddStoredItem(heldItem))
        {
            player.RemoveHeldItem();
        }
    }

    private void TryRemoveStoredItem(PlayerInteraction player)
    {
        PickupItem item = GetLastStoredItem();

        if (item != null)
        {
            player.TryPickUp(item);
        }
    }

    private bool AddStoredItem(PickupItem item)
    {
        if (item == null || storedItems.Contains(item))
        {
            return false;
        }

        ProductData product = item.GetProductData();

        if (!CanAcceptProduct(product))
        {
            return false;
        }

        if (!IsAssigned)
        {
            assignedProduct = product;
        }

        if (IsFull)
        {
            return false;
        }

        storedItems.Add(item);
        item.PlaceOnShelf(this, storedItems.Count - 1);

        return true;
    }

    public void RemoveItem(PickupItem item)
    {
        if (item == null)
        {
            return;
        }

        if (!storedItems.Remove(item))
        {
            return;
        }

        RepackStoredItems();
    }

    public bool ContainsProduct(ProductData productData)
    {
        return GetStoredProductCount(productData) > 0;
    }

    public bool TryTakeItemForCustomer(
        Transform carryPoint,
        out PickupItem takenItem
    )
    {
        takenItem = null;

        if (!customerAccessible)
        {
            return false;
        }

        RemoveNullEntries();

        if (storedItems.Count == 0)
        {
            return false;
        }

        PickupItem itemToTake = storedItems[0];

        if (itemToTake == null)
        {
            return false;
        }

        itemToTake.PickUpForCustomer(carryPoint);
        takenItem = itemToTake;

        return true;
    }

    private PickupItem GetLastStoredItem()
    {
        RemoveNullEntries();

        if (storedItems.Count == 0)
        {
            return null;
        }

        return storedItems[storedItems.Count - 1];
    }

    private void RepackStoredItems()
    {
        RemoveNullEntries();

        for (int i = 0; i < storedItems.Count; i++)
        {
            PickupItem item = storedItems[i];

            if (item != null)
            {
                item.PlaceOnShelf(this, i);
            }
        }
    }

    private void RemoveNullEntries()
    {
        for (int i = storedItems.Count - 1; i >= 0; i--)
        {
            if (storedItems[i] == null)
            {
                storedItems.RemoveAt(i);
            }
        }
    }

    private string GetRejectedProductPrompt(ProductData product)
    {
        if (product == null)
        {
            return "This item cannot be stocked here";
        }

        if (product.Category != allowedCategory)
        {
            return $"This shelf is for {allowedCategory}";
        }

        if (assignedProduct != null && assignedProduct != product)
        {
            return $"Facing assigned to {assignedProduct.ProductName}";
        }

        return "This product cannot be stocked here";
    }

    private void RegisterWithCorrectRegistry()
    {
        if (!customerAccessible)
        {
            return;
        }

        registeredShelfRegistry =
            Object.FindAnyObjectByType<ShelfRegistry>();

        if (registeredShelfRegistry == null)
        {
            Debug.LogError(
                $"ShelfSlot '{gameObject.name}' could not find a ShelfRegistry.",
                this
            );

            return;
        }

        registeredShelfRegistry.RegisterSlot(this);
    }

    private void UnregisterFromCurrentRegistry()
    {
        if (registeredShelfRegistry == null)
        {
            return;
        }

        registeredShelfRegistry.UnregisterSlot(this);
        registeredShelfRegistry = null;
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            GetDisplayPlacementFrame(),
            0.14f
        );
    }
}