using UnityEngine;

[DisallowMultipleComponent]
public class CustomerBasketStation : MonoBehaviour
{
    [Header("Basket Pickup")]
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private GameObject basketPrefab;

    public Transform PickupPoint => pickupPoint;

    public GameObject DispenseBasket(Transform carryPoint)
    {
        if (carryPoint == null || basketPrefab == null)
        {
            Debug.LogError(
                "CustomerBasketStation needs a Basket Prefab and a carry point.",
                this
            );
            return null;
        }

        // The station supplies a reusable-basket *visual* for this visit.
        // There is no finite basket stock in the prototype.
        // The actual products still belong to CustomerController.carriedItems.
        return Instantiate(basketPrefab, carryPoint, false);
    }
}