using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class LayoutModeUI : MonoBehaviour
{
    [Header("Layout Mode")] [SerializeField]
    private GameObject frameRoot;

    [SerializeField] private GameObject normalCrosshair;
    [SerializeField] private GameObject clockPanel;

    [Header("Placement Status")] [SerializeField]
    private FurniturePlacementController placementController;

    [FormerlySerializedAs("gridSnapStatusText")] [SerializeField]
    private TMP_Text placementModeStatusText;

    private bool previousCrosshairState;
    private bool hasStoredCrosshairState;

    private bool previousClockPanelState;
    private bool hasStoredClockPanelState;

    private void Awake()
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (frameRoot == null || !frameRoot.activeSelf)
        {
            return;
        }

        RefreshPlacementModeStatus();
    }

    public void SetLayoutModeActive(bool active)
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(active);
        }

        if (active)
        {
            RefreshPlacementModeStatus();
        }

        SetClockPanelForLayout(active);
        SetCrosshairForLayout(active);
    }

    private void SetClockPanelForLayout(bool active)
    {
        if (clockPanel == null)
        {
            return;
        }

        if (active)
        {
            if (!hasStoredClockPanelState)
            {
                previousClockPanelState = clockPanel.activeSelf;

                hasStoredClockPanelState = true;
            }

            clockPanel.SetActive(false);
            return;
        }

        if (!hasStoredClockPanelState)
        {
            return;
        }

        clockPanel.SetActive(previousClockPanelState);

        hasStoredClockPanelState = false;
    }

    private void SetCrosshairForLayout(bool active)
    {
        if (normalCrosshair == null)
        {
            return;
        }

        if (active)
        {
            if (!hasStoredCrosshairState)
            {
                previousCrosshairState = normalCrosshair.activeSelf;

                hasStoredCrosshairState = true;
            }

            normalCrosshair.SetActive(false);
            return;
        }

        if (!hasStoredCrosshairState)
        {
            return;
        }

        normalCrosshair.SetActive(previousCrosshairState);

        hasStoredCrosshairState = false;
    }

    private void RefreshPlacementModeStatus()
    {
        if (placementModeStatusText == null)
        {
            return;
        }

        if (placementController == null)
        {
            placementModeStatusText.text = "";
            return;
        }

        string status = placementController.IsStructuredPlacementActive
            ? $"STRUCTURED  ·  GRID {placementController.GridSize:0.00} m"
            : "FREE  ·  PRECISION";

        if (placementController.IsPlacementModeTemporarilyInverted)
        {
            status += "  ·  CTRL TEMPORARY";
        }

        if (placementController.IsMagneticallySnapped)
        {
            status += "  ·  MAGNET";
        }

        placementModeStatusText.text = status;
    }
}