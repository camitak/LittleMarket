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

    private int displayedDay = -1;

    private void Start()
    {
        BuildGoalRows();

        RefreshTitle();

        RefreshRows();
    }

    private void Update()
    {
        if (storeGoals == null)
        {
            return;
        }

        if (displayedDay
            != storeGoals.CurrentDay)
        {
            RefreshTitle();
        }

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
        if (storeGoals == null)
        {
            return;
        }

        displayedDay =
            storeGoals.CurrentDay;

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

            Destroy(
                row.gameObject
            );
        }

        spawnedRows.Clear();
    }
}