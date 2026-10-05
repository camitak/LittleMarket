using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem :
    MonoBehaviour,
    IInteractable
{
    [Header("Product")]
    [SerializeField]
    private ProductData productData;

    [Header("Placement")]
    [SerializeField]
    private Transform shelfPlacementAnchor;

    private Rigidbody itemRigidbody;

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

    public void Drop()
    {
        LeaveShelfIfNeeded();

        transform.SetParent(
            null
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
        if (shelfSlot == null)
        {
            return;
        }

        LeaveShelfIfNeeded();

        currentShelfSlot =
            shelfSlot;

        itemRigidbody.useGravity =
            false;

        itemRigidbody.isKinematic =
            true;

        transform.SetParent(
            null,
            true
        );

        AlignShelfAnchorTo(
            shelfSlot.transform
        );

        transform.SetParent(
            shelfSlot.transform,
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

    private void AlignShelfAnchorTo(
        Transform shelfTarget
    )
    {
        if (shelfTarget == null)
        {
            return;
        }

        if (shelfPlacementAnchor == null)
        {
            transform.SetPositionAndRotation(
                shelfTarget.position,
                shelfTarget.rotation
            );

            return;
        }

        Quaternion anchorRotationRelativeToRoot =
            Quaternion.Inverse(
                transform.rotation
            )
            * shelfPlacementAnchor.rotation;

        transform.rotation =
            shelfTarget.rotation
            * Quaternion.Inverse(
                anchorRotationRelativeToRoot
            );

        Vector3 positionCorrection =
            shelfTarget.position
            - shelfPlacementAnchor.position;

        transform.position +=
            positionCorrection;
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

    private void OnDrawGizmosSelected()
    {
        if (shelfPlacementAnchor == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(
            shelfPlacementAnchor.position,
            0.025f
        );

        Gizmos.DrawLine(
            shelfPlacementAnchor.position,
            shelfPlacementAnchor.position
            + shelfPlacementAnchor.forward
            * 0.12f
        );
    }
}