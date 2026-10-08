using System.Collections.Generic;
using UnityEngine;

[RequireComponent(
    typeof(Rigidbody),
    typeof(BoxCollider)
)]
public class RestockBasket :
    MonoBehaviour,
    IInteractable
{
    [Header("Contents")]
    [Min(1)]
    [SerializeField]
    private int capacity = 4;

    [SerializeField]
    private Transform[] itemPoints;

    [Header("Held Pose")]
    [SerializeField]
    private Vector3 heldLocalPosition =
        new Vector3(
            0f,
            -0.10f,
            0.10f
        );

    [SerializeField]
    private Vector3 heldLocalEulerAngles =
        Vector3.zero;

    private readonly List<PickupItem>
        items =
            new List<PickupItem>();

    private Rigidbody basketRigidbody;

    private Collider basketCollider;

    public int Capacity
    {
        get
        {
            if (itemPoints == null)
            {
                return 0;
            }

            if (itemPoints.Length <= 0)
            {
                return 0;
            }

            return Mathf.Min(
                Mathf.Max(
                    1,
                    capacity
                ),
                itemPoints.Length
            );
        }
    }

    public int ItemCount =>
        items.Count;

    public bool IsFull =>
        Capacity <= 0
        || ItemCount >= Capacity;

    public bool IsEmpty =>
        ItemCount <= 0;

    private void Awake()
    {
        basketRigidbody =
            GetComponent<Rigidbody>();

        basketCollider =
            GetComponent<Collider>();
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        if (Capacity <= 0)
        {
            return "Restock basket not configured";
        }

        if (player.GetHeldRestockBasket()
            != null)
        {
            return "Hands full";
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            if (IsFull)
            {
                return "Restock basket full"
                       + " ("
                       + ItemCount
                       + " / "
                       + Capacity
                       + ")";
            }

            return "[E] Add "
                   + heldItem.GetItemName()
                   + " to basket"
                   + " ("
                   + ItemCount
                   + " / "
                   + Capacity
                   + ")";
        }

        return "[E] Pick up restock basket"
               + " ("
               + ItemCount
               + " / "
               + Capacity
               + ")";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return;
        }

        if (Capacity <= 0)
        {
            return;
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            bool added =
                TryAddItem(
                    heldItem
                );

            if (added)
            {
                player.RemoveHeldItem();
            }

            return;
        }

        if (player.GetHeldRestockBasket()
            != null)
        {
            return;
        }

        player.TryPickUpRestockBasket(
            this
        );
    }

    public bool TryAddItem(
        PickupItem item
    )
    {
        if (item == null)
        {
            return false;
        }

        if (item.GetProductData() == null)
        {
            return false;
        }

        if (IsFull)
        {
            return false;
        }

        if (items.Contains(
                item
            ))
        {
            return false;
        }

        Transform targetPoint =
            itemPoints[
                items.Count
            ];

        if (targetPoint == null)
        {
            return false;
        }

        items.Add(
            item
        );

        item.PlaceInContainer(
            targetPoint
        );

        return true;
    }

    public bool TryTakeMatchingProduct(
        ProductData productData,
        out PickupItem item
    )
    {
        item =
            null;

        if (productData == null)
        {
            return false;
        }

        for (int i = 0;
             i < items.Count;
             i++)
        {
            PickupItem candidate =
                items[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.GetProductData()
                != productData)
            {
                continue;
            }

            item =
                candidate;

            items.RemoveAt(
                i
            );

            RepackItems();

            return true;
        }

        return false;
    }

    public bool TryTakeFirstItem(
        out PickupItem item
    )
    {
        item =
            null;

        if (items.Count <= 0)
        {
            return false;
        }

        item =
            items[0];

        items.RemoveAt(
            0
        );

        RepackItems();

        return item != null;
    }

    public ProductData GetFirstProductData()
    {
        if (items.Count <= 0)
        {
            return null;
        }

        PickupItem firstItem =
            items[0];

        if (firstItem == null)
        {
            return null;
        }

        return firstItem.GetProductData();
    }

    public int GetProductCount(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return 0;
        }

        int count =
            0;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            PickupItem item =
                items[i];

            if (item == null)
            {
                continue;
            }

            if (item.GetProductData()
                != productData)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public void PickUp(
        Transform holdPoint
    )
    {
        if (holdPoint == null)
        {
            return;
        }

        basketRigidbody.useGravity =
            false;

        basketRigidbody.isKinematic =
            true;

        if (basketCollider != null)
        {
            basketCollider.enabled =
                false;
        }

        transform.SetParent(
            holdPoint
        );

        transform.localPosition =
            heldLocalPosition;

        transform.localRotation =
            Quaternion.Euler(
                heldLocalEulerAngles
            );
    }

    public void Drop()
    {
        transform.SetParent(
            null
        );

        if (basketCollider != null)
        {
            basketCollider.enabled =
                true;
        }

        basketRigidbody.useGravity =
            true;

        basketRigidbody.isKinematic =
            false;
    }

    private void RepackItems()
    {
        int usableCount =
            Mathf.Min(
                items.Count,
                Capacity
            );

        for (int i = 0;
             i < usableCount;
             i++)
        {
            PickupItem item =
                items[i];

            Transform targetPoint =
                itemPoints[i];

            if (item == null ||
                targetPoint == null)
            {
                continue;
            }

            item.PlaceInContainer(
                targetPoint
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (itemPoints == null)
        {
            return;
        }

        for (int i = 0;
             i < itemPoints.Length;
             i++)
        {
            Transform itemPoint =
                itemPoints[i];

            if (itemPoint == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(
                itemPoint.position,
                0.03f
            );

            Gizmos.DrawLine(
                itemPoint.position,
                itemPoint.position
                + itemPoint.forward
                * 0.12f
            );
        }
    }
}