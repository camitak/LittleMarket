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
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private FurniturePlacementController furniturePlacementController;

    [Header("Cameras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayoutCameraController layoutCameraController;

    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;
    [SerializeField] private LayoutModeUI layoutModeUI;

    private bool isActive;
    private bool navigationDirty;

    public bool IsActive =>
        isActive;

    public Camera PlacementCamera
    {
        get
        {
            if (isActive &&
                layoutCameraController != null &&
                layoutCameraController.LayoutCamera != null)
            {
                return layoutCameraController.LayoutCamera;
            }

            return playerCamera;
        }
    }

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

    public bool TryEnterLayoutMode(
        out string reason)
    {
        if (isActive)
        {
            reason = "";
            return true;
        }

        if (!HasValidConfiguration())
        {
            reason =
                "Layout Mode is not configured.";

            return false;
        }

        if (Cursor.lockState !=
                CursorLockMode.Locked ||
            Time.timeScale <= 0f)
        {
            reason =
                "Close other menus before entering Layout Mode.";

            return false;
        }

        if (playerInteraction.HasAnythingHeld)
        {
            reason =
                "Put down what you're carrying before entering Layout Mode.";

            interactionUI.ShowPrompt(reason);
            return false;
        }

        if (furniturePlacementController
            .IsPlacementActive)
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
        playerController.enabled = false;

        playerCamera.gameObject.SetActive(false);

        layoutCameraController.ActivateFrom(
            playerController.transform
        );

        layoutModeUI.SetLayoutModeActive(true);

        interactionUI.ShowPrompt(
            "LAYOUT MODE  |  WASD Move  •  Mouse Look  •  Wheel Zoom  •  [G] Furniture  •  [L] Exit"
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
            furniturePlacementController
                .CancelActivePlacement();
        }

        if (navigationDirty)
        {
            Physics.SyncTransforms();
            navMeshSurface.BuildNavMesh();
        }

        customerFlow.ResumeFromLayout();
        storeClock.SetLayoutSuspended(false);

        layoutModeUI.SetLayoutModeActive(false);

        layoutCameraController.Deactivate();
        playerCamera.gameObject.SetActive(true);

        isActive = false;
        navigationDirty = false;

        playerController.enabled = true;
        playerInteraction.enabled = true;

        playerController.CaptureCursor();

        interactionUI.HidePrompt();
    }

    public void MarkNavigationDirty()
    {
        if (isActive)
        {
            navigationDirty = true;
        }
    }

    private bool HasValidConfiguration()
    {
        return
            storeClock != null &&
            customerFlow != null &&
            navMeshSurface != null &&
            playerController != null &&
            playerInteraction != null &&
            furniturePlacementController != null &&
            playerCamera != null &&
            layoutCameraController != null &&
            interactionUI != null &&
            layoutModeUI != null;
    }
}