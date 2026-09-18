using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private InteractionUI interactionUI;

    private PickupItem heldItem;

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

        if (!Keyboard.current.eKey.wasPressedThisFrame)
        {
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
                hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactionUI.ShowPrompt(
                    interactable.GetInteractionPrompt(this)
                );

                return;
            }
        }

        if (heldItem != null)
        {
            interactionUI.ShowPrompt(
                "[E] Drop " + heldItem.GetItemName()
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
                hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(this);

                return true;
            }
        }

        return false;
    }

    public void TryPickUp(PickupItem item)
    {
        if (heldItem != null)
        {
            return;
        }

        heldItem = item;

        heldItem.PickUp(holdPoint);
    }

    private void DropHeldItem()
    {
        if (heldItem == null)
        {
            return;
        }

        heldItem.Drop();

        heldItem = null;
    }

    public PickupItem GetHeldItem()
    {
        return heldItem;
    }

    public void RemoveHeldItem()
    {
        heldItem = null;
    }
}