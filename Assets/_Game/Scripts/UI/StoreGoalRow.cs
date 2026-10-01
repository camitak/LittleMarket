using TMPro;
using UnityEngine;

public class StoreGoalRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text goalNameText;

    [SerializeField]
    private TMP_Text progressText;

    private StoreGoalDefinition goal;

    private static readonly Color32 NormalNameColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 NormalProgressColor =
        new Color32(
            89,
            70,
            64,
            255
        );

    private static readonly Color32 CompletedColor =
        new Color32(
            111,
            207,
            151,
            255
        );

    public void Configure(
        StoreGoalDefinition newGoal
    )
    {
        goal =
            newGoal;

        if (goal == null)
        {
            goalNameText.text =
                "Goal not configured";

            progressText.text =
                "";

            return;
        }

        goalNameText.text =
            goal.GoalName;
    }

    public void Refresh(
        StoreGoals storeGoals
    )
    {
        if (goal == null)
        {
            return;
        }

        if (storeGoals == null)
        {
            return;
        }

        progressText.text =
            storeGoals.GetProgressText(
                goal
            );

        bool completed =
            storeGoals.IsGoalComplete(
                goal
            );

        if (completed)
        {
            goalNameText.color =
                CompletedColor;

            progressText.color =
                CompletedColor;

            return;
        }

        goalNameText.color =
            NormalNameColor;

        progressText.color =
            NormalProgressColor;
    }
}