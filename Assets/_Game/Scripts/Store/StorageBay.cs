using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageBay :
    MonoBehaviour,
    IInteractable,
    ISecondaryInteractable,
    ICarryInteractable,
    IManagementInteractable,
    IRepeatableInteractable
{
    [Header("Identity")] [SerializeField] private string slotID = "";

    [Header("Storage")] [Min(1)] [SerializeField]
    private int capacity = 12;

    [Header("Portable Carton")] [SerializeField]
    private DeliveryBox cartonPrefab;

    [Min(1)] [SerializeField] private int portableCartonCapacity = 4;

    [Header("Display")] [SerializeField] private GameObject assignedBoxVisual;

    [SerializeField] private TMP_Text statusText;

    [SerializeField] private Image productIcon;

    [SerializeField] private Image fillBar;

    private ProductData assignedProduct;

    private int quantity;

    private StorageRegistry registeredStorageRegistry;

    private static readonly Color32 EmptyColor =
        new(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 NormalColor =
        new(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 LowColor =
        new(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 MidColor =
        new(
            246,
            215,
            122,
            255
        );

    private static readonly Color32 HealthyColor =
        new(
            111,
            207,
            151,
            255
        );

    public string SlotID =>
        slotID;

    public ProductData AssignedProduct =>
        assignedProduct;

    public int Quantity =>
        quantity;

    public int Capacity =>
        Mathf.Max(
            1,
            capacity
        );

    public bool IsAssigned =>
        assignedProduct != null;

    public bool IsEmpty =>
        quantity <= 0;

    public bool IsFull =>
        quantity >= Capacity;

    private void Awake()
    {
        quantity =
            Mathf.Clamp(
                quantity,
                0,
                Capacity
            );

        RefreshDisplay();
    }

    private void Start()
    {
        registeredStorageRegistry =
            FindAnyObjectByType<
                StorageRegistry
            >();

        if (registeredStorageRegistry == null)
        {
            Debug.LogError(
                "StorageBay '"
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

    private void OnDestroy()
    {
        if (registeredStorageRegistry == null) return;

        registeredStorageRegistry.Unregister(
            this
        );
    }

    public bool CanAcceptProduct(
        ProductData productData
    )
    {
        if (productData == null) return false;

        if (!IsAssigned) return true;

        return assignedProduct
               == productData;
    }

    public int GetStoredProductCount(
        ProductData productData
    )
    {
        if (productData == null ||
            assignedProduct != productData)
            return 0;

        return quantity;
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null) return "";

        var heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
            return GetDeliveryBoxPrompt(
                heldBox
            );

        var basket =
            player.GetHeldRestockBasket();

        if (basket != null)
            return GetBasketPrompt(
                basket
            );

        var heldItem =
            player.GetHeldItem();

        if (heldItem != null)
            return GetHeldItemPrompt(
                heldItem
            );

        if (!IsAssigned) return "Empty storage bay";

        if (quantity <= 0)
            return assignedProduct.ProductName
                   + "  •  0 / "
                   + Capacity
                   + "\n[Hold X] Clear assignment";

        var cartonQuantity =
            Mathf.Min(
                portableCartonCapacity,
                quantity
            );

        return "[E] Take one "
               + assignedProduct.ProductName
               + "  •  "
               + quantity
               + " / "
               + Capacity
               + "\n[F] Take carton x"
               + cartonQuantity;
    }

    /*
     * E = natural primary action.
     */
    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null) return;

        var heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            TryStoreWholeCarton(
                player,
                heldBox
            );

            return;
        }

        var basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            /*
             * E mirrors the sales shelf:
             *
             * Basket -> Storage
             */
            TryReturnFromBasket(
                basket
            );

            return;
        }

        var heldItem =
            player.GetHeldItem();

        /*
         * A loose product in our hands is being
         * USED on this storage target.
         *
         * E is therefore the intuitive action.
         */
        if (heldItem != null)
        {
            TryStoreHeldItem(
                player,
                heldItem
            );

            return;
        }

        TryTakeIntoHand(
            player
        );
    }

    /*
     * R = reverse container transfer.
     */
    public void ReturnOne(
        PlayerInteraction player
    )
    {
        SecondaryInteract(
            player
        );
    }

    public void SecondaryInteract(
        PlayerInteraction player
    )
    {
        if (player == null) return;

        var heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            TryFillCartonFromBay(
                heldBox
            );

            return;
        }

        var basket =
            player.GetHeldRestockBasket();

        if (basket != null)
            /*
             * R mirrors the sales shelf:
             *
             * Storage -> Basket
             */
            TryTakeIntoBasket(
                basket
            );
    }

    public void HandleCarryAction(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld)
            return;

        if (!IsAssigned ||
            quantity <= 0)
            return;

        TryTakeCarton(
            player
        );
    }

    public void CarryInteract(
        PlayerInteraction player
    )
    {
        HandleCarryAction(
            player
        );
    }

    public void ClearAssignment()
    {
        if (!IsAssigned ||
            quantity > 0)
            return;

        assignedProduct =
            null;

        RefreshDisplay();
    }

    public void RestoreState(
        ProductData productData,
        int restoredQuantity
    )
    {
        assignedProduct =
            productData;

        quantity =
            Mathf.Clamp(
                restoredQuantity,
                0,
                Capacity
            );

        RefreshDisplay();
    }

    public void ClearForLoad()
    {
        assignedProduct =
            null;

        quantity =
            0;

        RefreshDisplay();
    }

    private string GetDeliveryBoxPrompt(
        DeliveryBox deliveryBox
    )
    {
        var boxProduct =
            deliveryBox.ProductData;

        if (boxProduct == null) return "Carton not configured";

        if (!CanAcceptProduct(
                boxProduct
            ))
            return "Bay assigned to "
                   + assignedProduct.ProductName;

        var availableSpace =
            Mathf.Max(
                0,
                Capacity - quantity
            );

        var unitsToStore =
            Mathf.Min(
                availableSpace,
                deliveryBox.RemainingQuantity
            );

        var boxFreeSpace =
            Mathf.Max(
                0,
                deliveryBox.Capacity
                - deliveryBox.RemainingQuantity
            );

        var unitsToReturn =
            IsAssigned &&
            assignedProduct == boxProduct
                ? Mathf.Min(
                    quantity,
                    boxFreeSpace
                )
                : 0;

        if (unitsToStore > 0 &&
            unitsToReturn > 0)
            return "[E] Place carton  •  +"
                   + unitsToStore
                   + " "
                   + boxProduct.ProductName
                   + "\n[R] Fill carton from bay  •  +"
                   + unitsToReturn;

        if (unitsToStore > 0)
        {
            var verb =
                IsAssigned
                    ? "Merge carton"
                    : "Place carton";

            return "[E] "
                   + verb
                   + "  •  +"
                   + unitsToStore
                   + " "
                   + boxProduct.ProductName;
        }

        if (unitsToReturn > 0)
            return "[R] Fill carton from bay  •  +"
                   + unitsToReturn
                   + " "
                   + assignedProduct.ProductName;

        if (IsFull)
            return "Storage bay full  "
                   + quantity
                   + " / "
                   + Capacity;

        if (deliveryBox.IsEmpty) return "Carton empty";

        return "";
    }

    private string GetBasketPrompt(
        RestockBasket basket
    )
    {
        if (basket == null) return "";

        /*
         * Empty bay:
         *
         * E places the first available basket SKU
         * into storage and assigns the bay.
         */
        if (!IsAssigned)
        {
            var firstProduct =
                basket.GetFirstProductData();

            if (firstProduct == null) return "Empty storage bay";

            return "[E] Store "
                   + firstProduct.ProductName
                   + "  •  Assign bay  •  Hold";
        }

        var matchingBasketCount =
            basket.GetProductCount(
                assignedProduct
            );

        var canStore =
            matchingBasketCount > 0
            && !IsFull;

        var canTake =
            quantity > 0
            && !basket.IsFull;

        var bayCount =
            quantity
            + " / "
            + Capacity;

        if (canStore &&
            canTake)
            return "[E] Store "
                   + assignedProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold"
                   + "\n[R] Take "
                   + assignedProduct.ProductName
                   + " into basket  •  Hold";

        if (canStore)
            return "[E] Store "
                   + assignedProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold";

        if (canTake)
            return "[R] Take "
                   + assignedProduct.ProductName
                   + " into basket  •  Bay "
                   + bayCount
                   + "  •  Hold";

        if (IsFull &&
            matchingBasketCount > 0)
            return "Storage bay full  "
                   + bayCount;

        if (basket.IsFull &&
            quantity > 0)
            return "Restock basket full";

        return assignedProduct.ProductName
               + "  •  "
               + bayCount;
    }

    private string GetHeldItemPrompt(
        PickupItem heldItem
    )
    {
        var heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null) return "This item cannot be stored";

        if (!CanAcceptProduct(
                heldProduct
            ))
            return "Bay assigned to "
                   + assignedProduct.ProductName;

        if (IsFull)
            return "Storage bay full  "
                   + quantity
                   + " / "
                   + Capacity;

        return "[E] Store "
               + heldProduct.ProductName
               + "  •  "
               + quantity
               + " / "
               + Capacity;
    }

    /*
     * Whole-carton placement.
     *
     * One E press transfers as much of the carton
     * as physically fits in the bay.
     */
    private void TryStoreWholeCarton(
        PlayerInteraction player,
        DeliveryBox deliveryBox
    )
    {
        if (player == null ||
            deliveryBox == null ||
            deliveryBox.ProductData == null ||
            deliveryBox.IsEmpty ||
            IsFull)
            return;

        var product =
            deliveryBox.ProductData;

        if (!CanAcceptProduct(
                product
            ))
            return;

        AssignIfNeeded(
            product
        );

        var availableSpace =
            Capacity
            - quantity;

        var requestedTransfer =
            Mathf.Min(
                availableSpace,
                deliveryBox.RemainingQuantity
            );

        var transferred =
            0;

        for (var i = 0;
             i < requestedTransfer;
             i++)
        {
            var tookUnit =
                deliveryBox.TryTakeOneUnit(
                    out var takenProduct
                );

            if (!tookUnit ||
                takenProduct != product)
                break;

            transferred++;
        }

        quantity +=
            transferred;

        RefreshDisplay();

        /*
         * A completely merged carton now belongs
         * to the storage representation.
         */
        if (deliveryBox.IsEmpty)
        {
            player.RemoveHeldDeliveryBox();

            Destroy(
                deliveryBox.gameObject
            );
        }
    }

    /*
     * R with a carried carton fills that carton
     * from this bay in one physical packing action.
     */
    private void TryFillCartonFromBay(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            !IsAssigned ||
            quantity <= 0 ||
            deliveryBox.IsFull ||
            deliveryBox.ProductData
            != assignedProduct)
            return;

        var availableBoxSpace =
            deliveryBox.Capacity
            - deliveryBox.RemainingQuantity;

        var requestedTransfer =
            Mathf.Min(
                quantity,
                availableBoxSpace
            );

        var transferred =
            0;

        for (var i = 0;
             i < requestedTransfer;
             i++)
        {
            var accepted =
                deliveryBox.TryAddOneUnit(
                    assignedProduct
                );

            if (!accepted) break;

            transferred++;
        }

        quantity -=
            transferred;

        RefreshDisplay();
    }

    private void TryTakeIntoBasket(
        RestockBasket basket
    )
    {
        if (basket == null ||
            basket.IsFull ||
            !IsAssigned ||
            quantity <= 0 ||
            assignedProduct.WorldPrefab == null)
            return;

        var spawnedItem =
            Instantiate(
                assignedProduct.WorldPrefab,
                transform.position,
                transform.rotation
            );

        var added =
            basket.TryAddItem(
                spawnedItem
            );

        if (!added)
        {
            Destroy(
                spawnedItem.gameObject
            );

            return;
        }

        quantity--;

        RefreshDisplay();
    }

    private void TryReturnFromBasket(
        RestockBasket basket
    )
    {
        if (basket == null ||
            basket.IsEmpty ||
            IsFull)
            return;

        var targetProduct =
            assignedProduct;

        /*
         * Empty bay:
         * use the first basket product to define
         * this player's chosen storage assignment.
         */
        if (targetProduct == null)
            targetProduct =
                basket.GetFirstProductData();

        if (targetProduct == null ||
            !CanAcceptProduct(
                targetProduct
            ))
            return;

        var foundItem =
            basket.TryTakeMatchingProduct(
                targetProduct,
                out var basketItem
            );

        if (!foundItem ||
            basketItem == null)
            return;

        AssignIfNeeded(
            targetProduct
        );

        quantity++;

        Destroy(
            basketItem.gameObject
        );

        RefreshDisplay();
    }

    private void TryTakeIntoHand(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld ||
            !IsAssigned ||
            quantity <= 0 ||
            assignedProduct.WorldPrefab == null)
            return;

        var spawnedItem =
            Instantiate(
                assignedProduct.WorldPrefab,
                transform.position,
                transform.rotation
            );

        var pickedUp =
            player.TryPickUp(
                spawnedItem
            );

        if (!pickedUp)
        {
            Destroy(
                spawnedItem.gameObject
            );

            return;
        }

        quantity--;

        RefreshDisplay();
    }

    private void TryStoreHeldItem(
        PlayerInteraction player,
        PickupItem heldItem
    )
    {
        if (player == null ||
            heldItem == null ||
            IsFull)
            return;

        var heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null ||
            !CanAcceptProduct(
                heldProduct
            ))
            return;

        AssignIfNeeded(
            heldProduct
        );

        player.RemoveHeldItem();

        quantity++;

        Destroy(
            heldItem.gameObject
        );

        RefreshDisplay();
    }

    private void TryTakeCarton(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld ||
            cartonPrefab == null ||
            !IsAssigned ||
            quantity <= 0)
            return;

        var cartonQuantity =
            Mathf.Min(
                Mathf.Max(
                    1,
                    portableCartonCapacity
                ),
                quantity
            );

        var createdCarton =
            Instantiate(
                cartonPrefab,
                transform.position,
                transform.rotation
            );

        createdCarton.Configure(
            assignedProduct,
            cartonQuantity
        );

        var pickedUp =
            player.TryPickUpDeliveryBox(
                createdCarton
            );

        if (!pickedUp)
        {
            Destroy(
                createdCarton.gameObject
            );

            return;
        }

        quantity -=
            cartonQuantity;

        RefreshDisplay();
    }

    private void AssignIfNeeded(
        ProductData productData
    )
    {
        if (assignedProduct != null) return;

        assignedProduct =
            productData;
    }

    private void RefreshDisplay()
    {
        var assigned =
            assignedProduct != null;

        if (assignedBoxVisual != null)
            assignedBoxVisual.SetActive(
                assigned
            );

        if (productIcon != null)
        {
            var icon =
                assigned
                    ? assignedProduct.Icon
                    : null;

            productIcon.sprite =
                icon;

            productIcon.gameObject.SetActive(
                icon != null
            );
        }

        if (fillBar != null)
        {
            fillBar.gameObject.SetActive(
                assigned
            );

            var ratio =
                assigned
                    ? Mathf.Clamp01(
                        (float)quantity
                        / Capacity
                    )
                    : 0f;

            var fillRect =
                fillBar.rectTransform;

            fillRect.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            fillRect.anchorMax =
                new Vector2(
                    ratio,
                    1f
                );

            fillRect.offsetMin =
                Vector2.zero;

            fillRect.offsetMax =
                Vector2.zero;

            if (ratio <= 0.25f)
                fillBar.color =
                    LowColor;
            else if (ratio <= 0.6f)
                fillBar.color =
                    MidColor;
            else
                fillBar.color =
                    HealthyColor;
        }

        if (statusText == null) return;

        if (!assigned)
        {
            statusText.text =
                "EMPTY";

            statusText.color =
                EmptyColor;

            return;
        }

        statusText.text =
            assignedProduct.ProductName
            + "\n"
            + quantity
            + " / "
            + Capacity;

        statusText.color =
            quantity <= 0
                ? LowColor
                : NormalColor;
    }

    public float ManagementHoldDuration =>
        0.6f;

    public bool CanUseManagementAction(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld)
            return false;

        return IsAssigned
               && quantity <= 0;
    }

    public void ManagementInteract(
        PlayerInteraction player
    )
    {
        if (!CanUseManagementAction(
                player
            ))
            return;

        ClearAssignment();
    }

    public bool CanRepeatPrimary(
        PlayerInteraction player
    )
    {
        if (player == null) return false;

        /*
         * Individual basket products move one
         * at a time, so holding E may repeat.
         *
         * Whole cartons are a one-action transfer.
         */
        return player.GetHeldRestockBasket()
               != null;
    }

    public bool CanRepeatSecondary(
        PlayerInteraction player
    )
    {
        if (player == null) return false;

        return player.GetHeldRestockBasket()
               != null;
    }
}