using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
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

    private PickupItem heldItem;

    private RestockBasket heldRestockBasket;

    private DeliveryBox heldDeliveryBox;

    private IInteractable focusedInteractable;

    private IInteractable repeatingTransferTarget;

    private TransferRepeatMode repeatingTransferMode =
        TransferRepeatMode.None;

    private float holdRepeatTimer;

    private IManagementInteractable
        managementHoldTarget;

    private float managementHoldTimer;

    public bool HasAnythingHeld =>
        heldItem != null
        || heldRestockBasket != null
        || heldDeliveryBox != null;

    private void Update()
    {
        focusedInteractable =
            FindFocusedInteractable();

        UpdateInteractionPrompt();

        HandleInteractionInput();
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
         * F = optional carry / bulk action.
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
         * X = optional hold-to-confirm
         * management action.
         */
        if (managementHeld)
        {
            ResetTransferRepeat();

            UpdateManagementHold();

            return;
        }

        ResetManagementHold();

        /*
         * Held E / R only repeat if the focused
         * interactable explicitly allows it.
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
         * Q = drop whatever we're carrying.
         */
        if (!Keyboard.current.qKey
            .wasPressedThisFrame)
        {
            return;
        }

        if (heldRestockBasket != null)
        {
            DropHeldRestockBasket();

            return;
        }

        if (heldDeliveryBox != null)
        {
            DropHeldDeliveryBox();

            return;
        }

        if (heldItem != null)
        {
            DropHeldItem();
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

        if (heldRestockBasket != null)
        {
            interactionUI.ShowPrompt(
                "[Q] Drop restock basket"
                + "  •  "
                + heldRestockBasket.ItemCount
                + " / "
                + heldRestockBasket.Capacity
            );

            return;
        }

        if (heldDeliveryBox != null)
        {
            ProductData boxProduct =
                heldDeliveryBox.ProductData;

            string boxName =
                boxProduct != null
                    ? boxProduct.ProductName
                    : "Delivery";

            interactionUI.ShowPrompt(
                "[Q] Drop "
                + boxName
                + " box"
                + "  •  "
                + heldDeliveryBox
                    .RemainingQuantity
                + " / "
                + heldDeliveryBox.Capacity
            );

            return;
        }

        if (heldItem != null)
        {
            interactionUI.ShowPrompt(
                "[Q] Drop "
                + heldItem.GetItemName()
            );

            return;
        }

        interactionUI.HidePrompt();
    }

    private IInteractable FindFocusedInteractable()
    {
        if (playerCamera == null)
        {
            return null;
        }

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        bool hasCenterHit =
            Physics.Raycast(
                ray,
                out RaycastHit centerHit,
                interactionDistance
            );

        if (hasCenterHit)
        {
            IInteractable directInteractable =
                FindInteractableOnCollider(
                    centerHit.collider
                );

            if (directInteractable != null)
            {
                return directInteractable;
            }
        }

        RaycastHit[] assistedHits =
            Physics.SphereCastAll(
                ray,
                interactionAssistRadius,
                interactionDistance
            );

        IInteractable bestInteractable =
            null;

        float bestScore =
            float.MaxValue;

        float maximumAllowedDistance =
            interactionDistance;

        if (hasCenterHit)
        {
            maximumAllowedDistance =
                centerHit.distance
                + interactionAssistRadius
                * 2f;
        }

        for (int i = 0;
             i < assistedHits.Length;
             i++)
        {
            RaycastHit assistedHit =
                assistedHits[i];

            if (assistedHit.collider == null)
            {
                continue;
            }

            if (assistedHit.distance
                > maximumAllowedDistance)
            {
                continue;
            }

            IInteractable interactable =
                FindInteractableOnCollider(
                    assistedHit.collider
                );

            if (interactable == null)
            {
                continue;
            }

            Vector3 toCenter =
                assistedHit.collider
                    .bounds.center
                - ray.origin;

            float forwardDistance =
                Vector3.Dot(
                    toCenter,
                    ray.direction
                );

            if (forwardDistance < 0f)
            {
                continue;
            }

            Vector3 closestPointOnRay =
                ray.origin
                + ray.direction
                * forwardDistance;

            float distanceFromCrosshair =
                Vector3.SqrMagnitude(
                    assistedHit.collider
                        .bounds.center
                    - closestPointOnRay
                );

            float score =
                distanceFromCrosshair
                + assistedHit.distance
                * 0.0001f;

            if (score >= bestScore)
            {
                continue;
            }

            bestScore =
                score;

            bestInteractable =
                interactable;
        }

        return bestInteractable;
    }

    private IInteractable FindInteractableOnCollider(
        Collider targetCollider
    )
    {
        if (targetCollider == null)
        {
            return null;
        }

        Transform currentTransform =
            targetCollider.transform;

        while (currentTransform != null)
        {
            MonoBehaviour[] behaviours =
                currentTransform
                    .GetComponents<
                        MonoBehaviour
                    >();

            for (int i = 0;
                 i < behaviours.Length;
                 i++)
            {
                MonoBehaviour behaviour =
                    behaviours[i];

                if (behaviour
                    is IInteractable interactable)
                {
                    return interactable;
                }
            }

            currentTransform =
                currentTransform.parent;
        }

        return null;
    }

    public bool TryPickUp(
        PickupItem item
    )
    {
        if (item == null)
        {
            return false;
        }

        if (heldItem != null ||
            heldRestockBasket != null ||
            heldDeliveryBox != null)
        {
            return false;
        }

        heldItem =
            item;

        heldItem.PickUp(
            holdPoint
        );

        return true;
    }

    public bool TryPickUpRestockBasket(
        RestockBasket basket
    )
    {
        if (basket == null)
        {
            return false;
        }

        if (heldItem != null ||
            heldRestockBasket != null ||
            heldDeliveryBox != null)
        {
            return false;
        }

        heldRestockBasket =
            basket;

        heldRestockBasket.PickUp(
            holdPoint,
            playerCamera.transform
        );

        return true;
    }

    public bool TryPickUpDeliveryBox(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null)
        {
            return false;
        }

        if (heldItem != null ||
            heldRestockBasket != null ||
            heldDeliveryBox != null)
        {
            return false;
        }

        heldDeliveryBox =
            deliveryBox;

        heldDeliveryBox.PickUp(
            holdPoint,
            playerCamera.transform
        );

        return true;
    }

    private void DropHeldItem()
    {
        if (heldItem == null)
        {
            return;
        }

        heldItem.Drop();

        heldItem =
            null;
    }

    private void DropHeldRestockBasket()
    {
        if (heldRestockBasket == null)
        {
            return;
        }

        heldRestockBasket.Drop();

        heldRestockBasket =
            null;
    }

    private void DropHeldDeliveryBox()
    {
        if (heldDeliveryBox == null)
        {
            return;
        }

        heldDeliveryBox.Drop();

        heldDeliveryBox =
            null;
    }

    public PickupItem GetHeldItem()
    {
        return heldItem;
    }

    public RestockBasket GetHeldRestockBasket()
    {
        return heldRestockBasket;
    }

    public DeliveryBox GetHeldDeliveryBox()
    {
        return heldDeliveryBox;
    }

    public void RemoveHeldItem()
    {
        heldItem =
            null;
    }

    public void RemoveHeldRestockBasket()
    {
        heldRestockBasket =
            null;
    }

    public void RemoveHeldDeliveryBox()
    {
        heldDeliveryBox =
            null;
    }
}