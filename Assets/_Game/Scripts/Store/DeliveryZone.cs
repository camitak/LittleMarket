using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [Header("Delivery Slots")] [SerializeField]
    private Transform[] deliverySlots;

    private DeliveryBox[] activeDeliveries;

    public bool HasSpace =>
        FindAvailableSlotIndex()
        >= 0;

    public int ActiveDeliveryCount
    {
        get
        {
            if (activeDeliveries == null) return 0;

            var count = 0;

            for (var i = 0;
                 i < activeDeliveries.Length;
                 i++)
            {
                if (activeDeliveries[i] == null) continue;

                count++;
            }

            return count;
        }
    }

    public int DeliverySlotCount
    {
        get
        {
            if (deliverySlots == null) return 0;

            return deliverySlots.Length;
        }
    }

    private void Awake()
    {
        var slotCount = 0;

        if (deliverySlots != null)
            slotCount =
                deliverySlots.Length;

        activeDeliveries =
            new DeliveryBox[slotCount];
    }

    public DeliveryBox GetActiveDelivery(
        int slotIndex
    )
    {
        if (activeDeliveries == null) return null;

        if (slotIndex < 0 ||
            slotIndex >= activeDeliveries.Length)
            return null;

        return activeDeliveries[
            slotIndex
        ];
    }

    public bool TryCreateDelivery(
        DeliveryBox deliveryBoxPrefab,
        ProductData productData,
        int quantity,
        out DeliveryBox createdDelivery
    )
    {
        createdDelivery = null;

        if (deliveryBoxPrefab == null) return false;

        if (productData == null) return false;

        if (quantity <= 0) return false;

        var slotIndex = FindAvailableSlotIndex();

        if (slotIndex < 0) return false;

        var deliverySlot = deliverySlots[slotIndex];

        createdDelivery =
            Instantiate(
                deliveryBoxPrefab,
                deliverySlot.position,
                deliverySlot.rotation
            );

        createdDelivery.PlaceAt(
            deliverySlot
        );

        createdDelivery.Configure(
            productData,
            quantity
        );

        activeDeliveries[slotIndex] = createdDelivery;
        createdDelivery.SetDeliveryZoneOwner(this);

        return true;
    }

    public bool TryRestoreDelivery(
        DeliveryBox deliveryBoxPrefab,
        int slotIndex,
        ProductData productData,
        int quantity,
        int capacity,
        bool isOpen,
        out DeliveryBox createdDelivery
    )
    {
        createdDelivery = null;

        if (deliveryBoxPrefab == null) return false;

        if (productData == null) return false;

        if (quantity < 0) return false;

        if (deliverySlots == null ||
            activeDeliveries == null)
            return false;

        if (slotIndex < 0 ||
            slotIndex >= deliverySlots.Length)
            return false;

        var deliverySlot =
            deliverySlots[slotIndex];

        if (deliverySlot == null) return false;

        if (activeDeliveries[slotIndex]
            != null)
            return false;

        createdDelivery =
            Instantiate(
                deliveryBoxPrefab,
                deliverySlot.position,
                deliverySlot.rotation
            );

        createdDelivery.PlaceAt(
            deliverySlot
        );

        createdDelivery.RestoreState(
            productData,
            quantity,
            capacity,
            isOpen
        );

        activeDeliveries[slotIndex] = createdDelivery;
        createdDelivery.SetDeliveryZoneOwner(this);
        return true;
    }

    public void ClearAllDeliveriesForLoad()
    {
        if (activeDeliveries == null) return;

        for (var i = 0;
             i < activeDeliveries.Length;
             i++)
        {
            var delivery =
                activeDeliveries[i];

            activeDeliveries[i] =
                null;

            if (delivery == null) continue;

            Destroy(
                delivery.gameObject
            );
        }
    }
    
    public void ReleaseDelivery(DeliveryBox deliveryBox)
    {
        if (deliveryBox == null || activeDeliveries == null)
        {
            return;
        }

        for (int i = 0; i < activeDeliveries.Length; i++)
        {
            if (activeDeliveries[i] != deliveryBox)
            {
                continue;
            }

            activeDeliveries[i] = null;
            return;
        }
    }

    public int GetSlotIndex(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            activeDeliveries == null)
            return -1;

        for (var i = 0;
             i < activeDeliveries.Length;
             i++)
            if (activeDeliveries[i]
                == deliveryBox)
                return i;

        return -1;
    }

    private int FindAvailableSlotIndex()
    {
        if (deliverySlots == null) return -1;

        if (activeDeliveries == null) return -1;

        for (var i = 0;
             i < deliverySlots.Length;
             i++)
        {
            if (deliverySlots[i] == null) continue;

            if (activeDeliveries[i] != null) continue;

            return i;
        }

        return -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (deliverySlots == null) return;

        for (var i = 0;
             i < deliverySlots.Length;
             i++)
        {
            var deliveryPoint =
                deliverySlots[i];

            if (deliveryPoint == null) continue;

            PlacementUtility.DrawFrameGizmo(
                deliveryPoint,
                0.16f
            );
        }
    }
}