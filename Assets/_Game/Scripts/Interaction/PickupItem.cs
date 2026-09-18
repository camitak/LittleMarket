using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Product")]
    [SerializeField] private ProductData productData;

    private Rigidbody rb;

    // If this item is currently on a shelf,
    // this remembers which slot owns it.
    private ShelfSlot currentShelfSlot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public string GetInteractionPrompt(PlayerInteraction  player)
    {
        if (player.GetHeldItem() != null)
        {
            return "Hands is full";
        }

        if (productData == null)
        {
            return "[E] Pick up Item";
        }
        
        return "[E] Pick up " +  productData.ProductName;
    }

    public void Interact(PlayerInteraction player)
    {
        player.TryPickUp(this);
    }

    public void PickUp(Transform holdPoint)
    {
        LeaveShelfIfNeeded();

        rb.useGravity = false;
        rb.isKinematic = true;

        transform.SetParent(holdPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void PlaceOnShelf(ShelfSlot shelfSlot)
    {
        currentShelfSlot = shelfSlot;

        rb.isKinematic = true;
        rb.useGravity = false;

        transform.SetParent(shelfSlot.transform);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void LeaveShelfIfNeeded()
    {
        if (currentShelfSlot == null)
        {
            return;
        }

        ShelfSlot previousSlot = currentShelfSlot;

        currentShelfSlot = null;

        previousSlot.RemoveItem(this);
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
}