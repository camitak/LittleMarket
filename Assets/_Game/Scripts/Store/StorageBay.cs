using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageBay :
    MonoBehaviour,
    IInteractable
{
    [Header("Identity")]
    [SerializeField]
    private string slotID = "";

    [Header("Storage")]
    [Min(1)]
    [SerializeField]
    private int capacity = 12;

    [Header("Portable Carton")]
    [SerializeField]
    private DeliveryBox cartonPrefab;

    [Min(1)]
    [SerializeField]
    private int portableCartonCapacity = 4;

    [Header("Display")]
    [SerializeField]
    private GameObject assignedBoxVisual;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private Image productIcon;

    [SerializeField]
    private Image fillBar;

    private ProductData assignedProduct;

    private int quantity;

    private StorageRegistry registeredStorageRegistry;

    private static readonly Color32 EmptyColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 NormalColor =
        new Color32(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 LowColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 MidColor =
        new Color32(
            246,
            215,
            122,
            255
        );

    private static readonly Color32 HealthyColor =
        new Color32(
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
            Object.FindAnyObjectByType<
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
        if (registeredStorageRegistry == null)
        {
            return;
        }

        registeredStorageRegistry.Unregister(
            this
        );
    }

    public bool CanAcceptProduct(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return false;
        }

        if (!IsAssigned)
        {
            return true;
        }

        return assignedProduct
               == productData;
    }

    public int GetStoredProductCount(
        ProductData productData
    )
    {
        if (productData == null ||
            assignedProduct != productData)
        {
            return 0;
        }

        return quantity;
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        DeliveryBox heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            return GetDeliveryBoxPrompt(
                heldBox
            );
        }

        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            return GetBasketPrompt(
                basket
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

        if (!IsAssigned)
        {
            return "Empty storage bay";
        }

        if (quantity <= 0)
        {
            return assignedProduct.ProductName
                   + "  •  0 / "
                   + Capacity
                   + "\n[F] Clear assignment";
        }

        int cartonQuantity =
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
        if (player == null)
        {
            return;
        }

        DeliveryBox heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            TryStoreWholeCarton(
                player,
                heldBox
            );

            return;
        }

        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            TryTakeIntoBasket(
                basket
            );

            return;
        }

        PickupItem heldItem =
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
        if (player == null)
        {
            return;
        }

        DeliveryBox heldBox =
            player.GetHeldDeliveryBox();

        if (heldBox != null)
        {
            TryFillCartonFromBay(
                heldBox
            );

            return;
        }

        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            TryReturnFromBasket(
                basket
            );
        }
    }

    /*
     * F has two storage-specific meanings:
     *
     * stocked bay -> take one portable carton
     * assigned empty bay -> clear assignment
     */
    public void HandleCarryAction(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld)
        {
            return;
        }

        if (!IsAssigned)
        {
            return;
        }

        if (quantity <= 0)
        {
            ClearAssignment();

            return;
        }

        TryTakeCarton(
            player
        );
    }

    public void ClearAssignment()
    {
        if (!IsAssigned ||
            quantity > 0)
        {
            return;
        }

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
        ProductData boxProduct =
            deliveryBox.ProductData;

        if (boxProduct == null)
        {
            return "Carton not configured";
        }

        if (!CanAcceptProduct(
                boxProduct
            ))
        {
            return "Bay assigned to "
                   + assignedProduct.ProductName;
        }

        int availableSpace =
            Mathf.Max(
                0,
                Capacity - quantity
            );

        int unitsToStore =
            Mathf.Min(
                availableSpace,
                deliveryBox.RemainingQuantity
            );

        int boxFreeSpace =
            Mathf.Max(
                0,
                deliveryBox.Capacity
                - deliveryBox.RemainingQuantity
            );

        int unitsToReturn =
            IsAssigned &&
            assignedProduct == boxProduct
                ? Mathf.Min(
                    quantity,
                    boxFreeSpace
                )
                : 0;

        if (unitsToStore > 0 &&
            unitsToReturn > 0)
        {
            return "[E] Place carton  •  +"
                   + unitsToStore
                   + " "
                   + boxProduct.ProductName
                   + "\n[R] Fill carton from bay  •  +"
                   + unitsToReturn;
        }

        if (unitsToStore > 0)
        {
            string verb =
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
        {
            return "[R] Fill carton from bay  •  +"
                   + unitsToReturn
                   + " "
                   + assignedProduct.ProductName;
        }

        if (IsFull)
        {
            return "Storage bay full  "
                   + quantity
                   + " / "
                   + Capacity;
        }

        if (deliveryBox.IsEmpty)
        {
            return "Carton empty";
        }

        return "";
    }

    private string GetBasketPrompt(
        RestockBasket basket
    )
    {
        /*
         * An unassigned bay can now be assigned
         * directly from the basket.
         *
         * R is Basket -> Storage, which remains
         * stable after assignment and avoids the
         * old direction-changing loop.
         */
        if (!IsAssigned)
        {
            ProductData firstProduct =
                basket.GetFirstProductData();

            if (firstProduct == null)
            {
                return "Empty storage bay";
            }

            return "[R] Store "
                   + firstProduct.ProductName
                   + "  •  Assign bay  •  Hold";
        }

        int matchingBasketCount =
            basket.GetProductCount(
                assignedProduct
            );

        bool canTake =
            quantity > 0
            && !basket.IsFull;

        bool canReturn =
            matchingBasketCount > 0
            && !IsFull;

        string bayCount =
            quantity
            + " / "
            + Capacity;

        if (canTake && canReturn)
        {
            return "[E] Take "
                   + assignedProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold"
                   + "\n[R] Put back one"
                   + "  •  Basket "
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity
                   + "  •  Hold";
        }

        if (canTake)
        {
            return "[E] Take "
                   + assignedProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold";
        }

        if (canReturn)
        {
            return "[R] Put back "
                   + assignedProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold";
        }

        if (basket.IsFull &&
            quantity > 0)
        {
            return "Restock basket full";
        }

        if (IsFull &&
            matchingBasketCount > 0)
        {
            return "Storage bay full";
        }

        return assignedProduct.ProductName
               + "  •  "
               + bayCount;
    }

    private string GetHeldItemPrompt(
        PickupItem heldItem
    )
    {
        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot be stored";
        }

        if (!CanAcceptProduct(
                heldProduct
            ))
        {
            return "Bay assigned to "
                   + assignedProduct.ProductName;
        }

        if (IsFull)
        {
            return "Storage bay full  "
                   + quantity
                   + " / "
                   + Capacity;
        }

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

        AssignIfNeeded(
            product
        );

        int availableSpace =
            Capacity
            - quantity;

        int requestedTransfer =
            Mathf.Min(
                availableSpace,
                deliveryBox.RemainingQuantity
            );

        int transferred =
            0;

        for (int i = 0;
             i < requestedTransfer;
             i++)
        {
            bool tookUnit =
                deliveryBox.TryTakeOneUnit(
                    out ProductData takenProduct
                );

            if (!tookUnit ||
                takenProduct != product)
            {
                break;
            }

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
        {
            return;
        }

        int availableBoxSpace =
            deliveryBox.Capacity
            - deliveryBox.RemainingQuantity;

        int requestedTransfer =
            Mathf.Min(
                quantity,
                availableBoxSpace
            );

        int transferred =
            0;

        for (int i = 0;
             i < requestedTransfer;
             i++)
        {
            bool accepted =
                deliveryBox.TryAddOneUnit(
                    assignedProduct
                );

            if (!accepted)
            {
                break;
            }

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
        {
            return;
        }

        PickupItem spawnedItem =
            Instantiate(
                assignedProduct.WorldPrefab,
                transform.position,
                transform.rotation
            );

        bool added =
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
        {
            return;
        }

        ProductData targetProduct =
            assignedProduct;

        /*
         * Empty bay:
         * use the first basket product to define
         * this player's chosen storage assignment.
         */
        if (targetProduct == null)
        {
            targetProduct =
                basket.GetFirstProductData();
        }

        if (targetProduct == null ||
            !CanAcceptProduct(
                targetProduct
            ))
        {
            return;
        }

        bool foundItem =
            basket.TryTakeMatchingProduct(
                targetProduct,
                out PickupItem basketItem
            );

        if (!foundItem ||
            basketItem == null)
        {
            return;
        }

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
        {
            return;
        }

        PickupItem spawnedItem =
            Instantiate(
                assignedProduct.WorldPrefab,
                transform.position,
                transform.rotation
            );

        bool pickedUp =
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
        {
            return;
        }

        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null ||
            !CanAcceptProduct(
                heldProduct
            ))
        {
            return;
        }

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
        {
            return;
        }

        int cartonQuantity =
            Mathf.Min(
                Mathf.Max(
                    1,
                    portableCartonCapacity
                ),
                quantity
            );

        DeliveryBox createdCarton =
            Instantiate(
                cartonPrefab,
                transform.position,
                transform.rotation
            );

        createdCarton.Configure(
            assignedProduct,
            cartonQuantity
        );

        bool pickedUp =
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
        if (assignedProduct != null)
        {
            return;
        }

        assignedProduct =
            productData;
    }

    private void RefreshDisplay()
    {
        bool assigned =
            assignedProduct != null;

        if (assignedBoxVisual != null)
        {
            assignedBoxVisual.SetActive(
                assigned
            );
        }

        if (productIcon != null)
        {
            Sprite icon =
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

            float ratio =
                assigned
                    ? Mathf.Clamp01(
                        (float)quantity
                        / Capacity
                    )
                    : 0f;

            fillBar.fillAmount =
                ratio;

            if (ratio <= 0.25f)
            {
                fillBar.color =
                    LowColor;
            }
            else if (ratio <= 0.6f)
            {
                fillBar.color =
                    MidColor;
            }
            else
            {
                fillBar.color =
                    HealthyColor;
            }
        }

        if (statusText == null)
        {
            return;
        }

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
}