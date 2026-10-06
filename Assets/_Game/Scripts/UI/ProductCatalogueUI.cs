using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ProductCatalogueUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject catalogueOverlay;

    [SerializeField]
    private TMP_Text progressionSummaryText;

    [SerializeField]
    private Transform catalogueListContent;

    [SerializeField]
    private ProductCatalogueRow catalogueRowPrefab;

    [SerializeField]
    private Button closeButton;

    [Header("Progression")]
    [SerializeField]
    private StoreProgression storeProgression;

    [SerializeField]
    private StoreLevelProgression
        storeLevelProgression;

    [Header("Other UI")]
    [SerializeField]
    private OrderingUI orderingUI;

    [SerializeField]
    private PauseMenuUI pauseMenuUI;

    [SerializeField]
    private EndOfDayUI endOfDayUI;

    [SerializeField]
    private InteractionUI interactionUI;

    [Header("Player")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    private readonly List<ProductCatalogueRow>
        spawnedRows =
            new List<ProductCatalogueRow>();

    private bool isOpen;

    public bool IsOpen =>
        isOpen;

    private void Awake()
    {
        closeButton.onClick.AddListener(
            Close
        );

        catalogueOverlay.SetActive(
            false
        );
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.cKey
            .wasPressedThisFrame)
        {
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

        if (orderingUI != null &&
            orderingUI.IsOpen)
        {
            return;
        }

        if (pauseMenuUI != null &&
            pauseMenuUI.IsOpen)
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

        BuildRows();

        RefreshProgressionSummary();

        catalogueOverlay.SetActive(
            true
        );

        catalogueOverlay.transform
            .SetAsLastSibling();

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

        if (playerController != null)
        {
            playerController.enabled =
                false;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled =
                false;
        }

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

        catalogueOverlay.SetActive(
            false
        );

        Time.timeScale =
            1f;

        if (playerController != null)
        {
            playerController.enabled =
                true;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled =
                true;
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    private void BuildRows()
    {
        ClearRows();

        if (storeProgression == null)
        {
            return;
        }

        if (catalogueListContent == null)
        {
            return;
        }

        if (catalogueRowPrefab == null)
        {
            return;
        }

        for (int i = 0;
             i < storeProgression
                 .CatalogProductCount;
             i++)
        {
            ProductData product =
                storeProgression
                    .GetCatalogProduct(
                        i
                    );

            if (product == null)
            {
                continue;
            }

            ProductCatalogueRow newRow =
                Instantiate(
                    catalogueRowPrefab,
                    catalogueListContent
                );

            newRow.Configure(
                product,
                storeProgression
            );

            spawnedRows.Add(
                newRow
            );
        }
    }

    private void RefreshProgressionSummary()
    {
        if (progressionSummaryText == null)
        {
            return;
        }

        if (storeProgression == null ||
            storeLevelProgression == null)
        {
            progressionSummaryText.text =
                "Progression not configured";

            return;
        }

        progressionSummaryText.text =
            "REP "
            + storeProgression
                .CurrentReputation
                .ToString("0")
            + " / 100"
            + "   •   "
            + storeLevelProgression
                .GetProgressText();
    }

    private void ClearRows()
    {
        for (int i = 0;
             i < spawnedRows.Count;
             i++)
        {
            ProductCatalogueRow row =
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

    private void OnDestroy()
    {
        Time.timeScale =
            1f;
    }
}