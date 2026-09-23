using UnityEngine;

public class CheckoutRegister :
    MonoBehaviour,
    IInteractable
{
    [Header("References")]
    [SerializeField]
    private CheckoutQueue checkoutQueue;

    [SerializeField]
    private StoreEconomy storeEconomy;

    [SerializeField]
    private CheckoutFeedback checkoutFeedback;

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player.GetHeldItem() != null)
        {
            return "Put down item to use checkout";
        }

        if (checkoutQueue == null)
        {
            return "Checkout not configured";
        }

        CustomerController customer =
            checkoutQueue.GetFrontCustomer();

        if (customer == null)
        {
            return "No customers waiting";
        }

        if (!customer.IsReadyForCheckout())
        {
            return "Customer approaching checkout";
        }

        int itemCount =
            customer.GetCarriedItemCount();

        if (itemCount <= 0)
        {
            return "Customer has no products";
        }

        float total =
            customer.GetCheckoutTotal();

        return "[E] Checkout "
               + itemCount
               + " item"
               + (itemCount == 1 ? "" : "s")
               + " - £"
               + total.ToString("0.00");
    }

    public void Interact(PlayerInteraction player)
    {
        if (player.GetHeldItem() != null)
        {
            return;
        }

        if (checkoutQueue == null)
        {
            return;
        }

        if (storeEconomy == null)
        {
            return;
        }

        CustomerController customer =
            checkoutQueue.GetFrontCustomer();

        if (customer == null)
        {
            return;
        }

        if (!customer.IsReadyForCheckout())
        {
            return;
        }

        float saleAmount =
            customer.GetCheckoutTotal();

        if (saleAmount <= 0f)
        {
            return;
        }

        bool checkoutCompleted =
            customer.CompleteCheckout();

        if (!checkoutCompleted)
        {
            return;
        }

        storeEconomy.AddMoney(
            saleAmount
        );

        if (checkoutFeedback != null)
        {
            checkoutFeedback.PlaySaleFeedback(
                saleAmount
            );
        }
    }
}