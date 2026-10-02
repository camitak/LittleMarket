using System.Collections.Generic;
using UnityEngine;

public class StoreGoals : MonoBehaviour
{
    [Header("Goal Schedule")]
    [SerializeField]
    private DayGoalSchedule goalSchedule;

    [Header("Store References")]
    [SerializeField]
    private StoreClock storeClock;

    [SerializeField]
    private DailyStats dailyStats;

    [SerializeField]
    private StoreEconomy storeEconomy;

    private List<string> rewardedGoalIDs =
        new List<string>();

    private bool allGoalsBonusClaimed;

    private float totalRewardsPaidToday;

    public int CurrentDay
    {
        get
        {
            if (storeClock == null)
            {
                return 1;
            }

            return storeClock.CurrentDay;
        }
    }

    public DailyGoalSet CurrentGoalSet
    {
        get
        {
            if (goalSchedule == null)
            {
                return null;
            }

            return goalSchedule.GetGoalSetForDay(
                CurrentDay
            );
        }
    }

    public int GoalCount
    {
        get
        {
            DailyGoalSet goalSet =
                CurrentGoalSet;

            if (goalSet == null)
            {
                return 0;
            }

            return goalSet.GoalCount;
        }
    }

    public int CompletedGoalCount
    {
        get
        {
            int completedCount = 0;

            for (int i = 0;
                 i < GoalCount;
                 i++)
            {
                StoreGoalDefinition goal =
                    GetGoal(i);

                if (!IsGoalComplete(
                        goal
                    ))
                {
                    continue;
                }

                completedCount++;
            }

            return completedCount;
        }
    }

    public bool AllGoalsCompleted
    {
        get
        {
            if (GoalCount <= 0)
            {
                return false;
            }

            return CompletedGoalCount
                   == GoalCount;
        }
    }

    public float CurrentAllGoalsBonus
    {
        get
        {
            DailyGoalSet goalSet =
                CurrentGoalSet;

            if (goalSet == null)
            {
                return 0f;
            }

            return goalSet.AllGoalsBonus;
        }
    }

    public bool AllGoalsBonusClaimed =>
        allGoalsBonusClaimed;

    public float TotalRewardsPaidToday =>
        totalRewardsPaidToday;

    private void Start()
    {
        ValidateGoalSchedule();
    }

    public StoreGoalDefinition GetGoal(
        int index
    )
    {
        DailyGoalSet goalSet =
            CurrentGoalSet;

        if (goalSet == null)
        {
            return null;
        }

        return goalSet.GetGoal(
            index
        );
    }

    public float GetCurrentValue(
        StoreGoalDefinition goal
    )
    {
        if (goal == null)
        {
            return 0f;
        }

        if (dailyStats == null)
        {
            return 0f;
        }

        switch (goal.GoalType)
        {
            case StoreGoalType.Revenue:
                return dailyStats.Revenue;

            case StoreGoalType.CustomersServed:
                return dailyStats.CustomersServed;

            case StoreGoalType.ItemsSold:
                return dailyStats.ItemsSold;

            case StoreGoalType.AverageSatisfaction:
                return dailyStats.AverageSatisfaction;
        }

        return 0f;
    }

    public bool IsGoalComplete(
        StoreGoalDefinition goal
    )
    {
        if (goal == null)
        {
            return false;
        }

        if (dailyStats == null)
        {
            return false;
        }

        if (goal.GoalType
            == StoreGoalType.AverageSatisfaction
            &&
            dailyStats.CustomersVisited <= 0)
        {
            return false;
        }

        float currentValue =
            GetCurrentValue(
                goal
            );

        return currentValue
               >= goal.TargetValue;
    }

    public string GetProgressText(
        StoreGoalDefinition goal
    )
    {
        if (goal == null)
        {
            return "";
        }

        float currentValue =
            GetCurrentValue(
                goal
            );

        switch (goal.GoalType)
        {
            case StoreGoalType.Revenue:
                return "£"
                       + currentValue
                           .ToString("0.00")
                       + " / £"
                       + goal.TargetValue
                           .ToString("0.00");

            case StoreGoalType.CustomersServed:
            case StoreGoalType.ItemsSold:
                return Mathf.FloorToInt(
                           currentValue
                       )
                       + " / "
                       + Mathf.CeilToInt(
                           goal.TargetValue
                       );

            case StoreGoalType.AverageSatisfaction:
                if (dailyStats != null &&
                    dailyStats.CustomersVisited <= 0)
                {
                    return "N/A / "
                           + goal.TargetValue
                               .ToString("0")
                           + "%";
                }

                return currentValue
                           .ToString("0")
                       + "% / "
                       + goal.TargetValue
                           .ToString("0")
                       + "%";
        }

        return currentValue
                   .ToString("0")
               + " / "
               + goal.TargetValue
                   .ToString("0");
    }

    public bool IsGoalRewarded(
        StoreGoalDefinition goal
    )
    {
        if (goal == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                goal.GoalID
            ))
        {
            return false;
        }

