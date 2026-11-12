
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerInteraction))]
public sealed class CashierWorkMode : MonoBehaviour
{
    private enum CameraTransition
    {
        None,
        Entering,
        Exiting
    }

    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Camera playerCamera;

    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;

    [Header("Camera Transition")]
    [Min(0.05f)] [SerializeField] private float enterTransitionSeconds = 0.45f;
    [Min(0.05f)] [SerializeField] private float exitTransitionSeconds = 0.35f;

    private CheckoutRegister activeCheckout;
    private bool isWorking;
    private int enteredFrame = -1;
    private int escapeConsumedFrame = -1;

    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private Vector3 transitionStartPosition;
    private Quaternion transitionStartRotation;
    private Vector3 transitionTargetPosition;
    private Quaternion transitionTargetRotation;
    private float transitionElapsed;
    private float transitionDuration;
    private CameraTransition cameraTransition;

    public bool IsWorking => isWorking;
    public bool ConsumedEscapeThisFrame => escapeConsumedFrame == Time.frameCount;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }
    }

    public bool IsWorkingAt(CheckoutRegister checkout)
    {
        return isWorking && activeCheckout == checkout && checkout != null;
    }

    public bool TryBegin(CheckoutRegister checkout)
    {
        if (isWorking || checkout == null || !checkout.isActiveAndEnabled ||
            playerController == null || playerInteraction == null ||
            playerCamera == null || interactionUI == null)
        {
            return false;
        }

        if (!playerController.isActiveAndEnabled ||
            !playerInteraction.isActiveAndEnabled ||
            playerInteraction.HasAnythingHeld ||
            Time.timeScale <= 0f ||
            Cursor.lockState != CursorLockMode.Locked)
        {
            return false;
        }

        CheckoutWorkArea workArea = checkout.GetComponent<CheckoutWorkArea>();

        if (workArea == null ||
            workArea.CashierViewPoint == null ||
            !workArea.CanServeFrom(transform.position))
        {
            return false;
        }

        originalCameraLocalPosition = playerCamera.transform.localPosition;
        originalCameraLocalRotation = playerCamera.transform.localRotation;

        activeCheckout = checkout;
        isWorking = true;
        enteredFrame = Time.frameCount;

        playerController.enabled = false;
        playerInteraction.enabled = false;
        playerController.ReleaseCursor();

        StartCameraTransition(
            workArea.CashierViewPoint.position,
            workArea.CashierViewPoint.rotation,
            enterTransitionSeconds,
            CameraTransition.Entering);

        RefreshPrompt();
        return true;
    }

    private void Update()
    {
        if (!isWorking)
        {
            return;
        }

        // A modal screen has taken control of gameplay.
        if (Time.timeScale <= 0f ||
            playerController == null || playerInteraction == null ||
            playerController.enabled || playerInteraction.enabled)
        {
            RelinquishToAnotherScreen();
            return;
        }

        if ((activeCheckout == null || !activeCheckout.isActiveAndEnabled) &&
            cameraTransition != CameraTransition.Exiting)
        {
            Exit();
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapeConsumedFrame = Time.frameCount;
                Exit();
                return;
            }

            if (cameraTransition == CameraTransition.None &&
                Time.frameCount != enteredFrame &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                activeCheckout.Interact(playerInteraction);
            }
        }

        RefreshPrompt();
    }

    private void LateUpdate()
    {
        if (!isWorking || playerCamera == null ||
            cameraTransition == CameraTransition.None ||
            Time.timeScale <= 0f)
        {
            return;
        }

        transitionElapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(transitionElapsed / transitionDuration);
        float easedProgress = progress * progress * (3f - 2f * progress);

        playerCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(transitionStartPosition, transitionTargetPosition, easedProgress),
            Quaternion.Slerp(transitionStartRotation, transitionTargetRotation, easedProgress));

        if (progress < 1f)
        {
            return;
        }

        if (cameraTransition == CameraTransition.Entering)
        {
            cameraTransition = CameraTransition.None;
            RefreshPrompt();
            return;
        }

        FinishExit();
    }

    public void Exit()
    {
        if (!isWorking || cameraTransition == CameraTransition.Exiting)
        {
            return;
        }

        if (playerCamera == null)
        {
            FinishExit();
            return;
        }

        Transform cameraParent = playerCamera.transform.parent;

        Vector3 targetPosition = cameraParent != null
            ? cameraParent.TransformPoint(originalCameraLocalPosition)
            : originalCameraLocalPosition;

        Quaternion targetRotation = cameraParent != null
            ? cameraParent.rotation * originalCameraLocalRotation
            : originalCameraLocalRotation;

        StartCameraTransition(
            targetPosition,
            targetRotation,
            exitTransitionSeconds,
            CameraTransition.Exiting);

        RefreshPrompt();
    }

    private void StartCameraTransition(
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration,
        CameraTransition nextTransition)
    {
        transitionStartPosition = playerCamera.transform.position;
        transitionStartRotation = playerCamera.transform.rotation;

        transitionTargetPosition = targetPosition;
        transitionTargetRotation = targetRotation;

        transitionElapsed = 0f;
        transitionDuration = Mathf.Max(0.05f, duration);
        cameraTransition = nextTransition;
    }

    private void RefreshPrompt()
    {
        if (!isWorking || interactionUI == null)
        {
            return;
        }

        if (cameraTransition == CameraTransition.Entering)
        {
            interactionUI.ShowPrompt("Moving to cashier view...\n[Esc] Cancel");
            return;
        }

        if (cameraTransition == CameraTransition.Exiting)
        {
            interactionUI.ShowPrompt("Leaving checkout...");
            return;
        }

        if (activeCheckout == null)
        {
            return;
        }

        string prompt = activeCheckout.GetInteractionPrompt(playerInteraction);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            prompt = "Checkout ready";
        }

        interactionUI.ShowPrompt($"{prompt}\n[Esc] Leave cashier position");
    }

    private void FinishExit()
    {
        RestoreOriginalCamera();
        ClearWorkingState();

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

        if (Time.timeScale > 0f && gameObject.activeInHierarchy)
        {
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.CaptureCursor();
            }

            if (playerInteraction != null)
            {
                playerInteraction.enabled = true;
            }
        }
    }

    private void RelinquishToAnotherScreen()
    {
        RestoreOriginalCamera();
        ClearWorkingState();

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

        // The other screen owns the cursor and player controls.
    }

    private void RestoreOriginalCamera()
    {
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.localPosition = originalCameraLocalPosition;
        playerCamera.transform.localRotation = originalCameraLocalRotation;
    }

    private void ClearWorkingState()
    {
        isWorking = false;
        activeCheckout = null;
        enteredFrame = -1;
        cameraTransition = CameraTransition.None;
        transitionElapsed = 0f;
    }

    private void OnDisable()
    {
        if (!isWorking)
        {
            return;
        }

        if (Time.timeScale <= 0f || !gameObject.activeInHierarchy)
        {
            RelinquishToAnotherScreen();
            return;
        }

        FinishExit();
    }
}
