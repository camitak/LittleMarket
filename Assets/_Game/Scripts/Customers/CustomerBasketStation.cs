using UnityEngine;

[DisallowMultipleComponent]
public class CustomerBasketStation : MonoBehaviour
{
    [Header("Customer Pickup")]
    [SerializeField] private Transform pickupPoint;

    [Header("Checkout Returns")]
    [SerializeField] private Transform checkoutReturnPoint;
    [Min(0.1f)]
    [SerializeField] private float returnedBasketDisplaySeconds = 5f;

    private int basketsReturned;

    public Transform PickupPoint => pickupPoint;

    public void LeaveBasketAtCheckout(GameObject returnedBasket)
    {
        if (returnedBasket == null)
            return;

        if (checkoutReturnPoint == null)
        {
            Debug.LogWarning(
                "CustomerBasketStation has no checkout return point. " +
                "The returned basket will be cleaned up without a visual.",
                this);
            Destroy(returnedBasket);
            return;
        }

        // This is a short-lived visual return, not persistent basket stock.
        returnedBasket.transform.SetParent(checkoutReturnPoint, false);
        returnedBasket.transform.localPosition =
            new Vector3(0f, (basketsReturned % 3) * 0.12f, 0f);
        returnedBasket.transform.localRotation = Quaternion.identity;
        returnedBasket.SetActive(true);

        basketsReturned++;
        Destroy(returnedBasket, returnedBasketDisplaySeconds);
    }
}