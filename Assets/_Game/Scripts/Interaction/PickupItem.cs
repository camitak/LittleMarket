using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem :
    MonoBehaviour,
    IInteractable
{
    [Header("Product")]
    [SerializeField]
    private ProductData productData;

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
            shelfSlot.transform
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;
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
}