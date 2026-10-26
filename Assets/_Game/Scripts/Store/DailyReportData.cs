
using System;
using System.Collections.Generic;

[Serializable]
public class DailyReportData
{
    public int day;

    public float revenue;
    public float stockSpending;

    public int itemsSold;
    public int customersServed;
    public int customersVisited;
    public int customersLeftWithoutBuying;
    public int missedProducts;

    public float averageSatisfaction;

    public float reputationAtEnd;
    public float reputationChange;

    public int goalsCompleted;
    public int goalsAvailable;
    public float goalRewards;
    public bool allGoalsBonusClaimed;

    public int storeLevelAtEnd;
    public int experienceGained;

    public float endingCash;

    public List<string> newlyUnlockedProductIDs = new();
}
