using System.Collections;
using UnityEngine;

public class CheckoutRegister : MonoBehaviour, IInteractable
{
    [Header("Store References")]
    [SerializeField] private CheckoutQueue checkoutQueue;
    [SerializeField] private StoreEconomy storeEconomy;
    [SerializeField] private DailyStats dailyStats;
    [SerializeField] private CheckoutFeedback checkoutFeedback;

    [Header("Physical Checkout")]
    [SerializeField] private Transform scanPoint;
    [SerializeField] private ShoppingBag shoppingBagPrefab;
    [SerializeField] private Transform bagPoint;

    [Min(0.05f)]
    [SerializeField] private float scanDisplaySeconds = 0.12f;

    private CustomerController activeCustomer;
    private ShoppingBag activeShoppingBag;

    private int scannedItemCount;
    private float scannedTotal;

    private bool scanInProgress;
    private PickupItem reservedScanItem;
    private int reservedScanIndex = -1;

    public string GetInteractionPrompt(PlayerInteraction player)
    {
        if (player == null)
        {
            return "";
        }

        if (player.HasAnythingHeld)
        {
            return "Put down what you're carrying to use checkout";
        }

        if (checkoutQueue == null)
        {
            return "Checkout not configured";
        }

        CustomerController customer = checkoutQueue.GetFrontCustomer();

        if (customer == null)
        {
            return "No customers waiting";
        }

        if (!customer.IsReadyForCheckout())
        {
            return "Customer approaching checkout";
        }

        int itemCount = customer.GetCarriedItemCount();

        if (itemCount <= 0)
        {
            return "Customer has no products";
        }

        EnsureCheckoutSession(customer);

        if (!HasValidPhysicalCheckout())
        {
            return "Checkout bagging points not configured";
        }

        if (scanInProgress)
        {
            return "Scanning...";
        }

        if (scannedItemCount < itemCount)
        {
            ProductData nextProduct =
                customer.GetCarriedProductData(scannedItemCount);

            if (nextProduct == null)
            {
                return "Checkout item not configured";
            }

            return
                $"[E] Scan {nextProduct.ProductName}\n" +
                $"{scannedItemCount} / {itemCount} scanned  •  £{scannedTotal:0.00}";
        }

        return $"[E] Take payment\nTOTAL £{scannedTotal:0.00}";
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null ||
            player.HasAnythingHeld ||
            scanInProgress)
        {
            return;
        }

        if (checkoutQueue == null || storeEconomy == null)
        {
            return;
        }

        CustomerController customer = checkoutQueue.GetFrontCustomer();

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

        int itemCount = customer.GetCarriedItemCount();

        if (itemCount <= 0)
        {
            ResetCheckoutSession();
            return;
        }

        EnsureCheckoutSession(customer);

        if (!HasValidPhysicalCheckout())
        {
            return;
        }

        if (scannedItemCount < itemCount)
        {
            BeginNextScan(customer);
            return;
        }

