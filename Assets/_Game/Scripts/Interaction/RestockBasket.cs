
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

    [Header("Held View Position")]
    [SerializeField]
    private Vector3 heldViewOffset =
        new Vector3(0.67f, -0.58f, 1.07f);

    [Header("Drop Placement")]
    [Min(0.5f)]
    [SerializeField]
    private float dropForwardDistance = 1.55f;

    [Min(0.01f)]
    [SerializeField]
    private float dropGroundClearance = 0.04f;

    [Min(1f)]
    [SerializeField]
    private float dropRayDistance = 3.5f;

    [SerializeField]
    private LayerMask dropSurfaceLayers = ~0;

    private readonly List<PickupItem> items =
        new List<PickupItem>();

    private Rigidbody basketRigidbody;
    private Collider basketCollider;

    private Transform heldView;
    private bool isHeld;

    public int Capacity
    {
        get
        {
            if (itemPoints == null ||
                itemPoints.Length <= 0)
            {
                return 0;
            }

            return Mathf.Min(
                Mathf.Max(1, capacity),
                itemPoints.Length
            );
        }
    }

    public int ItemCount => items.Count;

    public bool IsFull =>
        Capacity <= 0 ||
        ItemCount >= Capacity;

    public bool IsEmpty =>
        ItemCount <= 0;

    private void Awake()
    {
        basketRigidbody =
            GetComponent<Rigidbody>();

        basketCollider =
            GetComponent<Collider>();

        basketRigidbody.constraints |=
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void LateUpdate()
    {
        if (!isHeld || heldView == null)
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

        if (Capacity <= 0)
        {
            return "Restock basket not configured";
        }

        if (player.GetHeldRestockBasket() != null)
        {
            return "Hands full";
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            if (IsFull)
            {
                return "Restock basket full (" +
                       ItemCount + " / " + Capacity + ")";
            }

            return "[E] Add " +
                   heldItem.GetItemName() +
                   " to basket (" +
                   ItemCount + " / " + Capacity + ")";
        }

        return "[E] Pick up restock basket (" +
               ItemCount + " / " + Capacity + ")";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null || Capacity <= 0)
        {
            return;
        }

        PickupItem heldItem =
            player.GetHeldItem();

        if (heldItem != null)
        {
            if (TryAddItem(heldItem))
            {
                player.RemoveHeldItem();
            }

            return;
        }

        if (player.GetHeldRestockBasket() != null)
        {
            return;
        }

        player.TryPickUpRestockBasket(this);
    }

    public bool TryAddItem(PickupItem item)
    {
        if (item == null ||
            item.GetProductData() == null ||
            IsFull ||
            items.Contains(item))
        {
            return false;
        }

        Transform targetPoint =
            itemPoints[items.Count];

        if (targetPoint == null)
        {
            return false;
        }

        items.Add(item);

        item.PlaceInContainer(targetPoint);

        return true;
    }

    public bool TryTakeMatchingProduct(
        ProductData productData,
        out PickupItem item
    )
    {
        item = null;

        if (productData == null)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            PickupItem candidate = items[i];

            if (candidate == null ||
                candidate.GetProductData() != productData)
            {
                continue;
            }

            item = candidate;

            items.RemoveAt(i);

            RepackItems();

            return true;
        }

        return false;
    }

    public bool TryTakeFirstItem(
        out PickupItem item
    )
    {
        item = null;

        if (items.Count <= 0)
        {
            return false;
        }

        item = items[0];

        items.RemoveAt(0);

        RepackItems();

        return item != null;
    }

    public ProductData GetFirstProductData()
    {
        if (items.Count <= 0 || items[0] == null)
        {
            return null;
        }

        return items[0].GetProductData();
    }

    public int GetProductCount(
        ProductData productData
    )
    {
        if (productData == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            PickupItem item = items[i];

            if (item != null &&
                item.GetProductData() == productData)
            {
                count++;
            }
        }

        return count;
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

        heldView = viewTransform;
        isHeld = true;

        basketRigidbody.useGravity = false;
        basketRigidbody.isKinematic = true;

        if (basketCollider != null)
        {
            basketCollider.enabled = false;
        }

        transform.SetParent(holdPoint, true);

        UpdateHeldPose();
    }

    private void UpdateHeldPose()
    {
        if (heldView == null)
        {
            return;
        }

        // Camera-relative position keeps the basket
        // in a consistent lower-right screen area.
        Vector3 targetPosition =
            heldView.position +
            heldView.right * heldViewOffset.x +
            heldView.up * heldViewOffset.y +
            heldView.forward * heldViewOffset.z;

        // World-upright rotation ignores camera pitch.
        Vector3 horizontalForward =
            GetHorizontalForward(heldView);

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

    public void Drop()
    {
        if (!isHeld)
        {
            return;
        }

        Vector3 horizontalForward =
            GetHorizontalForward(heldView);

        Vector3 targetPosition =
            transform.position;

        if (heldView != null)
        {
            targetPosition =
                heldView.position +
                horizontalForward * dropForwardDistance;

            Vector3 rayOrigin =
                targetPosition + Vector3.up * 0.25f;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    dropRayDistance,
                    dropSurfaceLayers,
                    QueryTriggerInteraction.Ignore
                ))
            {
                targetPosition.y =
                    hit.point.y + dropGroundClearance;
            }
            else
            {
                // If no supporting surface is found,
                // let physics settle it naturally.
                targetPosition.y =
                    heldView.position.y - 1.0f;
            }
        }

        isHeld = false;
        heldView = null;

        transform.SetParent(null, true);

        transform.SetPositionAndRotation(
            targetPosition,
            Quaternion.LookRotation(
                -horizontalForward,
                Vector3.up
            )
        );

        basketRigidbody.linearVelocity =
            Vector3.zero;

        basketRigidbody.angularVelocity =
            Vector3.zero;

        basketRigidbody.constraints |=
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        if (basketCollider != null)
        {
            basketCollider.enabled = true;
        }

        basketRigidbody.isKinematic = false;
        basketRigidbody.useGravity = true;
    }

    private Vector3 GetHorizontalForward(
        Transform reference
    )
    {
        Vector3 direction =
            reference != null
                ? reference.forward
                : transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            // Handles the camera looking nearly
            // straight up or down.
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
                direction = Vector3.forward;
            }
        }

        return direction.normalized;
    }

    private void RepackItems()
    {
        int usableCount =
            Mathf.Min(items.Count, Capacity);

        for (int i = 0; i < usableCount; i++)
        {
            PickupItem item = items[i];

            Transform targetPoint = itemPoints[i];

            if (item == null || targetPoint == null)
            {
                continue;
            }

            item.PlaceInContainer(targetPoint);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (itemPoints == null)
        {
            return;
        }

        for (int i = 0; i < itemPoints.Length; i++)
        {
            Transform itemPoint = itemPoints[i];

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
                itemPoint.position +
                itemPoint.forward * 0.12f
            );
        }
    }
}
