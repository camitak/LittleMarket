using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 8;

    [Header("Store References")]
    [SerializeField]
    private StoreClock storeClock;

    [SerializeField]
    private StoreEconomy storeEconomy;

    [SerializeField]
    private StoreReputation storeReputation;

    [SerializeField]
    private StoreProgression storeProgression;

    [SerializeField]
    private DailyStats dailyStats;

    [SerializeField]
    private StoreGoals storeGoals;

    [SerializeField]
    private StoreLevelProgression
        storeLevelProgression;

    [SerializeField]
    private CustomerFlow customerFlow;

    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private DeliveryZone deliveryZone;

    [SerializeField]
    private WorldItemRegistry worldItemRegistry;

    [Header("Delivery")]
    [SerializeField]
    private DeliveryBox deliveryBoxPrefab;

    [Header("Player")]
    [SerializeField]
    private PlayerInteraction playerInteraction;

    public string LastOperationMessage
    {
        get;
        private set;
    } = "";

    public bool HasSaveFile =>
        File.Exists(
            SavePath
        );

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "little_market_save.json"
        );

    public bool SaveGame()
    {
        if (!HasValidConfiguration())
        {
            LastOperationMessage =
                "Save system is not configured.";

            Debug.LogError(
                LastOperationMessage,
                this
            );

            return false;
        }

        if (!CanUseSaveSystemNow())
        {
            return false;
        }

        StoreSaveData saveData =
            new StoreSaveData();

        saveData.saveVersion =
            CurrentSaveVersion;

        saveData.day =
            storeClock.CurrentDay;

        saveData.currentMinutes =
            storeClock.CurrentMinutes;

        saveData.money =
            storeEconomy.CurrentMoney;

        saveData.reputation =
            storeReputation.CurrentReputation;

        saveData.unlockedProductIDs =
            storeProgression
                .GetUnlockedProductIDs();

        saveData.shelfSlots =
            BuildShelfSlotSaveData();

        saveData.deliveries =
            BuildDeliverySaveData();

        saveData.dailyStats =
            dailyStats.CreateSaveData();

        saveData.looseProducts =
            BuildLooseProductSaveData();

        saveData.goalRewards =
            storeGoals.CreateSaveData();

        saveData.storeLevel =
            storeLevelProgression
                .CreateSaveData();

        string json =
            JsonUtility.ToJson(
                saveData,
                true
            );

        File.WriteAllText(
            SavePath,
            json
        );

        LastOperationMessage =
            "Game saved successfully.";

        Debug.Log(
            "Little Market saved "
            + "(version "
            + CurrentSaveVersion
            + ") at "
            + GetClockDebugText(
                saveData.currentMinutes
            )
            + ". Loose products: "
            + saveData.looseProducts.Count
            + ". Store Level: "
            + storeLevelProgression.CurrentLevel
            + ".\n"
            + SavePath
        );

        return true;
    }

    public bool LoadGame()
    {
        if (!HasValidConfiguration())
        {
            LastOperationMessage =
                "Save system is not configured.";

            Debug.LogError(
                LastOperationMessage,
                this
            );

            return false;
        }

        if (!CanUseSaveSystemNow())
        {
            return false;
        }

        if (!File.Exists(
                SavePath
            ))
        {
            LastOperationMessage =
                "No saved game exists yet.";

            Debug.LogWarning(
                LastOperationMessage
            );

            return false;
        }

        string json =
            File.ReadAllText(
                SavePath
            );

        StoreSaveData saveData =
            JsonUtility.FromJson<
                StoreSaveData
            >(
                json
            );

        if (saveData == null)
        {
            LastOperationMessage =
                "The save file could not be read.";

            Debug.LogError(
                LastOperationMessage
            );

            return false;
        }

        ApplySaveData(
            saveData
        );

        LastOperationMessage =
            "Game loaded successfully.";

        Debug.Log(
            "Little Market save loaded. "
            + "Save version: "
            + saveData.saveVersion
            + ". Current time: "
            + GetClockDebugText(
                storeClock.CurrentMinutes
            )
            + ". Store Level: "
            + storeLevelProgression.CurrentLevel
            + "."
        );

        return true;
    }

    private List<ShelfSlotSaveData>
        BuildShelfSlotSaveData()
    {
        List<ShelfSlotSaveData> savedSlots =
            new List<ShelfSlotSaveData>();

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

            ProductData product =
                slot.GetStoredProductData();

            if (product == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    slot.SlotID
                ))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    product.ProductID
                ))
            {
                continue;
            }

            ShelfSlotSaveData slotSaveData =
                new ShelfSlotSaveData();

            slotSaveData.slotID =
                slot.SlotID;

            slotSaveData.productID =
                product.ProductID;

            savedSlots.Add(
                slotSaveData
            );
        }

        return savedSlots;
    }

    private List<DeliveryBoxSaveData>
        BuildDeliverySaveData()
    {
        List<DeliveryBoxSaveData> savedDeliveries =
            new List<DeliveryBoxSaveData>();

        for (int i = 0;
             i < deliveryZone.DeliverySlotCount;
             i++)
        {
            DeliveryBox delivery =
                deliveryZone.GetActiveDelivery(
                    i
                );

            if (delivery == null)
            {
                continue;
            }

            ProductData product =
                delivery.ProductData;

            if (product == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    product.ProductID
                ))
            {
                continue;
            }

            DeliveryBoxSaveData deliverySaveData =
                new DeliveryBoxSaveData();

            deliverySaveData.slotIndex =
                i;

            deliverySaveData.productID =
                product.ProductID;

            deliverySaveData.quantity =
                delivery.RemainingQuantity;

            deliverySaveData.isOpen =
                delivery.IsOpen;

            savedDeliveries.Add(
                deliverySaveData
            );
        }

        return savedDeliveries;
    }

    private List<LooseProductSaveData>
        BuildLooseProductSaveData()
    {
        List<LooseProductSaveData> savedProducts =
            new List<LooseProductSaveData>();

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

            ProductData productData =
                item.GetProductData();

            if (productData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    productData.ProductID
                ))
            {
                continue;
            }

            LooseProductSaveData productSaveData =
                new LooseProductSaveData();

            productSaveData.productID =
                productData.ProductID;

            productSaveData.position =
                item.transform.position;

            productSaveData.rotation =
                item.transform.rotation;

            savedProducts.Add(
                productSaveData
            );
        }

        return savedProducts;
    }

    private void RestoreShelfInventory(
        List<ShelfSlotSaveData> savedSlots
    )
    {
        ClearCurrentShelfInventory();

        if (savedSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < savedSlots.Count;
             i++)
        {
            ShelfSlotSaveData slotSaveData =
                savedSlots[i];

            if (slotSaveData == null)
            {
                continue;
            }

            ShelfSlot slot =
                shelfRegistry.FindSlotByID(
                    slotSaveData.slotID
                );

            if (slot == null)
            {
                Debug.LogWarning(
                    "Saved ShelfSlot '"
                    + slotSaveData.slotID
                    + "' does not exist "
                    + "in the current scene."
                );

                continue;
            }

            ProductData product =
                storeProgression
                    .FindCatalogProductByID(
                        slotSaveData.productID
                    );

            if (product == null)
            {
                Debug.LogWarning(
                    "Saved product ID '"
                    + slotSaveData.productID
                    + "' does not exist "
                    + "in the current catalog."
                );

                continue;
            }

            bool restored =
                slot.RestoreProduct(
                    product
                );

            if (!restored)
            {
                Debug.LogWarning(
                    "Could not restore product '"
                    + slotSaveData.productID
                    + "' into ShelfSlot '"
                    + slotSaveData.slotID
                    + "'."
                );
            }
        }
    }

    private void RestoreDeliveryZone(
        List<DeliveryBoxSaveData> savedDeliveries
    )
    {
        deliveryZone
            .ClearAllDeliveriesForLoad();

        if (savedDeliveries == null)
        {
            return;
        }

        for (int i = 0;
             i < savedDeliveries.Count;
             i++)
        {
            DeliveryBoxSaveData deliverySaveData =
                savedDeliveries[i];

            if (deliverySaveData == null)
            {
                continue;
            }

            ProductData product =
                storeProgression
                    .FindCatalogProductByID(
                        deliverySaveData.productID
                    );

            if (product == null)
            {
                Debug.LogWarning(
                    "Saved delivery product ID '"
                    + deliverySaveData.productID
                    + "' does not exist "
                    + "in the current catalog."
                );

                continue;
            }

            bool restored =
                deliveryZone.TryRestoreDelivery(
                    deliveryBoxPrefab,
                    deliverySaveData.slotIndex,
                    product,
                    deliverySaveData.quantity,
                    deliverySaveData.isOpen,
                    out DeliveryBox restoredDelivery
                );

            if (!restored)
            {
                Debug.LogWarning(
                    "Could not restore delivery "
                    + "in slot "
                    + deliverySaveData.slotIndex
                    + " for product '"
                    + deliverySaveData.productID
                    + "'."
                );
            }
        }
    }

    private void RestoreLooseProducts(
        List<LooseProductSaveData> savedProducts
    )
    {
        worldItemRegistry
            .ClearLooseWorldItemsForLoad();

        if (savedProducts == null)
        {
            return;
        }

        for (int i = 0;
             i < savedProducts.Count;
             i++)
        {
            LooseProductSaveData productSaveData =
                savedProducts[i];

            if (productSaveData == null)
            {
                continue;
            }

            ProductData productData =
                storeProgression
                    .FindCatalogProductByID(
                        productSaveData.productID
                    );

            if (productData == null)
            {
                Debug.LogWarning(
                    "Saved loose product ID '"
                    + productSaveData.productID
                    + "' does not exist "
                    + "in the current catalog."
                );

                continue;
            }

            bool restored =
                worldItemRegistry
                    .RestoreLooseProduct(
                        productData,
                        productSaveData.position,
                        productSaveData.rotation
                    );

            if (!restored)
            {
                Debug.LogWarning(
                    "Could not restore loose product '"
                    + productSaveData.productID
                    + "'."
                );
            }
        }
    }

    private void ClearCurrentShelfInventory()
    {
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

            slot.ClearStoredItemForLoad();
        }
    }

    private void ApplySaveData(
        StoreSaveData saveData
    )
    {
        storeEconomy.SetMoney(
            saveData.money
        );

        storeReputation.SetReputation(
            saveData.reputation
        );

        storeProgression
            .RestoreUnlockedProducts(
                saveData.unlockedProductIDs
            );

        if (saveData.saveVersion >= 2)
        {
            RestoreShelfInventory(
                saveData.shelfSlots
            );
        }
        else
        {
            ClearCurrentShelfInventory();

            Debug.Log(
                "Older progression save loaded. "
                + "Shelf inventory was not restored "
                + "because that save predates "
                + "inventory persistence."
            );
        }

        if (saveData.saveVersion >= 3)
        {
            RestoreDeliveryZone(
                saveData.deliveries
            );
        }
        else
        {
            deliveryZone
                .ClearAllDeliveriesForLoad();

            Debug.Log(
                "Older save loaded. Delivery boxes "
                + "were not restored because that "
                + "save predates delivery persistence."
            );
        }

        if (saveData.saveVersion >= 6)
        {
            RestoreLooseProducts(
                saveData.looseProducts
            );
        }
        else
        {
            worldItemRegistry
                .ClearLooseWorldItemsForLoad();

            Debug.Log(
                "Older save loaded. Loose products "
                + "were cleared because that save "
                + "predates loose-product persistence."
            );
        }

        if (saveData.saveVersion >= 5 &&
            saveData.dailyStats != null)
        {
            dailyStats.RestoreFromSaveData(
                saveData.dailyStats
            );
        }
        else
        {
            dailyStats.ResetForNewDay();

            Debug.Log(
                "Older save loaded. Daily statistics "
                + "were reset because that save "
                + "predates daily-stat persistence."
            );
        }

        if (saveData.saveVersion >= 7 &&
            saveData.goalRewards != null)
        {
            storeGoals.RestoreFromSaveData(
                saveData.goalRewards
            );
        }
        else
        {
            storeGoals.ResetForNewDay();

            Debug.Log(
                "Older save loaded. Daily goal reward "
                + "claim state was reset because that "
                + "save predates goal-reward persistence."
            );
        }

        if (saveData.saveVersion >= 8 &&
            saveData.storeLevel != null)
        {
            storeLevelProgression
                .RestoreFromSaveData(
                    saveData.storeLevel
                );
        }
        else
        {
            storeLevelProgression
                .RestoreFromSaveData(
                    null
                );

            Debug.Log(
                "Older save loaded. Store Level "
                + "started at Level 1 because that "
                + "save predates Store Level progression."
            );
        }

        if (saveData.saveVersion >= 4)
        {
            storeClock.LoadState(
                saveData.day,
                saveData.currentMinutes
            );
        }
        else
        {
            storeClock.LoadDay(
                saveData.day
            );

            Debug.Log(
                "Older save loaded. Exact clock time "
                + "was not available, so the day "
                + "started at the configured start time."
            );
        }

        customerFlow.ResetForNewDay();
    }

    private bool CanUseSaveSystemNow()
    {
        if (storeClock.HasDayEnded)
        {
            LastOperationMessage =
                "Start the next day before saving or loading.";

            Debug.LogWarning(
                LastOperationMessage
            );

            return false;
        }

        if (customerFlow.ActiveCustomerCount > 0)
        {
            LastOperationMessage =
                "Wait until all customers have left.";

            Debug.LogWarning(
                LastOperationMessage
            );

            return false;
        }

        if (playerInteraction.GetHeldItem() != null)
        {
            LastOperationMessage =
                "Put away the item in your hands first.";

            Debug.LogWarning(
                LastOperationMessage
            );

            return false;
        }

        return true;
    }

    private string GetClockDebugText(
        float totalMinutes
    )
    {
        int wholeMinutes =
            Mathf.FloorToInt(
                totalMinutes
            );

        int hours =
            wholeMinutes / 60;

        int minutes =
            wholeMinutes % 60;

        return hours.ToString("00")
               + ":"
               + minutes.ToString("00");
    }

    private bool HasValidConfiguration()
    {
        if (storeClock == null)
        {
            return false;
        }

        if (storeEconomy == null)
        {
            return false;
        }

        if (storeReputation == null)
        {
            return false;
        }

        if (storeProgression == null)
        {
            return false;
        }

        if (dailyStats == null)
        {
            return false;
        }

        if (storeGoals == null)
        {
            return false;
        }

        if (storeLevelProgression == null)
        {
            return false;
        }

        if (customerFlow == null)
        {
            return false;
        }

        if (shelfRegistry == null)
        {
            return false;
        }

        if (deliveryZone == null)
        {
            return false;
        }

        if (worldItemRegistry == null)
        {
            return false;
        }

        if (deliveryBoxPrefab == null)
        {
            return false;
        }

        if (playerInteraction == null)
        {
            return false;
        }

        return true;
    }
}