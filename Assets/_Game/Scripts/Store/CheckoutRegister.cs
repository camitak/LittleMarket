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

        ProductData product =
            customer.GetCarriedProductData();

        if (product == null)
        {
            return "Customer has no product";
        }

        return "[E] Scan "
               + product.ProductName
               + " - £"
               + product.SellPrice.ToString("0.00");
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

        ProductData product =
            customer.GetCarriedProductData();

        if (product == null)
        {
            return;
        }

        bool checkoutCompleted =
            customer.CompleteCheckout();

        if (!checkoutCompleted)
        {
            return;
        }

        float saleAmount =
            product.SellPrice;

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