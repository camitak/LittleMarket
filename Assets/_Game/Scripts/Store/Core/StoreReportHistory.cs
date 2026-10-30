
using System.Collections.Generic;
using UnityEngine;

public class StoreReportHistory : MonoBehaviour
{
    [Header("Report Sources")]
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private StoreReputation storeReputation;
    [SerializeField] private StoreGoals storeGoals;
    [SerializeField] private StoreLevelProgression storeLevelProgression;
    [SerializeField] private StoreProgression storeProgression;

    private readonly List<DailyReportData> reports = new();

    public int ReportCount => reports.Count;

    public DailyReportData GetReport(int index)
    {
        if (index < 0 || index >= reports.Count)
            return null;

        return reports[index];
    }

    public void CaptureCompletedDay(int day)
    {
        if (day < 1 || !HasRequiredReferences())
        {
            Debug.LogError(
                "StoreReportHistory cannot capture the day. Check its references.",
                this
            );
            return;
        }

        foreach (DailyReportData existingReport in reports)
        {
            if (existingReport.day == day)
                return;
        }

        DailyReportData report = new()
        {
            day = day,

            revenue = dailyStats.Revenue,
            stockSpending = dailyStats.StockSpending,

            itemsSold = dailyStats.ItemsSold,
            customersServed = dailyStats.CustomersServed,
            customersVisited = dailyStats.CustomersVisited,
            customersLeftWithoutBuying = dailyStats.CustomersLeftWithoutBuying,
            missedProducts = dailyStats.MissedProducts,

            averageSatisfaction = dailyStats.AverageSatisfaction,

            reputationAtEnd = storeReputation.CurrentReputation,
            reputationChange = storeReputation.LastDailyChange,

            goalsCompleted = storeGoals.CompletedGoalCount,
            goalsAvailable = storeGoals.GoalCount,
            goalRewards = storeGoals.TotalRewardsPaidToday,
            allGoalsBonusClaimed = storeGoals.AllGoalsBonusClaimed,

            storeLevelAtEnd = storeLevelProgression.CurrentLevel,
            experienceGained = storeLevelProgression.LastExperienceGained,

            endingCash = storeEconomy.CurrentMoney
        };

        for (int i = 0; i < storeProgression.LastNewUnlockCount; i++)
        {
            ProductData product = storeProgression.GetLastNewUnlock(i);

            if (product == null || string.IsNullOrWhiteSpace(product.ProductID))
                continue;

            if (!report.newlyUnlockedProductIDs.Contains(product.ProductID))
                report.newlyUnlockedProductIDs.Add(product.ProductID);
        }

        reports.Add(report);
        reports.Sort((a, b) => a.day.CompareTo(b.day));

        Debug.Log(
            $"Little Market: Recorded Day {day} report. " +
            $"History contains {reports.Count} day(s)."
        );
    }

    public List<DailyReportData> CreateSaveData()
    {
        return new List<DailyReportData>(reports);
    }

    public void RestoreFromSaveData(List<DailyReportData> savedReports)
    {
        reports.Clear();

        if (savedReports != null)
        {
            foreach (DailyReportData report in savedReports)
            {
                if (report == null || report.day < 1)
                    continue;

                bool alreadyExists = reports.Exists(
                    existing => existing.day == report.day
                );

                if (alreadyExists)
                    continue;

                report.newlyUnlockedProductIDs ??= new List<string>();
                reports.Add(report);
            }

            reports.Sort((a, b) => a.day.CompareTo(b.day));
        }

        Debug.Log(
            $"Little Market: Restored {reports.Count} historical report(s)."
        );
    }

    private bool HasRequiredReferences()
    {
        return dailyStats != null &&
               storeEconomy != null &&
               storeReputation != null &&
               storeGoals != null &&
               storeLevelProgression != null &&
               storeProgression != null;
    }
}
