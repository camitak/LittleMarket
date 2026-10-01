using UnityEngine;

public class StoreGoals : MonoBehaviour
{
    [Header("Goal Set")]
    [SerializeField]
    private DailyGoalSet dailyGoalSet;

    [Header("Store References")]
    [SerializeField]
    private StoreClock storeClock;

    [SerializeField]
    private DailyStats dailyStats;

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
}