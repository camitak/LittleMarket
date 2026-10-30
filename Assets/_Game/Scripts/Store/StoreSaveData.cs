using System;
using System.Collections.Generic;

[Serializable]
public class StoreSaveData
{
    public int saveVersion;

    public int day = 1;
    public float currentMinutes = 480f;
    public float money = 100f;
    public float reputation = 50f;

    public List<string> unlockedProductIDs = new();
    public List<string> unlockedExpansionIDs = new();

    public List<ShelfSlotSaveData> shelfSlots = new();
    public List<FurniturePlacementSaveData> furniturePlacements = new();
    public List<StorageSlotSaveData> storageSlots = new();

    public List<DeliveryBoxSaveData> deliveries = new();
    public List<PendingDeliverySaveData> pendingDeliveries = new();

    public DailyStatsSaveData dailyStats;
    public List<LooseProductSaveData> looseProducts = new();
    public StoreGoalsSaveData goalRewards;
    public StoreLevelSaveData storeLevel;

    public List<DailyReportData> dailyReports = new();
}