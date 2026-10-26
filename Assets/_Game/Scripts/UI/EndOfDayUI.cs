using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndOfDayUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject endOfDayPanel;

    [Header("Compact Summary")]
    [SerializeField] private TMP_Text dayCompleteText;
    [SerializeField] private TMP_Text highlightsText;
    [SerializeField] private TMP_Text achievementText;

    [Header("Buttons")]
    [SerializeField] private Button startNextDayButton;

    [Header("Store")]
    [SerializeField] private StoreDayLifecycle storeDayLifecycle;
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private StoreGoals storeGoals;
    [SerializeField] private StoreLevelProgression storeLevelProgression;
    [SerializeField] private StoreProgression storeProgression;

    [Header("Other UI")]
    [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private InteractionUI interactionUI;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    private bool summaryShown;

    public bool IsOpen => summaryShown;

    private void Awake()
    {
        if (startNextDayButton != null)
            startNextDayButton.onClick.AddListener(StartNextDay);

        if (endOfDayPanel != null)
            endOfDayPanel.SetActive(false);
    }

    private void Update()
    {
        if (summaryShown)
            return;

        if (storeDayLifecycle == null || !storeDayLifecycle.IsDayComplete)
            return;

        ShowSummary();
    }

    private void ShowSummary()
    {
        summaryShown = true;

        if (orderingUI != null && orderingUI.IsOpen)
            orderingUI.Close();

        UpdateSummaryText();

        endOfDayPanel.SetActive(true);
        endOfDayPanel.transform.SetAsLastSibling();

        if (interactionUI != null)
            interactionUI.HidePrompt();

        if (playerController != null)
            playerController.enabled = false;

        if (playerInteraction != null)
            playerInteraction.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void UpdateSummaryText()
    {
        int day = storeClock.CurrentDay;
        int served = dailyStats.CustomersServed;
        string customerWord = served == 1 ? "customer" : "customers";

        dayCompleteText.text = $"DAY {day} COMPLETE!";

        highlightsText.text =
            $"Sales £{dailyStats.Revenue:0.00}   •   " +
            $"{served} {customerWord} served";

        if (achievementText != null)
            achievementText.text = GetAchievementMessage();

        if (startNextDayButton != null)
        {
            TMP_Text buttonText =
                startNextDayButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = $"START DAY {day + 1}";
        }
    }

    private string GetAchievementMessage()
    {
        if (storeLevelProgression != null &&
            storeLevelProgression.LeveledUpLastProcessing)
        {
            return $"Store level {storeLevelProgression.CurrentLevel} reached!";
        }

        if (storeProgression != null &&
            storeProgression.LastNewUnlockCount > 0)
        {
            int count = storeProgression.LastNewUnlockCount;
            string productWord = count == 1 ? "product" : "products";
            return $"{count} new {productWord} unlocked!";
        }

        if (storeGoals != null && storeGoals.AllGoalsCompleted)
            return "All today's goals complete!";

        return "Tomorrow brings a fresh start.";
    }

    private void StartNextDay()
    {
        if (storeDayLifecycle == null || !storeDayLifecycle.StartNextDay())
            return;

        Time.timeScale = 1f;
        endOfDayPanel.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        if (playerInteraction != null)
            playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        summaryShown = false;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
