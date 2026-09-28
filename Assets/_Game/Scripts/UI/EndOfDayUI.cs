using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField]
    private GameObject endOfDayPanel;

    [Header("Summary Text")]
    [SerializeField]
    private TMP_Text dayCompleteText;

    [SerializeField]
    private TMP_Text revenueText;

    [SerializeField]
    private TMP_Text stockSpendingText;

    [SerializeField]
    private TMP_Text netCashFlowText;

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

    private static readonly Color32 PositiveColor =
        new Color32(
            111,
            207,
            151,
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

    private void Awake()
    {
        startNextDayButton.onClick.AddListener(
            StartNextDay
        );

        endOfDayPanel.SetActive(false);
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
        summaryShown = true;

        if (orderingUI != null &&
            orderingUI.IsOpen)
        {
            orderingUI.Close();
        }

        UpdateSummaryText();

        endOfDayPanel.SetActive(true);

        interactionUI.HidePrompt();

        playerController.enabled = false;
        playerInteraction.enabled = false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void UpdateSummaryText()
    {
        int day =
            storeClock.CurrentDay;

        dayCompleteText.text =
            "DAY "
            + day
            + " COMPLETE";

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

        string netPrefix =
            netCashFlow > 0f
            ? "+£"
            : netCashFlow < 0f
                ? "-£"
                : "£";

        netCashFlowText.text =
            "Net Cash Flow     "
            + netPrefix
            + Mathf.Abs(
                netCashFlow
            ).ToString("0.00");

        if (netCashFlow > 0f)
        {
            netCashFlowText.color =
                PositiveColor;
        }
        else if (netCashFlow < 0f)
        {
            netCashFlowText.color =
                NegativeColor;
        }
        else
        {
            netCashFlowText.color =
                NeutralColor;
        }

        itemsSoldText.text =
            "Items Sold     "
            + dailyStats.ItemsSold;

        customersServedText.text =
            "Customers Served     "
            + dailyStats.CustomersServed;

        endingCashText.text =
            "Ending Cash     £"
            + storeEconomy.CurrentMoney
                .ToString("0.00");

        startNextDayButton
            .GetComponentInChildren<TMP_Text>()
            .text =
            "START DAY "
            + (day + 1);
    }

    private void StartNextDay()
    {
        Time.timeScale = 1f;

        dailyStats.ResetForNewDay();

        storeClock.StartNextDay();

        if (customerFlow != null)
        {
            customerFlow.ResetForNewDay();
        }

        endOfDayPanel.SetActive(false);

        playerController.enabled = true;
        playerInteraction.enabled = true;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        summaryShown = false;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}