using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction :
    MonoBehaviour
{
    private enum TransferRepeatMode
    {
        None,
        Primary,
        Secondary
    }

    [Header("Interaction")]
    [SerializeField]
    private float interactionDistance = 3f;

    [Range(0.02f, 0.3f)]
    [SerializeField]
    private float interactionAssistRadius = 0.14f;

    [Header("Held Interaction")]
    [Min(0.05f)]
    [SerializeField]
    private float holdRepeatInitialDelay = 0.30f;

    [Min(0.05f)]
    [SerializeField]
    private float holdRepeatInterval = 0.18f;

    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform holdPoint;

    [SerializeField]
    private InteractionUI interactionUI;

    private PlayerCarryState carryState;

    private IInteractable focusedInteractable;

    private IInteractable repeatingTransferTarget;

    private TransferRepeatMode repeatingTransferMode =
        TransferRepeatMode.None;

    private float holdRepeatTimer;

    private IManagementInteractable
        managementHoldTarget;

    private float managementHoldTimer;

    public bool HasAnythingHeld
    {
        get
        {
            EnsureCarryState();

            return carryState.HasAnythingHeld;
        }
    }

    private void Awake()
    {
        EnsureCarryState();
    }

    private void Update()
    {
        focusedInteractable =
            InteractionTargetFinder.Find(
                playerCamera,
                interactionDistance,
                interactionAssistRadius
            );

        UpdateInteractionPrompt();

        HandleInteractionInput();
    }

    private void EnsureCarryState()
    {
        if (carryState != null)
        {
            return;
        }

        Transform viewTransform =
            playerCamera != null
                ? playerCamera.transform
                : null;

        carryState =
            new PlayerCarryState(
                holdPoint,
                viewTransform
            );
    }

    private void HandleInteractionInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool primaryPressed =
            Keyboard.current.eKey
                .wasPressedThisFrame;

        bool primaryHeld =
            Keyboard.current.eKey
                .isPressed;

        bool secondaryPressed =
            Keyboard.current.rKey
                .wasPressedThisFrame;

        bool secondaryHeld =
            Keyboard.current.rKey
                .isPressed;

        bool carryPressed =
            Keyboard.current.fKey
                .wasPressedThisFrame;

        bool managementHeld =
            Keyboard.current.xKey
                .isPressed;

        /*
         * E = primary contextual action.
         */
        if (primaryPressed)
        {
            ResetManagementHold();

            if (focusedInteractable != null)
            {
                focusedInteractable.Interact(
                    this
                );
            }

            BeginTransferRepeat(
                TransferRepeatMode.Primary
            );

            return;
        }

        /*
         * R = optional secondary action.
         */
        if (secondaryPressed)
        {
            ResetManagementHold();

            PerformSecondaryAction(
                focusedInteractable
            );

            BeginTransferRepeat(
                TransferRepeatMode.Secondary
            );

            return;
        }

        /*
         * F = carry / bulk contextual action.
         */
        if (carryPressed)
        {
            ResetTransferRepeat();

            ResetManagementHold();

            PerformCarryAction(
                focusedInteractable
            );

            return;
        }

        /*
         * X = hold-to-confirm management action.
         */
        if (managementHeld)
        {
            ResetTransferRepeat();

            UpdateManagementHold();

            return;
        }

        ResetManagementHold();

        /*
         * E/R repeat only when the target
         * explicitly supports repetition.
         */
        if (primaryHeld)
        {
            UpdateTransferRepeat(
                TransferRepeatMode.Primary
            );

            return;
        }

        if (secondaryHeld)
        {
            UpdateTransferRepeat(
                TransferRepeatMode.Secondary
            );

            return;
        }

        ResetTransferRepeat();

        /*
         * Q belongs to the carry state now.
         */
        if (Keyboard.current.qKey
            .wasPressedThisFrame)
        {
            EnsureCarryState();

            carryState.DropCurrent();
        }
    }

    private void PerformSecondaryAction(
        IInteractable target
    )
    {
        if (!(target
              is ISecondaryInteractable secondary))
        {
            return;
        }

        secondary.SecondaryInteract(
            this
        );
    }

    private void PerformCarryAction(
        IInteractable target
    )
    {
        if (!(target
              is ICarryInteractable carry))
        {
            return;
        }

        carry.CarryInteract(
            this
        );
    }

    private void BeginTransferRepeat(
        TransferRepeatMode mode
    )
    {
        if (!CanRepeatTransfer(
                focusedInteractable,
                mode
            ))
        {
            ResetTransferRepeat();

            return;
        }

        repeatingTransferTarget =
            focusedInteractable;

        repeatingTransferMode =
            mode;

        holdRepeatTimer =
            holdRepeatInitialDelay;
    }

    private void UpdateTransferRepeat(
        TransferRepeatMode mode
    )
    {
        if (!CanRepeatTransfer(
                focusedInteractable,
                mode
            ))
        {
            ResetTransferRepeat();

            return;
        }

        if (focusedInteractable
                != repeatingTransferTarget ||
            mode != repeatingTransferMode)
        {
            repeatingTransferTarget =
                focusedInteractable;

            repeatingTransferMode =
                mode;

            holdRepeatTimer =
                holdRepeatInitialDelay;

            return;
        }

        holdRepeatTimer -=
            Time.unscaledDeltaTime;

        if (holdRepeatTimer > 0f)
        {
            return;
        }

        if (mode
            == TransferRepeatMode.Primary)
        {
            repeatingTransferTarget.Interact(
                this
            );
        }
        else if (mode
                 == TransferRepeatMode.Secondary)
        {
            PerformSecondaryAction(
                repeatingTransferTarget
            );
        }

        holdRepeatTimer =
            holdRepeatInterval;
    }

    private bool CanRepeatTransfer(
        IInteractable target,
        TransferRepeatMode mode
    )
    {
        if (!(target
              is IRepeatableInteractable repeatable))
        {
            return false;
        }

        if (mode
            == TransferRepeatMode.Primary)
        {
            return repeatable.CanRepeatPrimary(
                this
            );
        }

        if (mode
            == TransferRepeatMode.Secondary)
        {
            return repeatable.CanRepeatSecondary(
                this
            );
        }

        return false;
    }

    private void ResetTransferRepeat()
    {
        repeatingTransferTarget =
            null;

        repeatingTransferMode =
            TransferRepeatMode.None;

        holdRepeatTimer =
            0f;
    }

    private void UpdateManagementHold()
    {
        if (!(focusedInteractable
              is IManagementInteractable
                  managementTarget))
        {
            ResetManagementHold();

            return;
        }

        if (!managementTarget
            .CanUseManagementAction(
                this
            ))
        {
            ResetManagementHold();

            return;
        }

        if (managementHoldTarget
            != managementTarget)
        {
            managementHoldTarget =
                managementTarget;

            managementHoldTimer =
                0f;
        }

        managementHoldTimer +=
            Time.unscaledDeltaTime;

        float requiredDuration =
            Mathf.Max(
                0.05f,
                managementTarget
                    .ManagementHoldDuration
            );

        if (managementHoldTimer
            < requiredDuration)
        {
            return;
        }

        managementTarget.ManagementInteract(
            this
        );

        ResetManagementHold();
    }

    private void ResetManagementHold()
    {
        managementHoldTarget =
            null;

        managementHoldTimer =
            0f;
    }

    private void UpdateInteractionPrompt()
    {
        if (interactionUI == null)
        {
            return;
        }

        if (focusedInteractable != null)
        {
            string prompt =
                focusedInteractable
                    .GetInteractionPrompt(
                        this
                    );

            if (!string.IsNullOrWhiteSpace(
                    prompt
                ))
            {
                interactionUI.ShowPrompt(
                    prompt
                );

                return;
            }
        }

        EnsureCarryState();

        RestockBasket basket =
            carryState.HeldRestockBasket;

        if (basket != null)
        {
            interactionUI.ShowPrompt(
                "[Q] Drop restock basket"
                + "  •  "
                + basket.ItemCount
                + " / "
                + basket.Capacity
            );

            return;
        }

        DeliveryBox deliveryBox =
            carryState.HeldDeliveryBox;

        if (deliveryBox != null)
        {
            ProductData boxProduct =
                deliveryBox.ProductData;

            string boxName =
                boxProduct != null
                    ? boxProduct.ProductName
                    : "Delivery";

            interactionUI.ShowPrompt(
                "[Q] Drop "
                + boxName
                + " box"
                + "  •  "
                + deliveryBox.RemainingQuantity
                + " / "
                + deliveryBox.Capacity
            );

            return;
        }

        PickupItem item =
            carryState.HeldItem;

        if (item != null)
        {
            interactionUI.ShowPrompt(
                "[Q] Drop "
                + item.GetItemName()
            );

            return;
        }

        interactionUI.HidePrompt();
    }

    /*
     * Public compatibility API.
     *
     * Other gameplay scripts can keep talking
     * to PlayerInteraction exactly as before.
     *
     * Internally the work is now delegated to
     * PlayerCarryState.
     */

    public bool TryPickUp(
        PickupItem item
    )
    {
        EnsureCarryState();

        return carryState.TryPickUp(
            item
        );
    }

    public bool TryPickUpRestockBasket(
        RestockBasket basket
    )
    {
        EnsureCarryState();

        return carryState
            .TryPickUpRestockBasket(
                basket
            );
    }

    public bool TryPickUpDeliveryBox(
        DeliveryBox deliveryBox
    )
    {
        EnsureCarryState();

        return carryState
            .TryPickUpDeliveryBox(
                deliveryBox
            );
    }

    public PickupItem GetHeldItem()
    {
        EnsureCarryState();

        return carryState.HeldItem;
    }

    public RestockBasket GetHeldRestockBasket()
    {
        EnsureCarryState();

        return carryState.HeldRestockBasket;
    }

    public DeliveryBox GetHeldDeliveryBox()
    {
        EnsureCarryState();

        return carryState.HeldDeliveryBox;
    }

    public void RemoveHeldItem()
    {
        EnsureCarryState();

        carryState.RemoveHeldItem();
    }

    public void RemoveHeldRestockBasket()
    {
        EnsureCarryState();

        carryState.RemoveHeldRestockBasket();
    }

    public void RemoveHeldDeliveryBox()
    {
        EnsureCarryState();

        carryState.RemoveHeldDeliveryBox();
    }
}