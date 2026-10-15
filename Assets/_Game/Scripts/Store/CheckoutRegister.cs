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
    private DailyStats dailyStats;

    [SerializeField]
    private CheckoutFeedback checkoutFeedback;

    private CustomerController activeCustomer;

    private int scannedItemCount;

    private float scannedTotal;

    public string GetInteractionPrompt(
        PlayerInteraction player
    )
    {
        if (player == null)
        {
            return "";
        }

        if (player.HasAnythingHeld)
        {
            return "Put down what you're carrying "
                   + "to use checkout";
        }

        if (checkoutQueue == null)
        {
            ResetCheckoutSession();

            return "Checkout not configured";
        }

        CustomerController customer =
            checkoutQueue.GetFrontCustomer();

        if (customer == null)
        {
            ResetCheckoutSession();

            return "No customers waiting";
        }

        if (!customer.IsReadyForCheckout())
        {
            ResetCheckoutSession();

            return "Customer approaching checkout";
        }

        int itemCount =
            customer.GetCarriedItemCount();

        if (itemCount <= 0)
        {
            ResetCheckoutSession();

            return "Customer has no products";
        }

        EnsureCheckoutSession(
            customer
        );

        /*
         * Still scanning products.
         */
        if (scannedItemCount < itemCount)
        {
            ProductData nextProduct =
                customer.GetCarriedProductData(
                    scannedItemCount
                );

            if (nextProduct == null)
            {
                return "Checkout item not configured";
            }

            return "[E] Scan "
                   + nextProduct.ProductName
                   + "\n"
                   + scannedItemCount
                   + " / "
                   + itemCount
                   + " scanned"
                   + "  •  £"
                   + scannedTotal.ToString(
                       "0.00"
                   );
        }

        /*
         * Everything scanned.
         */
        return "[E] Take payment"
               + "\nTOTAL £"
               + scannedTotal.ToString(
                   "0.00"
               );
    }

    public void Interact(
        PlayerInteraction player
    )
    {
        if (player == null ||
            player.HasAnythingHeld)
        {
            return;
        }

        if (checkoutQueue == null ||
            storeEconomy == null)
        {
            return;
        }

        CustomerController customer =
            checkoutQueue.GetFrontCustomer();

        if (customer == null)
        {
            ResetCheckoutSession();

            return;
        }

        if (!customer.IsReadyForCheckout())
        {
            ResetCheckoutSession();

            return;
        }

        int itemCount =
            customer.GetCarriedItemCount();

        if (itemCount <= 0)
        {
            ResetCheckoutSession();

            return;
        }

        EnsureCheckoutSession(
            customer
        );

        /*
         * Stage 1:
         * scan one item per E press.
         */
        if (scannedItemCount < itemCount)
        {
            ScanNextItem(
                customer
            );

            return;
        }

        /*
         * Stage 2:
         * everything has been scanned.
         * The next E accepts payment.
         */
        CompletePayment(
            customer,
            itemCount
        );
    }

    private void ScanNextItem(
        CustomerController customer
    )
    {
        if (customer == null)
        {
            return;
        }

        int itemCount =
            customer.GetCarriedItemCount();

        if (scannedItemCount < 0 ||
            scannedItemCount >= itemCount)
        {
            return;
        }

        ProductData product =
            customer.GetCarriedProductData(
                scannedItemCount
            );

        if (product == null)
        {
            return;
        }

        scannedTotal +=
            product.SellPrice;

        scannedItemCount++;

        if (checkoutFeedback != null)
        {
            checkoutFeedback
                .PlayScanFeedback();
        }
    }

    private void CompletePayment(
        CustomerController customer,
        int itemCount
    )
    {
        if (customer == null)
        {
            return;
        }

        if (scannedItemCount
            < itemCount)
        {
            return;
        }

        if (scannedTotal <= 0f)
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
            scannedTotal
        );

        if (dailyStats != null)
        {
            dailyStats.RecordSale(
                scannedTotal,
                itemCount
            );
        }

        if (checkoutFeedback != null)
        {
            checkoutFeedback
                .PlayPaymentFeedback(
                    scannedTotal
                );
        }

        ResetCheckoutSession();
    }

    private void EnsureCheckoutSession(
        CustomerController customer
    )
    {
        if (customer == null)
        {
            ResetCheckoutSession();

            return;
        }

        if (activeCustomer
            == customer)
        {
            return;
        }

        activeCustomer =
            customer;

        scannedItemCount =
            0;

        scannedTotal =
            0f;
    }

    private void ResetCheckoutSession()
    {
        activeCustomer =
            null;

        scannedItemCount =
            0;

        scannedTotal =
            0f;
    }
}