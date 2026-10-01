using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDailyGoalSet",
    menuName = "Little Market/Daily Goal Set"
)]
public class DailyGoalSet : ScriptableObject
{
    [Header("Goals")]
    [SerializeField]
    private StoreGoalDefinition[] goals;

    public int GoalCount
    {
        get
        {
            if (goals == null)
            {
                return 0;
            }

            return goals.Length;
        }
    }

    public StoreGoalDefinition GetGoal(
        int index
    )
    {
        if (goals == null)
        {
            return null;
        }

        if (index < 0 ||
            index >= goals.Length)
        {
            return null;
        }

        return goals[index];
    }
}