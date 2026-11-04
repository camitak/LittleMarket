using UnityEngine;

public class StoreDayLifecycle : MonoBehaviour
{
    [Header("Store References")] 
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private StoreReputation storeReputation;
    [SerializeField] private StoreGoals storeGoals;
    [SerializeField] private StoreLevelProgression storeLevelProgression;
    [SerializeField] private StoreProgression storeProgression;
    [SerializeField] private StoreReportHistory storeReportHistory;

    private int completedDay = -1;

    public bool IsDayComplete => storeClock != null && storeClock.HasDayEnded && completedDay == storeClock.CurrentDay;

    private void Update()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (!storeClock.HasDayEnded)
        {
            completedDay = -1;
            return;
        }

        if (IsDayComplete)
        {
            return;
        }

        /*
         * A completed daily report is our durable proof
         * that this day's rewards/progression were already
         * processed before the game was saved.
         *
         * This is especially important when loading a save
         * made from the end-of-day summary.
         */
        if (storeReportHistory.HasReportForDay(storeClock.CurrentDay))
        {
            completedDay = storeClock.CurrentDay;

            return;
        }

        /*
         * During normal play we still allow customers who
         * entered before closing to finish their visit.
         *
         * This is not a management restriction. It only
         * determines when the day's results are finalized.
         */
        if (customerFlow.ActiveCustomerCount > 0)
        {
            return;
        }

        CompleteDay();
    }

    private void CompleteDay()
    {
        completedDay = storeClock.CurrentDay;

        int completedGoals = storeGoals.CompletedGoalCount;

        bool allGoalsCompleted = storeGoals.AllGoalsCompleted;

        storeReputation.ApplyDailySatisfaction(dailyStats.AverageSatisfaction, dailyStats.CustomersVisited);

        storeGoals.ClaimEndOfDayRewards();

        storeLevelProgression.ApplyEndOfDayProgression(completedDay, completedGoals, allGoalsCompleted);

        storeProgression.RefreshUnlocks();

        /*
         * Capture the report last.
         *
         * Its existence means all permanent effects above
         * have already been applied successfully.
         */
        storeReportHistory.CaptureCompletedDay(completedDay);

        Debug.Log($"Little Market: Day {completedDay} completed.");
    }

    public bool StartNextDay()
    {
        if (!IsDayComplete || !HasRequiredReferences())
        {
            return false;
        }

        dailyStats.ResetForNewDay();
        storeGoals.ResetForNewDay();

        storeClock.StartNextDay();
        customerFlow.ResetForNewDay();

        completedDay = -1;

        return true;
    }

    private bool HasRequiredReferences()
    {
        return storeClock != null && customerFlow != null && dailyStats != null && storeReputation != null &&
               storeGoals != null && storeLevelProgression != null && storeProgression != null &&
               storeReportHistory != null;
    }
}