        return rewardedGoalIDs.Contains(
            goal.GoalID
        );
    }

    public float ClaimEndOfDayRewards()
    {
        if (storeEconomy == null)
        {
            Debug.LogError(
                "StoreGoals cannot award rewards "
                + "because StoreEconomy is missing.",
                this
            );

            return 0f;
        }

        float awardedNow = 0f;

        for (int i = 0;
             i < GoalCount;
             i++)
        {
            StoreGoalDefinition goal =
                GetGoal(i);

            if (goal == null)
            {
                continue;
            }

            if (!IsGoalComplete(
                    goal
                ))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    goal.GoalID
                ))
            {
                Debug.LogWarning(
                    "Completed goal '"
                    + goal.GoalName
                    + "' has no stable Goal ID, "
                    + "so its reward was not paid.",
                    goal
                );

                continue;
            }

            if (rewardedGoalIDs.Contains(
                    goal.GoalID
                ))
            {
                continue;
            }

            float reward =
                Mathf.Max(
                    0f,
                    goal.MoneyReward
                );

            if (reward > 0f)
            {
                storeEconomy.AddMoney(
                    reward
                );
            }

            rewardedGoalIDs.Add(
                goal.GoalID
            );

            awardedNow +=
                reward;

            totalRewardsPaidToday +=
                reward;
        }

        if (AllGoalsCompleted &&
            !allGoalsBonusClaimed)
        {
            float bonus =
                Mathf.Max(
                    0f,
                    CurrentAllGoalsBonus
                );

            if (bonus > 0f)
            {
                storeEconomy.AddMoney(
                    bonus
                );
            }

            allGoalsBonusClaimed =
                true;

            awardedNow +=
                bonus;

            totalRewardsPaidToday +=
                bonus;
        }

        return awardedNow;
    }

    public StoreGoalsSaveData CreateSaveData()
    {
        StoreGoalsSaveData saveData =
            new StoreGoalsSaveData();

        saveData.rewardedGoalIDs =
            new List<string>(
                rewardedGoalIDs
            );

        saveData.allGoalsBonusClaimed =
            allGoalsBonusClaimed;

        saveData.totalRewardsPaidToday =
            totalRewardsPaidToday;

        return saveData;
    }

    public void RestoreFromSaveData(
        StoreGoalsSaveData saveData
    )
    {
        rewardedGoalIDs.Clear();

        allGoalsBonusClaimed =
            false;

        totalRewardsPaidToday =
            0f;

        if (saveData == null)
        {
            return;
        }

        if (saveData.rewardedGoalIDs != null)
        {
            for (int i = 0;
                 i < saveData.rewardedGoalIDs.Count;
                 i++)
            {
                string goalID =
                    saveData.rewardedGoalIDs[i];

                if (string.IsNullOrWhiteSpace(
                        goalID
                    ))
                {
                    continue;
                }

                if (rewardedGoalIDs.Contains(
                        goalID
                    ))
                {
                    continue;
                }

                rewardedGoalIDs.Add(
                    goalID
                );
            }
        }

        allGoalsBonusClaimed =
            saveData.allGoalsBonusClaimed;

        totalRewardsPaidToday =
            Mathf.Max(
                0f,
                saveData.totalRewardsPaidToday
            );
    }

    public void ResetForNewDay()
    {
        rewardedGoalIDs.Clear();

        allGoalsBonusClaimed =
            false;

        totalRewardsPaidToday =
            0f;
    }

    private void ValidateGoalSchedule()
    {
        if (goalSchedule == null)
        {
            Debug.LogError(
                "StoreGoals has no DayGoalSchedule.",
                this
            );

            return;
        }

        HashSet<int> seenStartDays =
            new HashSet<int>();

        bool hasDayOneEntry =
            false;

        for (int scheduleIndex = 0;
             scheduleIndex
             < goalSchedule.EntryCount;
             scheduleIndex++)
        {
            DayGoalScheduleEntry entry =
                goalSchedule.GetEntry(
                    scheduleIndex
                );

            if (entry == null)
            {
                continue;
            }

            if (entry.StartDay == 1)
            {
                hasDayOneEntry =
                    true;
            }

            if (!seenStartDays.Add(
                    entry.StartDay
                ))
            {
                Debug.LogError(
                    "Day Goal Schedule contains "
                    + "more than one entry starting "
                    + "on Day "
                    + entry.StartDay
                    + ".",
                    goalSchedule
                );
            }

            DailyGoalSet goalSet =
                entry.GoalSet;

            if (goalSet == null)
            {
                Debug.LogError(
                    "Day Goal Schedule entry for Day "
                    + entry.StartDay
                    + " has no Goal Set.",
                    goalSchedule
                );

                continue;
            }

            HashSet<string> goalIDsInSet =
                new HashSet<string>();

            for (int goalIndex = 0;
                 goalIndex < goalSet.GoalCount;
                 goalIndex++)
            {
                StoreGoalDefinition goal =
                    goalSet.GetGoal(
                        goalIndex
                    );

                if (goal == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        goal.GoalID
                    ))
                {
                    Debug.LogError(
                        "Goal '"
                        + goal.GoalName
                        + "' in the Day "
                        + entry.StartDay
                        + " goal set has no Goal ID.",
                        goal
                    );

                    continue;
                }

                if (!goalIDsInSet.Add(
                        goal.GoalID
                    ))
                {
                    Debug.LogError(
                        "Goal Set scheduled from Day "
                        + entry.StartDay
                        + " contains duplicate Goal ID '"
                        + goal.GoalID
                        + "'.",
                        goal
                    );
                }
            }
        }

        if (!hasDayOneEntry)
        {
            Debug.LogError(
                "Day Goal Schedule needs an entry "
                + "starting on Day 1.",
                goalSchedule
            );
        }
    }
}