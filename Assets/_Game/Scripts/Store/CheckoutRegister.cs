using System.Collections;
using UnityEngine;

public class CheckoutRegister :
    MonoBehaviour,
    IInteractable
{
    [Header("Store References")]
    [SerializeField]
    private CheckoutQueue checkoutQueue;

    [SerializeField]
    private StoreEconomy storeEconomy;

    [SerializeField]
    private DailyStats dailyStats;

    [SerializeField]
    private CheckoutFeedback checkoutFeedback;

    [Header("Physical Checkout")]
    [SerializeField]
    private Transform scanPoint;

    [SerializeField]
    private Transform[] scannedItemPoints;

    [Min(0.05f)]
    [SerializeField]
    private float scanDisplaySeconds = 0.12f;

    private CustomerController activeCustomer;

    private int scannedItemCount;

    private float scannedTotal;

    private bool scanInProgress;

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

        if (!HasValidPhysicalCheckout(
                itemCount
            ))
        {
            return "Checkout item points "
                   + "not configured";
        }

        /*
         * Briefly block another scan while the
         * current item is sitting over the scanner.
         */
        if (scanInProgress)
        {
            return "Scanning...";
        }

        /*
         * Products still waiting to be scanned.
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
         * Everything scanned and physically
         * sitting in the bagging area.
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
            player.HasAnythingHeld ||
            scanInProgress)
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

        if (!HasValidPhysicalCheckout(
                itemCount
            ))
        {
            return;
        }

        /*
         * Stage 1:
         * physically scan one product.
         */
        if (scannedItemCount < itemCount)
        {
            StartCoroutine(
                ScanNextItemRoutine(
                    customer
                )
            );

            return;
        }

        /*
         * Stage 2:
         * take payment.
         */
        CompletePayment(
            customer,
            itemCount
        );
    }

    private IEnumerator ScanNextItemRoutine(
        CustomerController customer
    )
    {
        if (customer == null)
        {
            yield break;
        }

        int itemCount =
            customer.GetCarriedItemCount();

        if (scannedItemCount < 0 ||
            scannedItemCount >= itemCount)
        {
            yield break;
        }

        if (scannedItemCount
            >= scannedItemPoints.Length)
        {
            yield break;
        }

        PickupItem item =
            customer.GetCarriedItem(
                scannedItemCount
            );

        if (item == null)
        {
            yield break;
        }

        ProductData product =
            item.GetProductData();

        if (product == null)
        {
            yield break;
        }

        Transform outputPoint =
            scannedItemPoints[
                scannedItemCount
            ];

        if (scanPoint == null ||
            outputPoint == null)
        {
            yield break;
        }

        scanInProgress =
            true;

        /*
         * Move the customer's REAL product onto
         * the scanner.
         *
         * PickupItem already knows how to align
         * different product shapes using its
         * placement anchor.
         */
        item.PlaceInContainer(
            scanPoint
        );

        if (checkoutFeedback != null)
        {
            checkoutFeedback
                .PlayScanFeedback();
        }

        /*
         * Let the player actually see the item
         * sitting on the scanner for a moment.
         */
        yield return new WaitForSeconds(
            Mathf.Max(
                0.05f,
                scanDisplaySeconds
            )
        );

        /*
         * Then move it to this transaction's
         * bagging/output slot.
         */
        if (item != null)
        {
            item.PlaceInContainer(
                outputPoint
            );
        }

        scannedTotal +=
            product.SellPrice;

        scannedItemCount++;

        scanInProgress =
            false;
    }

    private void CompletePayment(
        CustomerController customer,
        int itemCount
    )
    {
        if (customer == null ||
            scanInProgress)
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

        /*
         * CompleteCheckout destroys all products
         * belonging to that customer.
         *
         * Because the bagging-area products are
         * the SAME PickupItems still held in the
         * customer's carriedItems list, they are
         * safely destroyed here too.
         */
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

        scanInProgress =
            false;
    }

    private void ResetCheckoutSession()
    {
        activeCustomer =
            null;

        scannedItemCount =
            0;

        scannedTotal =
            0f;

        scanInProgress =
            false;
    }

    private bool HasValidPhysicalCheckout(
        int itemCount
    )
    {
        if (scanPoint == null)
        {
            return false;
        }

        if (scannedItemPoints == null)
        {
            return false;
        }

        if (scannedItemPoints.Length
            < itemCount)
        {
            return false;
        }

        for (int i = 0;
             i < itemCount;
             i++)
        {
            if (scannedItemPoints[i]
                == null)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (scanPoint != null)
        {
            Gizmos.DrawWireSphere(
                scanPoint.position,
                0.05f
            );
        }

        if (scannedItemPoints == null)
        {
            return;
        }

        for (int i = 0;
             i < scannedItemPoints.Length;
             i++)
        {
            Transform point =
                scannedItemPoints[i];

            if (point == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(
                point.position,
                0.04f
            );
        }
    }
}