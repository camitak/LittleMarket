using UnityEngine;

public class BasketStation :
    MonoBehaviour,
    IInteractable,
    IManagementInteractable,
    IFurnitureRuntimeParticipant
{
    [Header("Basket")]
    [SerializeField] private RestockBasket basket;
    [SerializeField] private Transform dockPoint;

    [Header("Locator")]
    [SerializeField] private BasketLocatorUI basketLocatorUI;

    [Header("Startup")]
    [SerializeField] private bool dockBasketOnStart = true;

    private bool runtimeRegistered;

    public float ManagementHoldDuration => 0.8f;

    private void Awake()
    {
        ResolveSceneReferences();
    }

    public void RegisterFurnitureRuntime()
    {
        if (runtimeRegistered)
        {
            return;
        }

        runtimeRegistered = true;

        ResolveSceneReferences();

        if (basketLocatorUI != null)
        {
            basketLocatorUI.Hide();
        }

        TryDockAvailableBasket();
    }

    public void UnregisterFurnitureRuntime()
    {
        if (!runtimeRegistered)
        {
            return;
        }

        runtimeRegistered = false;

        if (basketLocatorUI != null)
        {
            basketLocatorUI.Hide();
        }

        if (basket != null &&
            basket.IsDocked &&
            basket.transform.IsChildOf(transform))
        {
            basket.ReleaseFromDock();
        }
    }

    private void OnDestroy()
    {
        if (runtimeRegistered)
        {
            UnregisterFurnitureRuntime();
        }
    }

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player == null)
        {
            return "";
        }

        ResolveSceneReferences();

        if (basket == null || dockPoint == null)
        {
            return "Basket station not configured";
        }

        RestockBasket heldBasket = player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket != basket)
            {
                return "This basket does not belong to this station";
            }

            return "[E] Return restock basket\n" +
                   basket.ItemCount + " / " + basket.Capacity;
        }

        if (player.GetHeldDeliveryBox() != null || player.GetHeldItem() != null)
        {
            return "Put down what you're carrying to use the basket station";
        }

        if (basket.IsDocked)
        {
            return "[E] Take restock basket\n" +
                   basket.ItemCount + " / " + basket.Capacity;
        }

        if (basket.IsHeld)
        {
            return "Restock basket is currently in use";
        }

        if (!basket.IsEmpty)
        {
            return "Basket away  •  " +
                   basket.ItemCount + " / " + basket.Capacity +
                   "\n[E] Locate basket";
        }

        return "Basket away\n[E] Locate basket  •  [Hold X] Recall";
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            return;
        }

        ResolveSceneReferences();

        if (basket == null || dockPoint == null)
        {
            return;
        }

        RestockBasket heldBasket = player.GetHeldRestockBasket();

        if (heldBasket != null)
        {
            if (heldBasket != basket)
            {
                return;
            }

            basket.DockTo(dockPoint);
            player.RemoveHeldRestockBasket();

            if (basketLocatorUI != null)
            {
                basketLocatorUI.Hide();
            }

            return;
        }

        if (player.HasAnythingHeld)
        {
            return;
        }

        if (basket.IsDocked)
        {
            if (basketLocatorUI != null)
            {
                basketLocatorUI.Hide();
            }

            player.TryPickUpRestockBasket(basket);
            return;
        }

        if (basket.IsHeld)
        {
            return;
        }

        if (basketLocatorUI != null)
        {
            basketLocatorUI.Show(basket);
        }
    }

    public bool CanUseManagementAction(PlayerInteraction player)
    {
        ResolveSceneReferences();

        if (player == null ||
            basket == null ||
            dockPoint == null ||
            player.HasAnythingHeld ||
            basket.IsDocked ||
            basket.IsHeld)
        {
            return false;
        }

        return basket.IsEmpty;
    }

    public void ManagementInteract(PlayerInteraction player)
    {
        if (!CanUseManagementAction(player))
        {
            return;
        }

        basket.DockTo(dockPoint);

        if (basketLocatorUI != null)
        {
            basketLocatorUI.Hide();
        }
    }

    private void ResolveSceneReferences()
    {
        if (basket == null)
        {
            basket = FindFirstObjectByType<RestockBasket>(FindObjectsInactive.Include);
        }

        if (basketLocatorUI == null)
        {
            basketLocatorUI =
                FindFirstObjectByType<BasketLocatorUI>(FindObjectsInactive.Include);
        }
    }

    private void TryDockAvailableBasket()
    {
        if (!dockBasketOnStart ||
            basket == null ||
            dockPoint == null ||
            basket.IsHeld)
        {
            return;
        }

        if (!basket.IsDocked && !basket.IsEmpty)
        {
            return;
        }

        basket.DockTo(dockPoint);
    }

    private void OnDrawGizmosSelected()
    {
        PlacementUtility.DrawFrameGizmo(dockPoint, 0.15f);
    }
}