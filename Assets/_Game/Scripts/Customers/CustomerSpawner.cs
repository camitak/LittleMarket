using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer")]
    [SerializeField]
    private CustomerController customerPrefab;

    [SerializeField]
    private ProductData testProduct;

    [Header("Store")]
    [SerializeField]
    private ShelfRegistry shelfRegistry;

    [SerializeField]
    private CheckoutQueue checkoutQueue;

    [SerializeField]
    private Transform customerSpawnPoint;

    [SerializeField]
    private Transform customerExitPoint;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null)
        {
            return;
        }

        if (testProduct == null)
        {
            return;
        }

        if (shelfRegistry == null)
        {
            return;
        }

        if (checkoutQueue == null)
        {
            return;
        }

        if (customerSpawnPoint == null)
        {
            return;
        }

        if (customerExitPoint == null)
        {
            return;
        }

        CustomerController customer =
            Instantiate(
                customerPrefab,
                customerSpawnPoint.position,
                customerSpawnPoint.rotation
            );

        customer.Configure(
            testProduct,
            shelfRegistry,
            checkoutQueue,
            customerExitPoint
        );
    }
}