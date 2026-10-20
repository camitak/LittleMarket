using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DeliveryZone))]
public class SupplierDeliveryQueue : MonoBehaviour
{
    private class PendingOrder
    {
        public ProductData Product;
        public int Quantity;
        public int ArrivalDay;

        public PendingOrder(
            ProductData product,
            int quantity,
            int arrivalDay
        )
        {
            Product = product;
            Quantity = quantity;
            ArrivalDay = arrivalDay;
        }
    }

    [Header("References")]
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private DeliveryBox deliveryBoxPrefab;

    private readonly List<PendingOrder> pendingOrders = new();

    private DeliveryZone deliveryZone;

    public bool IsPastCutoff =>
        storeClock != null &&
        storeClock.IsPastSupplierCutoff;

    public int PendingOrderCount => pendingOrders.Count;

    public int NextOrderArrivalDay =>
        storeClock != null
            ? storeClock.CurrentDay + 1
            : 1;

    public int QueueCapacity =>
        deliveryZone != null
            ? deliveryZone.DeliverySlotCount
            : 0;

    public bool CanQueueTomorrowOrder =>
        QueueCapacity > 0 &&
        pendingOrders.Count < QueueCapacity;

    private void Awake()
    {
        deliveryZone = GetComponent<DeliveryZone>();
    }

    private void Update()
    {
        TryDeliverDueOrders();
    }

    public bool TryQueueForTomorrow(
        ProductData product,
        int quantity
    )
    {
        if (product == null ||
            quantity <= 0 ||
            storeClock == null ||
            !CanQueueTomorrowOrder)
        {
            return false;
        }

        pendingOrders.Add(
            new PendingOrder(
                product,
                quantity,
                storeClock.CurrentDay + 1
            )
        );

        return true;
    }

    public string GetTerminalStatusText()
    {
        if (storeClock == null)
        {
            return "";
        }

        int dueOrders = GetDueOrderCount();

        if (dueOrders > 0)
        {
            string cartonWord =
                dueOrders == 1 ? "carton" : "cartons";

            return
                $"{dueOrders} supplier {cartonWord} waiting • " +
                "Clear delivery space";
        }

        if (IsPastCutoff)
        {
            return
                $"Same-day cutoff passed • Orders arrive Day {NextOrderArrivalDay}\n" +
                $"Tomorrow's delivery {PendingOrderCount} / {QueueCapacity}";
        }

        return $"Same-day delivery until {storeClock.SupplierCutoffTimeText}";
    }

    public List<PendingDeliverySaveData> CreateSaveData()
    {
        List<PendingDeliverySaveData> saveData = new();

        for (int i = 0; i < pendingOrders.Count; i++)
        {
            PendingOrder order = pendingOrders[i];

            if (order == null ||
                order.Product == null ||
                string.IsNullOrWhiteSpace(order.Product.ProductID))
            {
                continue;
            }

            saveData.Add(
                new PendingDeliverySaveData
                {
                    productID = order.Product.ProductID,
                    quantity = order.Quantity,
                    arrivalDay = order.ArrivalDay
                }
            );
        }

        return saveData;
    }

    public void RestoreFromSaveData(
        List<PendingDeliverySaveData> savedOrders
    )
    {
        pendingOrders.Clear();

        if (savedOrders == null || storeProgression == null)
        {
            return;
        }

        for (int i = 0; i < savedOrders.Count; i++)
        {
            PendingDeliverySaveData savedOrder = savedOrders[i];

            if (savedOrder == null ||
                string.IsNullOrWhiteSpace(savedOrder.productID) ||
                savedOrder.quantity <= 0)
            {
                continue;
            }

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    savedOrder.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Pending delivery product '{savedOrder.productID}' no longer exists.",
                    this
                );

                continue;
            }

            pendingOrders.Add(
                new PendingOrder(
                    product,
                    savedOrder.quantity,
                    Mathf.Max(1, savedOrder.arrivalDay)
                )
            );
        }
    }

    public void ClearPendingOrders()
    {
        pendingOrders.Clear();
    }

    private void TryDeliverDueOrders()
    {
        if (storeClock == null ||
            storeClock.HasDayEnded ||
            deliveryZone == null ||
            deliveryBoxPrefab == null)
        {
            return;
        }

        for (int i = 0; i < pendingOrders.Count;)
        {
            PendingOrder order = pendingOrders[i];

            if (order == null || order.Product == null)
            {
                pendingOrders.RemoveAt(i);
                continue;
            }

            if (order.ArrivalDay > storeClock.CurrentDay)
            {
                i++;
                continue;
            }

            if (!deliveryZone.HasSpace)
            {
                return;
            }

            bool delivered = deliveryZone.TryCreateDelivery(
                deliveryBoxPrefab,
                order.Product,
                order.Quantity,
                out _
            );

            if (!delivered)
            {
                return;
            }

            pendingOrders.RemoveAt(i);
        }
    }

    private int GetDueOrderCount()
    {
        if (storeClock == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < pendingOrders.Count; i++)
        {
            PendingOrder order = pendingOrders[i];

            if (order != null &&
                order.ArrivalDay <= storeClock.CurrentDay)
            {
                count++;
            }
        }

        return count;
    }
}