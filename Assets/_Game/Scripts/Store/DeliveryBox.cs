using UnityEngine;

public class DeliveryBox :
    MonoBehaviour,
    IInteractable,
    ISecondaryInteractable,
    ICarryInteractable,
    IRepeatableInteractable
{
    [Header("Contents")] [SerializeField] private ProductData productData;

    [Min(0)] [SerializeField] private int quantity = 4;

    [Min(1)] [SerializeField] private int boxCapacity = 4;

    [Header("References")] [SerializeField]
    private Transform spawnPoint;

    [SerializeField] private Transform placementAnchor;

    [Header("Held View")] 
    [SerializeField] private Vector3 heldViewOffset = new(0f, -0.62f, 1.05f);

    [Header("Drop Placement")] 
    [Min(0.5f)] [SerializeField] private float dropForwardDistance = 1.3f;
    [Min(0.01f)] [SerializeField] private float dropGroundClearance = 0.02f;
    [Min(1f)] [SerializeField] private float dropRayDistance = 3.5f;
    [SerializeField] private LayerMask dropSurfaceLayers = ~0;

    private bool isOpen;

    private bool isHeld;

    private Transform heldView;

    private Collider boxCollider;
    
    private DeliveryZone deliveryZoneOwner;

    public ProductData ProductData => productData;

    public int RemainingQuantity => quantity;

    public int Capacity => Mathf.Max(1, boxCapacity);

    public bool IsOpen => isOpen;

    public bool IsFull => quantity >= Capacity;

    public bool IsEmpty => quantity <= 0;
    
    public void SetDeliveryZoneOwner(DeliveryZone owner)
    {
        deliveryZoneOwner = owner;
    }

    private void Awake()
    {
        boxCollider =
            GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        if (!isHeld ||
            heldView == null)
            return;

        UpdateHeldPose();
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null) return "";

        if (productData == null) return "Delivery box not configured";

        if (productData.WorldPrefab == null) return "Product prefab not configured";

        /*
         * BASKET + BOX
         */
        var basket =
            player.GetHeldRestockBasket();

        if (basket != null)
            return GetBasketPrompt(
                basket
            );

        /*
         * PRODUCT IN HAND + BOX
         */
        var heldItem =
            player.GetHeldItem();

        if (heldItem != null)
            return GetHeldItemPrompt(
                heldItem
            );

        /*
         * Another box is already being carried.
         */
        if (player.GetHeldDeliveryBox()
            != null)
            return "Hands full";

        /*
         * EMPTY BOX
         */
        if (quantity <= 0)
            return "[F] Carry empty carton"
                   + "\nRecycle it in the back room";

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
        if (player == null) return;

        /*
         * E + basket:
         *
         * Box -> Basket.
         */
        var basket =
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
        if (player.GetHeldItem() != null) return;

        if (player.GetHeldDeliveryBox()
            != null)
            return;

        if (quantity <= 0)
            /*
             * Empty cartons are physical waste now.
             *
             * They are disposed of at the recycling bin,
             * not magically destroyed with E.
             */
            return;

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
        if (player == null) return;

        /*
         * R + basket:
         *
         * Basket -> Box.
         */
        var basket =
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
        var heldItem =
            player.GetHeldItem();

        if (heldItem != null)
            TryReturnHeldItem(
                player,
                heldItem
            );
    }

    public void SecondaryInteract(
        PlayerInteraction player
    )
    {
        ReturnOne(
            player
        );
    }

    public void Carry(
        PlayerInteraction player
    )
    {
        if (player == null) return;

        player.TryPickUpDeliveryBox(
            this
        );
    }

    public void CarryInteract(
        PlayerInteraction player
    )
    {
        Carry(
            player
        );
    }

    public bool CanRepeatPrimary(
        PlayerInteraction player
    )
    {
        if (player == null) return false;

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

    private string GetBasketPrompt(
        RestockBasket basket
    )
    {
        var matchingBasketCount =
            basket.GetProductCount(
                productData
            );

        var canTake =
            quantity > 0
            && !basket.IsFull;

        var canReturn =
            matchingBasketCount > 0
            && !IsFull;

        var boxCount =
            quantity
            + " / "
            + Capacity;

        if (canTake && canReturn)
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

        if (canTake)
            return "[E] Take "
                   + productData.ProductName
                   + "  •  Box "
                   + boxCount
                   + "  •  Hold";

        if (canReturn)
            return "[R] Put back "
                   + productData.ProductName
                   + "  •  Box "
                   + boxCount
                   + "  •  Hold";

        if (basket.IsFull &&
            quantity > 0)
            return "Restock basket full";

        if (IsFull &&
            matchingBasketCount > 0)
            return "Delivery box full";

        if (quantity <= 0) return "Delivery box empty";

        return "No matching product in basket";
    }

    private string GetHeldItemPrompt(
        PickupItem heldItem
    )
    {
        var heldProduct =
            heldItem.GetProductData();

        if (heldProduct == null) return "This item cannot go in the box";

        if (heldProduct != productData)
            return "This box is for "
                   + productData.ProductName;

        if (IsFull)
            return "Delivery box full  "
                   + quantity
                   + " / "
                   + Capacity;

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
            return false;

        var sourcePoint =
            spawnPoint != null
                ? spawnPoint
                : transform;

        var spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                sourcePoint.position,
                sourcePoint.rotation
            );

        var pickupSucceeded = player.TryPickUp(spawnedItem);

        if (!pickupSucceeded)
        {
            Destroy(spawnedItem.gameObject);

            return false;
        }

        quantity--;

        /*
         * Kept internally for future visual
         * open/close animation state.
         *
         * Opening is NOT a player command anymore.
         */
        isOpen = true;

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
            return false;

        var sourcePoint =
            spawnPoint != null
                ? spawnPoint
                : transform;

        var spawnedItem =
            Instantiate(
                productData.WorldPrefab,
                sourcePoint.position,
                sourcePoint.rotation
            );

        var added = basket.TryAddItem(spawnedItem);

        if (!added)
        {
            Destroy(spawnedItem.gameObject);

            return false;
        }

        quantity--;

        isOpen = true;

        return true;
    }

    private bool TryReturnOneFromBasket(
        RestockBasket basket
    )
    {
        if (basket == null ||
            productData == null ||
            IsFull)
            return false;

        var foundItem =
            basket.TryTakeMatchingProduct(
                productData,
                out var basketItem
            );

        if (!foundItem ||
            basketItem == null)
            return false;

        quantity++;

        isOpen = true;

        Destroy(basketItem.gameObject);

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
            return false;

        var heldProduct = heldItem.GetProductData();

        if (heldProduct == null ||
            heldProduct != productData)
            return false;

        player.RemoveHeldItem();

        quantity++;

        isOpen = true;

        Destroy(heldItem.gameObject);

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
        takenProduct = null;

        if (quantity <= 0 ||
            productData == null)
            return false;

        quantity--;

        isOpen = true;

        takenProduct = productData;

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
            return false;

        quantity++;

        isOpen = true;

        return true;
    }

    public void PickUp(
        Transform holdPoint,
        Transform viewTransform
    )
    {
        if (holdPoint == null || viewTransform == null)
            return;
        
        if (deliveryZoneOwner != null)
        {
            deliveryZoneOwner.ReleaseDelivery(this);
            deliveryZoneOwner = null;
        }

        heldView = viewTransform;

        isHeld = true;

        if (boxCollider != null)
            boxCollider.enabled = false;

        transform.SetParent( holdPoint, true);

        UpdateHeldPose();
    }

    public void Drop()
    {
        if (!isHeld) return;

        var horizontalForward = GetHorizontalForward(heldView);

        var targetPosition = transform.position;

        var targetRotation =
            Quaternion.LookRotation(
                -horizontalForward,
                Vector3.up
            );

        var foundSurface = false;

        var surfaceY = 0f;

        if (heldView != null)
        {
            targetPosition =
                heldView.position
                + horizontalForward
                * dropForwardDistance;

            var rayOrigin =
                targetPosition
                + Vector3.up * 0.3f;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out var hit,
                    dropRayDistance,
                    dropSurfaceLayers,
                    QueryTriggerInteraction.Ignore
                ))
            {
                foundSurface = true;

                surfaceY = hit.point.y;

                /*
                 * Temporary height.
                 *
                 * Once the collider is re-enabled
                 * below, we'll align its real bottom
                 * to this surface.
                 */
                targetPosition.y = surfaceY;
            }
            else
            {
                targetPosition.y = heldView.position.y - 1f;
            }
        }

        isHeld = false;

        heldView = null;

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
            boxCollider.enabled = true;

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
                var desiredBottomY =
                    surfaceY
                    + dropGroundClearance;

                var heightCorrection =
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
        if (heldView == null) return;

        var targetPosition =
            heldView.position
            + heldView.right
            * heldViewOffset.x
            + heldView.up
            * heldViewOffset.y
            + heldView.forward
            * heldViewOffset.z;

        var horizontalForward =
            GetHorizontalForward(
                heldView
            );

        var targetRotation =
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
        var direction =
            reference != null
                ? reference.forward
                : transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            if (reference != null)
                direction =
                    Vector3.Cross(
                        reference.right,
                        Vector3.up
                    );
            else
                direction = Vector3.forward;
        }

        return direction.normalized;
    }

    public void RestoreWorldTransform(
        Vector3 position,
        Quaternion rotation
    )
    {
        isHeld = false;

        heldView = null;

        transform.SetParent(
            null,
            true
        );

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (boxCollider != null)
            boxCollider.enabled = true;
    }

    public void Configure(
        ProductData newProductData,
        int newQuantity
    )
    {
        productData = newProductData;

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

        isOpen = false;
    }

    public void RestoreState(
        ProductData newProductData,
        int newQuantity,
        int newCapacity,
        bool newIsOpen
    )
    {
        productData = newProductData;

        if (newCapacity > 0)
            boxCapacity =
                newCapacity;
        else
            boxCapacity =
                Mathf.Max(
                    boxCapacity,
                    newQuantity
                );

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

        isOpen = newIsOpen;
    }

    public void PlaceAt(
        Transform placementPoint
    )
    {
        if (placementPoint == null) return;

        PlacementUtility.AlignAnchorToTarget(
            transform,
            placementAnchor,
            placementPoint
        );
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            placementAnchor,
            0.16f
        );
    }
}