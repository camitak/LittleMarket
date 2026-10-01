using System;
using System.Collections.Generic;

[Serializable]
public class StoreGoalsSaveData
{
    public List<string> rewardedGoalIDs = new List<string>();

    public bool allGoalsBonusClaimed;

    public float totalRewardsPaidToday;
}