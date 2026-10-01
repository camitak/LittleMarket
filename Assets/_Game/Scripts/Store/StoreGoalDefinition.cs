using UnityEngine;

[CreateAssetMenu(
    fileName = "NewStoreGoal",
    menuName = "Little Market/Store Goal"
)]
public class StoreGoalDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField]
    private string goalName = "New Goal";

    [TextArea]
    [SerializeField]
    private string description = "";

    [Header("Requirement")]
    [SerializeField]
    private StoreGoalType goalType;

    [Min(0f)]
    [SerializeField]
    private float targetValue = 1f;

    public string GoalName =>
        goalName;

    public string Description =>
        description;

    public StoreGoalType GoalType =>
        goalType;

    public float TargetValue =>
        targetValue;
}