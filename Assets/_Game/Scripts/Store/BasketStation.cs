using UnityEngine;

public class BasketStation :
    MonoBehaviour,
    IInteractable,
    IManagementInteractable
{
    [Header("Basket")]
    [SerializeField]
    private RestockBasket basket;

    [SerializeField]
    private Transform dockPoint;

    [Header("Startup")]
    [SerializeField]
    private bool dockBasketOnStart = true;

    public float ManagementHoldDuration =>
        0.8f;

    private void Start()
    {
        if (!dockBasketOnStart ||
            basket == null ||
            dockPoint == null)
        {
            return;
        }

        basket.DockTo(
            dockPoint
        );
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        if (basket == null ||
            dockPoint == null)
        {
            return "Basket station not configured";
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        /*
         * Player arrived carrying our basket.
         */
        if (heldBasket != null)
        {
            if (heldBasket != basket)
            {
                return "This basket does not belong "
                       + "to this station";
            }

            return "[E] Return restock basket"
                   + "\n"
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity;
        }

        /*
         * Other carried objects must be put down.
         */
        if (player.GetHeldDeliveryBox()
                != null ||
            player.GetHeldItem()
                != null)
        {
            return "Put down what you're carrying "
                   + "to use the basket station";
        }

        /*
         * Normal docked state.
         */
        if (basket.IsDocked)
        {
            return "[E] Take restock basket"
                   + "\n"
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity;
        }

        /*
         * Defensive case.
         */
        if (basket.IsHeld)
        {
            return "Restock basket is currently in use";
        }

        /*
         * A loaded basket cannot be recalled.
         *
         * Otherwise this would allow stock to be
         * teleported from anywhere in the shop
         * directly back to the stockroom.
         */
        if (!basket.IsEmpty)
        {
            return "Basket away from stand"
                   + "  •  "
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity
                   + "\nEmpty it before recalling";
        }

        return "Basket away from stand"
               + "\n[Hold X] Recall empty basket";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null ||
            basket == null ||
            dockPoint == null)
        {
            return;
        }

        RestockBasket heldBasket =
            player.GetHeldRestockBasket();

        /*
         * E while carrying the basket:
         *
         * return it physically to its home.
         */
        if (heldBasket != null)
        {
            if (heldBasket != basket)
            {
                return;
            }

            basket.DockTo(
                dockPoint
            );

            player.RemoveHeldRestockBasket();

            return;
        }

        /*
         * Free hands are required to take it.
         */
        if (player.HasAnythingHeld)
        {
            return;
        }

        if (!basket.IsDocked)
        {
            return;
        }

        player.TryPickUpRestockBasket(
            basket
        );
    }

    public bool CanUseManagementAction(
        PlayerInteraction player
    )
    {
        if (player == null ||
            basket == null ||
            dockPoint == null)
        {
            return false;
        }

        if (player.HasAnythingHeld)
        {
            return false;
        }

        if (basket.IsDocked ||
            basket.IsHeld)
        {
            return false;
        }

        /*
         * Empty-only recovery prevents stock
         * teleportation.
         */
        return basket.IsEmpty;
    }

    public void ManagementInteract(
        PlayerInteraction player
    )
    {
        if (!CanUseManagementAction(
                player
            ))
        {
            return;
        }

        basket.DockTo(
            dockPoint
        );
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            dockPoint,
            0.15f
        );
    }
}