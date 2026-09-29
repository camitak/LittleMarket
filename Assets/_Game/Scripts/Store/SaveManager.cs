using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
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

        StoreSaveData saveData =
            new StoreSaveData();

        saveData.day =
            storeClock.CurrentDay;

        saveData.money =
            storeEconomy.CurrentMoney;

        saveData.reputation =
            storeReputation.CurrentReputation;

        saveData.unlockedProductIDs =
            storeProgression
                .GetUnlockedProductIDs();

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
            "Little Market saved.\n"
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

        if (customerFlow.ActiveCustomerCount > 0)
        {
            Debug.LogWarning(
                "Progression load blocked: "
                + "wait until no customers are "
                + "inside the store."
            );

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
            "Little Market progression loaded."
        );
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

        dailyStats.ResetForNewDay();

        storeClock.LoadDay(
            saveData.day
        );

        customerFlow.ResetForNewDay();
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

        return true;
    }
}