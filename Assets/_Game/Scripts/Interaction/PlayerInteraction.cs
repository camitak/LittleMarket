using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    private enum ShelfRepeatMode
    {
        None,
        Stock,
        Take
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

    private IInteractable focusedInteractable;
    
    private ShelfSlot repeatingShelfSlot;

    private ShelfRepeatMode repeatingShelfMode =
        ShelfRepeatMode.None;

    private float holdRepeatTimer;

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

        bool stockPressed =
            Keyboard.current.eKey
                .wasPressedThisFrame;

        bool stockHeld =
            Keyboard.current.eKey
                .isPressed;

        bool takePressed =
            Keyboard.current.rKey
                .wasPressedThisFrame;

        bool takeHeld =
            Keyboard.current.rKey
                .isPressed;

        /*
         * E = primary/use/stock.
         */
        if (stockPressed)
        {
            if (focusedInteractable != null)
            {
                focusedInteractable.Interact(
                    this
                );
            }

            BeginShelfRepeat(
                ShelfRepeatMode.Stock
            );

            return;
        }

        /*
         * R = deliberately take something
         * out of a shelf/storage position.
         */
        if (takePressed)
        {
            if (focusedInteractable
                is ShelfSlot shelfSlot)
            {
                shelfSlot.TakeOne(
                    this
                );
            }

            BeginShelfRepeat(
                ShelfRepeatMode.Take
            );

            return;
        }

        if (stockHeld)
        {
            UpdateShelfRepeat(
                ShelfRepeatMode.Stock
            );

            return;
        }

        if (takeHeld)
        {
            UpdateShelfRepeat(
                ShelfRepeatMode.Take
            );

            return;
        }

        ResetShelfRepeat();

        /*
         * Q = Drop.
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

        if (heldItem != null)
        {
            DropHeldItem();
        }
    }
    
    private void BeginShelfRepeat(
        ShelfRepeatMode mode
    )
    {
        /*
         * Continuous shelf transfers currently
         * only make sense while carrying the basket.
         */
        if (heldRestockBasket == null)
        {
            ResetShelfRepeat();

            return;
        }

        if (!(focusedInteractable
                is ShelfSlot shelfSlot))
        {
            ResetShelfRepeat();

            return;
        }

        repeatingShelfSlot =
            shelfSlot;

        repeatingShelfMode =
            mode;

        holdRepeatTimer =
            holdRepeatInitialDelay;
    }

    private void UpdateShelfRepeat(
        ShelfRepeatMode mode
    )
    {
        if (heldRestockBasket == null)
        {
            ResetShelfRepeat();

            return;
        }

        if (!(focusedInteractable
                is ShelfSlot currentShelfSlot))
        {
            ResetShelfRepeat();

            return;
        }

        /*
         * Moving aim to another shelf OR changing
         * direction restarts the safety delay.
         */
        if (currentShelfSlot
            != repeatingShelfSlot ||
            mode != repeatingShelfMode)
        {
            repeatingShelfSlot =
                currentShelfSlot;

            repeatingShelfMode =
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

        if (mode == ShelfRepeatMode.Stock)
        {
            repeatingShelfSlot.Interact(
                this
            );
        }
        else if (mode == ShelfRepeatMode.Take)
        {
            repeatingShelfSlot.TakeOne(
                this
            );
        }

        holdRepeatTimer =
            holdRepeatInterval;
    }

    private void ResetShelfRepeat()
    {
        repeatingShelfSlot =
            null;

        repeatingShelfMode =
            ShelfRepeatMode.None;

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
}