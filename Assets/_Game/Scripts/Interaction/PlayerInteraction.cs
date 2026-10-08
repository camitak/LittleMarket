using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private float interactionDistance = 3f;

    [Header("References")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private Transform holdPoint;

    [SerializeField]
    private InteractionUI interactionUI;

    private PickupItem heldItem;

    private RestockBasket heldRestockBasket;

    private void Update()
    {
        UpdateInteractionPrompt();

        HandleInteractionInput();
    }

    private void HandleInteractionInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.eKey
            .wasPressedThisFrame)
        {
            return;
        }

        if (heldRestockBasket != null)
        {
            if (!TryInteract())
            {
                DropHeldRestockBasket();
            }

            return;
        }

        if (heldItem != null)
        {
            if (!TryInteract())
            {
                DropHeldItem();
            }

            return;
        }

        TryInteract();
    }

    private void UpdateInteractionPrompt()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance
            ))
        {
            IInteractable interactable =
                hit.collider
                    .GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactionUI.ShowPrompt(
                    interactable
                        .GetInteractionPrompt(
                            this
                        )
                );

                return;
            }
        }

        if (heldRestockBasket != null)
        {
            interactionUI.ShowPrompt(
                "[E] Drop restock basket"
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
                "[E] Drop "
                + heldItem.GetItemName()
            );

            return;
        }

        interactionUI.HidePrompt();
    }

    private bool TryInteract()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance
            ))
        {
            IInteractable interactable =
                hit.collider
                    .GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(
                    this
                );

                return true;
            }
        }

        return false;
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
            holdPoint
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