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

    // Version 21.
    public List<string> unlockedExpansionIDs = new();

    public List<ShelfSlotSaveData> shelfSlots;

    // Version 18: position/rotation keyed by ShelfIdentity.PersistentID.
    public List<ShelfPlacementSaveData> shelfPlacements = new();

    public List<StorageSlotSaveData> storageSlots;

    public List<DeliveryBoxSaveData> deliveries;

    public List<PendingDeliverySaveData> pendingDeliveries = new();

    public DailyStatsSaveData dailyStats;

    public List<LooseProductSaveData> looseProducts;

    public StoreGoalsSaveData goalRewards;

    public StoreLevelSaveData storeLevel;

    public List<DailyReportData> dailyReports = new();
}