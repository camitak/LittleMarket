using System;
using UnityEngine;

[Serializable]
public class DayGoalScheduleEntry
{
    [Min(1)]
    [SerializeField]
    private int startDay = 1;

    [SerializeField]
    private DailyGoalSet goalSet;

    public int StartDay => startDay;

    public DailyGoalSet GoalSet => goalSet;
}