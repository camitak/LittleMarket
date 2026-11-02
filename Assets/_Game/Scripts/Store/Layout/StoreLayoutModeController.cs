using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class StoreLayoutModeController : MonoBehaviour
{
    [Header("Store")]
    [SerializeField] private StoreClock storeClock;
    [SerializeField] private CustomerFlow customerFlow;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Player")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private FurniturePlacementController furniturePlacementController;
    [SerializeField] private InteractionUI interactionUI;

    private bool isActive;
    private bool navigationDirty;

    public bool IsActive => isActive;

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.lKey.wasPressedThisFrame)
        {
            return;
        }

        if (Time.timeScale <= 0f ||
            Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        if (isActive)
        {
            ExitLayoutMode();
            return;
        }

        TryEnterLayoutMode(out _);
    }

    public bool TryEnterLayoutMode(out string reason)
    {
        if (isActive)
        {
            reason = "";
            return true;
        }

        if (!HasValidConfiguration())
        {
            reason = "Layout Mode is not configured.";
            return false;
        }

        if (Cursor.lockState != CursorLockMode.Locked ||
            Time.timeScale <= 0f)
        {
            reason = "Close other menus before entering Layout Mode.";
            return false;
        }

        if (playerInteraction.HasAnythingHeld)
        {
            reason =
                "Put down what you're carrying before entering Layout Mode.";

            interactionUI.ShowPrompt(reason);
            return false;
        }

        if (furniturePlacementController.IsPlacementActive)
        {
            reason =
                "Finish or cancel the current furniture placement first.";

            return false;
        }

        isActive = true;
        navigationDirty = false;

        storeClock.SetLayoutSuspended(true);
        customerFlow.SuspendForLayout();

        playerInteraction.enabled = false;

        interactionUI.ShowPrompt(
            "LAYOUT MODE  |  [G] Move furniture  |  [L] Exit"
        );

        reason = "";
        return true;
    }

    public void ExitLayoutMode()
    {
        if (!isActive)
            return;

        if (furniturePlacementController != null &&
            furniturePlacementController.IsPlacementActive)
        {
            furniturePlacementController.CancelActivePlacement();
        }

        if (navigationDirty)
        {
            Physics.SyncTransforms();
            navMeshSurface.BuildNavMesh();
        }

        customerFlow.ResumeFromLayout();
        storeClock.SetLayoutSuspended(false);

        isActive = false;
        navigationDirty = false;

        if (playerInteraction != null &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            playerInteraction.enabled = true;
        }

        if (interactionUI != null)
            interactionUI.HidePrompt();
    }

    public void MarkNavigationDirty()
    {
        if (isActive)
            navigationDirty = true;
    }

    private bool HasValidConfiguration()
    {
        return
            storeClock != null &&
            customerFlow != null &&
            navMeshSurface != null &&
            playerInteraction != null &&
            furniturePlacementController != null &&
            interactionUI != null;
    }
}