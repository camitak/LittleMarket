
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ManagerBookUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject managerBookOverlay;
    [SerializeField] private GameObject todayPage;
    [SerializeField] private GameObject stockPage;
    [SerializeField] private GameObject cataloguePage;

    [SerializeField] private Button todayButton;
    [SerializeField] private Button stockButton;
    [SerializeField] private Button catalogueButton;
    [SerializeField] private Button closeButton;

    [Header("Other UI")]
    [SerializeField] private OrderingUI orderingUI;
    [SerializeField] private ProductCatalogueUI productCatalogueUI;
    [SerializeField] private PauseMenuUI pauseMenuUI;
    [SerializeField] private EndOfDayUI endOfDayUI;
    [SerializeField] private InteractionUI interactionUI;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (todayButton != null)
            todayButton.onClick.AddListener(ShowTodayPage);

        if (stockButton != null)
            stockButton.onClick.AddListener(ShowStockPage);

        if (catalogueButton != null)
            catalogueButton.onClick.AddListener(ShowCataloguePage);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        ShowTodayPage();

        if (managerBookOverlay != null)
            managerBookOverlay.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.mKey.wasPressedThisFrame)
            return;

        if (isOpen)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        if (isOpen || managerBookOverlay == null)
            return;

        if (orderingUI != null && orderingUI.IsOpen)
            return;

        if (pauseMenuUI != null && pauseMenuUI.IsOpen)
            return;

        if (endOfDayUI != null && endOfDayUI.IsOpen)
            return;

        isOpen = true;

        ShowTodayPage();

        managerBookOverlay.SetActive(true);
        managerBookOverlay.transform.SetAsLastSibling();

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

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (managerBookOverlay != null)
            managerBookOverlay.SetActive(false);

        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        if (playerInteraction != null)
            playerInteraction.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowTodayPage()
    {
        ShowPage(todayPage);
    }

    public void ShowStockPage()
    {
        ShowPage(stockPage);
    }

    public void ShowCataloguePage()
    {
        if (cataloguePage == null)
            return;

        ShowPage(cataloguePage);

        if (productCatalogueUI != null)
            productCatalogueUI.RefreshPage();
    }

    private void ShowPage(GameObject page)
    {
        if (todayPage != null)
            todayPage.SetActive(todayPage == page);

        if (stockPage != null)
            stockPage.SetActive(stockPage == page);

        if (cataloguePage != null)
            cataloguePage.SetActive(cataloguePage == page);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
