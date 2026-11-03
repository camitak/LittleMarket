using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LayoutModeUI : MonoBehaviour
{
    [Header("Layout")] 
    [SerializeField] private GameObject frameRoot;
    [SerializeField] private GameObject normalCrosshair;

    [Header("Placement Status")] 
    [SerializeField] private FurniturePlacementController placementController;
    [SerializeField] private TMP_Text gridSnapStatusText;

    private bool previousCrosshairState;
    private bool hasStoredCrosshairState;

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

        RefreshGridSnapStatus();
    }

    public void SetLayoutModeActive(bool active)
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(active);
        }

        if (active)
        {
            RefreshGridSnapStatus();
        }

        if (normalCrosshair == null)
        {
            return;
        }

        if (active)
        {
            previousCrosshairState = normalCrosshair.activeSelf;

            hasStoredCrosshairState = true;

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

    private void RefreshGridSnapStatus()
    {
        if (gridSnapStatusText == null)
        {
            return;
        }

        if (placementController == null)
        {
            gridSnapStatusText.text = "";
            return;
        }

        if (placementController.GridSnapEnabled)
        {
            gridSnapStatusText.text = $"GRID  ON  ·  {placementController.GridSize:0.00} m  ·  [G]";

            return;
        }

        gridSnapStatusText.text = "GRID  OFF  ·  FREE  ·  [G]";
    }
}