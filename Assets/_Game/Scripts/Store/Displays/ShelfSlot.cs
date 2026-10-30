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
    [SerializeField] private string localSlotKey = "";

    [FormerlySerializedAs("slotID")]
    [SerializeField, HideInInspector]
    private string legacySlotID = "";

    // Retained only for very old ShelfSlot setups that have no parent SalesShelf.
    [FormerlySerializedAs("allowedCategory")]
    [SerializeField, HideInInspector]
    private ProductCategory fallbackAllowedCategory = ProductCategory.Cereal;

    [FormerlySerializedAs("acceptedProduct")]
    [SerializeField] private ProductData assignedProduct;

    [Header("Placement")]
    [SerializeField] private Transform displayOrigin;

    [FormerlySerializedAs("customerStandPoint")]
    [SerializeField, HideInInspector]
    private Transform fallbackCustomerStandPoint;

    [Header("Access")]
    [SerializeField] private bool customerAccessible = true;

    private readonly List<PickupItem> storedItems = new();

    private ShelfRegistry registeredShelfRegistry;
    private ShelfPriceTag priceTag;
    private ShelfIdentity shelfIdentity;
    private SalesShelf salesShelf;

    public string SlotID
    {
        get
        {
            if (shelfIdentity == null)
                shelfIdentity = GetComponentInParent<ShelfIdentity>();

            if (shelfIdentity != null &&
                !string.IsNullOrWhiteSpace(shelfIdentity.PersistentID) &&
                !string.IsNullOrWhiteSpace(localSlotKey))
            {
                return $"{shelfIdentity.PersistentID}:{localSlotKey}";
            }

            return legacySlotID;
        }
    }

    public string LegacySlotID => legacySlotID;
    public bool CustomerAccessible => customerAccessible;
    public bool IsAssigned => assignedProduct != null;
    public ProductData AssignedProduct => assignedProduct;
    public bool HasStoredItem => StoredItemCount > 0;
    public int StoredItemCount => storedItems.Count;
    public int Capacity => assignedProduct != null ? assignedProduct.SalesShelfCapacity : 0;
    public bool IsFull => IsAssigned && StoredItemCount >= Capacity;
    public float ManagementHoldDuration => 0.6f;

    public ProductDisplayType DisplayType =>
        salesShelf != null
            ? salesShelf.DisplayType
            : ProductDisplayType.StandardShelf;

    public ProductStorageCondition StorageCondition =>
        salesShelf != null
            ? salesShelf.StorageCondition
            : ProductStorageCondition.Ambient;

    public Transform CustomerStandPoint
    {
        get
        {
            if (salesShelf != null && salesShelf.CustomerStandPoint != null)
                return salesShelf.CustomerStandPoint;

            return fallbackCustomerStandPoint;
        }
    }

    private void Awake()
    {
        priceTag = GetComponentInChildren<ShelfPriceTag>(true);
        shelfIdentity = GetComponentInParent<ShelfIdentity>();
        salesShelf = GetComponentInParent<SalesShelf>();
    }

    private void Start()
    {
        RegisterWithCorrectRegistry();
        RefreshPriceTag();
    }

    private void OnDestroy()
    {
        UnregisterFromCurrentRegistry();
    }

    public bool IsActorAtShelfFront(Vector3 worldPosition)
    {
        if (salesShelf == null)
            salesShelf = GetComponentInParent<SalesShelf>();

        return salesShelf == null ||
               salesShelf.IsWorldPositionInFront(worldPosition);
    }

    private bool CanPlayerAccessShelf(PlayerInteraction player)
    {
        return player != null &&
               IsActorAtShelfFront(player.transform.position);
    }

    private bool IsProductCompatibleWithDisplay(ProductData productData)
    {
        if (productData == null)
            return false;

        if (salesShelf != null)
            return salesShelf.CanDisplayProduct(productData);

        // Backward-compatible behavior for any legacy orphan ShelfSlot.
        return productData.Category == fallbackAllowedCategory &&
               productData.CanBeDisplayedOn(ProductDisplayType.StandardShelf) &&
               productData.CanBeStoredIn(ProductStorageCondition.Ambient);
    }

    public bool CanAcceptProduct(ProductData productData)
    {
        if (productData == null || !customerAccessible)
            return false;

        if (!IsProductCompatibleWithDisplay(productData))
            return false;

        return assignedProduct == null || assignedProduct == productData;
    }

    public ProductData GetStoredProductData()
    {
        RemoveNullEntries();

        if (storedItems.Count == 0)
            return null;

        return storedItems[0]?.GetProductData();
    }

    public int GetStoredProductCount(ProductData productData)
    {
        if (productData == null)
            return 0;

        RemoveNullEntries();

        int count = 0;

        for (int i = 0; i < storedItems.Count; i++)
        {
            PickupItem item = storedItems[i];

            if (item != null && item.GetProductData() == productData)
                count++;
        }

        return count;
    }

    public Vector3 GetDisplayWorldPosition(int displayIndex)
    {
        Transform placementFrame = GetDisplayPlacementFrame();

        if (assignedProduct == null)
            return placementFrame.position;

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
            return;

        List<PickupItem> itemsToDestroy = new(storedItems);
        storedItems.Clear();

        for (int i = 0; i < itemsToDestroy.Count; i++)
        {
            PickupItem item = itemsToDestroy[i];

            if (item != null)
                Destroy(item.gameObject);
        }
    }

    public void ClearForLoad()
    {
        ClearStoredItemForLoad();
        assignedProduct = null;
        RefreshPriceTag();
    }

    public bool RestoreProduct(ProductData productData, int quantity = 1)
    {
        if (productData == null || productData.WorldPrefab == null)
            return false;

        if (!IsProductCompatibleWithDisplay(productData))
        {
            Debug.LogWarning(
                $"Restoring {productData.ProductName} into ShelfSlot '{SlotID}' even though it no longer matches this display's current requirements. Existing saved stock is preserved so it can be removed instead of being silently deleted.",
                this
            );
        }

        if (assignedProduct != null && assignedProduct != productData)
        {
            Debug.LogWarning(
                $"Cannot restore {productData.ProductName} into ShelfSlot '{SlotID}' because it is assigned to {assignedProduct.ProductName}.",
                this
            );

            return false;
        }

        ClearStoredItemForLoad();

        assignedProduct = productData;
        RefreshPriceTag();

        int restoreCount = Mathf.Clamp(quantity, 0, Capacity);

        for (int i = 0; i < restoreCount; i++)
        {
            PickupItem restoredItem = Instantiate(
                productData.WorldPrefab,
                transform.position,
                transform.rotation
            );

            // Loading may preserve stock that new stocking rules would reject.
            storedItems.Add(restoredItem);
            restoredItem.PlaceOnShelf(this, storedItems.Count - 1);
        }

        if (quantity > Capacity)
        {
            Debug.LogWarning(
                $"Saved ShelfSlot '{SlotID}' contained {quantity} units, but its current capacity is {Capacity}. Extra units were not restored.",
                this
            );
        }

        return true;
    }

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player == null)
            return "";

        if (!CanPlayerAccessShelf(player))
            return "Use the front of the shelf";

        RemoveNullEntries();

        string prompt;
        DeliveryBox heldDeliveryBox = player.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            ProductData boxProduct = heldDeliveryBox.ProductData;
            string warning = GetCompatibilityWarning(boxProduct);

            if (!string.IsNullOrEmpty(warning))
            {
                bool canRemoveExistingStock =
                    IsAssigned &&
                    assignedProduct == boxProduct &&
                    HasStoredItem &&
                    !heldDeliveryBox.IsFull;

                prompt = canRemoveExistingStock
                    ? $"{warning}\n[R] Return one to box"
                    : warning;
            }
            else
            {
                prompt = GetDeliveryBoxInteractionPrompt(heldDeliveryBox);
            }
        }
        else
        {
            RestockBasket heldBasket = player.GetHeldRestockBasket();

            if (heldBasket != null)
            {
                string warning =
                    IsAssigned
                        ? GetCompatibilityWarning(assignedProduct)
                        : "";

                if (!string.IsNullOrEmpty(warning))
                {
                    bool canRemoveExistingStock =
                        HasStoredItem &&
                        !heldBasket.IsFull;

                    prompt = canRemoveExistingStock
                        ? $"{warning}\n[R] Take one into basket"
                        : warning;
                }
                else
                {
                    prompt = GetBasketInteractionPrompt(heldBasket);
                }
            }
            else
            {
                PickupItem heldItem = player.GetHeldItem();

                if (heldItem != null)
                {
                    prompt = GetHeldItemPrompt(heldItem);
                }
                else if (HasStoredItem)
                {
                    string warning = GetCompatibilityWarning(assignedProduct);

                    prompt = !string.IsNullOrEmpty(warning)
                        ? $"{warning}\n{GetOccupiedEmptyHandsPrompt()}"
                        : GetOccupiedEmptyHandsPrompt();
                }
                else
                {
                    string warning =
                        IsAssigned
                            ? GetCompatibilityWarning(assignedProduct)
                            : "";

                    prompt = !string.IsNullOrEmpty(warning)
                        ? $"{warning}\n{GetEmptySlotPrompt()}"
                        : GetEmptySlotPrompt();
                }
            }
        }

        return AppendManagementPrompt(prompt, player);
    }

    private string AppendManagementPrompt(
        string prompt,
        PlayerInteraction player)
    {
        if (!CanUseManagementAction(player))
            return prompt;

        const string managementPrompt = "[Hold X] Clear facing";

        if (string.IsNullOrWhiteSpace(prompt))
            return managementPrompt;

        return $"{prompt}\n{managementPrompt}";
    }

    public void Interact(PlayerInteraction player)
    {
        if (!CanPlayerAccessShelf(player))
            return;

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
            TryStoreHeldItem(player);
    }

    public void TakeOne(PlayerInteraction player)
    {
        if (!CanPlayerAccessShelf(player))
            return;

        RemoveNullEntries();

        if (!HasStoredItem)
            return;

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
                return;

            PickupItem item = GetLastStoredItem();

            if (item != null)
                basket.TryAddItem(item);

            return;
        }

        if (player.GetHeldItem() == null)
            TryRemoveStoredItem(player);
    }

    public void SecondaryInteract(PlayerInteraction player)
    {
        TakeOne(player);
    }

    public bool CanRepeatPrimary(PlayerInteraction player)
    {
        if (!CanPlayerAccessShelf(player))
            return false;

        return player.GetHeldRestockBasket() != null ||
               player.GetHeldDeliveryBox() != null;
    }

    public bool CanRepeatSecondary(PlayerInteraction player)
    {
        if (!CanPlayerAccessShelf(player))
            return false;

        return player.GetHeldRestockBasket() != null ||
               player.GetHeldDeliveryBox() != null;
    }

    public bool CanUseManagementAction(PlayerInteraction player)
    {
        if (!CanPlayerAccessShelf(player))
            return false;

        RemoveNullEntries();

        return IsAssigned && StoredItemCount == 0;
    }

    public void ManagementInteract(PlayerInteraction player)
    {
        if (CanUseManagementAction(player))
            ClearAssignment();
    }

    public void ClearAssignment()
    {
        RemoveNullEntries();

        if (StoredItemCount > 0)
            return;

        assignedProduct = null;
        RefreshPriceTag();
    }

    private string GetHeldItemPrompt(PickupItem heldItem)
    {
        if (heldItem == null)
            return "";

        ProductData heldProduct = heldItem.GetProductData();

        if (heldProduct == null)
            return "This item cannot be stocked here";

        if (!CanAcceptProduct(heldProduct))
            return GetRejectedProductPrompt(heldProduct);

        int capacity =
            IsAssigned
                ? Capacity
                : heldProduct.SalesShelfCapacity;

        if (IsAssigned && IsFull)
            return $"Shelf full  {StoredItemCount} / {Capacity}";

        if (!IsAssigned)
        {
            return
                $"[E] Stock {heldProduct.ProductName}  •  Assign facing  •  0 / {capacity}";
        }

        return
            $"[E] Stock {heldProduct.ProductName}  •  {StoredItemCount} / {capacity}";
    }

    private string GetDeliveryBoxInteractionPrompt(DeliveryBox deliveryBox)
    {
        if (deliveryBox == null || deliveryBox.ProductData == null)
            return "";

        ProductData boxProduct = deliveryBox.ProductData;

        if (!CanAcceptProduct(boxProduct))
            return GetRejectedProductPrompt(boxProduct);

        int capacity =
            IsAssigned
                ? Capacity
                : boxProduct.SalesShelfCapacity;

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
            string assignment =
                IsAssigned
                    ? ""
                    : "  •  Assign facing";

            return
                $"[E] Stock {boxProduct.ProductName} from box  •  {shelfCount}{assignment}  •  Hold";
        }

        if (canReturn)
        {
            return
                $"[R] Return {boxProduct.ProductName} to box  •  Box {boxCount}  •  Hold";
        }

        if (IsFull)
            return $"Shelf full  {shelfCount}";

        if (deliveryBox.IsEmpty)
            return "Delivery box empty";

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
            return;

        bool tookUnit =
            deliveryBox.TryTakeOneUnit(out ProductData takenProduct);

        if (!tookUnit || takenProduct == null)
            return;

        PickupItem item = Instantiate(
            takenProduct.WorldPrefab,
            transform.position,
            transform.rotation
        );

        if (AddStoredItem(item))
            return;

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
            return;

        ProductData itemProduct = item.GetProductData();

        if (itemProduct == null ||
            itemProduct != deliveryBox.ProductData)
        {
            return;
        }

        if (!deliveryBox.TryAddOneUnit(itemProduct))
            return;

        RemoveItem(item);
        Destroy(item.gameObject);
    }

    private string GetBasketInteractionPrompt(RestockBasket basket)
    {
        if (basket == null)
            return "";

        if (!IsAssigned)
        {
            ProductData candidate =
                basket.GetFirstProductData(IsProductCompatibleWithDisplay);

            if (candidate == null)
            {
                ProductData firstProduct = basket.GetFirstProductData();

                if (firstProduct == null)
                    return "Restock basket empty";

                return GetRejectedProductPrompt(firstProduct);
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
                "[R] Take one into basket";
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
            return $"Shelf full  {shelfCount}";

        if (basket.IsFull)
            return "Restock basket full";

        return $"Basket has no {assignedProduct.ProductName}";
    }

    private string GetOccupiedEmptyHandsPrompt()
    {
        string productName =
            assignedProduct != null
                ? assignedProduct.ProductName
                : "product";

        return
            $"[R] Take {productName}  •  {StoredItemCount} / {Capacity}";
    }

    private string GetEmptySlotPrompt()
    {
        if (!IsAssigned)
        {
            string displayName = GetCurrentDisplayName();

            return
                $"Empty {displayName} facing\nStock a compatible product to assign";
        }

        return $"{assignedProduct.ProductName}  •  0 / {Capacity}";
    }

    private void HandleBasketInteraction(RestockBasket basket)
    {
        if (basket == null)
            return;

        ProductData targetProduct =
            assignedProduct ??
            basket.GetFirstProductData(IsProductCompatibleWithDisplay);

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
            return;

        if (AddStoredItem(basketItem))
            return;

        basket.TryAddItem(basketItem);
    }

    private void TryStoreHeldItem(PlayerInteraction player)
    {
        PickupItem heldItem = player.GetHeldItem();

        if (heldItem == null)
            return;

        ProductData heldProduct = heldItem.GetProductData();

        if (!CanAcceptProduct(heldProduct) || IsFull)
            return;

        if (AddStoredItem(heldItem))
            player.RemoveHeldItem();
    }

    private void TryRemoveStoredItem(PlayerInteraction player)
    {
        PickupItem item = GetLastStoredItem();

        if (item != null)
            player.TryPickUp(item);
    }

    private bool AddStoredItem(PickupItem item)
    {
        if (item == null || storedItems.Contains(item))
            return false;

        ProductData product = item.GetProductData();

        if (!CanAcceptProduct(product))
            return false;

        if (!IsAssigned)
        {
            assignedProduct = product;
            RefreshPriceTag();
        }

        if (IsFull)
            return false;

        storedItems.Add(item);
        item.PlaceOnShelf(this, storedItems.Count - 1);

        return true;
    }

    public void RemoveItem(PickupItem item)
    {
        if (item == null)
            return;

        if (!storedItems.Remove(item))
            return;

        RepackStoredItems();
    }

    public bool ContainsProduct(ProductData productData)
    {
        return GetStoredProductCount(productData) > 0;
    }

    public bool TryTakeItemForCustomer(
        Transform carryPoint,
        Transform customerRoot,
        out PickupItem takenItem)
    {
        takenItem = null;

        if (customerRoot == null ||
            !IsActorAtShelfFront(customerRoot.position))
        {
            return false;
        }

        if (!customerAccessible)
            return false;

        RemoveNullEntries();

        if (storedItems.Count == 0)
            return false;

        PickupItem itemToTake = storedItems[0];

        if (itemToTake == null)
            return false;

        itemToTake.PickUpForCustomer(carryPoint);

        takenItem = itemToTake;
        return true;
    }

    private PickupItem GetLastStoredItem()
    {
        RemoveNullEntries();

        if (storedItems.Count == 0)
            return null;

        return storedItems[storedItems.Count - 1];
    }

    private void RepackStoredItems()
    {
        RemoveNullEntries();

        for (int i = 0; i < storedItems.Count; i++)
        {
            PickupItem item = storedItems[i];

            if (item != null)
                item.PlaceOnShelf(this, i);
        }
    }

    private void RemoveNullEntries()
    {
        for (int i = storedItems.Count - 1; i >= 0; i--)
        {
            if (storedItems[i] == null)
                storedItems.RemoveAt(i);
        }
    }

    private string GetRejectedProductPrompt(ProductData product)
    {
        if (product == null)
            return "This item cannot be stocked here";

        string compatibilityWarning = GetCompatibilityWarning(product);

        if (!string.IsNullOrEmpty(compatibilityWarning))
            return compatibilityWarning;

        if (assignedProduct != null && assignedProduct != product)
            return $"Facing assigned to {assignedProduct.ProductName}";

        return "This product cannot be stocked here";
    }

    private string GetCompatibilityWarning(ProductData product)
    {
        if (product == null)
            return "";

        if (salesShelf == null &&
            product.Category != fallbackAllowedCategory)
        {
            return $"This legacy shelf is for {fallbackAllowedCategory}";
        }

        if (!product.CanBeDisplayedOn(DisplayType))
        {
            return
                $"{product.ProductName} needs {GetRequiredDisplayName(product.RequiredDisplayType)}";
        }

        if (!product.CanBeStoredIn(StorageCondition))
        {
            if (product.RequiredStorageCondition == ProductStorageCondition.Chilled)
                return $"{product.ProductName} needs a chilled display";

            return $"{product.ProductName} needs compatible storage";
        }

        return "";
    }

    private string GetCurrentDisplayName()
    {
        string displayName =
            DisplayType switch
            {
                ProductDisplayType.BakeryDisplay => "bakery display",
                _ => "shelf"
            };

        if (StorageCondition == ProductStorageCondition.Chilled)
            displayName = "chilled " + displayName;

        return displayName;
    }

    private static string GetRequiredDisplayName(
        ProductDisplayType requiredDisplayType)
    {
        return requiredDisplayType switch
        {
            ProductDisplayType.BakeryDisplay => "a bakery display",
            ProductDisplayType.StandardShelf => "a standard shelf",
            _ => "a compatible display"
        };
    }

    private void RegisterWithCorrectRegistry()
    {
        if (!customerAccessible)
            return;

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
            return;

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

    private void RefreshPriceTag()
    {
        if (priceTag != null)
            priceTag.Refresh(assignedProduct);
    }
}