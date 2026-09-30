using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject pauseOverlay;

    [SerializeField]
    private Button resumeButton;

    [SerializeField]
    private Button saveGameButton;

    [SerializeField]
    private Button loadGameButton;

    [SerializeField]
    private TMP_Text saveStatusText;

    [Header("Systems")]
    [SerializeField]
    private SaveManager saveManager;

    [SerializeField]
    private OrderingUI orderingUI;

    [SerializeField]
    private EndOfDayUI endOfDayUI;

    [SerializeField]
    private InteractionUI interactionUI;

    [Header("Player")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    private bool isOpen;

    private static readonly Color32 SuccessColor =
        new Color32(
            111,
            207,
            151,
            255
        );

    private static readonly Color32 FailureColor =
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

    public bool IsOpen =>
        isOpen;

    private void Awake()
    {
        resumeButton.onClick.AddListener(
            Close
        );

        saveGameButton.onClick.AddListener(
            SaveGame
        );

        loadGameButton.onClick.AddListener(
            LoadGame
        );

        saveStatusText.text = "";

        pauseOverlay.SetActive(
            false
        );
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey
            .wasPressedThisFrame)
        {
            return;
        }

        if (endOfDayUI != null &&
            endOfDayUI.IsOpen)
        {
            return;
        }

        if (orderingUI != null &&
            orderingUI.IsOpen)
        {
            orderingUI.Close();

            return;
        }

        if (isOpen)
        {
            Close();

            return;
        }

        Open();
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        if (endOfDayUI != null &&
            endOfDayUI.IsOpen)
        {
            return;
        }

        isOpen =
            true;

        saveStatusText.text =
            "";

        saveStatusText.color =
            NeutralColor;

        RefreshLoadButton();

        pauseOverlay.SetActive(
            true
        );

        pauseOverlay.transform
            .SetAsLastSibling();

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

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

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen =
            false;

        pauseOverlay.SetActive(
            false
        );

        Time.timeScale =
            1f;

        playerController.enabled =
            true;

        playerInteraction.enabled =
            true;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    private void SaveGame()
    {
        if (saveManager == null)
        {
            ShowStatus(
                "Save system is not configured.",
                false
            );

            return;
        }

        bool succeeded =
            saveManager.SaveGame();

        ShowStatus(
            saveManager.LastOperationMessage,
            succeeded
        );

        RefreshLoadButton();
    }

    private void LoadGame()
    {
        if (saveManager == null)
        {
            ShowStatus(
                "Save system is not configured.",
                false
            );

            return;
        }

        bool succeeded =
            saveManager.LoadGame();

        ShowStatus(
            saveManager.LastOperationMessage,
            succeeded
        );

        RefreshLoadButton();
    }

    private void ShowStatus(
        string message,
        bool success
    )
    {
        saveStatusText.text =
            message;

        saveStatusText.color =
            success
            ? SuccessColor
            : FailureColor;
    }

    private void RefreshLoadButton()
    {
        if (saveManager == null)
        {
            loadGameButton.interactable =
                false;

            return;
        }

        loadGameButton.interactable =
            saveManager.HasSaveFile;
    }

    private void OnDestroy()
    {
        Time.timeScale =
            1f;
    }
}