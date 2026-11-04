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
    [SerializeField] private Button saveGameButton;
    [SerializeField] private Button startNextDayButton;

    [Header("Save Feedback")] 
    [SerializeField] private TMP_Text saveStatusText;
    [SerializeField] private SaveManager saveManager;

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

    private static readonly Color32 SuccessColor = new(111, 207, 151, 255);

    private static readonly Color32 FailureColor = new(196, 107, 107, 255);

    private static readonly Color32 NeutralColor = new(122, 104, 96, 255);

    public bool IsOpen => summaryShown;

    private void Awake()
    {
        if (saveGameButton != null)
        {
            saveGameButton.onClick.AddListener(SaveGame);
        }

        if (startNextDayButton != null)
        {
            startNextDayButton.onClick.AddListener(StartNextDay);
        }

        if (saveStatusText != null)
        {
            saveStatusText.text = "";
            saveStatusText.color = NeutralColor;
        }

        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (summaryShown)
        {
            return;
        }

        if (storeDayLifecycle == null || !storeDayLifecycle.IsDayComplete)
        {
            return;
        }

        ShowSummary();
    }

    private void ShowSummary()
    {
        summaryShown = true;

        if (orderingUI != null && orderingUI.IsOpen)
        {
            orderingUI.Close();
        }

        UpdateSummaryText();

        if (saveStatusText != null)
        {
            saveStatusText.text = "";
            saveStatusText.color = NeutralColor;
        }

        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(true);
            endOfDayPanel.transform.SetAsLastSibling();
        }

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = false;
        }

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

        highlightsText.text = $"Sales £{dailyStats.Revenue:0.00}   •   " + $"{served} {customerWord} served";

        if (achievementText != null)
        {
            achievementText.text = GetAchievementMessage();
        }

        if (startNextDayButton != null)
        {
            TMP_Text buttonText = startNextDayButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = $"START DAY {day + 1}";
            }
        }
    }

    private string GetAchievementMessage()
    {
        if (storeLevelProgression != null && storeLevelProgression.LeveledUpLastProcessing)
        {
            return $"Store level {storeLevelProgression.CurrentLevel} reached!";
        }

        if (storeProgression != null && storeProgression.LastNewUnlockCount > 0)
        {
            int count = storeProgression.LastNewUnlockCount;

            string productWord = count == 1 ? "product" : "products";

            return $"{count} new {productWord} unlocked!";
        }

        if (storeGoals != null && storeGoals.AllGoalsCompleted)
        {
            return "All today's goals complete!";
        }

        return "Tomorrow brings a fresh start.";
    }

    private void SaveGame()
    {
        if (saveManager == null)
        {
            ShowSaveStatus("Save system is not configured.", false);

            return;
        }

        bool succeeded = saveManager.SaveGame();

        ShowSaveStatus(saveManager.LastOperationMessage, succeeded);
    }

    private void ShowSaveStatus(string message, bool success)
    {
        if (saveStatusText == null)
        {
            return;
        }

        saveStatusText.text = message;

        saveStatusText.color = success ? SuccessColor : FailureColor;
    }

    private void StartNextDay()
    {
        if (storeDayLifecycle == null || !storeDayLifecycle.StartNextDay())
        {
            return;
        }

        Time.timeScale = 1f;

        if (endOfDayPanel != null)
        {
            endOfDayPanel.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = false;

        summaryShown = false;
    }

    private void OnDestroy()
    {
        if (saveGameButton != null)
        {
            saveGameButton.onClick.RemoveListener(SaveGame);
        }

        if (startNextDayButton != null)
        {
            startNextDayButton.onClick.RemoveListener(StartNextDay);
        }

        Time.timeScale = 1f;
    }
}