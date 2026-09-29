using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 2;

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

        saveData.money =
            storeEconomy.CurrentMoney;

        saveData.reputation =
            storeReputation.CurrentReputation;

        saveData.unlockedProductIDs =
            storeProgression
                .GetUnlockedProductIDs();

        saveData.shelfSlots =
            BuildShelfSlotSaveData();

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
            + ").\n"
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
            Debug.Log(
                "Older progression save loaded. "
                + "Shelf inventory was not restored "
                + "because that save predates "
                + "inventory persistence."
            );
        }

        dailyStats.ResetForNewDay();

        storeClock.LoadDay(
            saveData.day
        );

        customerFlow.ResetForNewDay();
    }

    private bool CanUseSaveSystemNow()
    {
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

        if (deliveryZone.ActiveDeliveryCount > 0)
        {
            Debug.LogWarning(
                "Save/Load blocked: clear the "
                + "Delivery Zone first."
            );

            return false;
        }

        return true;
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

        if (playerInteraction == null)
        {
            return false;
        }

        return true;
    }
}