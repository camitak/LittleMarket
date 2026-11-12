
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CheckoutConveyor : MonoBehaviour
{
    [Header("Belt Geometry")]
    [SerializeField] private Transform beltSurface;

    [Header("Product Spacing")]
    [Min(0f)] [SerializeField] private float endPadding = 0.04f;
    [Min(0f)] [SerializeField] private float sidePadding = 0.025f;
    [Min(0f)] [SerializeField] private float productGap = 0.035f;
    [Min(0f)] [SerializeField] private float surfaceLift = 0.005f;

    private sealed class PresentedItem
    {
        public PickupItem Item;
        public Transform OriginalParent;
        public Vector3 OriginalLocalPosition;
        public Quaternion OriginalLocalRotation;
        public Vector3 OriginalLocalScale;
        public Vector3 OriginalWorldPosition;
        public Quaternion OriginalWorldRotation;
        public bool OriginallyActive;

        public GameObject Slot;
        public float Width;
        public float RightOffset;
        public bool Scanned;
    }

    private readonly List<PresentedItem> presentedItems = new();
    private CustomerController customer;
    private int nextItemIndex;

    public bool IsConfigured => beltSurface != null;

    public void BeginSession(CustomerController nextCustomer)
    {
        if (customer == nextCustomer)
        {
            return;
        }

        EndSession(false);

        if (nextCustomer == null || !IsConfigured)
        {
            return;
        }

        customer = nextCustomer;
        nextItemIndex = 0;
        FillAvailableSpace();
    }

    public bool IsPresented(PickupItem item)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Item == item && !entry.Scanned)
            {
                return true;
            }
        }

        return false;
    }

    public void MarkScanned(PickupItem item)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Item != item || entry.Scanned)
            {
                continue;
            }

            entry.Scanned = true;
            DestroySlot(entry);

            RepositionWaitingItems();
            FillAvailableSpace();
            return;
        }
    }

    public void EndSession(bool completed)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (!completed)
            {
                RestoreItem(entry);
            }

            DestroySlot(entry);
        }

        presentedItems.Clear();
        customer = null;
        nextItemIndex = 0;
    }

    private void FillAvailableSpace()
    {
        if (customer == null || !IsConfigured)
        {
            return;
        }

        GetBeltLimits(out float leftEdge, out float rightEdge,
            out float centerZ, out float halfDepth, out float topY);

        float nextRightEdge = GetNextRightEdge(rightEdge);
        int itemCount = customer.GetCarriedItemCount();

        while (nextItemIndex < itemCount)
        {
            PickupItem item = customer.GetCarriedItem(nextItemIndex);

            if (item == null)
            {
                // Missing items must not be silently skipped and charged for.
                break;
            }

            PresentedItem entry = CaptureItem(item);

            GameObject slot = new($"CheckoutBeltItem_{nextItemIndex:00}");
            slot.transform.SetParent(transform, false);

            slot.transform.localPosition = new Vector3(
                (leftEdge + rightEdge) * 0.5f, topY, centerZ);

            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            entry.Slot = slot;

            customer.RestoreBasketItemForCheckout(item);
            item.PlaceInContainer(slot.transform);

            MeasureItem(entry, out float minX, out float maxX,
                out float minZ, out float maxZ);

            entry.Width = Mathf.Max(0.01f, maxX - minX);
            entry.RightOffset = maxX - slot.transform.localPosition.x;

            // Correct visual pivot offsets so items share a belt centreline.
            float centerCorrectionZ = centerZ - (minZ + maxZ) * 0.5f;
            slot.transform.localPosition += new Vector3(0f, 0f, centerCorrectionZ);

            float desiredX = nextRightEdge - entry.RightOffset;
            float itemLeft = desiredX + (minX - slot.transform.localPosition.x);

            bool itemFitsLength = itemLeft >= leftEdge;
            bool itemFitsDepth = (maxZ - minZ) <= halfDepth * 2f;
            bool hasWaitingItem = HasWaitingItem();

            if ((!itemFitsLength || !itemFitsDepth) && hasWaitingItem)
            {
                // No space yet. The actual product goes back to its owner.
                RestoreItem(entry);
                DestroySlot(entry);
                break;
            }

            if (!itemFitsLength || !itemFitsDepth)
            {
                // A large item may use the belt alone rather than jam it.
                desiredX = (leftEdge + rightEdge) * 0.5f -
                           (minX + maxX) * 0.5f +
                           slot.transform.localPosition.x;

                Debug.LogWarning(
                    $"'{item.GetItemName()}' is larger than the checkout belt's available space. " +
                    "Showing it alone until scanned.", item);
            }

            slot.transform.localPosition = new Vector3(
                desiredX,
                slot.transform.localPosition.y,
                slot.transform.localPosition.z);

            presentedItems.Add(entry);
            nextItemIndex++;

            nextRightEdge = desiredX +
                            (minX - (leftEdge + rightEdge) * 0.5f) -
                            productGap;

            if (!itemFitsLength || !itemFitsDepth)
            {
                break;
            }
        }
    }

    private float GetNextRightEdge(float rightEdge)
    {
        float current = rightEdge;

        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Scanned || entry.Slot == null)
            {
                continue;
            }

            float left = entry.Slot.transform.localPosition.x +
                         entry.RightOffset - entry.Width;

            current = Mathf.Min(current, left - productGap);
        }

        return current;
    }

    private bool HasWaitingItem()
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (!entry.Scanned)
            {
                return true;
            }
        }

        return false;
    }

    private void RepositionWaitingItems()
    {
        if (!IsConfigured)
        {
            return;
        }

        GetBeltLimits(out _, out float rightEdge, out _, out _, out _);
        float nextRightEdge = rightEdge;

        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Scanned || entry.Slot == null)
            {
                continue;
            }

            Transform slot = entry.Slot.transform;

            float x = nextRightEdge - entry.RightOffset;

            Vector3 position = slot.localPosition;
            position.x = x;
            slot.localPosition = position;

            nextRightEdge = x + entry.RightOffset -
                            entry.Width - productGap;
        }
    }

    private PresentedItem CaptureItem(PickupItem item)
    {
        Transform itemTransform = item.transform;

        return new PresentedItem
        {
            Item = item,
            OriginalParent = itemTransform.parent,
            OriginalLocalPosition = itemTransform.localPosition,
            OriginalLocalRotation = itemTransform.localRotation,
            OriginalLocalScale = itemTransform.localScale,
            OriginalWorldPosition = itemTransform.position,
            OriginalWorldRotation = itemTransform.rotation,
            OriginallyActive = item.gameObject.activeSelf
        };
    }

    private void RestoreItem(PresentedItem entry)
    {
        if (entry.Item == null)
        {
            return;
        }

        PickupItem item = entry.Item;
        item.gameObject.SetActive(entry.OriginallyActive);

        if (entry.OriginalParent != null)
        {
            item.PickUpForCustomer(entry.OriginalParent);

            item.transform.localPosition = entry.OriginalLocalPosition;
            item.transform.localRotation = entry.OriginalLocalRotation;
            item.transform.localScale = entry.OriginalLocalScale;
        }
        else
        {
            item.RestoreAsLooseWorldItem(
                entry.OriginalWorldPosition,
                entry.OriginalWorldRotation);

            item.transform.localScale = entry.OriginalLocalScale;
        }
    }

    private void DestroySlot(PresentedItem entry)
    {
        if (entry.Slot == null)
        {
            return;
        }

        Destroy(entry.Slot);
        entry.Slot = null;
    }

    private void GetBeltLimits(out float leftEdge, out float rightEdge,
        out float centerZ, out float halfDepth, out float topY)
    {
        Vector3 low = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(-0.5f, 0f, 0f)));

        Vector3 high = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0.5f, 0f, 0f)));

        Vector3 near = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0f, -0.5f)));

        Vector3 far = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0f, 0.5f)));

        Vector3 top = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0.5f, 0f)));

        leftEdge = Mathf.Min(low.x, high.x) + endPadding;
        rightEdge = Mathf.Max(low.x, high.x) - endPadding;

        centerZ = (near.z + far.z) * 0.5f;

        halfDepth = Mathf.Max(
            0f,
            Mathf.Abs(far.z - near.z) * 0.5f - sidePadding);

        topY = top.y + surfaceLift;
    }

    private void MeasureItem(PresentedItem entry,
        out float minX, out float maxX,
        out float minZ, out float maxZ)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;

        Renderer[] renderers = entry.Item.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;

            // Convert renderer bounds into checkout-local coordinates.
            // This also works when the entire fixture has been rotated.
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center +
                            Vector3.Scale(bounds.extents, new Vector3(x, y, z));

                        Vector3 localCorner = transform.InverseTransformPoint(corner);

                        minX = Mathf.Min(minX, localCorner.x);
                        maxX = Mathf.Max(maxX, localCorner.x);
                        minZ = Mathf.Min(minZ, localCorner.z);
                        maxZ = Mathf.Max(maxZ, localCorner.z);
                    }
                }
            }
        }

        if (float.IsPositiveInfinity(minX))
        {
            // Geometry fallback, not a product-count limit.
            Vector3 center = entry.Slot.transform.localPosition;

            minX = center.x - 0.1f;
            maxX = center.x + 0.1f;
            minZ = center.z - 0.1f;
            maxZ = center.z + 0.1f;
        }
    }

    private void OnDestroy()
    {
        EndSession(false);
    }
}
