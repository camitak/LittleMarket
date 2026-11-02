using UnityEngine;

[DisallowMultipleComponent]
public class LayoutModeUI : MonoBehaviour
{
    [SerializeField] private GameObject frameRoot;

    private void Awake()
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(false);
        }
    }

    public void SetLayoutModeActive(
        bool active)
    {
        if (frameRoot != null)
        {
            frameRoot.SetActive(active);
        }
    }
}