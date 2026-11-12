
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerInteraction))]
public sealed class CashierWorkMode : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;

    private CheckoutRegister activeCheckout;
    private bool isWorking;
    private int enteredFrame = -1;
    private int escapeConsumedFrame = -1;

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
    }

    public bool IsWorkingAt(CheckoutRegister checkout)
    {
        return isWorking && activeCheckout == checkout && checkout != null;
    }

    public bool TryBegin(CheckoutRegister checkout)
    {
        if (isWorking || checkout == null || !checkout.isActiveAndEnabled ||
            playerController == null || playerInteraction == null || interactionUI == null)
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

        if (workArea == null || !workArea.CanServeFrom(transform.position))
        {
            return false;
        }

        activeCheckout = checkout;
        isWorking = true;
        enteredFrame = Time.frameCount;

        playerController.enabled = false;
        playerInteraction.enabled = false;
        playerController.ReleaseCursor();

        RefreshPrompt();
        return true;
    }

    private void Update()
    {
        if (!isWorking)
        {
            return;
        }

        // Another modal screen, such as the day summary or Manager Book,
        // has taken control. Do not override its cursor or input settings.
        if (Time.timeScale <= 0f || playerController.enabled || playerInteraction.enabled)
        {
            RelinquishToAnotherScreen();
            return;
        }

        if (activeCheckout == null || !activeCheckout.isActiveAndEnabled)
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

            // The E press that enters cashier mode must never scan an item.
            if (Time.frameCount != enteredFrame && Keyboard.current.eKey.wasPressedThisFrame)
            {
                activeCheckout.Interact(playerInteraction);
            }
        }

        RefreshPrompt();
    }

    public void Exit()
    {
        if (!isWorking)
        {
            return;
        }

        isWorking = false;
        activeCheckout = null;
        enteredFrame = -1;

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }

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

    private void RefreshPrompt()
    {
        if (!isWorking || activeCheckout == null || interactionUI == null)
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

    private void RelinquishToAnotherScreen()
    {
        isWorking = false;
        activeCheckout = null;
        enteredFrame = -1;

        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }
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

        Exit();
    }
}
