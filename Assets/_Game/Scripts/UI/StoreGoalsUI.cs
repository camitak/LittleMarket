using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StoreGoalsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private TMP_Text goalsTitleText;

    [SerializeField]
    private Transform goalsListContent;

    [SerializeField]
    private StoreGoalRow goalRowPrefab;

    [Header("Store")]
    [SerializeField]
    private StoreGoals storeGoals;

    private List<StoreGoalRow> spawnedRows =
        new List<StoreGoalRow>();

    private int displayedDay =
        -1;

    private DailyGoalSet displayedGoalSet;

    private void Start()
    {
        RebuildForCurrentDay();
    }

    private void Update()
    {
        if (storeGoals == null)
        {
            return;
        }

        int currentDay =
            storeGoals.CurrentDay;

        DailyGoalSet currentGoalSet =
            storeGoals.CurrentGoalSet;

        if (displayedDay != currentDay ||
            displayedGoalSet != currentGoalSet)
        {
            RebuildForCurrentDay();

            return;
        }

        RefreshRows();
    }

    private void RebuildForCurrentDay()
    {
        if (storeGoals == null)
        {
            return;
        }

        displayedDay =
            storeGoals.CurrentDay;

        displayedGoalSet =
            storeGoals.CurrentGoalSet;

        BuildGoalRows();

        RefreshTitle();

        RefreshRows();
    }

    private void BuildGoalRows()
    {
        ClearGoalRows();

        if (storeGoals == null)
        {
            return;
        }

        if (goalsListContent == null)
        {
            return;
        }

        if (goalRowPrefab == null)
        {
            return;
        }

        for (int i = 0;
             i < storeGoals.GoalCount;
             i++)
        {
            StoreGoalDefinition goal =
                storeGoals.GetGoal(
                    i
                );

            if (goal == null)
            {
                continue;
            }

            StoreGoalRow newRow =
                Instantiate(
                    goalRowPrefab,
                    goalsListContent
                );

            newRow.Configure(
                goal
            );

            spawnedRows.Add(
                newRow
            );
        }
    }

    private void RefreshRows()
    {
        if (storeGoals == null)
        {
            return;
        }

        for (int i = 0;
             i < spawnedRows.Count;
             i++)
        {
            StoreGoalRow row =
                spawnedRows[i];

            if (row == null)
            {
                continue;
            }

            row.Refresh(
                storeGoals
            );
        }
    }

    private void RefreshTitle()
    {
        if (goalsTitleText == null)
        {
            return;
        }

        goalsTitleText.text =
            "DAY "
            + displayedDay
            + " GOALS";
    }

    private void ClearGoalRows()
    {
        for (int i = 0;
             i < spawnedRows.Count;
             i++)
        {
            StoreGoalRow row =
                spawnedRows[i];

            if (row == null)
            {
                continue;
            }

            row.gameObject.SetActive(
                false
            );

            Destroy(
                row.gameObject
            );
        }

        spawnedRows.Clear();
    }
}