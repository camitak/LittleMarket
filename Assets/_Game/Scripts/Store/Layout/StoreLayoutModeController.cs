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
    [SerializeField] private StoreBuildArea buildArea;

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
    [SerializeField] private BuildCatalogUI buildCatalogUI;
    [SerializeField] private StoreExpansionLayoutController expansionLayoutController;
    [SerializeField] private StoreDesignFurnitureContextMenuUI furnitureContextMenuUI;

    private bool isActive;
    private bool navigationDirty;

    /*
     * Prevent the same Escape press from exiting
     * Store Design and opening Pause on the same
     * frame.
     */
    private int escapeConsumedFrame = -1;

    public bool IsActive => isActive;

    public bool ConsumedEscapeThisFrame => escapeConsumedFrame == Time.frameCount;

    public bool IsCameraManipulating => isActive && layoutCameraController != null && layoutCameraController.IsOrbiting;

    public Camera PlacementCamera
    {
        get
        {
            if (isActive && layoutCameraController != null && layoutCameraController.LayoutCamera != null)
            {
                return layoutCameraController.LayoutCamera;
            }

            return playerCamera;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || Time.timeScale <= 0f)
        {
            return;
        }

        /*
         * Store Design owns Escape while active.
         *
         * This is intentionally centralized here
         * rather than spread across placement,
         * catalog and pause components.
         */
        if (isActive && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapePressed();
            return;
        }

        if (!Keyboard.current.lKey.wasPressedThisFrame)
        {
            return;
        }

        if (isActive)
        {
            ExitLayoutMode();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
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
            reason = "Store Design Mode is not configured.";

            return false;
        }

        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f)
        {
            reason = "Close other menus before entering Store Design Mode.";

            return false;
        }

        if (playerInteraction.HasAnythingHeld)
        {
            reason = "Put down what you're carrying before entering Store Design Mode.";

            interactionUI.ShowPrompt(reason);

            return false;
        }

        if (furniturePlacementController.IsPlacementActive)
        {
            reason = "Finish or cancel the current furniture placement first.";

            return false;
        }

        isActive = true;
        navigationDirty = false;

        storeClock.SetLayoutSuspended(true);

        customerFlow.SuspendForLayout();

        playerInteraction.enabled = false;

        playerController.enabled = false;

        playerCamera.gameObject.SetActive(false);

        Vector3 playerPosition = playerController.transform.position;

        if (buildArea.TryGetWorkingZoneBounds(playerPosition, out Bounds workingBounds, out _))
        {
            layoutCameraController.ActivateForWorkingBounds(workingBounds);
        }
        else
        {
            layoutCameraController.ActivateFrom(playerController.transform);
        }

        playerController.ReleaseCursor();

        layoutModeUI.SetLayoutModeActive(true);

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
        
        if (furnitureContextMenuUI != null)
        {
            furnitureContextMenuUI.CloseMenu();
        }
        
        if (expansionLayoutController != null)
        {
            expansionLayoutController
                .ClearSelection();
        }

        /*
         * Clear transient Store Design UI before
         * cancelling placement.
         *
         * This also prevents a catalog that was
         * waiting to reopen after placement from
         * coming back while we are exiting.
         */
        if (buildCatalogUI != null)
        {
            buildCatalogUI.Close();
        }

        if (furniturePlacementController != null && furniturePlacementController.IsPlacementActive)
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

    public bool FocusPlacementArea(StoreAreaType requiredArea)
    {
        if (!isActive || buildArea == null || layoutCameraController == null)
        {
            return false;
        }

        Vector3 cameraPivot = layoutCameraController.PivotPosition;

        /*
         * Already working in the correct area.
         * Preserve the player's current camera.
         */
        if (buildArea.IsPointInsideAvailableArea(cameraPivot, requiredArea))
        {
            return true;
        }

        if (!buildArea.TryGetWorkingZoneBounds(cameraPivot, requiredArea, out Bounds targetBounds))
        {
            return false;
        }

        layoutCameraController.FocusWorkingBoundsImmediate(targetBounds);

        return true;
    }

    
    private void HandleEscapePressed()
    {
        escapeConsumedFrame = Time.frameCount;

        if (furniturePlacementController != null &&
            furniturePlacementController.IsPlacementActive)
        {
            furniturePlacementController.CancelActivePlacement();
            return;
        }

        if (buildCatalogUI != null && buildCatalogUI.IsOpen)
        {
            buildCatalogUI.Close();
            return;
        }

        if (expansionLayoutController != null && expansionLayoutController.HasSelection)
        {
            expansionLayoutController.ClearSelection();
            return;
        }

        if (furnitureContextMenuUI != null && furnitureContextMenuUI.HandleBack())
        {
            return;
        }

        ExitLayoutMode();
    }

    private bool HasValidConfiguration()
    {
        return storeClock != null && customerFlow != null && navMeshSurface != null && buildArea != null &&
               playerController != null && playerInteraction != null && furniturePlacementController != null &&
               playerCamera != null && layoutCameraController != null && interactionUI != null && layoutModeUI != null;
    }
}