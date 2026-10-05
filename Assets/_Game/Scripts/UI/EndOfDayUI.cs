using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject endOfDayPanel;

    [Header("Financial Summary")]
    [SerializeField]
    private TMP_Text dayCompleteText;

    [SerializeField]
    private TMP_Text revenueText;

    [SerializeField]
    private TMP_Text stockSpendingText;

    [SerializeField]
    private TMP_Text netCashFlowText;

    [Header("Customer Summary")]
    [SerializeField]
    private TMP_Text averageSatisfactionText;

    [SerializeField]
    private TMP_Text reputationResultText;

    [SerializeField]
    private TMP_Text unlockText;

    [SerializeField]
    private TMP_Text goalResultsText;

    [SerializeField]
    private TMP_Text storeLevelResultText;

    [SerializeField]
    private TMP_Text missedProductsText;

    [SerializeField]
    private TMP_Text emptyHandedText;

    [SerializeField]
    private TMP_Text itemsSoldText;

    [SerializeField]
    private TMP_Text customersServedText;

    [SerializeField]
    private TMP_Text endingCashText;

    [Header("Buttons")]
    [SerializeField]
    private Button startNextDayButton;

    [Header("Store")]
    [SerializeField]
    private StoreClock storeClock;

    [SerializeField]
    private DailyStats dailyStats;

    [SerializeField]
    private StoreEconomy storeEconomy;

    [SerializeField]
    private StoreReputation storeReputation;

    [SerializeField]
    private StoreProgression storeProgression;

    [SerializeField]
    private StoreGoals storeGoals;

    [SerializeField]
    private StoreLevelProgression
        storeLevelProgression;

    [SerializeField]
    private CustomerFlow customerFlow;

    [Header("Other UI")]
    [SerializeField]
    private OrderingUI orderingUI;

    [SerializeField]
    private InteractionUI interactionUI;

    [Header("Player")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    private bool summaryShown;

    public bool IsOpen =>
        summaryShown;

    private static readonly Color32 PositiveColor =
        new Color32(
            111,
            207,
            151,
            255
        );

    private static readonly Color32 MediumColor =
        new Color32(
            246,
            215,
            122,
            255
        );

    private static readonly Color32 NegativeColor =
        new Color32(
            196,
            107,
            107,
            255
        );

    private static readonly Color32 NeutralColor =
        new Color32(
            122,
            104,
            96,
            255
        );

    private static readonly Color32 UnlockColor =
        new Color32(
            143,
            197,
            232,
            255
        );

    private void Awake()
    {
        startNextDayButton.onClick.AddListener(
            StartNextDay
        );

        endOfDayPanel.SetActive(
            false
        );
    }

    private void Update()
    {
        if (summaryShown)
        {
            return;
        }

        if (storeClock == null)
        {
            return;
        }

        if (!storeClock.HasDayEnded)
        {
            return;
        }

        if (customerFlow != null &&
            customerFlow.ActiveCustomerCount > 0)
        {
            return;
        }

        ShowSummary();
    }

    private void ShowSummary()
    {
        summaryShown =
            true;

        if (orderingUI != null &&
            orderingUI.IsOpen)
        {
            orderingUI.Close();
        }

        if (storeReputation != null &&
            dailyStats != null)
        {
            storeReputation.ApplyDailySatisfaction(
                dailyStats.AverageSatisfaction,
                dailyStats.CustomersVisited
            );
        }

        int completedGoals =
            0;

        bool allGoalsCompleted =
            false;

        if (storeGoals != null)
        {
            completedGoals =
                storeGoals.CompletedGoalCount;

            allGoalsCompleted =
                storeGoals.AllGoalsCompleted;

            storeGoals
                .ClaimEndOfDayRewards();
        }

        if (storeLevelProgression != null)
        {
            storeLevelProgression
                .ApplyEndOfDayProgression(
                    storeClock.CurrentDay,
                    completedGoals,
                    allGoalsCompleted
                );
        }

        if (storeProgression != null)
        {
            storeProgression.RefreshUnlocks();
        }

        UpdateSummaryText();

        endOfDayPanel.SetActive(
            true
        );

        interactionUI.HidePrompt();

        playerController.enabled =
            false;

        playerInteraction.enabled =
            false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;

        Time.timeScale =
            0f;
    }

    private void UpdateSummaryText()
    {
        int day =
            storeClock.CurrentDay;

        dayCompleteText.text =
            "DAY "
            + day
            + " COMPLETE";

        UpdateFinancialText();

        UpdateCustomerText();

        UpdateReputationText();

        UpdateUnlockText();

        UpdateGoalResultsText();

        UpdateStoreLevelText();

        endingCashText.text =
            "Ending Cash     £"
            + storeEconomy.CurrentMoney
                .ToString("0.00");

        TMP_Text buttonText =
            startNextDayButton
                .GetComponentInChildren<
                    TMP_Text
                >();

        if (buttonText != null)
        {
            buttonText.text =
                "START DAY "
                + (day + 1);
        }
    }

    private void UpdateFinancialText()
    {
        float revenue =
            dailyStats.Revenue;

        float stockSpending =
            dailyStats.StockSpending;

        float netCashFlow =
            dailyStats.NetCashFlow;

        revenueText.text =
            "Revenue     +£"
            + revenue.ToString("0.00");

        stockSpendingText.text =
            "Stock Orders     -£"
            + stockSpending.ToString("0.00");

        string netPrefix;

        if (netCashFlow > 0f)
        {
            netPrefix =
                "+£";

            netCashFlowText.color =
                PositiveColor;
        }
        else if (netCashFlow < 0f)
        {
            netPrefix =
                "-£";

            netCashFlowText.color =
                NegativeColor;
        }
        else
        {
            netPrefix =
                "£";

            netCashFlowText.color =
                NeutralColor;
        }

        netCashFlowText.text =
            "Net Cash Flow     "
            + netPrefix
            + Mathf.Abs(
                netCashFlow
            ).ToString("0.00");
    }

    private void UpdateCustomerText()
    {
        if (dailyStats.CustomersVisited <= 0)
        {
            averageSatisfactionText.text =
                "Average Satisfaction     N/A";

            averageSatisfactionText.color =
                NeutralColor;
        }
        else
        {
            float satisfaction =
                dailyStats.AverageSatisfaction;

            averageSatisfactionText.text =
                "Average Satisfaction     "
                + satisfaction.ToString("0")
                + "%";

            if (satisfaction >= 80f)
            {
                averageSatisfactionText.color =
                    PositiveColor;
            }
            else if (satisfaction >= 50f)
            {
                averageSatisfactionText.color =
                    MediumColor;
            }
            else
            {
                averageSatisfactionText.color =
                    NegativeColor;
            }
        }

        missedProductsText.text =
            "Products Missed     "
            + dailyStats.MissedProducts;

        emptyHandedText.text =
            "Left Empty-Handed     "
            + dailyStats
                .CustomersLeftWithoutBuying;

        itemsSoldText.text =
            "Items Sold     "
            + dailyStats.ItemsSold;

        customersServedText.text =
            "Customers Served     "
            + dailyStats.CustomersServed;
    }

    private void UpdateReputationText()
    {
        if (storeReputation == null)
        {
            reputationResultText.text =
                "Reputation     Not configured";

            reputationResultText.color =
                NeutralColor;

            return;
        }

        float reputation =
            storeReputation.CurrentReputation;

        float change =
            storeReputation.LastDailyChange;

        string changeText;

        if (change > 0f)
        {
            changeText =
                "+"
                + change.ToString("0.0");

            reputationResultText.color =
                PositiveColor;
        }
        else if (change < 0f)
        {
            changeText =
                change.ToString("0.0");

            reputationResultText.color =
                NegativeColor;
        }
        else
        {
            changeText =
                "0.0";

            reputationResultText.color =
                NeutralColor;
        }

        reputationResultText.text =
            "Reputation     "
            + reputation.ToString("0.0")
            + " / 100 ("
            + changeText
            + ")";
    }

    private void UpdateUnlockText()
    {
        if (storeProgression == null)
        {
            unlockText.text =
                "New Unlocks     Not configured";

            unlockText.color =
                NeutralColor;

            return;
        }

        if (storeProgression.LastNewUnlockCount <= 0)
        {
            unlockText.text =
                "New Unlocks     None";

            unlockText.color =
                UnlockColor;

            return;
        }

        string unlockedNames =
            "";

        for (int i = 0;
             i < storeProgression.LastNewUnlockCount;
             i++)
        {
            ProductData product =
                storeProgression
                    .GetLastNewUnlock(
                        i
                    );

            if (product == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(
                    unlockedNames
                ))
            {
                unlockedNames +=
                    ", ";
            }

            unlockedNames +=
                product.ProductName;
        }

        if (storeProgression.LastNewUnlockCount == 1)
        {
            unlockText.text =
                "NEW PRODUCT UNLOCKED     "
                + unlockedNames;
        }
        else
        {
            unlockText.text =
                "NEW PRODUCTS UNLOCKED     "
                + unlockedNames;
        }

        unlockText.color =
            PositiveColor;
    }

    private void UpdateGoalResultsText()
    {
        if (storeGoals == null)
        {
            goalResultsText.text =
                "Daily Goals     Not configured";

            goalResultsText.color =
                NeutralColor;

            return;
        }

        int completed =
            storeGoals.CompletedGoalCount;

        int total =
            storeGoals.GoalCount;

        string bonusSuffix =
            "";

        if (storeGoals.AllGoalsBonusClaimed)
        {
            bonusSuffix =
                "  •  ALL GOALS BONUS!";
        }

        goalResultsText.text =
            "Daily Goals     "
            + completed
            + " / "
            + total
            + "\nGoal Rewards     +£"
            + storeGoals
                .TotalRewardsPaidToday
                .ToString("0.00")
            + bonusSuffix;

        goalResultsText.color =
            storeGoals.AllGoalsCompleted
            ? PositiveColor
            : NeutralColor;
    }

    private void UpdateStoreLevelText()
    {
        if (storeLevelProgression == null)
        {
            storeLevelResultText.text =
                "Store Level     Not configured";

            storeLevelResultText.color =
                NeutralColor;

            return;
        }

        string levelUpSuffix =
            "";

        if (storeLevelProgression
            .LeveledUpLastProcessing)
        {
            levelUpSuffix =
                "  •  LEVEL UP!";
        }

        storeLevelResultText.text =
            "Store Level     "
            + storeLevelProgression
                .CurrentLevel
            + "  •  +"
            + storeLevelProgression
                .LastExperienceGained
            + " XP"
            + levelUpSuffix;

        storeLevelResultText.color =
            storeLevelProgression
                .LeveledUpLastProcessing
            ? PositiveColor
            : UnlockColor;
    }

    private void StartNextDay()
    {
        Time.timeScale =
            1f;

        dailyStats.ResetForNewDay();

        if (storeGoals != null)
        {
            storeGoals.ResetForNewDay();
        }

        storeClock.StartNextDay();

        if (customerFlow != null)
        {
            customerFlow.ResetForNewDay();
        }

        endOfDayPanel.SetActive(
            false
        );

        playerController.enabled =
            true;

        playerInteraction.enabled =
            true;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;

        summaryShown =
            false;
    }

    private void OnDestroy()
    {
        Time.timeScale =
            1f;
    }
}