using System.Collections.Generic;
using UnityEngine;

public class StoreGoals : MonoBehaviour
{
    [Header("Goal Set")]
    [SerializeField]
    private DailyGoalSet dailyGoalSet;

    [Header("Rewards")]
    [Min(0f)]
    [SerializeField]
    private float allGoalsBonus = 5f;

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

    public int GoalCount
    {
        get
        {
            if (dailyGoalSet == null)
            {
                return 0;
            }

            return dailyGoalSet.GoalCount;
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

    public bool AllGoalsBonusClaimed =>
        allGoalsBonusClaimed;

    public float TotalRewardsPaidToday =>
        totalRewardsPaidToday;

    private void Start()
    {
        ValidateGoalDefinitions();
    }

    public StoreGoalDefinition GetGoal(
        int index
    )
    {
        if (dailyGoalSet == null)
        {
            return null;
        }

        return dailyGoalSet.GetGoal(
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
                    allGoalsBonus
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

    private void ValidateGoalDefinitions()
    {
        HashSet<string> seenGoalIDs =
            new HashSet<string>();

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

            if (string.IsNullOrWhiteSpace(
                    goal.GoalID
                ))
            {
                Debug.LogError(
                    "Goal '"
                    + goal.GoalName
                    + "' has no Goal ID.",
                    goal
                );

                continue;
            }

            if (!seenGoalIDs.Add(
                    goal.GoalID
                ))
            {
                Debug.LogError(
                    "Duplicate Goal ID '"
                    + goal.GoalID
                    + "'. Goal IDs must be unique.",
                    goal
                );
            }
        }
    }
}