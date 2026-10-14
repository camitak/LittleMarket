using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private enum TransferRepeatMode
    {
        None,
        Primary,
        Reverse
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

        bool reversePressed =
            Keyboard.current.rKey
                .wasPressedThisFrame;

        bool reverseHeld =
            Keyboard.current.rKey
                .isPressed;

        bool carryPressed =
            Keyboard.current.fKey
                .wasPressedThisFrame;

        /*
         * E = natural primary action for the target.
         */
        if (primaryPressed)
        {
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
         * R = reverse the normal transfer direction.
         */
        if (reversePressed)
        {
            PerformReverseAction(
                focusedInteractable
            );

            BeginTransferRepeat(
                TransferRepeatMode.Reverse
            );

            return;
        }

        /*
         * F = carry an entire container.
         *
         * For now this applies to DeliveryBox.
         */
        if (carryPressed)
        {
            if (focusedInteractable
                is DeliveryBox deliveryBox)
            {
                deliveryBox.Carry(
                    this
                );

                ResetTransferRepeat();

                return;
            }

            if (focusedInteractable
                is StorageBay storageBay)
            {
                storageBay.HandleCarryAction(
                    this
                );

                ResetTransferRepeat();

                return;
            }
        }

        if (primaryHeld)
        {
            UpdateTransferRepeat(
                TransferRepeatMode.Primary
            );

            return;
        }

        if (reverseHeld)
        {
            UpdateTransferRepeat(
                TransferRepeatMode.Reverse
            );

            return;
        }

        ResetTransferRepeat();

        /*
         * Q = drop the carried object.
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

    private void PerformReverseAction(
        IInteractable target
    )
    {
        if (target is ShelfSlot shelfSlot)
        {
            shelfSlot.TakeOne(
                this
            );

            return;
        }
        
        if (target is StorageBay storageBay)
        {
            storageBay.ReturnOne(
                this
            );

            return;
        }
        
        if (target is RestockBasket restockBasket)
        {
            restockBasket.TakeOne(
                this
            );

            return;
        }

        if (target is DeliveryBox deliveryBox)
        {
            deliveryBox.ReturnOne(
                this
            );
        }
    }

    private void BeginTransferRepeat(
        TransferRepeatMode mode
    )
    {
        if (!CanRepeatTransfer(
                focusedInteractable
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
                focusedInteractable
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
                 == TransferRepeatMode.Reverse)
        {
            PerformReverseAction(
                repeatingTransferTarget
            );
        }

        holdRepeatTimer =
            holdRepeatInterval;
    }

    private bool CanRepeatTransfer(
        IInteractable target
    )
    {
        if (target == null)
        {
            return false;
        }

        /*
         * Shelf transfers can repeat when we're
         * carrying either stock container.
         */
        if (target is ShelfSlot)
        {
            return heldRestockBasket != null
                   || heldDeliveryBox != null;
        }

        /*
         * Grounded DeliveryBox transfers repeat
         * while using the basket.
         *
         * Empty hands intentionally do not repeat:
         * the first E gives us one product and
         * our hands become full.
         */
        if (target is DeliveryBox)
        {
            return heldRestockBasket != null;
        }
        
        /*
         * Storage transfers repeat while using
         * either stock container.
         */
        if (target is StorageBay)
        {
            /*
             * Basket transfers happen one unit at a time,
             * so holding E/R repeats.
             *
             * Cartons transfer as one whole physical
             * packing action, so they do NOT repeat.
             */
            return heldRestockBasket != null;
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
                + heldDeliveryBox.RemainingQuantity
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

        /*
         * First try a normal precise ray.
         *
         * If the crosshair is directly over
         * something interactable, that always wins.
         */
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

        /*
         * If the precise ray missed the interaction
         * collider, look in a small radius around it.
         *
         * This makes shelves and objects easier to
         * target without turning interaction into a
         * huge invisible cone.
         */
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

        /*
         * If our exact centre ray hit solid geometry,
         * don't allow the assist to select something
         * far behind that obstruction.
         */
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
                assistedHit.collider.bounds.center
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

            /*
             * Crosshair alignment matters much more
             * than a tiny distance difference.
             */
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

        /*
         * Some interactable objects may have their
         * collider on a child object.
         *
         * Walk upward until an IInteractable is found.
         */
        while (currentTransform != null)
        {
            MonoBehaviour[] behaviours =
                currentTransform
                    .GetComponents<MonoBehaviour>();

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

        if (heldItem != null)
        {
            return false;
        }

        if (heldRestockBasket != null)
        {
            return false;
        }
        
        if (heldDeliveryBox != null)
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

        if (heldItem != null)
        {
            return false;
        }

        if (heldRestockBasket != null)
        {
            return false;
        }
        
        if (heldDeliveryBox != null)
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

    public PickupItem GetHeldItem()
    {
        return heldItem;
    }

    public RestockBasket GetHeldRestockBasket()
    {
        return heldRestockBasket;
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

    public DeliveryBox GetHeldDeliveryBox()
    {
        return heldDeliveryBox;
    }

    public void RemoveHeldDeliveryBox()
    {
        heldDeliveryBox =
            null;
    }
}