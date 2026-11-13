
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerInteraction), typeof(CharacterController))]
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

    private CharacterController characterController;
    private CheckoutRegister activeCheckout;

    private bool isWorking;
    private int enteredFrame = -1;
    private int escapeConsumedFrame = -1;

    private Vector3 normalCameraLocalPosition;
    private Quaternion normalCameraLocalRotation;

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

        characterController = GetComponent<CharacterController>();
    }

    public bool IsWorkingAt(CheckoutRegister checkout)
    {
        return isWorking && activeCheckout == checkout && checkout != null;
    }

    public bool TryBegin(CheckoutRegister checkout)
    {
        if (isWorking || checkout == null || !checkout.isActiveAndEnabled ||
            playerController == null || playerInteraction == null ||
            playerCamera == null || interactionUI == null ||
            characterController == null)
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
            workArea.CashierStandPoint == null ||
            workArea.CashierViewPoint == null)
        {
            return false;
        }

        if (!TryFindWorkPosition(workArea, out Vector3 workPosition))
        {
            Debug.LogWarning(
                "Cannot enter cashier mode: the cashier standing area is blocked. " +
                "Clear the staff-side space and try again.",
                checkout);
            return false;
        }

        Vector3 startingCameraPosition = playerCamera.transform.position;
        Quaternion startingCameraRotation = playerCamera.transform.rotation;

        normalCameraLocalPosition = playerCamera.transform.localPosition;

        playerController.enabled = false;
        playerInteraction.enabled = false;

        // Move the actual player body to the safe cashier position.
        // The camera's previous WORLD pose is restored immediately afterward
        // so the player only perceives the existing smooth camera transition.
        bool wasControllerEnabled = characterController.enabled;
        characterController.enabled = false;

        transform.position = workPosition;
        playerController.AlignLookToWorldDirection(workArea.CashierViewPoint.forward);
        normalCameraLocalRotation = playerCamera.transform.localRotation;

        characterController.enabled = wasControllerEnabled;

        playerCamera.transform.SetPositionAndRotation(
            startingCameraPosition, startingCameraRotation);

        activeCheckout = checkout;
        isWorking = true;
        enteredFrame = Time.frameCount;

        playerController.ReleaseCursor();

        StartCameraTransition(
            workArea.CashierViewPoint.position,
            workArea.CashierViewPoint.rotation,
            enterTransitionSeconds,
            CameraTransition.Entering);

        RefreshPrompt();
        return true;
    }

    private bool TryFindWorkPosition(CheckoutWorkArea workArea, out Vector3 position)
    {
        position = Vector3.zero;

        Transform standPoint = workArea.CashierStandPoint;
        Vector3 centre = standPoint.position;

        // The centre is preferred. Small nearby fallbacks avoid rejecting
        // workstation entry just because another character is close by.
        Vector3[] candidates =
        {
            centre,
            centre + workArea.transform.right * 0.25f,
            centre - workArea.transform.right * 0.25f,
            centre + workArea.transform.forward * 0.25f
        };

        foreach (Vector3 candidate in candidates)
        {
            if (!workArea.CanServeFrom(candidate))
            {
                continue;
            }

            if (!IsStandingPositionClear(candidate))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private bool IsStandingPositionClear(Vector3 destination)
    {
        if (characterController == null)
        {
            return false;
        }

        float radius = Mathf.Max(
            0.05f,
            characterController.radius - characterController.skinWidth * 0.5f);

        float halfSegment = Mathf.Max(
            0f,
            characterController.height * 0.5f - radius);

        Vector3 capsuleCentre = destination + transform.TransformVector(characterController.center);
        Vector3 bottom = capsuleCentre + Vector3.down * halfSegment;
        Vector3 top = capsuleCentre + Vector3.up * halfSegment;

        Collider[] overlapping = Physics.OverlapCapsule(
            bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider other in overlapping)
        {
            if (other == null || other.transform.IsChildOf(transform))
            {
                continue;
            }

            // The authored player capsule is centred at local Y = 0.
            // Ignore the supporting floor, but not counter geometry.
            if (other.bounds.max.y <= destination.y + 0.15f)
            {
                continue;
            }

            return false;
        }

        // NavMeshAgents may not have physics colliders. Avoid placing the
        // player body directly over a customer who is already standing here.
        NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

        foreach (NavMeshAgent agent in agents)
        {
            if (agent == null || !agent.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 separation = agent.transform.position - destination;
            separation.y = 0f;

            float minimumDistance = characterController.radius + agent.radius;

            if (separation.sqrMagnitude < minimumDistance * minimumDistance)
            {
                return false;
            }
        }

        return true;
    }

    private void Update()
    {
        if (!isWorking)
        {
            return;
        }

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

        // The camera returns to normal first-person height and look angle,
        // but the PLAYER remains at the cashier standing position.
        Vector3 targetPosition = transform.TransformPoint(normalCameraLocalPosition);
        Quaternion targetRotation = transform.rotation * normalCameraLocalRotation;

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
        RestoreNormalCamera();
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
        RestoreNormalCamera();
        ClearWorkingState();

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

        // Another screen owns the cursor and movement controls.
    }

    private void RestoreNormalCamera()
    {
        if (playerCamera == null)
        {
            return;
        }

        playerCamera.transform.localPosition = normalCameraLocalPosition;
        playerCamera.transform.localRotation = normalCameraLocalRotation;
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
