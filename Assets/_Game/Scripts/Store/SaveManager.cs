using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 17;

    [Header("Store References")] 
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreReputation storeReputation;
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private StoreGoals storeGoals;
    [SerializeField] private StoreLevelProgression storeLevelProgression;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private ShelfRegistry shelfRegistry;
    [SerializeField] private StorageRegistry storageRegistry;
    [SerializeField] private DeliveryZone deliveryZone;
    [SerializeField] private WorldItemRegistry worldItemRegistry;
    [SerializeField] private StoreReportHistory storeReportHistory;

    [Header("Delivery")] 
    [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [SerializeField] private SupplierDeliveryQueue supplierDeliveryQueue;

    [Header("Player")] 
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private RestockBasket restockBasket;

    public string LastOperationMessage { get; private set; } = "";

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

        if (!CanUseSaveSystemNow()) return false;

        var saveData = new StoreSaveData();

        saveData.saveVersion = CurrentSaveVersion;

        saveData.day = storeClock.CurrentDay;

        saveData.currentMinutes = storeClock.CurrentMinutes;

        saveData.money = storeEconomy.CurrentMoney;

        saveData.reputation = storeReputation.CurrentReputation;

        saveData.unlockedProductIDs = storeProgression.GetUnlockedProductIDs();

        saveData.shelfSlots = BuildShelfSlotSaveData();

        saveData.storageSlots = BuildStorageSlotSaveData();

        saveData.deliveries = BuildDeliverySaveData();
        
        saveData.pendingDeliveries = supplierDeliveryQueue.CreateSaveData();

        saveData.dailyStats = dailyStats.CreateSaveData();

        saveData.looseProducts = BuildLooseProductSaveData();

        saveData.goalRewards = storeGoals.CreateSaveData();

        saveData.storeLevel = storeLevelProgression.CreateSaveData();
        
        saveData.dailyReports = storeReportHistory.CreateSaveData();

        var json =
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

        if (!CanUseSaveSystemNow()) return false;

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

        var json =
            File.ReadAllText(
                SavePath
            );

        var saveData =
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

    private List<ShelfSlotSaveData> BuildShelfSlotSaveData()
    {
        List<ShelfSlotSaveData> savedSlots =
            new List<ShelfSlotSaveData>();

        for (int i = 0; i < shelfRegistry.RegisteredSlotCount; i++)
        {
            ShelfSlot slot = shelfRegistry.GetRegisteredSlot(i);

            if (slot == null || !slot.IsAssigned)
            {
                continue;
            }

            ProductData product = slot.AssignedProduct;

            if (product == null ||
                string.IsNullOrWhiteSpace(slot.SlotID) ||
                string.IsNullOrWhiteSpace(product.ProductID))
            {
                continue;
            }

            ShelfSlotSaveData slotSaveData = new ShelfSlotSaveData
            {
                slotID = slot.SlotID,
                productID = product.ProductID,
                quantity = slot.StoredItemCount
            };

            savedSlots.Add(slotSaveData);
        }

        return savedSlots;
    }

    private List<StorageSlotSaveData>
        BuildStorageSlotSaveData()
    {
        var savedSlots =
            new List<StorageSlotSaveData>();

        for (var i = 0;
             i < storageRegistry.RegisteredSlotCount;
             i++)
        {
            var bay =
                storageRegistry.GetRegisteredSlot(
                    i
                );

            if (bay == null ||
                !bay.IsAssigned)
                continue;

            var product =
                bay.AssignedProduct;

            if (product == null) continue;

            if (string.IsNullOrWhiteSpace(
                    bay.SlotID
                ))
                continue;

            if (string.IsNullOrWhiteSpace(
                    product.ProductID
                ))
                continue;

            var slotSaveData =
                new StorageSlotSaveData();

            slotSaveData.slotID =
                bay.SlotID;

            slotSaveData.productID =
                product.ProductID;

            slotSaveData.quantity =
                bay.Quantity;

            savedSlots.Add(
                slotSaveData
            );
        }

        return savedSlots;
    }

    private void RestoreStorageInventory(
        List<StorageSlotSaveData> savedSlots,
        int saveVersion
    )
    {
        ClearCurrentStorageInventory();

        if (savedSlots == null) return;

        for (var i = 0;
             i < savedSlots.Count;
             i++)
        {
            var slotSaveData =
                savedSlots[i];

            if (slotSaveData == null) continue;

            var bay =
                storageRegistry.FindSlotByID(
                    slotSaveData.slotID
                );

            if (bay == null)
            {
                Debug.LogWarning(
                    "Saved Storage Bay '"
                    + slotSaveData.slotID
                    + "' does not exist "
                    + "in the current scene."
                );

                continue;
            }

            var product =
                storeProgression
                    .FindCatalogProductByID(
                        slotSaveData.productID
                    );

            if (product == null)
            {
                Debug.LogWarning(
                    "Saved storage product ID '"
                    + slotSaveData.productID
                    + "' does not exist "
                    + "in the current catalog."
                );

                continue;
            }

            /*
             * Before Version 12, each storage slot
             * represented exactly one product.
             */
            var restoredQuantity =
                saveVersion >= 12
                    ? Mathf.Max(
                        0,
                        slotSaveData.quantity
                    )
                    : 1;

            bay.RestoreState(
                product,
                restoredQuantity
            );
        }
    }

    private void ClearCurrentStorageInventory()
    {
        for (var i = 0;
             i < storageRegistry.RegisteredSlotCount;
             i++)
        {
            var bay =
                storageRegistry.GetRegisteredSlot(
                    i
                );

            if (bay == null) continue;

            bay.ClearForLoad();
        }
    }

    private List<DeliveryBoxSaveData>
        BuildDeliverySaveData()
    {
        var savedDeliveries =
            new List<DeliveryBoxSaveData>();

        var allBoxes =
            FindObjectsByType<
                DeliveryBox
            >(
                FindObjectsSortMode.None
            );

        for (var i = 0;
             i < allBoxes.Length;
             i++)
        {
            var delivery =
                allBoxes[i];

            if (delivery == null) continue;

            var product =
                delivery.ProductData;

            if (product == null ||
                string.IsNullOrWhiteSpace(
                    product.ProductID
                ))
                continue;

            var deliverySaveData =
                new DeliveryBoxSaveData();

            /*
             * >= 0 = original DeliveryZone order
             * -1   = portable loose carton
             */
            deliverySaveData.slotIndex =
                deliveryZone.GetSlotIndex(
                    delivery
                );

            deliverySaveData.productID =
                product.ProductID;

            deliverySaveData.quantity =
                delivery.RemainingQuantity;

            deliverySaveData.capacity =
                delivery.Capacity;

            deliverySaveData.isOpen =
                delivery.IsOpen;

            deliverySaveData.hasWorldTransform =
                true;

            deliverySaveData.position =
                delivery.transform.position;

            deliverySaveData.rotation =
                delivery.transform.rotation;

            savedDeliveries.Add(
                deliverySaveData
            );
        }

        return savedDeliveries;
    }

    private List<LooseProductSaveData>
        BuildLooseProductSaveData()
    {
        List<LooseProductSaveData> savedProducts = new();

        for (var i = 0;
             i < worldItemRegistry.RegisteredItemCount;
             i++)
        {
            PickupItem item =
                worldItemRegistry.GetRegisteredItem(
                    i
                );

            if (item == null) continue;

            if (!item.IsLooseWorldItem) continue;

            var productData =
                item.GetProductData();

            if (productData == null) continue;

            if (string.IsNullOrWhiteSpace(
                    productData.ProductID
                ))
                continue;

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

        for (int i = 0; i < savedSlots.Count; i++)
        {
            ShelfSlotSaveData slotSaveData = savedSlots[i];

            if (slotSaveData == null)
            {
                continue;
            }

            ShelfSlot slot =
                shelfRegistry.FindSlotByID(slotSaveData.slotID);

            if (slot == null)
            {
                Debug.LogWarning(
                    $"Saved ShelfSlot '{slotSaveData.slotID}' does not exist in the current scene."
                );

                continue;
            }

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    slotSaveData.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Saved product ID '{slotSaveData.productID}' does not exist in the current catalog."
                );

                continue;
            }

            int quantity = Mathf.Max(0, slotSaveData.quantity);

            bool restored = slot.RestoreProduct(product, quantity);

            if (!restored)
            {
                Debug.LogWarning(
                    $"Could not restore product '{slotSaveData.productID}' into ShelfSlot '{slotSaveData.slotID}'."
                );
            }
        }
    }

    private void RestoreDeliveryZone(
        List<DeliveryBoxSaveData> savedDeliveries
    )
    {
        /*
         * Destroy loose cartons created from storage
         * before rebuilding the save state.
         */
        var existingBoxes =
            FindObjectsByType<
                DeliveryBox
            >(
                FindObjectsSortMode.None
            );

        for (var i = 0;
             i < existingBoxes.Length;
             i++)
        {
            var existingBox =
                existingBoxes[i];

            if (existingBox == null) continue;

            if (deliveryZone.GetSlotIndex(
                    existingBox
                ) >= 0)
                continue;

            Destroy(
                existingBox.gameObject
            );
        }

        /*
         * This clears the boxes owned by actual
         * delivery slots and resets their references.
         */
        deliveryZone
            .ClearAllDeliveriesForLoad();

        if (savedDeliveries == null) return;

        for (var i = 0;
             i < savedDeliveries.Count;
             i++)
        {
            var deliverySaveData =
                savedDeliveries[i];

            if (deliverySaveData == null) continue;

            var product =
                storeProgression
                    .FindCatalogProductByID(
                        deliverySaveData.productID
                    );

            if (product == null)
            {
                Debug.LogWarning(
                    "Saved carton product ID '"
                    + deliverySaveData.productID
                    + "' does not exist "
                    + "in the current catalog."
                );

                continue;
            }

            /*
             * Normal ordered delivery.
             */
            if (deliverySaveData.slotIndex >= 0)
            {
                var restored =
                    deliveryZone.TryRestoreDelivery(
                        deliveryBoxPrefab,
                        deliverySaveData.slotIndex,
                        product,
                        deliverySaveData.quantity,
                        deliverySaveData.capacity,
                        deliverySaveData.isOpen,
                        out var restoredDelivery
                    );

                if (restored &&
                    restoredDelivery != null &&
                    deliverySaveData.hasWorldTransform)
                    restoredDelivery.RestoreWorldTransform(
                        deliverySaveData.position,
                        deliverySaveData.rotation
                    );

                if (!restored)
                    Debug.LogWarning(
                        "Could not restore delivery "
                        + "in slot "
                        + deliverySaveData.slotIndex
                        + " for product '"
                        + deliverySaveData.productID
                        + "'."
                    );

                continue;
            }

            /*
             * Version 13 portable carton.
             *
             * It is intentionally NOT registered to a
             * delivery slot.
             */
            if (!deliverySaveData.hasWorldTransform) continue;

            var looseCarton =
                Instantiate(
                    deliveryBoxPrefab,
                    deliverySaveData.position,
                    deliverySaveData.rotation
                );

            looseCarton.RestoreState(
                product,
                deliverySaveData.quantity,
                deliverySaveData.capacity,
                deliverySaveData.isOpen
            );

            looseCarton.RestoreWorldTransform(
                deliverySaveData.position,
                deliverySaveData.rotation
            );
        }
    }

    private void RestoreLooseProducts(
        List<LooseProductSaveData> savedProducts
    )
    {
        worldItemRegistry
            .ClearLooseWorldItemsForLoad();

        if (savedProducts == null) return;

        for (var i = 0;
             i < savedProducts.Count;
             i++)
        {
            LooseProductSaveData productSaveData =
                savedProducts[i];

            if (productSaveData == null) continue;

            var productData =
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
                Debug.LogWarning(
                    "Could not restore loose product '"
                    + productSaveData.productID
                    + "'."
                );
        }
    }

    private void ClearCurrentShelfInventory()
    {
        for (int i = 0; i < shelfRegistry.RegisteredSlotCount; i++)
        {
            ShelfSlot slot = shelfRegistry.GetRegisteredSlot(i);

            if (slot != null)
            {
                slot.ClearForLoad();
            }
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

        storeProgression.RestoreUnlockedProducts(saveData.unlockedProductIDs);

        if (saveData.saveVersion >= 2)
        {
            RestoreShelfInventory(saveData.shelfSlots);
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
            RestoreDeliveryZone(saveData.deliveries);
        }
        else
        {
            deliveryZone.ClearAllDeliveriesForLoad();

            Debug.Log(
                "Older save loaded. Delivery boxes "
                + "were not restored because that "
                + "save predates delivery persistence."
            );
        }

        if (saveData.saveVersion >= 6)
        {
            RestoreLooseProducts(saveData.looseProducts);
        }
        else
        {
            worldItemRegistry.ClearLooseWorldItemsForLoad();

            Debug.Log(
                "Older save loaded. Loose products "
                + "were cleared because that save "
                + "predates loose-product persistence."
            );
        }

        if (saveData.saveVersion >= 5 && saveData.dailyStats != null)
        {
            dailyStats.RestoreFromSaveData(saveData.dailyStats);
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

        if (saveData.saveVersion >= 7 && saveData.goalRewards != null)
        {
            storeGoals.RestoreFromSaveData(saveData.goalRewards);
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

        if (saveData.saveVersion >= 8 && saveData.storeLevel != null)
        {
            storeLevelProgression.RestoreFromSaveData(saveData.storeLevel);
        }
        else
        {
            storeLevelProgression.RestoreFromSaveData(null);

            Debug.Log(
                "Older save loaded. Store Level "
                + "started at Level 1 because that "
                + "save predates Store Level progression."
            );
        }

        storeProgression.RefreshUnlocks();

        if (saveData.saveVersion >= 4)
        {
            storeClock.LoadState(
                saveData.day,
                saveData.currentMinutes
            );
        }
        else
        {
            storeClock.LoadDay(saveData.day);

            Debug.Log(
                "Older save loaded. Exact clock time "
                + "was not available, so the day "
                + "started at the configured start time."
            );
        }
        
        if (saveData.saveVersion >= 16)
        {
            supplierDeliveryQueue.RestoreFromSaveData(
                saveData.pendingDeliveries
            );
        }
        else
        {
            supplierDeliveryQueue.ClearPendingOrders();

            Debug.Log(
                "Older save loaded. There were no saved pending supplier deliveries."
            );
        }

        if (saveData.saveVersion >= 9)
        {
            RestoreStorageInventory(
                saveData.storageSlots,
                saveData.saveVersion
            );
        }
        else
        {
            ClearCurrentStorageInventory();

            Debug.Log(
                "Older save loaded. Back-room storage "
                + "was cleared because that save predates "
                + "storage persistence."
            );
        }
        
        
        if (saveData.saveVersion >= 17)
        {
            storeReportHistory.RestoreFromSaveData(saveData.dailyReports);
        }
        else
        {
            storeReportHistory.RestoreFromSaveData(null);

            Debug.Log(
                "Older save loaded. Historical day reports begin with version 17."
            );
        }

        customerFlow.ResetForNewDay();
    }

    private bool CanUseSaveSystemNow()
    {
        if (storeClock.HasDayEnded)
        {
            LastOperationMessage = "Start the next day before saving or loading.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        if (customerFlow.ActiveCustomerCount > 0)
        {
            LastOperationMessage = "Wait until all customers have left.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        var heldBasket = playerInteraction.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket.ItemCount > 0)
                LastOperationMessage =
                    "Empty and put down the restock "
                    + "basket before saving or loading.";
            else
                LastOperationMessage =
                    "Put down the restock basket "
                    + "before saving or loading.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        if (restockBasket.ItemCount > 0)
        {
            LastOperationMessage =
                "Empty the restock basket before "
                + "saving or loading.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        var heldDeliveryBox = playerInteraction.GetHeldDeliveryBox();

        if (heldDeliveryBox != null)
        {
            LastOperationMessage =
                "Put down the delivery box before "
                + "saving or loading.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        if (playerInteraction.GetHeldItem() != null)
        {
            LastOperationMessage = "Put away the item in your hands first.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        return true;
    }

    private string GetClockDebugText(float totalMinutes)
    {
        var wholeMinutes = Mathf.FloorToInt(totalMinutes);

        var hours = wholeMinutes / 60;

        var minutes = wholeMinutes % 60;

        return hours.ToString("00")
               + ":"
               + minutes.ToString("00");
    }

    private bool HasValidConfiguration()
    {
        if (storeClock == null) return false;

        if (storeEconomy == null) return false;

        if (storeReputation == null) return false;

        if (storeProgression == null) return false;

        if (dailyStats == null) return false;

        if (storeGoals == null) return false;

        if (storeLevelProgression == null) return false;

        if (customerFlow == null) return false;

        if (shelfRegistry == null) return false;

        if (deliveryZone == null) return false;
        
        if (supplierDeliveryQueue == null) return false;

        if (worldItemRegistry == null) return false;

        if (deliveryBoxPrefab == null) return false;

        if (playerInteraction == null) return false;

        if (storageRegistry == null) return false;

        if (restockBasket == null) return false;
        
        if (storeReportHistory == null) return false;

        return true;
    }
}