        CompletePayment(customer, itemCount);
    }

    private void BeginNextScan(CustomerController customer)
    {
        if (customer == null || scanInProgress)
        {
            return;
        }

        int itemCount = customer.GetCarriedItemCount();
        int scanIndex = scannedItemCount;

        if (scanIndex < 0 || scanIndex >= itemCount)
        {
            return;
        }

        PickupItem item = customer.GetCarriedItem(scanIndex);

        if (item == null)
        {
            return;
        }

        ProductData product = item.GetProductData();

        if (product == null)
        {
            return;
        }

        if (!HasValidPhysicalCheckout())
        {
            return;
        }

        scanInProgress = true;
        reservedScanItem = item;
        reservedScanIndex = scanIndex;

        StartCoroutine(
            ScanReservedItemRoutine(
                customer,
                item,
                product,
                scanIndex
            )
        );
    }

    private IEnumerator ScanReservedItemRoutine(
        CustomerController customer,
        PickupItem item,
        ProductData product,
        int scanIndex
    )
    {
        if (!IsCurrentScanReservation(customer, item, scanIndex))
        {
            yield break;
        }

        item.PlaceInContainer(scanPoint);

        if (checkoutFeedback != null)
        {
            checkoutFeedback.PlayScanFeedback();
        }

        yield return new WaitForSeconds(
            Mathf.Max(0.05f, scanDisplaySeconds)
        );

        if (!IsCurrentScanReservation(customer, item, scanIndex))
        {
            yield break;
        }

        if (item == null)
        {
            ClearScanReservation();
            yield break;
        }

        EnsureShoppingBag();

        if (activeShoppingBag == null)
        {
            ClearScanReservation();
            yield break;
        }

        scannedTotal += product.SellPrice;
        scannedItemCount = scanIndex + 1;

        /*
         * The customer's PickupItem remains in their carriedItems list
         * until payment, but its visible GameObject is hidden after
         * scanning. CompleteCheckout() destroys it normally.
         */
        item.gameObject.SetActive(false);

        activeShoppingBag.RegisterScannedItem();

        ClearScanReservation();
    }

    private void EnsureShoppingBag()
    {
        if (activeShoppingBag != null)
        {
            return;
        }

        if (shoppingBagPrefab == null || bagPoint == null)
        {
            return;
        }

        activeShoppingBag = Instantiate(
            shoppingBagPrefab,
            bagPoint.position,
            bagPoint.rotation,
            bagPoint
        );

        activeShoppingBag.transform.localPosition = Vector3.zero;
        activeShoppingBag.transform.localRotation = Quaternion.identity;
    }

    private bool IsCurrentScanReservation(
        CustomerController customer,
        PickupItem item,
        int scanIndex
    )
    {
        return
            scanInProgress &&
            activeCustomer == customer &&
            reservedScanItem == item &&
            reservedScanIndex == scanIndex;
    }

    private void ClearScanReservation()
    {
        scanInProgress = false;
        reservedScanItem = null;
        reservedScanIndex = -1;
    }

    private void CompletePayment(
        CustomerController customer,
        int itemCount
    )
    {
        if (customer == null || scanInProgress)
        {
            return;
        }

        if (scannedItemCount < itemCount || scannedTotal <= 0f)
        {
            return;
        }

        ShoppingBag purchasedBag = activeShoppingBag;

        bool checkoutCompleted =
            customer.CompleteCheckout(purchasedBag);

        if (!checkoutCompleted)
        {
            return;
        }

        /*
         * Ownership has moved to the customer.
         * ResetCheckoutSession must not destroy this bag.
         */
        activeShoppingBag = null;

        storeEconomy.AddMoney(scannedTotal);

        if (dailyStats != null)
        {
            dailyStats.RecordSale(scannedTotal, itemCount);
        }

        if (checkoutFeedback != null)
        {
            checkoutFeedback.PlayPaymentFeedback(scannedTotal);
        }

        ResetCheckoutSession();
    }

    private void EnsureCheckoutSession(CustomerController customer)
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

        StopAllCoroutines();
        ClearScanReservation();
        DestroyActiveShoppingBag();

        activeCustomer = customer;
        scannedItemCount = 0;
        scannedTotal = 0f;
    }

    private void ResetCheckoutSession()
    {
        StopAllCoroutines();
        ClearScanReservation();
        DestroyActiveShoppingBag();

        activeCustomer = null;
        scannedItemCount = 0;
        scannedTotal = 0f;
    }

    private void DestroyActiveShoppingBag()
    {
        if (activeShoppingBag == null)
        {
            return;
        }

        Destroy(activeShoppingBag.gameObject);
        activeShoppingBag = null;
    }

    private bool HasValidPhysicalCheckout()
    {
        return
            scanPoint != null &&
            shoppingBagPrefab != null &&
            bagPoint != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (scanPoint != null)
        {
            Gizmos.DrawWireSphere(scanPoint.position, 0.05f);
        }

        if (bagPoint != null)
        {
            Gizmos.DrawWireSphere(bagPoint.position, 0.06f);
        }
    }
}