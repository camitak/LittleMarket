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