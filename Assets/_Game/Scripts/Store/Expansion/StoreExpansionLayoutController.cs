using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StoreExpansionLayoutController : MonoBehaviour
{
    [Header("Store Design")]
    [SerializeField] private StoreLayoutModeController layoutMode;
    [SerializeField] private LayoutCameraController layoutCameraController;
    [SerializeField] private FurniturePlacementController placementController;
    [SerializeField] private BuildCatalogUI buildCatalogUI;
    [SerializeField] private LayoutWorldVisualizer layoutWorldVisualizer;

    [Header("Expansion")]
    [SerializeField] private StoreExpansionSystem expansionSystem;
    [SerializeField] private StoreExpansionWorldVisualizer worldVisualizer;

    [Header("Details Panel")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private CanvasGroup detailsCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;
    [SerializeField] private Button closeButton;

    private StoreExpansionZone selectedZone;
    private StoreExpansionZone hoveredZone;
    private bool confirmationPending;

    public bool HasSelection => selectedZone != null;

    private void Awake()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClearSelection);
        }

        HideDetailsPanel();
    }

    private void Update()
    {
        if (layoutMode == null || !layoutMode.IsActive)
        {
            ClearSelection();
            return;
        }

        if (buildCatalogUI != null && buildCatalogUI.IsOpen)
        {
            ClearSelection();
            return;
        }

        if (placementController != null && placementController.IsPlacementActive)
        {
            ClearSelection();
            return;
        }

        if (Mouse.current == null ||
            layoutCameraController == null ||
            layoutCameraController.LayoutCamera == null ||
            worldVisualizer == null)
        {
            ClearHoverPreview();
            return;
        }

        if (layoutMode.IsCameraManipulating)
        {
            ClearHoverPreview();
            return;
        }

        if (IsPointerOverUI())
        {
            ClearHoverPreview();
            return;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();

        Ray ray = layoutCameraController.LayoutCamera.ScreenPointToRay(pointerPosition);

        worldVisualizer.TryGetZoneAtRay(ray, out StoreExpansionZone zone);

        if (selectedZone == null)
        {
            SetHoveredZone(zone);
        }
        else
        {
            SetHoveredZone(null);
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (zone != null)
        {
            SelectZone(zone);
            return;
        }

        if (selectedZone != null)
        {
            ClearSelection();
        }
    }

    public void ClearSelection()
    {
        selectedZone = null;
        hoveredZone = null;
        confirmationPending = false;

        HideDetailsPanel();

        worldVisualizer?.SetInteractionState(null, null);
    }

    private void ClearHoverPreview()
    {
        if (selectedZone != null)
        {
            if (hoveredZone != null)
            {
                hoveredZone = null;
                worldVisualizer?.SetInteractionState(selectedZone, null);
            }

            return;
        }

        if (hoveredZone == null)
        {
            return;
        }

        hoveredZone = null;

        HideDetailsPanel();

        worldVisualizer?.SetInteractionState(null, null);
    }

    private void SetHoveredZone(StoreExpansionZone zone)
    {
        if (selectedZone != null)
        {
            if (hoveredZone != null)
            {
                hoveredZone = null;
                worldVisualizer?.SetInteractionState(selectedZone, null);
            }

            return;
        }

        if (hoveredZone == zone)
        {
            return;
        }

        hoveredZone = zone;

        if (hoveredZone == null)
        {
            HideDetailsPanel();
            worldVisualizer?.SetInteractionState(null, null);
            return;
        }

        ShowDetailsPanel(hoveredZone, true);

        worldVisualizer?.SetInteractionState(null, hoveredZone);
    }

    private void SelectZone(StoreExpansionZone zone)
    {
        if (zone == null || zone.IsUnlocked || !zone.IsConfigured)
        {
            return;
        }

        bool changed = selectedZone != zone;

        selectedZone = zone;
        hoveredZone = null;

        if (changed)
        {
            confirmationPending = false;
        }

        ShowDetailsPanel(selectedZone, false);

        worldVisualizer?.SetInteractionState(selectedZone, null);

        if (!changed || layoutCameraController == null)
        {
            return;
        }

        if (selectedZone.TryGetPreviewBounds(out Bounds bounds))
        {
            layoutCameraController.FocusWorkingBounds(bounds);
        }
    }

    private void HandlePurchaseClicked()
    {
        if (selectedZone == null || expansionSystem == null)
        {
            return;
        }

        if (selectedZone.IsUnlocked)
        {
            ShowDetailsPanel(selectedZone, false);
            return;
        }

        if (!expansionSystem.CanPurchase(selectedZone, out string reason))
        {
            confirmationPending = false;
            ShowDetailsPanel(selectedZone, false, reason);
            return;
        }

        StoreExpansionDefinition definition = selectedZone.Definition;

        if (!confirmationPending)
        {
            confirmationPending = true;
            ShowDetailsPanel(selectedZone, false);
            return;
        }

        bool purchased = expansionSystem.TryPurchase(selectedZone, out reason);

        confirmationPending = false;

        if (purchased)
        {
            worldVisualizer?.RefreshVisuals();
            layoutWorldVisualizer?.RefreshVisuals();

            if (layoutCameraController != null &&
                selectedZone.TryGetPreviewBounds(out Bounds bounds))
            {
                layoutCameraController.FocusWorkingBounds(bounds);
            }
        }

        ShowDetailsPanel(selectedZone, false, reason);
    }

    private void ShowDetailsPanel(StoreExpansionZone zone, bool previewMode, string statusOverride = null)
    {
        if (zone == null || zone.Definition == null)
        {
            HideDetailsPanel();
            return;
        }

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
            detailsPanel.transform.SetAsLastSibling();
        }

        ConfigurePanelInteraction(previewMode);
        RefreshPanel(zone, previewMode, statusOverride);
    }

    private void HideDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }

    private void ConfigurePanelInteraction(bool previewMode)
    {
        if (detailsCanvasGroup != null)
        {
            detailsCanvasGroup.alpha = 1f;
            detailsCanvasGroup.interactable = !previewMode;
            detailsCanvasGroup.blocksRaycasts = !previewMode;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(!previewMode);
        }
    }

    private void RefreshPanel(StoreExpansionZone zone, bool previewMode, string statusOverride = null)
    {
        StoreExpansionDefinition definition = zone.Definition;

        if (titleText != null)
        {
            titleText.text = definition.DisplayName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = definition.Description;
        }

        if (requirementText != null)
        {
            string areaLabel = zone.BuildZone != null &&
                               zone.BuildZone.AreaType == StoreAreaType.Backroom
                ? "BACKROOM"
                : "SALES FLOOR";

            requirementText.text =
                $"{areaLabel}  •  STORE LV {definition.RequiredStoreLevel}";
        }

        if (zone.IsUnlocked)
        {
            ShowOwnedState(statusOverride);
            return;
        }

        if (priceText != null)
        {
            priceText.text = $"£{definition.PurchasePrice:0.00}";
        }

        string reason = "Store expansion is not configured.";

        bool canPurchase = expansionSystem != null &&
                           expansionSystem.CanPurchase(zone, out reason);

        if (previewMode)
        {
            ShowPreviewState(zone, canPurchase, reason, statusOverride);
            return;
        }

        if (!canPurchase)
        {
            confirmationPending = false;

            if (purchaseButton != null)
            {
                purchaseButton.interactable = false;
            }

            if (purchaseButtonText != null)
            {
                purchaseButtonText.text = GetUnavailableButtonText(zone);
            }

            if (statusText != null)
            {
                statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                    ? statusOverride
                    : reason;
            }

            return;
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable = true;
        }

        if (purchaseButtonText != null)
        {
            purchaseButtonText.text = confirmationPending
                ? $"CONFIRM £{definition.PurchasePrice:0.00}"
                : "BUY EXPANSION";
        }

        if (statusText != null)
        {
            if (!string.IsNullOrWhiteSpace(statusOverride))
            {
                statusText.text = statusOverride;
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

    private void ShowPreviewState(
        StoreExpansionZone zone,
        bool canPurchase,
        string reason,
        string statusOverride)
    {
        if (purchaseButton != null)
        {
            purchaseButton.interactable = false;
        }

        if (purchaseButtonText != null)
        {
            purchaseButtonText.text = canPurchase
                ? "CLICK AREA TO SELECT"
                : GetUnavailableButtonText(zone);
        }

        if (statusText == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(statusOverride))
        {
            statusText.text = statusOverride;
            return;
        }

        statusText.text = canPurchase
            ? "Click this expansion to focus it and review the permanent purchase."
            : reason;
    }

    private void ShowOwnedState(string statusOverride)
    {
        if (priceText != null)
        {
            priceText.text = "OWNED";
        }

        if (purchaseButtonText != null)
        {
            purchaseButtonText.text = "OWNED";
        }

        if (purchaseButton != null)
        {
            purchaseButton.interactable = false;
        }

        if (statusText != null)
        {
            statusText.text = !string.IsNullOrWhiteSpace(statusOverride)
                ? statusOverride
                : "New floor space is ready to build.";
        }
    }

    private string GetUnavailableButtonText(StoreExpansionZone zone)
    {
        if (expansionSystem == null || zone == null || zone.Definition == null)
        {
            return "UNAVAILABLE";
        }

        if (!expansionSystem.MeetsLevelRequirement(zone))
        {
            return $"STORE LV {zone.Definition.RequiredStoreLevel} REQUIRED";
        }

        if (!expansionSystem.CanAfford(zone))
        {
            return "NOT ENOUGH MONEY";
        }

        return "UNAVAILABLE";
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Mouse.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClearSelection);
        }
    }
}