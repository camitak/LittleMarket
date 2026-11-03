using UnityEngine;

[DisallowMultipleComponent]
public class LayoutModeUI : MonoBehaviour
{
    [SerializeField] private GameObject frameRoot;
    [SerializeField] private GameObject normalCrosshair;

    private bool previousCrosshairState;
    private bool hasStoredCrosshairState;

    private void Awake()
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(false);
        }
    }

    public void SetLayoutModeActive(bool active)
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(active);
        }

        if (normalCrosshair == null)
            return;

        if (active)
        {
            previousCrosshairState = normalCrosshair.activeSelf;

            hasStoredCrosshairState = true;

            normalCrosshair.SetActive(false);

            return;
        }

        if (!hasStoredCrosshairState)
            return;

        normalCrosshair.SetActive(previousCrosshairState);

        hasStoredCrosshairState = false;
    }
}