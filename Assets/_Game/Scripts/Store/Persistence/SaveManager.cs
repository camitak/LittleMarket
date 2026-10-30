using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "little_market_dev_v2_save.json";

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
    [SerializeField] private StoreFurnitureLayout storeFurnitureLayout;
    [SerializeField] private StoreExpansionSystem storeExpansionSystem;
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

    public bool HasSaveFile => File.Exists(SavePath);

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );

    public bool SaveGame()
    {
        if (!HasValidConfiguration())
        {
            LastOperationMessage = "Save system is not configured.";
            Debug.LogError(LastOperationMessage, this);

            return false;
        }

        if (!CanUseSaveSystemNow())
            return false;

        if (!storeFurnitureLayout.TryCreateSaveData(
                out List<FurniturePlacementSaveData> furniturePlacements))
        {
            LastOperationMessage =
                "Cannot save: check furniture identities and definitions.";

            Debug.LogError(LastOperationMessage, this);

            return false;
        }

        StoreSaveData saveData = new()
        {
            saveVersion = CurrentSaveVersion,
            day = storeClock.CurrentDay,
            currentMinutes = storeClock.CurrentMinutes,
            money = storeEconomy.CurrentMoney,
            reputation = storeReputation.CurrentReputation,
            unlockedProductIDs = storeProgression.GetUnlockedProductIDs(),
            unlockedExpansionIDs = storeExpansionSystem.GetUnlockedExpansionIDs(),
            shelfSlots = BuildShelfSlotSaveData(),
            furniturePlacements = furniturePlacements,
            storageSlots = BuildStorageSlotSaveData(),
            deliveries = BuildDeliverySaveData(),
            pendingDeliveries = supplierDeliveryQueue.CreateSaveData(),
            dailyStats = dailyStats.CreateSaveData(),
            looseProducts = BuildLooseProductSaveData(),
            goalRewards = storeGoals.CreateSaveData(),
            storeLevel = storeLevelProgression.CreateSaveData(),
            dailyReports = storeReportHistory.CreateSaveData()
        };

        string json =
            JsonUtility.ToJson(saveData, true);

        File.WriteAllText(SavePath, json);

        LastOperationMessage = "Game saved successfully.";

        Debug.Log(
            $"Little Market saved development schema {CurrentSaveVersion} " +
            $"at {GetClockDebugText(saveData.currentMinutes)}.\n{SavePath}"
        );

        return true;
    }

    public bool LoadGame()
    {
        if (!HasValidConfiguration())
        {
            LastOperationMessage = "Save system is not configured.";
            Debug.LogError(LastOperationMessage, this);

            return false;
        }

        if (!CanUseSaveSystemNow())
            return false;

        if (!File.Exists(SavePath))
        {
            LastOperationMessage = "No saved game exists yet.";
            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        string json = File.ReadAllText(SavePath);
        StoreSaveData saveData =
            JsonUtility.FromJson<StoreSaveData>(json);

        if (saveData == null)
        {
            LastOperationMessage =
                "The save file could not be read.";

            Debug.LogError(LastOperationMessage);

            return false;
        }

        if (saveData.saveVersion != CurrentSaveVersion)
        {
            LastOperationMessage =
                "This save belongs to a different development build. Start a new save for this build.";

            Debug.LogWarning(LastOperationMessage);

            return false;
        }

        if (!storeFurnitureLayout.CanRestoreSaveData(
                saveData.furniturePlacements))
        {
            LastOperationMessage =
                "Cannot load: saved furniture layout does not match the current store.";

            Debug.LogError(LastOperationMessage, this);

            return false;
        }

        ApplySaveData(saveData);

        LastOperationMessage = "Game loaded successfully.";

        Debug.Log(
            $"Little Market save loaded. Schema {saveData.saveVersion}. " +
            $"Time {GetClockDebugText(storeClock.CurrentMinutes)}."
        );

        return true;
    }

    private List<ShelfSlotSaveData> BuildShelfSlotSaveData()
    {
        List<ShelfSlotSaveData> savedSlots = new();

        for (int i = 0; i < shelfRegistry.RegisteredSlotCount; i++)
        {
            ShelfSlot slot = shelfRegistry.GetRegisteredSlot(i);

            if (slot == null || !slot.IsAssigned)
                continue;

            ProductData product = slot.AssignedProduct;

            if (product == null ||
                string.IsNullOrWhiteSpace(slot.SlotID) ||
                string.IsNullOrWhiteSpace(product.ProductID))
            {
                continue;
            }

            savedSlots.Add(new ShelfSlotSaveData
            {
                slotID = slot.SlotID,
                productID = product.ProductID,
                quantity = slot.StoredItemCount
            });
        }

        return savedSlots;
    }

    private List<StorageSlotSaveData> BuildStorageSlotSaveData()
    {
        List<StorageSlotSaveData> savedSlots = new();

        for (int i = 0; i < storageRegistry.RegisteredSlotCount; i++)
        {
            StorageBay bay =
                storageRegistry.GetRegisteredSlot(i);

            if (bay == null || !bay.IsAssigned)
                continue;

            ProductData product = bay.AssignedProduct;

            if (product == null ||
                string.IsNullOrWhiteSpace(bay.SlotID) ||
                string.IsNullOrWhiteSpace(product.ProductID))
            {
                continue;
            }

            savedSlots.Add(new StorageSlotSaveData
            {
                slotID = bay.SlotID,
                productID = product.ProductID,
                quantity = bay.Quantity
            });
        }

        return savedSlots;
    }

    private void RestoreStorageInventory(
        List<StorageSlotSaveData> savedSlots)
    {
        ClearCurrentStorageInventory();

        if (savedSlots == null)
            return;

        foreach (StorageSlotSaveData savedSlot in savedSlots)
        {
            if (savedSlot == null)
                continue;

            StorageBay bay =
                storageRegistry.FindSlotByID(savedSlot.slotID);

            if (bay == null)
            {
                Debug.LogWarning(
                    $"Saved StorageBay '{savedSlot.slotID}' does not exist."
                );

                continue;
            }

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    savedSlot.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Saved storage product '{savedSlot.productID}' does not exist."
                );

                continue;
            }

            bay.RestoreState(
                product,
                Mathf.Max(0, savedSlot.quantity)
            );
        }
    }

    private void ClearCurrentStorageInventory()
    {
        for (int i = 0; i < storageRegistry.RegisteredSlotCount; i++)
        {
            StorageBay bay =
                storageRegistry.GetRegisteredSlot(i);

            if (bay != null)
                bay.ClearForLoad();
        }
    }

    private List<DeliveryBoxSaveData> BuildDeliverySaveData()
    {
        List<DeliveryBoxSaveData> savedDeliveries = new();

        DeliveryBox[] boxes = FindObjectsByType<DeliveryBox>();

        foreach (DeliveryBox delivery in boxes)
        {
            if (delivery == null)
                continue;

            ProductData product = delivery.ProductData;

            if (product == null ||
                string.IsNullOrWhiteSpace(product.ProductID))
            {
                continue;
            }

            savedDeliveries.Add(new DeliveryBoxSaveData
            {
                slotIndex = deliveryZone.GetSlotIndex(delivery),
                productID = product.ProductID,
                quantity = delivery.RemainingQuantity,
                capacity = delivery.Capacity,
                isOpen = delivery.IsOpen,
                hasWorldTransform = true,
                position = delivery.transform.position,
                rotation = delivery.transform.rotation
            });
        }

        return savedDeliveries;
    }

    private List<LooseProductSaveData> BuildLooseProductSaveData()
    {
        List<LooseProductSaveData> savedProducts = new();

        for (int i = 0; i < worldItemRegistry.RegisteredItemCount; i++)
        {
            PickupItem item =
                worldItemRegistry.GetRegisteredItem(i);

            if (item == null || !item.IsLooseWorldItem)
                continue;

            ProductData product = item.GetProductData();

            if (product == null ||
                string.IsNullOrWhiteSpace(product.ProductID))
            {
                continue;
            }

            savedProducts.Add(new LooseProductSaveData
            {
                productID = product.ProductID,
                position = item.transform.position,
                rotation = item.transform.rotation
            });
        }

        return savedProducts;
    }

    private void RestoreShelfInventory(
        List<ShelfSlotSaveData> savedSlots)
    {
        ClearCurrentShelfInventory();

        if (savedSlots == null)
            return;

        foreach (ShelfSlotSaveData savedSlot in savedSlots)
        {
            if (savedSlot == null)
                continue;

            ShelfSlot slot =
                shelfRegistry.FindSlotByID(savedSlot.slotID);

            if (slot == null)
            {
                Debug.LogWarning(
                    $"Saved ShelfSlot '{savedSlot.slotID}' does not exist."
                );

                continue;
            }

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    savedSlot.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Saved product '{savedSlot.productID}' does not exist."
                );

                continue;
            }

            bool restored = slot.RestoreProduct(
                product,
                Mathf.Max(0, savedSlot.quantity)
            );

            if (!restored)
            {
                Debug.LogWarning(
                    $"Could not restore '{savedSlot.productID}' into '{savedSlot.slotID}'."
                );
            }
        }
    }

    private void RestoreDeliveryZone(
        List<DeliveryBoxSaveData> savedDeliveries)
    {
        DeliveryBox[] existingBoxes = FindObjectsByType<DeliveryBox>();

        foreach (DeliveryBox existingBox in existingBoxes)
        {
            if (existingBox == null)
                continue;

            if (deliveryZone.GetSlotIndex(existingBox) >= 0)
                continue;

            Destroy(existingBox.gameObject);
        }

        deliveryZone.ClearAllDeliveriesForLoad();

        if (savedDeliveries == null)
            return;

        foreach (DeliveryBoxSaveData savedDelivery in savedDeliveries)
        {
            if (savedDelivery == null)
                continue;

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    savedDelivery.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Saved carton product '{savedDelivery.productID}' does not exist."
                );

                continue;
            }

            if (savedDelivery.slotIndex >= 0)
            {
                bool restored = deliveryZone.TryRestoreDelivery(
                    deliveryBoxPrefab,
                    savedDelivery.slotIndex,
                    product,
                    savedDelivery.quantity,
                    savedDelivery.capacity,
                    savedDelivery.isOpen,
                    out DeliveryBox restoredDelivery
                );

                if (restored &&
                    restoredDelivery != null &&
                    savedDelivery.hasWorldTransform)
                {
                    restoredDelivery.RestoreWorldTransform(
                        savedDelivery.position,
                        savedDelivery.rotation
                    );
                }

                if (!restored)
                {
                    Debug.LogWarning(
                        $"Could not restore delivery slot {savedDelivery.slotIndex}."
                    );
                }

                continue;
            }

            if (!savedDelivery.hasWorldTransform)
                continue;

            DeliveryBox looseCarton = Instantiate(
                deliveryBoxPrefab,
                savedDelivery.position,
                savedDelivery.rotation
            );

            looseCarton.RestoreState(
                product,
                savedDelivery.quantity,
                savedDelivery.capacity,
                savedDelivery.isOpen
            );

            looseCarton.RestoreWorldTransform(
                savedDelivery.position,
                savedDelivery.rotation
            );
        }
    }

    private void RestoreLooseProducts(
        List<LooseProductSaveData> savedProducts)
    {
        worldItemRegistry.ClearLooseWorldItemsForLoad();

        if (savedProducts == null)
            return;

        foreach (LooseProductSaveData savedProduct in savedProducts)
        {
            if (savedProduct == null)
                continue;

            ProductData product =
                storeProgression.FindCatalogProductByID(
                    savedProduct.productID
                );

            if (product == null)
            {
                Debug.LogWarning(
                    $"Saved loose product '{savedProduct.productID}' does not exist."
                );

                continue;
            }

            bool restored =
                worldItemRegistry.RestoreLooseProduct(
                    product,
                    savedProduct.position,
                    savedProduct.rotation
                );

            if (!restored)
            {
                Debug.LogWarning(
                    $"Could not restore loose product '{savedProduct.productID}'."
                );
            }
        }
    }

    private void ClearCurrentShelfInventory()
    {
        for (int i = 0; i < shelfRegistry.RegisteredSlotCount; i++)
        {
            ShelfSlot slot =
                shelfRegistry.GetRegisteredSlot(i);

            if (slot != null)
                slot.ClearForLoad();
        }
    }

    private void ApplySaveData(StoreSaveData saveData)
    {
        storeEconomy.SetMoney(saveData.money);
        storeReputation.SetReputation(saveData.reputation);

        storeProgression.RestoreUnlockedProducts(
            saveData.unlockedProductIDs
        );

        storeExpansionSystem.RestoreUnlockedExpansionIDs(
            saveData.unlockedExpansionIDs
        );

        storeFurnitureLayout.RestoreLayout(
            saveData.furniturePlacements
        );

        RestoreShelfInventory(saveData.shelfSlots);
        RestoreDeliveryZone(saveData.deliveries);
        RestoreLooseProducts(saveData.looseProducts);

        dailyStats.RestoreFromSaveData(saveData.dailyStats);
        storeGoals.RestoreFromSaveData(saveData.goalRewards);
        storeLevelProgression.RestoreFromSaveData(saveData.storeLevel);

        storeProgression.RefreshUnlocks();

        storeClock.LoadState(
            saveData.day,
            saveData.currentMinutes
        );

        supplierDeliveryQueue.RestoreFromSaveData(
            saveData.pendingDeliveries
        );

        RestoreStorageInventory(saveData.storageSlots);

        storeReportHistory.RestoreFromSaveData(
            saveData.dailyReports
        );

        customerFlow.ResetForNewDay();
    }

    private bool CanUseSaveSystemNow()
    {
        FurniturePlacementController furniturePlacement =
            FindAnyObjectByType<FurniturePlacementController>();

        if (furniturePlacement != null &&
            furniturePlacement.IsPlacementActive)
        {
            LastOperationMessage =
                "Finish or cancel furniture placement before saving or loading.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        if (storeClock.HasDayEnded)
        {
            LastOperationMessage =
                "Start the next day before saving or loading.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        if (customerFlow.ActiveCustomerCount > 0)
        {
            LastOperationMessage =
                "Wait until all customers have left.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        RestockBasket heldBasket =
            playerInteraction.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            LastOperationMessage = heldBasket.ItemCount > 0
                ? "Empty and put down the restock basket before saving or loading."
                : "Put down the restock basket before saving or loading.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        if (restockBasket.ItemCount > 0)
        {
            LastOperationMessage =
                "Empty the restock basket before saving or loading.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        if (playerInteraction.GetHeldDeliveryBox() != null)
        {
            LastOperationMessage =
                "Put down the delivery box before saving or loading.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        if (playerInteraction.GetHeldItem() != null)
        {
            LastOperationMessage =
                "Put away the item in your hands first.";

            Debug.LogWarning(LastOperationMessage);
            return false;
        }

        return true;
    }

    private static string GetClockDebugText(float totalMinutes)
    {
        int wholeMinutes =
            Mathf.FloorToInt(totalMinutes);

        int hours = wholeMinutes / 60;
        int minutes = wholeMinutes % 60;

        return $"{hours:00}:{minutes:00}";
    }

    private bool HasValidConfiguration()
    {
        return storeClock != null &&
               storeEconomy != null &&
               storeReputation != null &&
               storeProgression != null &&
               dailyStats != null &&
               storeGoals != null &&
               storeLevelProgression != null &&
               customerFlow != null &&
               shelfRegistry != null &&
               storeFurnitureLayout != null &&
               storeExpansionSystem != null &&
               storageRegistry != null &&
               deliveryZone != null &&
               worldItemRegistry != null &&
               storeReportHistory != null &&
               deliveryBoxPrefab != null &&
               supplierDeliveryQueue != null &&
               playerInteraction != null &&
               restockBasket != null;
    }
}