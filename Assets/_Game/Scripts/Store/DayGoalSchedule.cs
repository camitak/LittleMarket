using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDayGoalSchedule",
    menuName = "Little Market/Day Goal Schedule"
)]
public class DayGoalSchedule : ScriptableObject
{
    [Header("Schedule")]
    [SerializeField]
    private DayGoalScheduleEntry[] entries;

    public int EntryCount
    {
        get
        {
            if (entries == null)
            {
                return 0;
            }

            return entries.Length;
        }
    }

    public DayGoalScheduleEntry GetEntry(
        int index
    )
    {
        if (entries == null)
        {
            return null;
        }

        if (index < 0 ||
            index >= entries.Length)
        {
            return null;
        }

        return entries[index];
    }

    public DailyGoalSet GetGoalSetForDay(
        int day
    )
    {
        day =
            Mathf.Max(
                1,
                day
            );

        DailyGoalSet bestGoalSet =
            null;

        int bestStartDay =
            int.MinValue;

        for (int i = 0;
             i < EntryCount;
             i++)
        {
            DayGoalScheduleEntry entry =
                entries[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.GoalSet == null)
            {
                continue;
            }

            if (entry.StartDay > day)
            {
                continue;
            }

            if (entry.StartDay
                <= bestStartDay)
            {
                continue;
            }

            bestStartDay =
                entry.StartDay;

            bestGoalSet =
                entry.GoalSet;
        }

        return bestGoalSet;
    }
}