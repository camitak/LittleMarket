using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 5;

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
    private CustomerFlow customerFlow;

    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private DeliveryZone deliveryZone;

    [Header("Delivery")]
    [SerializeField]
    private DeliveryBox deliveryBoxPrefab;

    [Header("Player")]
    [SerializeField]
    private PlayerInteraction playerInteraction;

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "little_market_save.json"
        );

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f5Key
            .wasPressedThisFrame)
        {
            SaveGame();
        }

        if (Keyboard.current.f9Key
            .wasPressedThisFrame)
        {
            LoadGame();
        }
    }

    public void SaveGame()
    {
        if (!HasValidConfiguration())
        {
            Debug.LogError(
                "SaveManager is not fully configured.",
                this
            );

            return;
        }

        if (!CanUseSaveSystemNow())
        {
            return;
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

        string json =
            JsonUtility.ToJson(
                saveData,
                true
            );

        File.WriteAllText(
            SavePath,
            json
        );

        Debug.Log(
            "Little Market saved "
            + "(version "
            + CurrentSaveVersion
            + ") at "
            + GetClockDebugText(
                saveData.currentMinutes
            )
            + ". Revenue today: £"
            + saveData.dailyStats.revenue
                .ToString("0.00")
            + ".\n"
            + SavePath
        );
    }

    public void LoadGame()
    {
        if (!HasValidConfiguration())
        {
            Debug.LogError(
                "SaveManager is not fully configured.",
                this
            );

            return;
        }

        if (!CanUseSaveSystemNow())
        {
            return;
        }

        if (!File.Exists(
                SavePath
            ))
        {
            Debug.LogWarning(
                "No Little Market save file exists yet."
            );

            return;
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
            Debug.LogError(
                "Could not read Little Market save data."
            );

            return;
        }

        ApplySaveData(
            saveData
        );

        Debug.Log(
            "Little Market save loaded. "
            + "Save version: "
            + saveData.saveVersion
            + ". Current time: "
            + GetClockDebugText(
                storeClock.CurrentMinutes
            )
            + ". Revenue today: £"
            + dailyStats.Revenue
                .ToString("0.00")
            + "."
        );
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
            Debug.LogWarning(
                "Save/Load blocked after the day "
                + "has ended. Start the next day "
                + "before using the manual save system."
            );

            return false;
        }

        if (customerFlow.ActiveCustomerCount > 0)
        {
            Debug.LogWarning(
                "Save/Load blocked: wait until "
                + "all customers have left."
            );

            return false;
        }

        if (playerInteraction.GetHeldItem() != null)
        {
            Debug.LogWarning(
                "Save/Load blocked: put down or "
                + "stock the item in your hands first."
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