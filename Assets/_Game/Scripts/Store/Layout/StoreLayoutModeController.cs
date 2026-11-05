using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class StoreLayoutModeController : MonoBehaviour
{
    [Header("Store")]
    [SerializeField]
    private StoreClock storeClock;

    [SerializeField]
    private CustomerFlow customerFlow;

    [SerializeField]
    private NavMeshSurface navMeshSurface;

    [SerializeField]
    private StoreBuildArea buildArea;

    [Header("Player")]
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInteraction playerInteraction;

    [SerializeField]
    private FurniturePlacementController
        furniturePlacementController;

    [Header("Cameras")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private LayoutCameraController
        layoutCameraController;

    [Header("UI")]
    [SerializeField]
    private InteractionUI interactionUI;

    [SerializeField]
    private LayoutModeUI layoutModeUI;

    private bool isActive;
    private bool navigationDirty;

    public bool IsActive =>
        isActive;

    public bool IsCameraManipulating =>
        isActive &&
        layoutCameraController != null &&
        layoutCameraController.IsOrbiting;

    public Camera PlacementCamera
    {
        get
        {
            if (isActive &&
                layoutCameraController != null &&
                layoutCameraController.LayoutCamera != null)
            {
                return
                    layoutCameraController
                        .LayoutCamera;
            }

            return playerCamera;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current
                .lKey
                .wasPressedThisFrame)
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (isActive)
        {
            ExitLayoutMode();
            return;
        }

        if (Cursor.lockState !=
            CursorLockMode.Locked)
        {
            return;
        }

        TryEnterLayoutMode(
            out _
        );
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
                "Store Design Mode is not configured.";

            return false;
        }

        if (Cursor.lockState !=
                CursorLockMode.Locked ||
            Time.timeScale <= 0f)
        {
            reason =
                "Close other menus before entering Store Design Mode.";

            return false;
        }

        if (playerInteraction
            .HasAnythingHeld)
        {
            reason =
                "Put down what you're carrying before entering Store Design Mode.";

            interactionUI.ShowPrompt(
                reason
            );

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

        storeClock.SetLayoutSuspended(
            true
        );

        customerFlow.SuspendForLayout();

        playerInteraction.enabled = false;
        playerController.enabled = false;

        playerCamera.gameObject.SetActive(
            false
        );

        Vector3 playerPosition =
            playerController
                .transform
                .position;

        if (buildArea.TryGetWorkingZoneBounds(
                playerPosition,
                out Bounds workingBounds,
                out _))
        {
            layoutCameraController
                .ActivateForWorkingBounds(
                    workingBounds
                );
        }
        else
        {
            layoutCameraController
                .ActivateFrom(
                    playerController.transform
                );
        }

        playerController.ReleaseCursor();

        layoutModeUI.SetLayoutModeActive(
            true
        );

        interactionUI.HidePrompt();

        reason = "";

        return true;
    }

    public void ExitLayoutMode()
    {
        if (!isActive)
        {
            return;
        }

        if (furniturePlacementController != null &&
            furniturePlacementController
                .IsPlacementActive)
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

        storeClock.SetLayoutSuspended(
            false
        );

        layoutModeUI.SetLayoutModeActive(
            false
        );

        layoutCameraController.Deactivate();

        playerCamera.gameObject.SetActive(
            true
        );

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
    
    public bool FocusPlacementArea(
        StoreAreaType requiredArea)
    {
        if (!isActive ||
            buildArea == null ||
            layoutCameraController == null)
        {
            return false;
        }

        Vector3 cameraPivot =
            layoutCameraController
                .PivotPosition;

        /*
         * Already working in a valid zone for this
         * furniture type.
         *
         * Do not annoy the player by recentering the
         * camera every time they buy another item.
         */
        if (buildArea.IsPointInsideAvailableArea(
                cameraPivot,
                requiredArea))
        {
            return true;
        }

        /*
         * Find the nearest currently available zone
         * of the furniture's required area type.
         */
        if (!buildArea.TryGetWorkingZoneBounds(
                cameraPivot,
                requiredArea,
                out Bounds targetBounds))
        {
            return false;
        }

        layoutCameraController
            .FocusWorkingBoundsImmediate(
                targetBounds
            );

        return true;
    }

    private bool HasValidConfiguration()
    {
        return
            storeClock != null &&
            customerFlow != null &&
            navMeshSurface != null &&
            buildArea != null &&
            playerController != null &&
            playerInteraction != null &&
            furniturePlacementController != null &&
            playerCamera != null &&
            layoutCameraController != null &&
            interactionUI != null &&
            layoutModeUI != null;
    }
}