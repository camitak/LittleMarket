using UnityEngine;

public class BasketStation :
    MonoBehaviour,
    IInteractable,
    IManagementInteractable
{
    [Header("Basket")] [SerializeField] private RestockBasket basket;

    [SerializeField] private Transform dockPoint;

    [Header("Locator")] [SerializeField] private BasketLocatorUI basketLocatorUI;

    [Header("Startup")] [SerializeField] private bool dockBasketOnStart = true;

    public float ManagementHoldDuration =>
        0.8f;

    private void Start()
    {
        if (basketLocatorUI != null) basketLocatorUI.Hide();

        if (!dockBasketOnStart ||
            basket == null ||
            dockPoint == null)
            return;

        basket.DockTo(
            dockPoint
        );
    }

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null) return "";

        if (basket == null ||
            dockPoint == null)
            return "Basket station not configured";

        var heldBasket =
            player.GetHeldRestockBasket();

        /*
         * Player returned carrying our basket.
         */
        if (heldBasket != null)
        {
            if (heldBasket != basket)
                return "This basket does not belong "
                       + "to this station";

            return "[E] Return restock basket"
                   + "\n"
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity;
        }

        /*
         * Other carried objects block the station.
         */
        if (player.GetHeldDeliveryBox()
            != null ||
            player.GetHeldItem()
            != null)
            return "Put down what you're carrying "
                   + "to use the basket station";

        /*
         * Normal docked state.
         */
        if (basket.IsDocked)
            return "[E] Take restock basket"
                   + "\n"
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity;

        /*
         * Defensive state.
         */
        if (basket.IsHeld) return "Restock basket is currently in use";

        /*
         * Loaded basket:
         *
         * locating is allowed,
         * teleporting is not.
         */
        if (!basket.IsEmpty)
            return "Basket away"
                   + "  •  "
                   + basket.ItemCount
                   + " / "
                   + basket.Capacity
                   + "\n[E] Locate basket";

        /*
         * Empty basket:
         *
         * locate OR recall.
         */
        return "Basket away"
               + "\n[E] Locate basket"
               + "  •  "
               + "[Hold X] Recall";
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null ||
            basket == null ||
            dockPoint == null)
            return;

        var heldBasket =
            player.GetHeldRestockBasket();

        /*
         * Return carried basket.
         */
        if (heldBasket != null)
        {
            if (heldBasket != basket) return;

            basket.DockTo(
                dockPoint
            );

            player.RemoveHeldRestockBasket();

            if (basketLocatorUI != null) basketLocatorUI.Hide();

            return;
        }

        /*
         * Other carried things block station use.
         */
        if (player.HasAnythingHeld) return;

        /*
         * Take basket from dock.
         */
        if (basket.IsDocked)
        {
            if (basketLocatorUI != null) basketLocatorUI.Hide();

            player.TryPickUpRestockBasket(
                basket
            );

            return;
        }

        if (basket.IsHeld) return;

        /*
         * Basket is somewhere in the world.
         *
         * E gives information rather than moving
         * the object.
         */
        if (basketLocatorUI != null)
            basketLocatorUI.Show(
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
            return false;

        if (player.HasAnythingHeld) return false;

        if (basket.IsDocked ||
            basket.IsHeld)
            return false;

        /*
         * Only an empty basket may teleport home.
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
            return;

        basket.DockTo(
            dockPoint
        );

        if (basketLocatorUI != null) basketLocatorUI.Hide();
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(
            dockPoint,
            0.15f
        );
    }
}