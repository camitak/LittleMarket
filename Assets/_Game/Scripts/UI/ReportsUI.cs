
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReportsUI : MonoBehaviour
{
    [Header("Report UI")]
    [SerializeField] private TMP_Text reportSelectionText;
    [SerializeField] private TMP_Text reportFinanceText;
    [SerializeField] private TMP_Text reportProgressText;

    [SerializeField] private Button previousReportButton;
    [SerializeField] private Button nextReportButton;

    [Header("Store")]
    [SerializeField] private StoreReportHistory reportHistory;
    [SerializeField] private StoreProgression storeProgression;

    private int selectedReportIndex = -1;

    private void Awake()
    {
        if (previousReportButton != null)
            previousReportButton.onClick.AddListener(ShowPreviousReport);

        if (nextReportButton != null)
            nextReportButton.onClick.AddListener(ShowNextReport);
    }

    public void ShowLatestReport()
    {
        selectedReportIndex = reportHistory != null
            ? reportHistory.ReportCount - 1
            : -1;

        RefreshPage();
    }

    private void ShowPreviousReport()
    {
        if (selectedReportIndex <= 0)
            return;

        selectedReportIndex--;
        RefreshPage();
    }

    private void ShowNextReport()
    {
        if (reportHistory == null)
            return;

        if (selectedReportIndex >= reportHistory.ReportCount - 1)
            return;

        selectedReportIndex++;
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (reportSelectionText == null ||
            reportFinanceText == null ||
            reportProgressText == null)
        {
            return;
        }

        int reportCount = reportHistory != null
            ? reportHistory.ReportCount
            : 0;

        if (reportCount == 0)
        {
            selectedReportIndex = -1;

            reportSelectionText.text = "NO COMPLETED DAYS";
            reportFinanceText.text =
                "Complete a store day to see your first report here.";
            reportProgressText.text = "";

            UpdateNavigationButtons(reportCount);
            return;
        }

        selectedReportIndex = Mathf.Clamp(
            selectedReportIndex,
            0,
            reportCount - 1
        );

        DailyReportData report =
            reportHistory.GetReport(selectedReportIndex);

        if (report == null)
        {
            reportSelectionText.text = "REPORT UNAVAILABLE";
            reportFinanceText.text = "";
            reportProgressText.text = "";

            UpdateNavigationButtons(0);
            return;
        }

        reportSelectionText.text =
            $"DAY {report.day}  •  {selectedReportIndex + 1} / {reportCount}";

        UpdateFinanceText(report);
        UpdateProgressText(report);
        UpdateNavigationButtons(reportCount);
    }

    private void UpdateFinanceText(DailyReportData report)
    {
        float netCashFlow =
            report.revenue - report.stockSpending;

        string satisfaction = report.customersVisited > 0
            ? $"{report.averageSatisfaction:0}%"
            : "N/A";

        reportFinanceText.text =
            "<b>FINANCES</b>\n" +
            $"Revenue: £{report.revenue:0.00}\n" +
            $"Stock orders: £{report.stockSpending:0.00}\n" +
            $"Net cash flow: {FormatSignedMoney(netCashFlow)}\n" +
            $"Ending cash: £{report.endingCash:0.00}\n" +
            "\n" +
            "<b>CUSTOMERS</b>\n" +
            $"Visited: {report.customersVisited}\n" +
            $"Served: {report.customersServed}\n" +
            $"Items sold: {report.itemsSold}\n" +
            $"Left empty-handed: {report.customersLeftWithoutBuying}\n" +
            $"Products missed: {report.missedProducts}\n" +
            $"Average satisfaction: {satisfaction}";
    }

    private void UpdateProgressText(DailyReportData report)
    {
        string reputationChange = report.reputationChange
            .ToString("+0.0;-0.0;0.0");

        string allGoalsBonus = report.allGoalsBonusClaimed
            ? "Yes"
            : "No";

        string unlocks = BuildUnlockText(report);

        reportProgressText.text =
            "<b>GOALS</b>\n" +
            $"Completed: {report.goalsCompleted} / {report.goalsAvailable}\n" +
            $"Rewards: £{report.goalRewards:0.00}\n" +
            $"All-goals bonus: {allGoalsBonus}\n" +
            "\n" +
            "<b>STORE PROGRESSION</b>\n" +
            $"Reputation: {report.reputationAtEnd:0.0} / 100\n" +
            $"Reputation change: {reputationChange}\n" +
            $"Store level: {report.storeLevelAtEnd}\n" +
            $"Experience earned: +{report.experienceGained} XP\n" +
            "\n" +
            "<b>NEW PRODUCTS</b>\n" +
            unlocks;
    }

    private string BuildUnlockText(DailyReportData report)
    {
        if (report.newlyUnlockedProductIDs == null ||
            report.newlyUnlockedProductIDs.Count == 0)
        {
            return "None";
        }

        List<string> productNames = new();

        foreach (string productID in report.newlyUnlockedProductIDs)
        {
            ProductData product = storeProgression != null
                ? storeProgression.FindCatalogProductByID(productID)
                : null;

            string productName = product != null
                ? product.ProductName
                : productID;

            productNames.Add(productName);
        }

        return string.Join(", ", productNames);
    }

    private void UpdateNavigationButtons(int reportCount)
    {
        if (previousReportButton != null)
            previousReportButton.interactable =
                reportCount > 0 && selectedReportIndex > 0;

        if (nextReportButton != null)
            nextReportButton.interactable =
                reportCount > 0 && selectedReportIndex < reportCount - 1;
    }

    private static string FormatSignedMoney(float amount)
    {
        if (amount > 0f)
            return $"+£{amount:0.00}";

        if (amount < 0f)
            return $"-£{Mathf.Abs(amount):0.00}";

        return "£0.00";
    }
}
