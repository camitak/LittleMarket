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

    [Min(1)]
    [SerializeField]
    private int boxCapacity = 4;

    [Header("References")]
    [SerializeField]
    private Transform spawnPoint;

    [Header("Held View")]
    [SerializeField]
    private Vector3 heldViewOffset =
        new Vector3(
            0f,
            -0.62f,
            1.05f
        );

    [Header("Drop Placement")]
    [Min(0.5f)]
    [SerializeField]
    private float dropForwardDistance = 1.3f;

    [Min(0.01f)]
    [SerializeField]
    private float dropGroundClearance = 0.02f;

    [Min(1f)]
    [SerializeField]
    private float dropRayDistance = 3.5f;

    [SerializeField]
    private LayerMask dropSurfaceLayers = ~0;

    private bool isOpen;

    private bool isHeld;

    private Transform heldView;

    private Collider boxCollider;

    public ProductData ProductData =>
        productData;

    public int RemainingQuantity =>
        quantity;

    public int Capacity =>
        Mathf.Max(
            1,
            boxCapacity
        );

    public bool IsOpen =>
        isOpen;

    public bool IsFull =>
        quantity >= Capacity;

    public bool IsEmpty =>
        quantity <= 0;

    private void Awake()
    {
        boxCollider =
            GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        if (!isHeld ||
            heldView == null)
        {
            return;
        }

        UpdateHeldPose();
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        if (productData == null)
        {
            return "Delivery box not configured";
        }

        if (productData.WorldPrefab == null)
        {
            return "Product prefab not configured";
        }

        /*
         * BASKET + BOX
         */
        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            return GetBasketPrompt(
                basket
            );
        }

        /*
         * PRODUCT IN HAND + BOX
         */
        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            return GetHeldItemPrompt(
                heldItem
            );
        }

        /*
         * Another box is already being carried.
         */
        if (player.GetHeldDeliveryBox()
            != null)
        {
            return "Hands full";
        }

        /*
         * EMPTY BOX
         */
        if (quantity <= 0)
        {
            return "[E] Remove empty box"
                   + "\n[F] Carry empty box";
        }

        /*
         * NORMAL GROUNDED BOX
         *
         * E works with the contents.
         * F works with the container itself.
         */
        return "[E] Take "
               + productData.ProductName
               + "  •  "
               + quantity
               + " / "
               + Capacity
               + "\n[F] Carry box";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        /*
         * E + basket:
         *
         * Box -> Basket.
         */
        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            TryDispenseOneToBasket(
                basket
            );

            return;
        }

        /*
         * E is NOT used to return an individual
         * product to the box anymore.
         *
         * R owns the reverse action.
         */
        if (player.GetHeldItem() != null)
        {
            return;
        }

        if (player.GetHeldDeliveryBox()
            != null)
        {
            return;
        }

        /*
         * Empty box:
         *
         * E removes the cardboard.
         */
        if (quantity <= 0)
        {
            Destroy(
                gameObject
            );

            return;
        }

        /*
         * E + empty hands:
         *
         * Take exactly one product.
         */
        TryDispenseOneToHand(
            player
        );
    }

    public void ReturnOne(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        /*
         * R + basket:
         *
         * Basket -> Box.
         */
        RestockBasket basket =
            player.GetHeldRestockBasket();

        if (basket != null)
        {
            TryReturnOneFromBasket(
                basket
            );

            return;
        }

        /*
         * R + individual matching product:
         *
         * Hand -> Box.
         */
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

    public void Carry(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        player.TryPickUpDeliveryBox(
            this
        );
    }

    private string GetBasketPrompt(
        RestockBasket basket
    )
    {
        int matchingBasketCount =
            basket.GetProductCount(
                productData
            );

        bool canTake =
            quantity > 0
            && !basket.IsFull;

        bool canReturn =
            matchingBasketCount > 0
            && !IsFull;

        string boxCount =
            quantity
            + " / "
            + Capacity;

        if (canTake && canReturn)
        {
            return "[E] Take "
                   + productData.ProductName
                   + "  •  Box "
                   + boxCount
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
                   + productData.ProductName
                   + "  •  Box "
                   + boxCount
                   + "  •  Hold";
        }

        if (canReturn)
        {
            return "[R] Put back "
                   + productData.ProductName
                   + "  •  Box "
                   + boxCount
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
            return "Delivery box full";
        }

        if (quantity <= 0)
        {
            return "Delivery box empty";
        }

        return "No matching product in basket";
    }

    private string GetHeldItemPrompt(
        PickupItem heldItem
    )
    {
        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null)
        {
            return "This item cannot go in the box";
        }

        if (heldProduct != productData)
        {
            return "This box is for "
                   + productData.ProductName;
        }

        if (IsFull)
        {
            return "Delivery box full  "
                   + quantity
                   + " / "
                   + Capacity;
        }

        return "[R] Put "
               + productData.ProductName
               + " back into box  •  "
               + quantity
               + " / "
               + Capacity;
    }

    private bool TryDispenseOneToHand(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.GetHeldItem() != null ||
            player.GetHeldRestockBasket() != null ||
            player.GetHeldDeliveryBox() != null ||
            quantity <= 0 ||
            productData == null ||
            productData.WorldPrefab == null)
        {
            return false;
        }

        Transform sourcePoint =
            spawnPoint != null
                ? spawnPoint
                : transform;

        PickupItem spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                sourcePoint.position,
                sourcePoint.rotation
            );

        bool pickupSucceeded =
            player.TryPickUp(
                spawnedItem
            );

        if (!pickupSucceeded)
        {
            Destroy(
                spawnedItem.gameObject
            );

            return false;
        }

        quantity--;

        /*
         * Kept internally for future visual
         * open/close animation state.
         *
         * Opening is NOT a player command anymore.
         */
        isOpen =
            true;

        return true;
    }

    private bool TryDispenseOneToBasket(
        RestockBasket basket
    )
    {
        if (basket == null ||
            basket.IsFull ||
            quantity <= 0 ||
            productData == null ||
            productData.WorldPrefab == null)
        {
            return false;
        }

        Transform sourcePoint =
            spawnPoint != null
                ? spawnPoint
                : transform;

        PickupItem spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                sourcePoint.position,
                sourcePoint.rotation
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

            return false;
        }

        quantity--;

        isOpen =
            true;

        return true;
    }

    private bool TryReturnOneFromBasket(
        RestockBasket basket
    )
    {
        if (basket == null ||
            productData == null ||
            IsFull)
        {
            return false;
        }

        bool foundItem =
            basket.TryTakeMatchingProduct(
                productData,
                out PickupItem basketItem
            );

        if (!foundItem ||
            basketItem == null)
        {
            return false;
        }

        quantity++;

        isOpen =
            true;

        Destroy(
            basketItem.gameObject
        );

        return true;
    }

    private bool TryReturnHeldItem(
        PlayerInteraction player,
        PickupItem heldItem
    )
    {
        if (player == null ||
            heldItem == null ||
            IsFull)
        {
            return false;
        }

        ProductData heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null ||
            heldProduct != productData)
        {
            return false;
        }

        player.RemoveHeldItem();

        quantity++;

        isOpen =
            true;

        Destroy(
            heldItem.gameObject
        );

        return true;
    }

    /*
     * Used while the whole box itself is being
     * carried to a shelf/storage position.
     */
    public bool TryTakeOneUnit(
        out ProductData takenProduct
    )
    {
        takenProduct =
            null;

        if (quantity <= 0 ||
            productData == null)
        {
            return false;
        }

        quantity--;

        isOpen =
            true;

        takenProduct =
            productData;

        return true;
    }

    /*
     * Used when reversing a transfer from
     * a shelf/storage position into the held box.
     */
    public bool TryAddOneUnit(
        ProductData incomingProduct
    )
    {
        if (incomingProduct == null ||
            incomingProduct != productData ||
            IsFull)
        {
            return false;
        }

        quantity++;

        isOpen =
            true;

        return true;
    }

    public void PickUp(
        Transform holdPoint,
        Transform viewTransform
    )
    {
        if (holdPoint == null ||
            viewTransform == null)
        {
            return;
        }

        heldView =
            viewTransform;

        isHeld =
            true;

        if (boxCollider != null)
        {
            boxCollider.enabled =
                false;
        }

        transform.SetParent(
            holdPoint,
            true
        );

        UpdateHeldPose();
    }

    public void Drop()
    {
        if (!isHeld)
        {
            return;
        }

        Vector3 horizontalForward =
            GetHorizontalForward(
                heldView
            );

        Vector3 targetPosition =
            transform.position;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                -horizontalForward,
                Vector3.up
            );

        bool foundSurface =
            false;

        float surfaceY =
            0f;

        if (heldView != null)
        {
            targetPosition =
                heldView.position
                + horizontalForward
                * dropForwardDistance;

            Vector3 rayOrigin =
                targetPosition
                + Vector3.up * 0.3f;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    dropRayDistance,
                    dropSurfaceLayers,
                    QueryTriggerInteraction.Ignore
                ))
            {
                foundSurface =
                    true;

                surfaceY =
                    hit.point.y;

                /*
                 * Temporary height.
                 *
                 * Once the collider is re-enabled
                 * below, we'll align its real bottom
                 * to this surface.
                 */
                targetPosition.y =
                    surfaceY;
            }
            else
            {
                targetPosition.y =
                    heldView.position.y
                    - 1f;
            }
        }

        isHeld =
            false;

        heldView =
            null;

        transform.SetParent(
            null,
            true
        );

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        if (boxCollider != null)
        {
            boxCollider.enabled =
                true;

            /*
             * The object pivot is at the CENTRE of
             * the current box collider.
             *
             * PF_DeliveryBox currently has a
             * 0.5 m tall collider, so placing the
             * pivot directly on the floor buried
             * roughly half of it underground.
             *
             * Using bounds.min makes this work even
             * if we later resize or reposition the
             * collider.
             */
            if (foundSurface)
            {
                float desiredBottomY =
                    surfaceY
                    + dropGroundClearance;

                float heightCorrection =
                    desiredBottomY
                    - boxCollider.bounds.min.y;

                transform.position +=
                    Vector3.up
                    * heightCorrection;
            }
        }
    }

    private void UpdateHeldPose()
    {
        if (heldView == null)
        {
            return;
        }

        Vector3 targetPosition =
            heldView.position
            + heldView.right
            * heldViewOffset.x
            + heldView.up
            * heldViewOffset.y
            + heldView.forward
            * heldViewOffset.z;

        Vector3 horizontalForward =
            GetHorizontalForward(
                heldView
            );

        Quaternion targetRotation =
            Quaternion.LookRotation(
                -horizontalForward,
                Vector3.up
            );

        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );
    }

    private Vector3 GetHorizontalForward(
        Transform reference
    )
    {
        Vector3 direction =
            reference != null
                ? reference.forward
                : transform.forward;

        direction.y =
            0f;

        if (direction.sqrMagnitude
            < 0.001f)
        {
            if (reference != null)
            {
                direction =
                    Vector3.Cross(
                        reference.right,
                        Vector3.up
                    );
            }
            else
            {
                direction =
                    Vector3.forward;
            }
        }

        return direction.normalized;
    }

    public void RestoreWorldTransform(
        Vector3 position,
        Quaternion rotation
    )
    {
        isHeld =
            false;

        heldView =
            null;

        transform.SetParent(
            null,
            true
        );

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (boxCollider != null)
        {
            boxCollider.enabled =
                true;
        }
    }

    public void Configure(
        ProductData newProductData,
        int newQuantity
    )
    {
        productData =
            newProductData;

        boxCapacity =
            Mathf.Max(
                Mathf.Max(
                    1,
                    boxCapacity
                ),
                newQuantity
            );

        quantity =
            Mathf.Clamp(
                newQuantity,
                0,
                boxCapacity
            );

        isOpen =
            false;
    }

    public void RestoreState(
        ProductData newProductData,
        int newQuantity,
        int newCapacity,
        bool newIsOpen
    )
    {
        productData =
            newProductData;

        if (newCapacity > 0)
        {
            boxCapacity =
                newCapacity;
        }
        else
        {
            boxCapacity =
                Mathf.Max(
                    boxCapacity,
                    newQuantity
                );
        }

        boxCapacity =
            Mathf.Max(
                1,
                boxCapacity
            );

        quantity =
            Mathf.Clamp(
                newQuantity,
                0,
                boxCapacity
            );

        isOpen =
            newIsOpen;
    }
}