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
    
    private PickupItem reservedScanItem;

    private int reservedScanIndex = -1;

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
            BeginNextScan(
                customer
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
    
    private void BeginNextScan(
        CustomerController customer
    )
    {
        if (customer == null ||
            scanInProgress)
        {
            return;
        }

        int itemCount =
            customer.GetCarriedItemCount();

        int scanIndex =
            scannedItemCount;

        if (scanIndex < 0 ||
            scanIndex >= itemCount)
        {
            return;
        }

        if (scannedItemPoints == null ||
            scanIndex
            >= scannedItemPoints.Length)
        {
            return;
        }

        PickupItem item =
            customer.GetCarriedItem(
                scanIndex
            );

        if (item == null)
        {
            return;
        }

        ProductData product =
            item.GetProductData();

        if (product == null)
        {
            return;
        }

        Transform outputPoint =
            scannedItemPoints[
                scanIndex
            ];

        if (scanPoint == null ||
            outputPoint == null)
        {
            return;
        }

        /*
         * Claim this exact item BEFORE starting
         * the coroutine.
         *
         * From this point until completion no other
         * checkout interaction may claim it again.
         */
        scanInProgress =
            true;

        reservedScanItem =
            item;

        reservedScanIndex =
            scanIndex;

        StartCoroutine(
            ScanReservedItemRoutine(
                customer,
                item,
                product,
                outputPoint,
                scanIndex
            )
        );
    }

    private IEnumerator
        ScanReservedItemRoutine(
            CustomerController customer,
            PickupItem item,
            ProductData product,
            Transform outputPoint,
            int scanIndex
        )
    {
        /*
         * Defensive validation:
         *
         * this coroutine is allowed to finish only
         * if it still owns the active reservation.
         */
        if (!IsCurrentScanReservation(
                customer,
                item,
                scanIndex
            ))
        {
            yield break;
        }

        item.PlaceInContainer(
            scanPoint
        );

        if (checkoutFeedback != null)
        {
            checkoutFeedback
                .PlayScanFeedback();
        }

        yield return new WaitForSeconds(
            Mathf.Max(
                0.05f,
                scanDisplaySeconds
            )
        );

        /*
         * The session may theoretically have been
         * reset while the animation was running.
         *
         * A stale scan is not allowed to modify the
         * transaction.
         */
        if (!IsCurrentScanReservation(
                customer,
                item,
                scanIndex
            ))
        {
            yield break;
        }

        if (item == null)
        {
            ClearScanReservation();

            yield break;
        }

        item.PlaceInContainer(
            outputPoint
        );

        scannedTotal +=
            product.SellPrice;

        /*
         * Set the explicit next index instead of
         * blindly incrementing a possibly changed
         * value.
         */
        scannedItemCount =
            scanIndex + 1;

        ClearScanReservation();
    }

    private bool IsCurrentScanReservation(
        CustomerController customer,
        PickupItem item,
        int scanIndex
    )
    {
        return scanInProgress
               && activeCustomer == customer
               && reservedScanItem == item
               && reservedScanIndex == scanIndex;
    }

    private void ClearScanReservation()
    {
        scanInProgress =
            false;

        reservedScanItem =
            null;

        reservedScanIndex =
            -1;
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
            checkoutFeedback.PlayPaymentFeedback(scannedTotal);
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

        if (activeCustomer == customer)
        {
            return;
        }

        /*
         * If the front customer changed, invalidate
         * anything still belonging to the previous
         * transaction.
         */
        StopAllCoroutines();

        ClearScanReservation();

        activeCustomer = customer;

        scannedItemCount = 0;

        scannedTotal = 0f;
    }

    private void ResetCheckoutSession()
    {
        /*
         * A previous transaction must never leave
         * a delayed coroutine alive that can later
         * modify the next customer's checkout.
         */
        StopAllCoroutines();

        ClearScanReservation();

        activeCustomer =
            null;

        scannedItemCount =
            0;

        scannedTotal =
            0f;
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