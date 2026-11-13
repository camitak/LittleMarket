
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
    
    [Header("Diagnostics")]
    [SerializeField] private bool logPackingDiagnostics;
    
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
        public float RelativeMinX;
        public float RelativeMaxX;
        public float RelativeMinZ;
        public float RelativeMaxZ;
        public float MinX;
        public float MaxX;
        public float MinZ;
        public float MaxZ;
        public bool Scanned;
        public bool Oversized;
    }

    private readonly List<PresentedItem> presentedItems = new();
    private CustomerController customer;
    private int nextItemIndex;

    public bool IsConfigured => beltSurface != null;

    public void BeginSession(CustomerController nextCustomer)
    {
        if (customer == nextCustomer) return;

        EndSession(false);
        if (nextCustomer == null || !IsConfigured) return;

        customer = nextCustomer;
        nextItemIndex = 0;
        
        if (logPackingDiagnostics)
        {
            Debug.Log(
                $"Checkout conveyor started with {customer.GetCarriedItemCount()} actual customer products.",
                this);
        }
        
        FillAvailableSpace();
    }

    public bool IsPresented(PickupItem item)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Item == item && !entry.Scanned) return true;
        }

        return false;
    }

    public void MarkScanned(PickupItem item)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (entry.Item != item || entry.Scanned) continue;

            entry.Scanned = true;
            DestroySlot(entry);

            // Existing products keep their positions for now. Fill the free
            // rectangles with waiting real products; animation comes later.
            FillAvailableSpace();
            return;
        }
    }

    public void EndSession(bool completed)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (!completed) RestoreItem(entry);
            DestroySlot(entry);
        }

        presentedItems.Clear();
        customer = null;
        nextItemIndex = 0;
    }

    private void FillAvailableSpace()
    {
        if (customer == null || !IsConfigured || HasOversizedWaitingItem()) return;

        GetBeltLimits(out float leftEdge, out float rightEdge,
            out float nearEdge, out float farEdge, out float topY);

        int itemCount = customer.GetCarriedItemCount();

        while (nextItemIndex < itemCount)
        {
            PickupItem item = customer.GetCarriedItem(nextItemIndex);
            if (item == null) break;

            PresentedItem entry = CaptureItem(item);
            GameObject slot = new($"CheckoutBeltItem_{nextItemIndex:00}");
            slot.transform.SetParent(transform, false);
            slot.transform.localPosition = new Vector3(0f, topY, 0f);
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            entry.Slot = slot;

            customer.RestoreBasketItemForCheckout(item);
            item.PlaceInContainer(slot.transform);

            MeasureItem(entry, out float minX, out float maxX,
                out float minZ, out float maxZ);

            // Measurements are in checkout-local space, relative to the slot.
            entry.RelativeMinX = minX;
            entry.RelativeMaxX = maxX;
            entry.RelativeMinZ = minZ;
            entry.RelativeMaxZ = maxZ;

            bool fits = TryFindPosition(entry, leftEdge, rightEdge,
                nearEdge, farEdge, out float slotX, out float slotZ);
            
            if (!fits && logPackingDiagnostics)
            {
                float itemWidth = maxX - minX;
                float itemDepth = maxZ - minZ;
                float beltWidth = rightEdge - leftEdge;
                float beltDepth = farEdge - nearEdge;

                Debug.Log(
                    $"Conveyor could not fit item {nextItemIndex + 1}/{itemCount} " +
                    $"({item.GetItemName()}). " +
                    $"Item footprint: {itemWidth:0.00}m × {itemDepth:0.00}m. " +
                    $"Usable belt: {beltWidth:0.00}m × {beltDepth:0.00}m. " +
                    "This can also be caused by occupied space or the packing order.",
                    this);
            }

            if (!fits && HasWaitingItem())
            {
                RestoreItem(entry);
                DestroySlot(entry);
                break;
            }

            if (!fits)
            {
                // A product larger than the usable belt must not deadlock
                // checkout. Display only that product, centred and unscaled.
                slotX = (leftEdge + rightEdge - minX - maxX) * 0.5f;
                slotZ = (nearEdge + farEdge - minZ - maxZ) * 0.5f;
                entry.Oversized = true;

                Debug.LogWarning($"'{item.GetItemName()}' exceeds the checkout belt. " +
                    "Showing it alone until scanned.", item);
            }

            slot.transform.localPosition = new Vector3(slotX, topY, slotZ);
            entry.MinX = slotX + minX;
            entry.MaxX = slotX + maxX;
            entry.MinZ = slotZ + minZ;
            entry.MaxZ = slotZ + maxZ;

            presentedItems.Add(entry);
            nextItemIndex++;

            if (entry.Oversized) break;
        }
    }

    private bool TryFindPosition(PresentedItem entry, float leftEdge, float rightEdge,
        float nearEdge, float farEdge, out float slotX, out float slotZ)
    {
        slotX = 0f;
        slotZ = 0f;
        bool found = false;
        float bestX = float.NegativeInfinity;
        float bestZ = float.PositiveInfinity;

        // Candidate X positions: against the scanner-side edge, or just
        // left of an occupied item's rectangle. Candidate Z positions:
        // against the customer-side edge, or behind an occupied rectangle.
        // No fixed grid, row count or item count is required.
        for (int xCandidate = -1; xCandidate < presentedItems.Count; xCandidate++)
        {
            if (xCandidate >= 0 && !IsOccupyingSpace(presentedItems[xCandidate])) continue;

            float right = xCandidate < 0 ? rightEdge : presentedItems[xCandidate].MinX - productGap;
            float x = right - entry.RelativeMaxX;
            float itemMinX = x + entry.RelativeMinX;
            float itemMaxX = x + entry.RelativeMaxX;

            if (itemMinX < leftEdge - 0.0001f || itemMaxX > rightEdge + 0.0001f) continue;

            for (int zCandidate = -1; zCandidate < presentedItems.Count; zCandidate++)
            {
                if (zCandidate >= 0 && !IsOccupyingSpace(presentedItems[zCandidate])) continue;

                float near = zCandidate < 0 ? nearEdge : presentedItems[zCandidate].MaxZ + productGap;
                float z = near - entry.RelativeMinZ;
                float itemMinZ = z + entry.RelativeMinZ;
                float itemMaxZ = z + entry.RelativeMaxZ;

                if (itemMinZ < nearEdge - 0.0001f || itemMaxZ > farEdge + 0.0001f) continue;
                if (OverlapsWaitingItem(itemMinX, itemMaxX, itemMinZ, itemMaxZ)) continue;

                // Prefer positions nearest the scanner, then customer side.
                if (!found || x > bestX + 0.0001f ||
                    (Mathf.Abs(x - bestX) <= 0.0001f && z < bestZ))
                {
                    found = true;
                    bestX = x;
                    bestZ = z;
                }
            }
        }

        if (!found) return false;

        slotX = bestX;
        slotZ = bestZ;
        return true;
    }

    private bool OverlapsWaitingItem(float minX, float maxX, float minZ, float maxZ)
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (!IsOccupyingSpace(entry)) continue;

            bool overlapsX = minX < entry.MaxX + productGap && maxX > entry.MinX - productGap;
            bool overlapsZ = minZ < entry.MaxZ + productGap && maxZ > entry.MinZ - productGap;
            if (overlapsX && overlapsZ) return true;
        }

        return false;
    }

    private static bool IsOccupyingSpace(PresentedItem entry)
    {
        return !entry.Scanned && entry.Slot != null;
    }

    private bool HasWaitingItem()
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (IsOccupyingSpace(entry)) return true;
        }

        return false;
    }

    private bool HasOversizedWaitingItem()
    {
        foreach (PresentedItem entry in presentedItems)
        {
            if (IsOccupyingSpace(entry) && entry.Oversized) return true;
        }

        return false;
    }

    private static PresentedItem CaptureItem(PickupItem item)
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

    private static void RestoreItem(PresentedItem entry)
    {
        if (entry.Item == null) return;

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
            item.RestoreAsLooseWorldItem(entry.OriginalWorldPosition, entry.OriginalWorldRotation);
            item.transform.localScale = entry.OriginalLocalScale;
        }
    }

    private void DestroySlot(PresentedItem entry)
    {
        if (entry.Slot == null) return;

        Destroy(entry.Slot);
        entry.Slot = null;
    }

    private void GetBeltLimits(out float leftEdge, out float rightEdge,
        out float nearEdge, out float farEdge, out float topY)
    {
        Vector3 left = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(-0.5f, 0f, 0f)));
        Vector3 right = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0.5f, 0f, 0f)));
        Vector3 near = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0f, -0.5f)));
        Vector3 far = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0f, 0.5f)));
        Vector3 top = transform.InverseTransformPoint(
            beltSurface.TransformPoint(new Vector3(0f, 0.5f, 0f)));

        leftEdge = Mathf.Min(left.x, right.x) + endPadding;
        rightEdge = Mathf.Max(left.x, right.x) - endPadding;
        nearEdge = Mathf.Min(near.z, far.z) + sidePadding;
        farEdge = Mathf.Max(near.z, far.z) - sidePadding;
        topY = top.y + surfaceLift;
    }

    private void MeasureItem(PresentedItem entry, out float minX, out float maxX,
        out float minZ, out float maxZ)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;

        Renderer[] renderers = entry.Item.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

            // Renderer.bounds is world-axis-aligned. Using its corners would
            // overestimate dimensions when the entire checkout is rotated.
            Bounds bounds = renderer.localBounds;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = bounds.center +
                            Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                        Vector3 worldCorner = renderer.transform.TransformPoint(corner);
                        Vector3 checkoutCorner = transform.InverseTransformPoint(worldCorner);

                        minX = Mathf.Min(minX, checkoutCorner.x);
                        maxX = Mathf.Max(maxX, checkoutCorner.x);
                        minZ = Mathf.Min(minZ, checkoutCorner.z);
                        maxZ = Mathf.Max(maxZ, checkoutCorner.z);
                    }
                }
            }
        }

        if (!float.IsPositiveInfinity(minX)) return;

        // Keep checkout operable even when an item has no visible renderer.
        minX = -0.1f;
        maxX = 0.1f;
        minZ = -0.1f;
        maxZ = 0.1f;
    }

    private void OnDestroy()
    {
        EndSession(false);
    }
}
