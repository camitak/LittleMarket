using TMPro;
using UnityEngine;

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

    [Header("Display")]
    [SerializeField]
    private GameObject assignedBoxVisual;

    [SerializeField]
    private TMP_Text statusText;

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

    private static readonly Color32 OutColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 FullColor =
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

        return "[E] Take "
               + assignedProduct.ProductName
               + "  •  "
               + quantity
               + " / "
               + Capacity;
    }

    /*
     * E = natural PRIMARY action.
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
            TryStoreFromDeliveryBox(
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

        /*
         * A directly held product goes back into
         * storage with R, not E.
         */
        if (player.GetHeldItem() != null)
        {
            return;
        }

        TryTakeIntoHand(
            player
        );
    }

    /*
     * R = reverse the primary action.
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
            TryReturnToDeliveryBox(
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

            return;
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            TryReturnHeldItem(
                player,
                heldItem
            );
        }
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
            return "Delivery box not configured";
        }

        if (!CanAcceptProduct(
                boxProduct
            ))
        {
            return "Bay assigned to "
                   + assignedProduct.ProductName;
        }

        bool canStore =
            !deliveryBox.IsEmpty
            && !IsFull;

        bool canReturn =
            IsAssigned
            && quantity > 0
            && assignedProduct == boxProduct
            && !deliveryBox.IsFull;

        string bayCount =
            quantity
            + " / "
            + Capacity;

        string boxCount =
            deliveryBox.RemainingQuantity
            + " / "
            + deliveryBox.Capacity;

        if (canStore && canReturn)
        {
            string verb =
                IsAssigned
                    ? "Store "
                    : "Assign ";

            return "[E] "
                   + verb
                   + boxProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold"
                   + "\n[R] Return one to box"
                   + "  •  Box "
                   + boxCount
                   + "  •  Hold";
        }

        if (canStore)
        {
            string verb =
                IsAssigned
                    ? "Store "
                    : "Assign ";

            return "[E] "
                   + verb
                   + boxProduct.ProductName
                   + "  •  Bay "
                   + bayCount
                   + "  •  Hold";
        }

        if (canReturn)
        {
            return "[R] Return "
                   + assignedProduct.ProductName
                   + " to box  •  Bay "
                   + bayCount
                   + "  •  Hold";
        }

        if (IsFull)
        {
            return "Storage bay full  "
                   + bayCount;
        }

        if (deliveryBox.IsEmpty)
        {
            return "Delivery box empty";
        }

        return "";
    }

    private string GetBasketPrompt(
        RestockBasket basket
    )
    {
        if (!IsAssigned)
        {
            return "Empty bay"
                   + "\nAssign it with a delivery box";
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

        return "[R] Store "
               + heldProduct.ProductName
               + "  •  "
               + quantity
               + " / "
               + Capacity;
    }

    private void TryStoreFromDeliveryBox(
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

        bool tookUnit =
            deliveryBox.TryTakeOneUnit(
                out ProductData takenProduct
            );

        if (!tookUnit ||
            takenProduct == null)
        {
            return;
        }

        AssignIfNeeded(
            takenProduct
        );

        quantity++;

        RefreshDisplay();

        /*
         * When the delivery box has been completely
         * transferred into storage, its cardboard
         * becomes the storage representation.
         *
         * This also frees the delivery slot.
         */
        if (deliveryBox.IsEmpty)
        {
            player.RemoveHeldDeliveryBox();

            Destroy(
                deliveryBox.gameObject
            );
        }
    }

    private void TryReturnToDeliveryBox(
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

        bool accepted =
            deliveryBox.TryAddOneUnit(
                assignedProduct
            );

        if (!accepted)
        {
            return;
        }

        quantity--;

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
            !IsAssigned ||
            IsFull)
        {
            return;
        }

        bool foundItem =
            basket.TryTakeMatchingProduct(
                assignedProduct,
                out PickupItem basketItem
            );

        if (!foundItem ||
            basketItem == null)
        {
            return;
        }

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
            player.GetHeldItem() != null ||
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

    private void TryReturnHeldItem(
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

        if (quantity <= 0)
        {
            statusText.color =
                OutColor;

            return;
        }

        if (IsFull)
        {
            statusText.color =
                FullColor;

            return;
        }

        statusText.color =
            NormalColor;
    }
}