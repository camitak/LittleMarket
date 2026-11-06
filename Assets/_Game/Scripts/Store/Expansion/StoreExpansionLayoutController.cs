using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreExpansionLayoutController :
    MonoBehaviour
{
    [Header("Store Design")]
    [SerializeField]
    private StoreLayoutModeController layoutMode;

    [SerializeField]
    private LayoutCameraController
        layoutCameraController;

    [SerializeField]
    private FurniturePlacementController
        placementController;

    [SerializeField]
    private BuildCatalogUI buildCatalogUI;

    [SerializeField]
    private LayoutWorldVisualizer
        layoutWorldVisualizer;

    [Header("Expansion")]
    [SerializeField]
    private StoreExpansionSystem expansionSystem;

    [SerializeField]
    private StoreExpansionWorldVisualizer
        worldVisualizer;

    [Header("Details Panel")]
    [SerializeField]
    private GameObject detailsPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text requirementText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private Button purchaseButton;

    [SerializeField]
    private TMP_Text purchaseButtonText;

    [SerializeField]
    private Button closeButton;

    private StoreExpansionZone selectedZone;
    private StoreExpansionZone hoveredZone;

    private bool confirmationPending;

    public bool HasSelection =>
        selectedZone != null;

    private void Awake()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick
                .AddListener(
                    HandlePurchaseClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .AddListener(
                    ClearSelection
                );
        }

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(
                false
            );
        }
    }

    private void Update()
    {
        if (layoutMode == null ||
            !layoutMode.IsActive)
        {
            if (selectedZone != null ||
                (
                    detailsPanel != null &&
                    detailsPanel.activeSelf
                ))
            {
                ClearSelection();
            }
            else
            {
                worldVisualizer?
                    .SetInteractionState(
                        null,
                        null
                    );
            }

            return;
        }

        /*
         * Build Catalog and furniture placement
         * own interaction while active.
         */
        if (buildCatalogUI != null &&
            buildCatalogUI.IsOpen)
        {
            ClearSelection();
            return;
        }

        if (placementController != null &&
            placementController
                .IsPlacementActive)
        {
            ClearSelection();
            return;
        }

        if (Mouse.current == null ||
            layoutCameraController == null ||
            layoutCameraController
                .LayoutCamera == null ||
            worldVisualizer == null)
        {
            SetHoveredZone(
                null
            );

            return;
        }

        if (layoutMode.IsCameraManipulating ||
            IsPointerOverUI())
        {
            SetHoveredZone(
                null
            );

            return;
        }

        Vector2 pointerPosition =
            Mouse.current
                .position
                .ReadValue();

        Ray ray =
            layoutCameraController
                .LayoutCamera
                .ScreenPointToRay(
                    pointerPosition
                );

        worldVisualizer.TryGetZoneAtRay(
            ray,
            out StoreExpansionZone zone
        );

        SetHoveredZone(
            zone
        );

        if (!Mouse.current
                .leftButton
                .wasPressedThisFrame)
        {
            return;
        }

        if (zone != null)
        {
            SelectZone(
                zone
            );

            return;
        }

        if (selectedZone != null)
        {
            ClearSelection();
        }
    }

    public void ClearSelection()
    {
        selectedZone =
            null;

        hoveredZone =
            null;

        confirmationPending =
            false;

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(
                false
            );
        }

        worldVisualizer?
            .SetInteractionState(
                null,
                null
            );
    }

    private void SelectZone(
        StoreExpansionZone zone)
    {
        if (zone == null ||
            zone.IsUnlocked ||
            !zone.IsConfigured)
        {
            return;
        }

        bool changed =
            selectedZone != zone;

        selectedZone =
            zone;

        confirmationPending =
            false;

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(
                true
            );

            detailsPanel.transform
                .SetAsLastSibling();
        }

        RefreshPanel();

        worldVisualizer?
            .SetInteractionState(
                selectedZone,
                hoveredZone
            );

        if (changed &&
            layoutCameraController != null &&
            zone.TryGetPreviewBounds(
                out Bounds bounds))
        {
            /*
             * Keep the user's comfortable editing
             * distance rather than auto-fitting the
             * whole growing store.
             */
            layoutCameraController
                .FocusWorkingBounds(
                    bounds
                );
        }
    }

    private void SetHoveredZone(
        StoreExpansionZone zone)
    {
        if (hoveredZone == zone)
        {
            return;
        }

        hoveredZone =
            zone;

        worldVisualizer?
            .SetInteractionState(
                selectedZone,
                hoveredZone
            );
    }

    private void HandlePurchaseClicked()
    {
        if (selectedZone == null ||
            expansionSystem == null)
        {
            return;
        }

        if (selectedZone.IsUnlocked)
        {
            RefreshPanel();
            return;
        }

        if (!expansionSystem.CanPurchase(
                selectedZone,
                out string reason))
        {
            confirmationPending =
                false;

            RefreshPanel(
                reason
            );

            return;
        }

        StoreExpansionDefinition definition =
            selectedZone.Definition;

        if (!confirmationPending)
        {
            confirmationPending =
                true;

            RefreshPanel();
            return;
        }

        bool purchased =
            expansionSystem.TryPurchase(
                selectedZone,
                out reason
            );

        confirmationPending =
            false;

        if (purchased)
        {
            worldVisualizer?
                .RefreshVisuals();

            layoutWorldVisualizer?
                .RefreshVisuals();

            if (layoutCameraController != null &&
                selectedZone.TryGetPreviewBounds(
                    out Bounds bounds))
            {
                layoutCameraController
                    .FocusWorkingBounds(
                        bounds
                    );
            }
        }

        RefreshPanel(
            reason
        );
    }

    private void RefreshPanel(
        string statusOverride = null)
    {
        if (selectedZone == null ||
            selectedZone.Definition == null)
        {
            if (detailsPanel != null)
            {
                detailsPanel.SetActive(
                    false
                );
            }

            return;
        }

        StoreExpansionDefinition definition =
            selectedZone.Definition;

        if (titleText != null)
        {
            titleText.text =
                definition.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                definition.Description;
        }

        if (requirementText != null)
        {
            string areaLabel =
                selectedZone.BuildZone != null &&
                selectedZone
                    .BuildZone
                    .AreaType ==
                StoreAreaType.Backroom
                    ? "BACKROOM"
                    : "SALES FLOOR";

            requirementText.text =
                $"{areaLabel}  •  STORE LV {definition.RequiredStoreLevel}";
        }

        if (selectedZone.IsUnlocked)
        {
            if (priceText != null)
            {
                priceText.text =
                    "OWNED";
            }

            if (purchaseButtonText != null)
            {
                purchaseButtonText.text =
                    "OWNED";
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable =
                    false;
            }

            if (statusText != null)
            {
                statusText.text =
                    !string.IsNullOrWhiteSpace(
                        statusOverride
                    )
                        ? statusOverride
                        : "New floor space is ready to build.";
            }

            return;
        }

        if (priceText != null)
        {
            priceText.text =
                $"£{definition.PurchasePrice:0.00}";
        }

        string reason =
            "Store expansion is not configured.";

        bool canPurchase =
            expansionSystem != null &&
            expansionSystem.CanPurchase(
                selectedZone,
                out reason
            );

        if (!canPurchase)
        {
            confirmationPending =
                false;

            if (purchaseButton != null)
            {
                purchaseButton.interactable =
                    false;
            }

            if (purchaseButtonText != null)
            {
                bool levelLocked =
                    expansionSystem != null &&
                    !expansionSystem
                        .MeetsLevelRequirement(
                            selectedZone
                        );

                purchaseButtonText.text =
                    levelLocked
                        ? $"STORE LV {definition.RequiredStoreLevel} REQUIRED"
                        : expansionSystem == null
                            ? "UNAVAILABLE"
                            : "NOT ENOUGH MONEY";
            }

            if (statusText != null)
            {
                statusText.text =
                    !string.IsNullOrWhiteSpace(
                        statusOverride
                    )
                        ? statusOverride
                        : reason;
            }

            return;
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable =
                true;
        }

        if (purchaseButtonText != null)
        {
            purchaseButtonText.text =
                confirmationPending
                    ? $"CONFIRM £{definition.PurchasePrice:0.00}"
                    : "BUY EXPANSION";
        }

        if (statusText != null)
        {
            if (!string.IsNullOrWhiteSpace(
                    statusOverride))
            {
                statusText.text =
                    statusOverride;
            }
            else if (confirmationPending)
            {
                statusText.text =
                    $"Permanent purchase. Click CONFIRM to spend £{definition.PurchasePrice:0.00}.";
            }
            else
            {
                statusText.text =
                    "Permanent expansion. Purchase unlocks this floor immediately.";
            }
        }
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null ||
            Mouse.current == null)
        {
            return false;
        }

        return EventSystem.current
            .IsPointerOverGameObject(
                Mouse.current.deviceId
            );
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick
                .RemoveListener(
                    HandlePurchaseClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(
                    ClearSelection
                );
        }
    }
}