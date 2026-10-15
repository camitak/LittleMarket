using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem :
    MonoBehaviour,
    IInteractable
{
    [Header("Product")]
    [SerializeField]
    private ProductData productData;

    [Header("Placement")]
    [FormerlySerializedAs(
        "shelfPlacementAnchor"
    )]
    [SerializeField]
    private Transform placementAnchor;

    private Rigidbody itemRigidbody;

    private Collider[] itemColliders;

    private ShelfSlot currentShelfSlot;

    private WorldItemRegistry worldItemRegistry;

    public bool IsOnShelf =>
        currentShelfSlot != null;

    public bool IsLooseWorldItem
    {
        get
        {
            if (currentShelfSlot != null)
            {
                return false;
            }

            if (transform.parent != null)
            {
                return false;
            }

            return true;
        }
    }

    private void Awake()
    {
        itemRigidbody =
            GetComponent<Rigidbody>();

        itemColliders =
            GetComponentsInChildren<
                Collider
            >(
                true
            );
    }

    private void Start()
    {
        FindAndRegisterWithWorldItemRegistry();
    }

    private void OnDestroy()
    {
        if (worldItemRegistry == null)
        {
            return;
        }

        worldItemRegistry.UnregisterItem(
            this
        );
    }

    private void FindAndRegisterWithWorldItemRegistry()
    {
        worldItemRegistry =
            Object.FindAnyObjectByType<
                WorldItemRegistry
            >();

        if (worldItemRegistry == null)
        {
            Debug.LogError(
                "PickupItem '"
                + gameObject.name
                + "' could not find a "
                + "WorldItemRegistry in the scene.",
                this
            );

            return;
        }

        worldItemRegistry.RegisterItem(
            this
        );
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket.IsFull)
            {
                return "Restock basket full"
                       + " ("
                       + heldBasket.ItemCount
                       + " / "
                       + heldBasket.Capacity
                       + ")";
            }

            return "[E] Load "
                   + GetItemName()
                   + " into basket"
                   + " ("
                   + heldBasket.ItemCount
                   + " / "
                   + heldBasket.Capacity
                   + ")";
        }

        if (player.GetHeldItem() != null)
        {
            return "Hands full";
        }

        return "[E] Pick up "
               + GetItemName();
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            heldBasket.TryAddItem(
                this
            );

            return;
        }

        if (player.GetHeldItem() != null)
        {
            return;
        }

        player.TryPickUp(
            this
        );
    }

    public void PickUp(
        Transform holdPoint
    )
    {
        if (holdPoint == null)
        {
            return;
        }

        LeaveShelfIfNeeded();

        /*
         * A product held directly in front of the
         * player must not block the interaction ray.
         *
         * Colliders come back when the item is
         * dropped or placed somewhere.
         */
        SetItemCollidersEnabled(
            false
        );

        itemRigidbody.useGravity =
            false;

        itemRigidbody.isKinematic =
            true;

        transform.SetParent(
            holdPoint
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;
    }
    
    public void PickUpForCustomer(
        Transform carryPoint
    )
    {
        if (carryPoint == null)
        {
            return;
        }

        LeaveShelfIfNeeded();

        /*
         * Once a customer owns this product,
         * it is visual cargo rather than something
         * the player can interact with.
         */
        SetItemCollidersEnabled(
            false
        );

        itemRigidbody.useGravity =
            false;

        itemRigidbody.isKinematic =
            true;

        transform.SetParent(
            carryPoint
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;
    }

    public void Drop()
    {
        LeaveShelfIfNeeded();

        transform.SetParent(
            null
        );

        SetItemCollidersEnabled(
            true
        );

        itemRigidbody.useGravity =
            true;

        itemRigidbody.isKinematic =
            false;
    }

    public void PlaceOnShelf(
        ShelfSlot shelfSlot
    )
    {
        PlaceOnShelf(
            shelfSlot,
            0
        );
    }

    public void PlaceOnShelf(
        ShelfSlot shelfSlot,
        int displayIndex
    )
    {
        if (shelfSlot == null)
        {
            return;
        }
        
        if (currentShelfSlot != shelfSlot)
        {
            LeaveShelfIfNeeded();
        }

        currentShelfSlot =
            shelfSlot;

        itemRigidbody.useGravity =
            false;

        itemRigidbody.isKinematic =
            true;
        
        SetItemCollidersEnabled(
            false
        );

        transform.SetParent(
            null,
            true
        );

        PlacementUtility.AlignAnchorToPose(
            transform,
            placementAnchor,
            shelfSlot.GetDisplayWorldPosition(
                displayIndex
            ),
            shelfSlot.GetDisplayWorldRotation()
        );

        transform.SetParent(
            shelfSlot.transform,
            true
        );
    }

    public void PlaceInContainer(
        Transform containerPoint
    )
    {
        if (containerPoint == null)
        {
            return;
        }

        LeaveShelfIfNeeded();

        itemRigidbody.useGravity =
            false;

        itemRigidbody.isKinematic =
            true;

        /*
         * Basket contents are visual cargo.
         *
         * Their colliders are disabled so four
         * nested rigidbodies do not fight the
         * basket or block the player's raycast.
         */
        SetItemCollidersEnabled(
            false
        );

        transform.SetParent(
            null,
            true
        );

        PlacementUtility.AlignAnchorToTarget(
            transform,
            placementAnchor,
            containerPoint
        );

        transform.SetParent(
            containerPoint,
            true
        );
    }

    public void RestoreAsLooseWorldItem(
        Vector3 position,
        Quaternion rotation
    )
    {
        LeaveShelfIfNeeded();

        transform.SetParent(
            null
        );

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        SetItemCollidersEnabled(
            true
        );

        itemRigidbody.useGravity =
            true;

        itemRigidbody.isKinematic =
            false;
    }

    public string GetItemName()
    {
        if (productData == null)
        {
            return "Item";
        }

        return productData.ProductName;
    }

    public ProductData GetProductData()
    {
        return productData;
    }

    private void LeaveShelfIfNeeded()
    {
        if (currentShelfSlot == null)
        {
            return;
        }

        ShelfSlot previousShelfSlot =
            currentShelfSlot;

        currentShelfSlot =
            null;

        previousShelfSlot.RemoveItem(
            this
        );
    }

    private void SetItemCollidersEnabled(
        bool enabled
    )
    {
        if (itemColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < itemColliders.Length;
             i++)
        {
            Collider itemCollider =
                itemColliders[i];

            if (itemCollider == null)
            {
                continue;
            }

            itemCollider.enabled =
                enabled;
        }
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            placementAnchor,
            0.12f
        );
    }
}