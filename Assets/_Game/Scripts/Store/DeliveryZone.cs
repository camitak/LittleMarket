using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [Header("Delivery Slots")]
    [SerializeField]
    private Transform[] deliverySlots;

    private DeliveryBox[] activeDeliveries;

    public bool HasSpace
    {
        get
        {
            return FindAvailableSlotIndex()
                   >= 0;
        }
    }

    public int ActiveDeliveryCount
    {
        get
        {
            if (activeDeliveries == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0;
                 i < activeDeliveries.Length;
                 i++)
            {
                if (activeDeliveries[i] == null)
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }

    public int DeliverySlotCount
    {
        get
        {
            if (deliverySlots == null)
            {
                return 0;
            }

            return deliverySlots.Length;
        }
    }

    private void Awake()
    {
        int slotCount = 0;

        if (deliverySlots != null)
        {
            slotCount =
                deliverySlots.Length;
        }

        activeDeliveries =
            new DeliveryBox[slotCount];
    }

    public DeliveryBox GetActiveDelivery(
        int slotIndex
    )
    {
        if (activeDeliveries == null)
        {
            return null;
        }

        if (slotIndex < 0 ||
            slotIndex >= activeDeliveries.Length)
        {
            return null;
        }

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

        if (deliveryBoxPrefab == null)
        {
            return false;
        }

        if (productData == null)
        {
            return false;
        }

        if (quantity <= 0)
        {
            return false;
        }

        int slotIndex =
            FindAvailableSlotIndex();

        if (slotIndex < 0)
        {
            return false;
        }

        Transform deliverySlot =
            deliverySlots[slotIndex];

        createdDelivery =
            Instantiate(
                deliveryBoxPrefab,
                deliverySlot.position,
                deliverySlot.rotation
            );

        createdDelivery.Configure(
            productData,
            quantity
        );

        activeDeliveries[slotIndex] =
            createdDelivery;

        return true;
    }

    public bool TryRestoreDelivery(
        DeliveryBox deliveryBoxPrefab,
        int slotIndex,
        ProductData productData,
        int quantity,
        bool isOpen,
        out DeliveryBox createdDelivery
    )
    {
        createdDelivery = null;

        if (deliveryBoxPrefab == null)
        {
            return false;
        }

        if (productData == null)
        {
            return false;
        }

        if (quantity < 0)
        {
            return false;
        }

        if (deliverySlots == null ||
            activeDeliveries == null)
        {
            return false;
        }

        if (slotIndex < 0 ||
            slotIndex >= deliverySlots.Length)
        {
            return false;
        }

        Transform deliverySlot =
            deliverySlots[slotIndex];

        if (deliverySlot == null)
        {
            return false;
        }

        if (activeDeliveries[slotIndex]
            != null)
        {
            return false;
        }

        createdDelivery =
            Instantiate(
                deliveryBoxPrefab,
                deliverySlot.position,
                deliverySlot.rotation
            );

        createdDelivery.RestoreState(
            productData,
            quantity,
            isOpen
        );

        activeDeliveries[slotIndex] =
            createdDelivery;

        return true;
    }

    public void ClearAllDeliveriesForLoad()
    {
        if (activeDeliveries == null)
        {
            return;
        }

        for (int i = 0;
             i < activeDeliveries.Length;
             i++)
        {
            DeliveryBox delivery =
                activeDeliveries[i];

            activeDeliveries[i] =
                null;

            if (delivery == null)
            {
                continue;
            }

            Destroy(
                delivery.gameObject
            );
        }
    }

    private int FindAvailableSlotIndex()
    {
        if (deliverySlots == null)
        {
            return -1;
        }

        if (activeDeliveries == null)
        {
            return -1;
        }

        for (int i = 0;
             i < deliverySlots.Length;
             i++)
        {
            if (deliverySlots[i] == null)
            {
                continue;
            }

            if (activeDeliveries[i] != null)
            {
                continue;
            }

            return i;
        }

        return -1;
    }
}