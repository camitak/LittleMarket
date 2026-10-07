using UnityEngine;

public class StoreStockOverview : MonoBehaviour
{
    [Header("Stock Rules")]
    [Min(0)]
    [SerializeField]
    private int lowStockThreshold = 2;

    [Header("Store References")]
    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private StorageRegistry storageRegistry;

    [SerializeField]
    private DeliveryZone deliveryZone;

    [SerializeField]
    private WorldItemRegistry worldItemRegistry;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    public int LowStockThreshold =>
        lowStockThreshold;

    public int GetShelfCount(
        ProductData productData
    )
    {
        if (productData == null ||
            shelfRegistry == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0;
             i < shelfRegistry.RegisteredSlotCount;
             i++)
        {
            ShelfSlot slot =
                shelfRegistry.GetRegisteredSlot(
                    i
                );

            if (slot == null)
            {
                continue;
            }

            ProductData storedProduct =
                slot.GetStoredProductData();

            if (storedProduct != productData)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public int GetStorageCount(
        ProductData productData
    )
    {
        if (productData == null ||
            storageRegistry == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0;
             i < storageRegistry.RegisteredSlotCount;
             i++)
        {
            ShelfSlot slot =
                storageRegistry.GetRegisteredSlot(
                    i
                );

            if (slot == null)
            {
                continue;
            }

            ProductData storedProduct =
                slot.GetStoredProductData();

            if (storedProduct != productData)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public int GetDeliveryCount(
        ProductData productData
    )
    {
        if (productData == null ||
            deliveryZone == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0;
             i < deliveryZone.DeliverySlotCount;
             i++)
        {
            DeliveryBox deliveryBox =
                deliveryZone.GetActiveDelivery(
                    i
                );

            if (deliveryBox == null)
            {
                continue;
            }

            if (deliveryBox.ProductData
                != productData)
            {
                continue;
            }

            count +=
                Mathf.Max(
                    0,
                    deliveryBox.RemainingQuantity
                );
        }

        return count;
    }

    public int GetLooseCount(
        ProductData productData
    )
    {
        if (productData == null ||
            worldItemRegistry == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0;
             i < worldItemRegistry.RegisteredItemCount;
             i++)
        {
            PickupItem item =
                worldItemRegistry.GetRegisteredItem(
                    i
                );

            if (item == null)
            {
                continue;
            }

            if (!item.IsLooseWorldItem)
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

    public int GetHeldCount(
        ProductData productData
    )
    {
        if (productData == null ||
            playerInteraction == null)
        {
            return 0;
        }

        PickupItem heldItem =
            playerInteraction.GetHeldItem();

        if (heldItem == null)
        {
            return 0;
        }

        if (heldItem.GetProductData()
            != productData)
        {
            return 0;
        }

        return 1;
    }

    public int GetOtherBackStockCount(
        ProductData productData
    )
    {
        return GetDeliveryCount(
                   productData
               )
               + GetLooseCount(
                   productData
               )
               + GetHeldCount(
                   productData
               );
    }

    public int GetBackStockCount(
        ProductData productData
    )
    {
        return GetStorageCount(
                   productData
               )
               + GetOtherBackStockCount(
                   productData
               );
    }

    public int GetTotalStockCount(
        ProductData productData
    )
    {
        return GetShelfCount(
                   productData
               )
               + GetBackStockCount(
                   productData
               );
    }

    public StockStatus GetStockStatus(
        ProductData productData
    )
    {
        int shelfCount =
            GetShelfCount(
                productData
            );

        int backStockCount =
            GetBackStockCount(
                productData
            );

        int totalStock =
            shelfCount
            + backStockCount;

        if (totalStock <= 0)
        {
            return StockStatus.OutOfStock;
        }

        if (shelfCount <= 0 &&
            backStockCount > 0)
        {
            return StockStatus.Restock;
        }

        if (totalStock
            <= lowStockThreshold)
        {
            return StockStatus.Low;
        }

        return StockStatus.Healthy;
    }

    public string GetStatusLabel(
        ProductData productData
    )
    {
        switch (GetStockStatus(
                    productData
                ))
        {
            case StockStatus.Low:
                return "LOW";

            case StockStatus.Restock:
                return "RESTOCK";

            case StockStatus.OutOfStock:
                return "OUT";

            default:
                return "OK";
        }
    }
}