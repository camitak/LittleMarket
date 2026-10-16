using UnityEngine;

public sealed class PlayerCarryState
{
    private readonly Transform holdPoint;

    private readonly Transform viewTransform;

    private PickupItem heldItem;

    private RestockBasket heldRestockBasket;

    private DeliveryBox heldDeliveryBox;

    public bool HasAnythingHeld =>
        heldItem != null
        || heldRestockBasket != null
        || heldDeliveryBox != null;

    public PickupItem HeldItem =>
        heldItem;

    public RestockBasket HeldRestockBasket =>
        heldRestockBasket;

    public DeliveryBox HeldDeliveryBox =>
        heldDeliveryBox;

    public PlayerCarryState(
        Transform holdPoint,
        Transform viewTransform
    )
    {
        this.holdPoint =
            holdPoint;

        this.viewTransform =
            viewTransform;
    }

    public bool TryPickUp(
        PickupItem item
    )
    {
        if (item == null ||
            holdPoint == null ||
            HasAnythingHeld)
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
        if (basket == null ||
            holdPoint == null ||
            viewTransform == null ||
            HasAnythingHeld)
        {
            return false;
        }

        heldRestockBasket =
            basket;

        heldRestockBasket.PickUp(
            holdPoint,
            viewTransform
        );

        return true;
    }

    public bool TryPickUpDeliveryBox(
        DeliveryBox deliveryBox
    )
    {
        if (deliveryBox == null ||
            holdPoint == null ||
            viewTransform == null ||
            HasAnythingHeld)
        {
            return false;
        }

        heldDeliveryBox =
            deliveryBox;

        heldDeliveryBox.PickUp(
            holdPoint,
            viewTransform
        );

        return true;
    }

    public void DropCurrent()
    {
        /*
         * Preserve our existing priority.
         */
        if (heldRestockBasket != null)
        {
            heldRestockBasket.Drop();

            heldRestockBasket =
                null;

            return;
        }

        if (heldDeliveryBox != null)
        {
            heldDeliveryBox.Drop();

            heldDeliveryBox =
                null;

            return;
        }

        if (heldItem != null)
        {
            heldItem.Drop();

            heldItem =
                null;
        }
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